using MinecraftLocalizer.Models;
using MinecraftLocalizer.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace MinecraftLocalizer.Models.Localization
{
    public partial class LocalizationArchiveWriter
    {
        private static void WriteLocalizationContent(
            TextWriter writer,
            string fileFormat,
            IEnumerable<LocalizationItem>? items = null,
            List<string>? comments = null)
        {
            switch (fileFormat.ToLowerInvariant())
            {
                case "json":
                    WriteJsonContent(writer, items);
                    break;
                case "lang":
                    WriteLangContent(writer, items, comments);
                    break;
                case "snbt":
                    WriteSnbtContent(writer, items);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported file format: {fileFormat}");
            }
        }

        private static void WriteRawContent(TextWriter writer, string? text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            writer.Write(text);
            if (!text.EndsWith(Environment.NewLine))
                writer.WriteLine();
        }

        private static void WriteJsonContent(TextWriter writer, IEnumerable<LocalizationItem>? items)
        {
            if (items == null)
                return;

            var jsonItems = items.Where(e =>
                    !string.IsNullOrWhiteSpace(e.ID) || !string.IsNullOrWhiteSpace(e.ReferencePath))
                .ToList();

            if (jsonItems.Count == 0)
            {
                writer.Write("{}");
                return;
            }

            bool useJsonReferences = jsonItems.All(i =>
                (i.ReferencePath ?? i.ID)!.StartsWith("#", StringComparison.Ordinal));
            if (useJsonReferences)
            {
                writer.Write(BuildJsonFromReferences(jsonItems).ToString(Formatting.Indented));
                return;
            }

            var entries = jsonItems.ToDictionary(
                e => e.ID!,
                e => ParseJsonValue(e.TranslatedString?.Trim() ?? "", e.DataType?.Name));

            writer.Write(JsonConvert.SerializeObject(entries, Formatting.Indented));
        }

        private static JToken BuildJsonFromReferences(IEnumerable<LocalizationItem> items)
        {
            JToken root = new JObject();

            foreach (var item in items)
            {
                string? reference = item.ReferencePath ?? item.ID;
                if (string.IsNullOrWhiteSpace(reference))
                    continue;

                var segments = ParseJsonReference(reference);
                var parsedValue = ParseJsonValue(item.TranslatedString?.Trim() ?? "", item.DataType?.Name);
                var valueToken = parsedValue is JToken token ? token : JToken.FromObject(parsedValue);

                SetJsonReferenceValue(ref root, segments, valueToken);
            }

            return root;
        }

        private static List<string> ParseJsonReference(string reference)
        {
            if (reference == "#")
                return [];
            if (!reference.StartsWith("#/", StringComparison.Ordinal))
                return [];

            return reference[2..]
                .Split('/', StringSplitOptions.None)
                .Select(UnescapeJsonReferenceSegment)
                .ToList();
        }

        private static string UnescapeJsonReferenceSegment(string segment) =>
            segment.Replace("~1", "/", StringComparison.Ordinal)
                   .Replace("~0", "~", StringComparison.Ordinal);

        private static void SetJsonReferenceValue(ref JToken root, List<string> segments, JToken value)
        {
            if (segments.Count == 0)
            {
                root = value;
                return;
            }

            bool rootIsArray = IsArrayIndex(segments[0]);
            if ((rootIsArray && root is not JArray) || (!rootIsArray && root is not JObject))
            {
                root = rootIsArray ? new JArray() : new JObject();
            }

            JToken current = root;

            for (int i = 0; i < segments.Count; i++)
            {
                bool isLast = i == segments.Count - 1;
                string segment = segments[i];

                if (current is JObject obj)
                {
                    if (isLast)
                    {
                        obj[segment] = value;
                        return;
                    }

                    bool nextIsArray = IsArrayIndex(segments[i + 1]);
                    obj[segment] ??= nextIsArray ? new JArray() : new JObject();
                    current = obj[segment]!;
                }
                else if (current is JArray arr)
                {
                    if (!int.TryParse(segment, out int index))
                        return;

                    EnsureArraySize(arr, index + 1);

                    if (isLast)
                    {
                        arr[index] = value;
                        return;
                    }

                    bool nextIsArray = IsArrayIndex(segments[i + 1]);
                    if (arr[index] == null || arr[index]!.Type == JTokenType.Null)
                    {
                        arr[index] = nextIsArray ? new JArray() : new JObject();
                    }

                    current = arr[index]!;
                }
            }
        }

        private static bool IsArrayIndex(string segment) =>
            int.TryParse(segment, out _);

        private static void EnsureArraySize(JArray array, int requiredSize)
        {
            while (array.Count < requiredSize)
            {
                array.Add(JValue.CreateNull());
            }
        }

        private static object ParseJsonValue(string value, string? dataType)
        {
            if (value.StartsWith('[') || value.StartsWith('{'))
            {
                try
                {
                    return JToken.Parse(value);
                }
                catch
                {
                }
            }

            return dataType switch
            {
                nameof(Int64) when long.TryParse(value, out var l) => l,
                nameof(Double) when double.TryParse(value, out var d) => d,
                nameof(Boolean) when bool.TryParse(value, out var b) => b,
                _ => value
            };
        }

        private static void WriteLangContent(
            TextWriter writer,
            IEnumerable<LocalizationItem>? items,
            List<string>? comments)
        {
            if (comments?.Count > 0)
            {
                foreach (var comment in comments)
                {
                    writer.WriteLine(comment);
                }

                writer.WriteLine();
            }

            if (items == null)
                return;

            foreach (var item in items.Where(i => i?.ID != null))
            {
                writer.WriteLine($"{item.ID}={item.TranslatedString}");
            }
        }

        private static void WriteSnbtContent(TextWriter writer, IEnumerable<LocalizationItem>? items)
        {
            if (items == null)
                return;

            writer.WriteLine("{");
            var entries = items.Where(e => e?.ID != null).ToList();

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.ID == null)
                    continue;

                string value = SnbtConverter.FormatSnbtEntry(entry.TranslatedString);
                string line = $"\t{entry.ID}: {value}";

                if (i < entries.Count - 1 && !line.Contains("default_hide_dependency_lines"))
                    line += ",";

                writer.WriteLine(line);
            }

            writer.WriteLine("}");
        }
    }
}





