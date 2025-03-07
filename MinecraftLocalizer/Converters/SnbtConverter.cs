using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;

namespace MinecraftLocalizer.Converters;

public static partial class SnbtConverter
{
    [GeneratedRegex(@"^([\w\.-]+):\s*\[(.*)\]$", RegexOptions.Compiled)]
    private static partial Regex ArrayLineRegex();


    [GeneratedRegex(@"^([\w\.-]+):\s*\[$", RegexOptions.Compiled)]
    private static partial Regex ArrayStartRegex();


    public static OrderedDictionary ParseSnbt(string snbtContent)
    {
        var result = new OrderedDictionary();
        var lines = snbtContent.Split(["\r\n", "\n"], StringSplitOptions.None);

        string? currentKey = null;
        List<string>? currentArray = null;
        var inArray = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;

            if (inArray)
            {
                ProcessArrayLine(line, ref currentArray, ref inArray, result, currentKey);
                continue;
            }

            ProcessValueLine(line, ref currentKey, ref currentArray, ref inArray, result);
        }

        return result;
    }

    private static void ProcessArrayLine(
        string line,
        ref List<string>? currentArray,
        ref bool inArray,
        OrderedDictionary result,
        string? currentKey)
    {
        if (line == "]")
        {
            if (currentKey != null) result[currentKey] = currentArray ?? [];
            currentArray = null;
            inArray = false;
            return;
        }

        currentArray ??= [];
        var processedLine = line.TrimEnd(',').Trim();
        currentArray.Add(ProcessStringValue(processedLine));
    }

    private static void ProcessValueLine(
        string line,
        ref string? currentKey,
        ref List<string>? currentArray,
        ref bool inArray,
        OrderedDictionary result)
    {
        if (ArrayLineRegex().Match(line) is { Success: true } arrayMatch)
        {
            currentKey = arrayMatch.Groups[1].Value;
            var arrayContent = arrayMatch.Groups[2].Value;
            result[currentKey] = string.IsNullOrWhiteSpace(arrayContent)
                ? []
                : ParseInlineArray(arrayContent);
            return;
        }

        if (ArrayStartRegex().Match(line) is { Success: true } arrayStartMatch)
        {
            currentKey = arrayStartMatch.Groups[1].Value;
            currentArray = [];
            inArray = true;
            return;
        }

        if (line.Split([':'], 2) is [var key, var value])
        {
            result[key.Trim()] = ProcessStringValue(value.Trim());
        }
    }

    private static string ProcessStringValue(string input) => input switch
    {
        ['"', .. var content, '"'] => content.Replace("\\\"", "\"").Replace("\\\\", "\\"),
            _ => input
    };

    private static List<string> ParseInlineArray(string arrayContent)
    {
        var items = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var c in arrayContent)
        {
            switch (c)
            {
                case '"':
                    inQuotes = !inQuotes;
                    break;
                case ',' when !inQuotes:
                    items.Add(ProcessStringValue(current.ToString().Trim()));
                    current.Clear();
                    continue;
            }
            current.Append(c);
        }

        if (current.Length > 0)
            items.Add(ProcessStringValue(current.ToString().Trim()));

        return items;
    }

    public static string ToSnbt(OrderedDictionary data)
    {
        var sb = new StringBuilder("{\n");

        foreach (DictionaryEntry entry in data)
        {
            sb.Append($"\t{entry.Key}: ");

            if (entry.Value is List<string> list)
                AppendArray(sb, list);
            else
                sb.AppendLine($"\"{EscapeString(entry.Value?.ToString() ?? "")}\"");
        }

        return sb.AppendLine("}").ToString();
    }

    private static void AppendArray(StringBuilder sb, List<string> list)
    {
        if (list.Count == 1)
        {
            sb.AppendLine($"[\"{EscapeString(list[0])}\"]");
            return;
        }

        sb.AppendLine("[");
        foreach (var item in list)
            sb.AppendLine($"\t\t\"{EscapeString(item)}\"");
        sb.AppendLine("\t]");
    }

    private static string EscapeString(string input) => input
        .Replace("\\", "\\\\")
        .Replace("\n", "\\n")
        .Replace("\"", "\\\"");

    public static string ConvertSnbtToJson(string snbtContent) =>
        JsonConvert.SerializeObject(ParseSnbt(snbtContent), new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
        });

    public static string? ConvertJsonToSnbt(string jsonContent)
    {
        var dict = JsonConvert.DeserializeObject<OrderedDictionary>(jsonContent);
        if (dict == null) return null;

        ConvertJArraysToList(dict);
        return ToSnbt(dict);
    }

    private static void ConvertJArraysToList(OrderedDictionary dict)
    {
        foreach (var key in dict.Keys.Cast<object>().ToList())
        {
            if (dict[key] is JArray jArray)
                dict[key] = jArray.Select(item => item.ToString()).ToList();
        }
    }
}