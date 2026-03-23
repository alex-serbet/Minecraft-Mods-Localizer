using MinecraftLocalizer.Models;
using MinecraftLocalizer.Converters;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Globalization;

namespace MinecraftLocalizer.Models.Localization
{
    public static class LocalizationContentParser
    {
        public static (List<LocalizationItem> Items, string RawContent) Process(
            string content,
            string fileExtension)
        {
            return ProcessStructuredContent(content, fileExtension);
        }

        private static (List<LocalizationItem> Items, string RawContent) ProcessStructuredContent(
            string content,
            string fileExtension)
        {
            return fileExtension.ToLowerInvariant() switch
            {
                ".json" => (ProcessJson(content), content),
                ".lang" => (ProcessLang(content), content),
                ".snbt" => (ProcessSnbt(content), content),
                _ => throw new NotSupportedException($"Unsupported file format: {fileExtension}")
            };
        }

        private static List<LocalizationItem> ProcessJson(string content)
        {
            try
            {
                var root = JToken.Parse(content);
                var items = new List<LocalizationItem>();
                FlattenJsonToReferences(root, [], items);
                return items;
            }
            catch (Exception ex)
            {
                LocalizationDialogContext.DialogService.ShowError($"Error processing JSON: {ex.Message}");
                return [];
            }
        }

        private static void FlattenJsonToReferences(
            JToken token,
            List<string> pathSegments,
            List<LocalizationItem> items)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var property in token.Children<JProperty>())
                    {
                        var nextPath = new List<string>(pathSegments)
                        {
                            EscapeJsonReferenceSegment(property.Name)
                        };
                        FlattenJsonToReferences(property.Value, nextPath, items);
                    }
                    break;

                case JTokenType.Array:
                    int index = 0;
                    foreach (var child in token.Children())
                    {
                        var nextPath = new List<string>(pathSegments)
                        {
                            index.ToString(CultureInfo.InvariantCulture)
                        };
                        FlattenJsonToReferences(child, nextPath, items);
                        index++;
                    }
                    break;

                default:
                    if (token is JValue valueToken)
                    {
                        var rawValue = valueToken.Value;
                        string text = rawValue?.ToString() ?? string.Empty;

                        items.Add(new LocalizationItem
                        {
                            ID = GetDisplayId(pathSegments),
                            ReferencePath = BuildJsonReference(pathSegments),
                            OriginalString = text,
                            TranslatedString = text,
                            DataType = rawValue?.GetType(),
                            IsSelected = token.Type == JTokenType.String
                        });
                    }
                    break;
            }
        }

        private static string BuildJsonReference(List<string> pathSegments) =>
            pathSegments.Count == 0 ? "#" : $"#/{string.Join("/", pathSegments)}";

        private static string GetDisplayId(List<string> pathSegments)
        {
            if (pathSegments.Count == 0)
                return "#";

            string tail = pathSegments[^1];
            return tail.Replace("~1", "/", StringComparison.Ordinal)
                       .Replace("~0", "~", StringComparison.Ordinal);
        }

        private static string EscapeJsonReferenceSegment(string segment) =>
            segment.Replace("~", "~0", StringComparison.Ordinal)
                   .Replace("/", "~1", StringComparison.Ordinal);

        private static List<LocalizationItem> ProcessLang(string content)
        {
            return content.Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith('#'))
                .Select(line => line.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .Select(parts => new LocalizationItem
                {
                    ID = parts[0].Trim(),
                    ReferencePath = parts[0].Trim(),
                    OriginalString = parts[1].Trim(),
                    TranslatedString = parts[1].Trim(),
                    DataType = typeof(string)
                }).ToList();
        }

        private static List<LocalizationItem> ProcessSnbt(string content)
        {
            try
            {
                var parsedData = SnbtConverter.ParseSnbt(content);
                return parsedData.Cast<DictionaryEntry>()
                    .Select(entry => new LocalizationItem
                    {
                        ID = entry.Key.ToString()!,
                        ReferencePath = entry.Key.ToString()!,
                        OriginalString = FormatSnbtValue(entry.Value),
                        TranslatedString = FormatSnbtValue(entry.Value)
                    }).ToList();
            }
            catch (Exception ex)
            {
                LocalizationDialogContext.DialogService.ShowError($"Error processing SNBT: {ex.Message}");
                return [];
            }
        }

        private static string FormatSnbtValue(object? value)
        {
            switch (value)
            {
                case null:
                    return string.Empty;
                case string s:
                    return s;
                case List<string> list:
                    return FormatStringList(list);
                case System.Collections.IList list:
                    return FormatList(list);
                case System.Collections.Specialized.OrderedDictionary obj:
                    return FormatObject(obj);
                default:
                    return value.ToString() ?? string.Empty;
            }
        }

        private static string FormatStringList(List<string> list)
        {
            return $"[{string.Join(", ", list.Select(v => $"\"{v}\""))}]";
        }

        private static string FormatList(System.Collections.IList list)
        {
            var parts = new List<string>();
            foreach (var item in list)
            {
                if (item is string s)
                {
                    parts.Add($"\"{s}\"");
                }
                else
                {
                    parts.Add(FormatSnbtValue(item));
                }
            }

            return $"[{string.Join(", ", parts)}]";
        }

        private static string FormatObject(System.Collections.Specialized.OrderedDictionary obj)
        {
            var parts = new List<string>();
            foreach (System.Collections.DictionaryEntry entry in obj)
            {
                string key = entry.Key?.ToString() ?? string.Empty;
                string val = FormatSnbtValue(entry.Value);
                parts.Add($"{key}: {val}");
            }

            return $"{{{string.Join(", ", parts)}}}";
        }
    }
}







