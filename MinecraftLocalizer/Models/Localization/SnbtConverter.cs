using System.Collections;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;

namespace MinecraftLocalizer.Converters
{
    public static partial class SnbtConverter
    {
        [GeneratedRegex(@"^([\w\.-]+):\s*\[(.*)\]$", RegexOptions.Compiled)]
        private static partial Regex ArrayLineRegex();

        [GeneratedRegex(@"^([\w\.-]+):\s*\[$", RegexOptions.Compiled)]
        private static partial Regex ArrayStartRegex();

        [GeneratedRegex(@"^([\w\.-]+):\s*\{$", RegexOptions.Compiled)]
        private static partial Regex ObjectStartRegex();

        [GeneratedRegex(@"(?:^|,)(?=(?:[^""]*""[^""]*"")*[^""]*$)", RegexOptions.Compiled)]
        private static partial Regex CommaRegex();

        public static OrderedDictionary ParseSnbt(string snbtContent)
        {
            var result = new OrderedDictionary();
            var lines = snbtContent.Split(["\r\n", "\n"], StringSplitOptions.None);

            string? currentKey = null;
            List<object>? currentArray = null;
            bool inArray = false;
            bool inObject = false;
            int objectNesting = 0;
            StringBuilder? currentObjectBuilder = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var rawLine = lines[i];
                var line = rawLine.Trim();
                if (line.Length == 0) continue;

                if (inObject)
                {
                    currentObjectBuilder!.AppendLine(rawLine);
                    if (line.Contains("{")) objectNesting++;
                    if (line.Contains("}"))
                    {
                        objectNesting--;
                        if (objectNesting == 0)
                        {
                            var objContent = currentObjectBuilder.ToString();
                            var parsedObj = ParseObject(objContent);
                            result[currentKey!] = parsedObj;
                            currentObjectBuilder = null;
                            inObject = false;
                        }
                    }
                    continue;
                }

                if (inArray)
                {
                    if (line.StartsWith("{"))
                    {
                        var objLines = new List<string>();
                        int nesting = 1;
                        int j = i;
                        objLines.Add(lines[j]);

                        while (nesting > 0 && j + 1 < lines.Length)
                        {
                            j++;
                            var nestedLine = lines[j];
                            objLines.Add(nestedLine);
                            if (nestedLine.Contains("{")) nesting++;
                            if (nestedLine.Contains("}")) nesting--;
                        }

                        var objContent = string.Join("\n", objLines);
                        var parsedObj = ParseObject(objContent);
                        currentArray!.Add(parsedObj);
                        i = j;
                    }
                    else if (line.EndsWith(']') || line.EndsWith("],"))
                    {
                        result[currentKey!] = currentArray!;
                        currentArray = null;
                        inArray = false;
                    }
                    else
                    {
                        currentArray!.Add(ProcessStringValue(line.TrimEnd(',')));
                    }
                    continue;
                }

                if (ObjectStartRegex().Match(line) is { Success: true } objMatch)
                {
                    currentKey = objMatch.Groups[1].Value;
                    currentObjectBuilder = new StringBuilder();
                    currentObjectBuilder.AppendLine(line);
                    inObject = true;
                    objectNesting = 1;
                }
                else if (ArrayStartRegex().Match(line) is { Success: true } arrayStartMatch)
                {
                    currentKey = arrayStartMatch.Groups[1].Value;
                    currentArray = new List<object>();
                    inArray = true;
                }
                else if (ArrayLineRegex().Match(line) is { Success: true } arrayMatch)
                {
                    currentKey = arrayMatch.Groups[1].Value;
                    var arrayContent = arrayMatch.Groups[2].Value;
                    result[currentKey] = ParseInlineArray(arrayContent);
                }
                else if (line.Split(new[] { ':' }, 2) is string[] parts && parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim().TrimEnd(',');
                    result[key] = ProcessStringValue(value);
                }
            }

            return result;
        }

        private static OrderedDictionary ParseObject(string objectContent)
        {
            var obj = new OrderedDictionary();
            var lines = objectContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            int i = 0;

            while (i < lines.Length)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line == "{" || line == "}")
                {
                    i++;
                    continue;
                }

                var colonPos = line.IndexOf(':');
                if (colonPos < 0)
                {
                    i++;
                    continue;
                }

                var key = line.Substring(0, colonPos).Trim();
                var valuePart = line.Substring(colonPos + 1).Trim();

