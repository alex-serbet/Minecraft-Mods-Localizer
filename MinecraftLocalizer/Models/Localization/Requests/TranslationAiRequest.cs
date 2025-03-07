using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MinecraftLocalizer.Models.Localization.Requests
{
    public class TranslationAiRequest
    {
        public readonly HttpClient client;

        public TranslationAiRequest()
        {
            client = new HttpClient();
        }

        public async Task<string> TranslateTextAsync(string sourceText, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sourceText))
                return string.Empty;

            return await TranslateTextInternal(sourceText, cancellationToken);
        }

        private async Task<string> TranslateTextInternal(string text, CancellationToken cancellationToken)
        {
            string targetLanguageSetting = Properties.Settings.Default.TargetLanguage;

            string url = "http://localhost:1337/v1/chat/completions";
            var body = new
            {
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = $"{text}\n\n" +
                                  $"Translate the text into the language of this language tag {targetLanguageSetting}, leaving all special characters. " +
                                  $"Keep in mind that the translation is in the context of the Minecraft game with mods. " +
                                  $"You don’t need to add your own words, just a translation!"
                    }
            },
                model = "deepseek-v3",
                provider = "Blackbox"
            };

            var jsonBody = JsonSerializer.Serialize(body);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var response = await client.PostAsync(url, content, cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Error during request: {response.StatusCode}");
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(jsonResponse);

                    if (doc.RootElement.TryGetProperty("choices", out var choices))
                    {
                        foreach (var choice in choices.EnumerateArray())
                        {
                            if (choice.TryGetProperty("message", out var message) &&
                                message.TryGetProperty("content", out JsonElement contentProp))
                            {
                                return contentProp.GetString() ?? string.Empty;
                            }
                        }
                    }

                    Console.WriteLine("Property 'choices' not found in response.");
                }
                catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON processing error: {ex.Message}");
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Request error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Request error: {ex.Message}");
                }

                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}


//private const string GoogleTranslateApiUrl = "https://translate.googleapis.com/translate_a/single?client=gtx";


//public async Task<string> TranslateWithFormattingAsync(string sourceText, string targetLang, string sourceLang = "auto")
//{
//    if (string.IsNullOrEmpty(sourceText) || string.IsNullOrEmpty(targetLang))
//    {
//        Console.WriteLine("Недостаточно аргументов");
//        return sourceText;
//    }

//    var parameters = new Dictionary<string, string>
//    {
//        { "dt", "t" },
//        { "sl", sourceLang },
//        { "tl", targetLang },
//        { "q", sourceText }
//    };

//    // Шаг 1: Разделить строку на текст и спецсимволы
//    var (textParts, formattingParts) = SeparateTextAndFormatting(sourceText);

//    // Шаг 2: Заменить все спецсимволы на #
//    var textWithHashes = string.Join("", textParts);
//    var formattingWithHashes = formattingParts.Select(f => "#").ToList(); // Заменяем все спецсимволы на #, сохраняя порядок

//    // Шаг 3: Отправить строку без спецсимволов на перевод
//    var translatedText = await TranslateAsync(textWithHashes, targetLang, sourceLang);

//    // Шаг 4: Восстановить спецсимволы в переведенной строке
//    var finalResult = ReassembleTextWithFormatting(translatedText, formattingWithHashes);

//    return finalResult;
//}

//// Разделяем строку на текст и спецсимволы
//private (List<string> textParts, List<string> formattingParts) SeparateTextAndFormattingSNBT(string input)
//{
//    var textParts = new List<string>();
//    var formattingParts = new List<string>();

//    // Символы форматирования для Minecraft
//    var formattingSymbols = new HashSet<string>
//{
//    "&1", "&2", "&3", "&4", "&5", "&6", "&7", "&8", "&9", "&r", "&a", "&l", "&c", "&b", "&e", "&d", "&f",
//    "\\&", "\\n"
//};

//    // Регулярное выражение для поиска спецсимволов
//    var specialCharPattern = new Regex(@"(&[0-9a-fklmnor])|(\\[&n])");

//    int lastIndex = 0;

//    // Пробежка по строке
//    foreach (Match match in specialCharPattern.Matches(input))
//    {
//        // Добавляем текст перед спецсимволом
//        if (match.Index > lastIndex)
//        {
//            textParts.Add(input.Substring(lastIndex, match.Index - lastIndex));
//        }

//        // Добавляем сам спецсимвол
//        formattingParts.Add(match.Value);

//        // Обновляем индекс
//        lastIndex = match.Index + match.Length;
//    }

//    // Добавляем последний текст (после последнего спецсимвола)
//    if (lastIndex < input.Length)
//    {
//        textParts.Add(input.Substring(lastIndex));
//    }

//    return (textParts, formattingParts);
//}


//private string ReassembleTextWithFormatting(string translatedText, List<string> formattingParts)
//{
//    StringBuilder result = new StringBuilder();
//    int textIndex = 0;
//    int formattingIndex = 0;

//    // Мы объединяем текст и форматирование, поочередно добавляем из textParts и formattingParts
//    for (int i = 0; i < translatedText.Length + formattingParts.Count; i++)
//    {
//        if (textIndex < translatedText.Length)
//        {
//            result.Append(translatedText[textIndex]);
//            textIndex++;
//        }

//        // Если есть еще спецсимволы, добавляем их
//        if (formattingIndex < formattingParts.Count)
//        {
//            result.Append(formattingParts[formattingIndex]);
//            formattingIndex++;
//        }
//    }

//    return result.ToString();
//}

//public async Task<string> TranslateAsync(string sourceText, string targetLang, string sourceLang = "auto")
//{
//    if (string.IsNullOrEmpty(sourceText) || string.IsNullOrEmpty(targetLang))
//    {
//        Console.WriteLine("Недостаточно аргументов");
//        return sourceText;
//    }

//    var parameters = new Dictionary<string, string>
//    {
//        { "dt", "t" },
//        { "sl", sourceLang },
//        { "tl", targetLang },
//        { "q", sourceText }
//    };

//    var response = await _httpClient.GetAsync(BuildUrl(GoogleTranslateApiUrl, parameters));

//    if (response.IsSuccessStatusCode)
//    {
//        var jsonResponse = await response.Content.ReadAsStringAsync();
//        return ParseTranslatedText(jsonResponse);
//    }

//    Console.WriteLine($"Ошибка перевода: {response.StatusCode}");
//    return sourceText;
//}