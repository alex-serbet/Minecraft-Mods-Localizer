using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;

namespace MinecraftLocalizer.Converters
{
    public static partial class SnbtManager
    {
        [GeneratedRegex(@"^([\w\.-]+):\s*\[(.*)\]$", RegexOptions.Compiled)]
        private static partial Regex ArrayLineRegex();

        [GeneratedRegex(@"^([\w\.-]+):\s*\[$", RegexOptions.Compiled)]
        private static partial Regex ArrayStartRegex();

        [GeneratedRegex(@"(?:^|,)(?=(?:[^""]*""[^""]*"")*[^""]*$)", RegexOptions.Compiled)]
        private static partial Regex CommaRegex();

        #region SNBT Parsing

        /// <summary>
        /// Parses SNBT content into an OrderedDictionary.
        /// </summary>
        public static OrderedDictionary ParseSnbt(string snbtContent)
        {
            var result = new OrderedDictionary();
            var lines = snbtContent.Split(["\r\n", "\n"], StringSplitOptions.None);

            string? currentKey = null;
            List<string>? currentArray = null;
            bool inArray = false;

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
                if (currentKey != null)
                    result[currentKey] = currentArray ?? [];

                currentArray = null;
                inArray = false;
                return;
            }

            currentArray ??= [];
            currentArray.Add(ProcessStringValue(line.TrimEnd(',').Trim()));
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
                result[currentKey] = string.IsNullOrWhiteSpace(arrayMatch.Groups[2].Value)
                    ? []
                    : ParseInlineArray(arrayMatch.Groups[2].Value);
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
                result[key.Trim()] = ProcessStringValue(value.Trim());
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

        #endregion

        #region SNBT Formatting

        /// <summary>
        /// Formats an individual SNBT value.
        /// If enclosed in square brackets, it attempts to interpret it as an array.
        /// </summary>
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

                bool isArray = inner.StartsWith('\"') || inner.Contains('\n') || inner.Contains(',');

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

            // If the string starts and ends with quotes, remove them
            if (element.StartsWith('\"') && element.EndsWith('\"'))
            {
                element = element[1..^1];
            }

            // Escape all quotes inside the string
            element = EscapeSnbtString(element);

            // Return the string enclosed in quotes
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

        #endregion
    }
}