                if (valuePart == "{")
                {
                    var nestedObjLines = new List<string>();
                    int nesting = 1;
                    i++;

                    while (i < lines.Length && nesting > 0)
                    {
                        var currentLine = lines[i].Trim();
                        nestedObjLines.Add(lines[i]);

                        if (currentLine == "{") nesting++;
                        else if (currentLine == "}") nesting--;

                        i++;
                    }

                    var nestedContent = string.Join("\n", nestedObjLines);
                    obj[key] = ParseObject(nestedContent);
                }
                else if (valuePart == "[")
                {
                    var arrayLines = new List<string>();
                    int nesting = 1;
                    i++;

                    while (i < lines.Length && nesting > 0)
                    {
                        var currentLine = lines[i].Trim();
                        arrayLines.Add(lines[i]);

                        if (currentLine == "[") nesting++;
                        else if (currentLine == "]" || currentLine.EndsWith("],")) nesting--;

                        i++;
                    }

                    var arrayContent = string.Join("\n", arrayLines);
                    obj[key] = ParseArrayContent(arrayContent);
                }
                else
                {
                    var value = valuePart.TrimEnd(',');
                    obj[key] = ProcessStringValue(value);
                    i++;
                }
            }

            return obj;
        }

        private static List<object> ParseArrayContent(string arrayContent)
        {
            var items = new List<object>();
            var lines = arrayContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            int i = 0;

            while (i < lines.Length)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line == "[" || line == "]" || line == "],")
                {
                    i++;
                    continue;
                }

                if (line == "{")
                {
                    var objLines = new List<string>();
                    int nesting = 1;
                    objLines.Add(lines[i]);
                    i++;

                    while (i < lines.Length && nesting > 0)
                    {
                        var currentLine = lines[i].Trim();
                        objLines.Add(lines[i]);

                        if (currentLine == "{") nesting++;
                        else if (currentLine == "}") nesting--;

                        i++;
                    }

                    var objContent = string.Join("\n", objLines);
                    items.Add(ParseObject(objContent));
                }
                else if (line == "[")
                {
                    var nestedArrayLines = new List<string>();
                    int nesting = 1;
                    nestedArrayLines.Add(lines[i]);
                    i++;

                    while (i < lines.Length && nesting > 0)
                    {
                        var currentLine = lines[i].Trim();
                        nestedArrayLines.Add(lines[i]);

                        if (currentLine == "[") nesting++;
                        else if (currentLine == "]" || currentLine.EndsWith("],")) nesting--;

                        i++;
                    }

                    var nestedArrayContent = string.Join("\n", nestedArrayLines);
                    items.Add(ParseArrayContent(nestedArrayContent));
                }
                else
                {
                    var value = line.TrimEnd(',');
                    items.Add(ProcessStringValue(value));
                    i++;
                }
            }

            return items;
        }

        private static List<object> ParseInlineArray(string arrayContent)
        {
            var items = new List<object>();
            var current = new StringBuilder();
            bool inQuotes = false;

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

        public static string FormatSnbtEntry(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string trimmed = value.TrimStart();

            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                string inner = trimmed[1..^1].Trim();
                if (string.IsNullOrEmpty(inner))
                    return "[]";

                bool isArray = inner.StartsWith('"') || inner.Contains('\n') || inner.Contains(',');

                return isArray
                    ? FormatArray(inner)
                    : EscapeSnbtValue(trimmed);
            }

            return EscapeSnbtValue(trimmed);
        }

        private static string FormatArray(string content)
        {
            List<string> elements = content.Contains('\n')
                ? [.. content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(ProcessArrayElement)]
                : [.. SplitByCommas(content).Select(ProcessArrayElement)];

            return content.Contains('\n') ? FormatMultiLineArray(elements) : $"[{string.Join(", ", elements)}]";
        }

        private static string ProcessArrayElement(string element)
        {
            element = element.Trim();

            if (element.StartsWith('"') && element.EndsWith('"'))
            {
                element = element[1..^1];
            }

            element = EscapeSnbtString(element);

            return $"\"{element}\"";
        }

        private static string FormatMultiLineArray(List<string> elements)
        {
            var sb = new StringBuilder("[\n");
            for (int i = 0; i < elements.Count; i++)
            {
                sb.Append("\t\t").Append(elements[i]);
                if (i < elements.Count - 1) sb.Append(',');
                sb.AppendLine();
            }
            sb.Append("\t]");
            return sb.ToString();
        }

        private static string EscapeSnbtValue(string input) => $"\"{EscapeSnbtString(input)}\"";

        private static string EscapeSnbtString(string input) =>
            input.Replace("\\", "\\\\")
                 .Replace("\"", "\\\"")
                 .Replace("\n", "\\n")
                 .Replace("\t", "\\t");

        private static List<string> SplitByCommas(string input) =>
            [.. CommaRegex().Split(input).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim())];

        public static string FormatComplexValue(object value)
        {
            return value switch
            {
                OrderedDictionary dict => FormatDictionary(dict),
                List<object> list => FormatList(list),
                string str => str,
                _ => value.ToString() ?? string.Empty
            };
        }

        private static string FormatDictionary(OrderedDictionary dict)
        {
            var sb = new StringBuilder("{");
            bool first = true;

            foreach (DictionaryEntry entry in dict)
            {
                if (!first) sb.Append(", ");
                first = false;

                sb.Append(entry.Key).Append(": ").Append(FormatComplexValue(entry.Value ?? string.Empty));
            }

            sb.Append("}");
            return sb.ToString();
        }

        private static string FormatList(List<object> list)
        {
            var sb = new StringBuilder("[");
            bool first = true;

            foreach (var item in list)
            {
                if (!first) sb.Append(", ");
                first = false;

                sb.Append(FormatComplexValue(item));
            }

            sb.Append("]");
            return sb.ToString();
        }

        private static object ProcessStringValue(string input)
        {
            if (input.Length >= 2 && input.StartsWith('"') && input.EndsWith('"'))
            {
                return input[1..^1].Replace("\\\"", "\"").Replace("\\\\", "\\");
            }

            if (bool.TryParse(input, out bool boolValue)) return boolValue;
            if (long.TryParse(input, out long longValue)) return longValue;
            if (double.TryParse(input.TrimEnd('d', 'f'), out double doubleValue)) return doubleValue;

            return input;
        }
    }
}
