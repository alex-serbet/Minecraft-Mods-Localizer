using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MinecraftLocalizer.Models.Localization.Requests
{
    public partial class TranslationAiRequest
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
            string targetLanguage = Properties.Settings.Default.TargetLanguage;
            string prompt = string.Format(Properties.Settings.Default.Prompt, targetLanguage);

            string url = "http://localhost:1337/v1/chat/completions";

            var body = new
            {
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = $"{text}\n\n{prompt}"
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
                        Debug.WriteLine($"Error during request: {response.StatusCode}");
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

                    Debug.WriteLine("Property 'choices' not found in response.");
                }
                catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine($"JSON processing error: {ex.Message}");
                }
                catch (HttpRequestException ex)
                {
                    Debug.WriteLine($"Request error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Request error: {ex.Message}");
                }

                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}
