using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MinecraftLocalizer.Models.Localization.Requests
{
    public partial class TranslationAiRequest
    {
        private async Task<string> TranslateWithGpt4FreeAsync(
            string sourceText,
            CancellationToken externalCancellationToken,
            Action<string>? onChunkReceived)
        {
            string requestPayload = BuildGpt4FreeRequestPayload(sourceText);
            string? apiKey = ResolveGpt4FreeApiKey();

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                externalCancellationToken.ThrowIfCancellationRequested();

                Log(attempt == 1
                    ? "Sending request to GPT4Free..."
                    : $"Sending request to GPT4Free ({attempt}/{MaxAttempts})...");

                try
                {
                    return await SendStreamingGpt4FreeRequestAsync(
                        requestPayload,
                        externalCancellationToken,
                        onChunkReceived,
                        apiKey).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (externalCancellationToken.IsCancellationRequested)
                {
                    Log("Translation request canceled by user.");
                    throw;
                }
                catch (Exception ex)
                {
                    Log($"GPT4Free request attempt {attempt} failed: {ex.Message}");
                    Debug.WriteLine($"Attempt {attempt}/{MaxAttempts} failed: {ex.Message}");

                    if (attempt == MaxAttempts)
                    {
                        Debug.WriteLine("Max retry attempts exceeded. Skipping current request and continuing.");
                        return string.Empty;
                    }

                    int delayMs = BaseRetryDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delayMs, externalCancellationToken).ConfigureAwait(false);
                }
            }

            return string.Empty;
        }

        private async Task<string> SendStreamingGpt4FreeRequestAsync(
            string requestPayload,
            CancellationToken externalCancellationToken,
            Action<string>? onChunkReceived,
            string? apiKey = null)
        {
            const int lineTimeoutSeconds = 60;
            const int requestTimeoutMinutes = 5;
            bool hasApiKey = !string.IsNullOrWhiteSpace(apiKey);
            int providerErrorCount = 0;

            while (true)
            {
                var translatedText = new StringBuilder();
                bool rateLimited = false;
                string? providerErrorMessage = null;
                TimeSpan? providerRetryAfter = null;

                await Gpt4FreeRequestLimiter.Instance
                    .WaitForAvailabilityAsync(hasApiKey, externalCancellationToken, Log)
                    .ConfigureAwait(false);

                using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
                requestCts.CancelAfter(TimeSpan.FromMinutes(requestTimeoutMinutes));

                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, Gpt4FreeApiUrl)
                    {
                        Content = new StringContent(requestPayload, Encoding.UTF8, "application/json")
                    };

                    if (!string.IsNullOrWhiteSpace(apiKey))
                        request.Headers.Add("Authorization", $"Bearer {apiKey}");

                    using var response = await _httpClient!.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        requestCts.Token);

                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        var retryAfter = TryGetRetryAfter(response);
                        var wait = Gpt4FreeRequestLimiter.Instance.RegisterServerLimit(RateLimitType.Generic, retryAfter);
                        if (wait <= TimeSpan.Zero)
                            wait = TimeSpan.FromMinutes(1);

                        Gpt4FreeRequestLimiter.Instance.SuppressServerCooldownLog(wait);
                        Log($"GPT4Free HTTP 429 (unknown limit). Waiting ~{FormatWait(wait)} before retry.");
                        continue;
                    }

                    response.EnsureSuccessStatusCode();

                    await using var stream = await response.Content.ReadAsStreamAsync(requestCts.Token);
                    using var reader = new StreamReader(stream);

                    var lastUiUpdate = DateTime.UtcNow;
                    var debounceInterval = TimeSpan.FromMilliseconds(100);

                    while (!requestCts.Token.IsCancellationRequested)
                    {
                        string? line;

                        try
                        {
                            line = await reader
                                .ReadLineAsync()
                                .WaitAsync(TimeSpan.FromSeconds(lineTimeoutSeconds), requestCts.Token);
                        }
                        catch (TimeoutException)
                        {
                            Log("Stream stalled. Restarting request...");

                            if (translatedText.Length > 0)
                                return translatedText.ToString();

                            break;
                        }

                        if (line == null || !line.StartsWith("data: ", StringComparison.Ordinal))
                            continue;

                        string data = line[6..];
                        if (data == "[DONE]")
                            break;

                        if (TryGetErrorMessage(data, out var errorMessage, out var retryAfter))
                        {
                            if (TryClassifyRateLimit(errorMessage, out var limitType, out bool isExplicit429))
                            {
                                var wait = Gpt4FreeRequestLimiter.Instance.RegisterServerLimit(limitType, retryAfter);
                                if (wait > TimeSpan.Zero)
                                {
                                    Gpt4FreeRequestLimiter.Instance.SuppressServerCooldownLog(wait);
                                    if (limitType == RateLimitType.PerMinute || limitType == RateLimitType.PerHour)
                                    {
                                        Log($"GPT4Free provider limit ({FormatLimitType(limitType)}). Waiting ~{FormatWait(wait)} before retry.");
                                    }
                                    else if (isExplicit429)
                                    {
                                        Log($"GPT4Free provider error 429 (unknown limit). Waiting ~{FormatWait(wait)} before retry.");
                                    }
                                    else
                                    {
                                        Log($"GPT4Free provider limit (unknown). Waiting ~{FormatWait(wait)} before retry.");
                                    }
                                }

                                rateLimited = true;
                                break;
                            }

                            providerErrorMessage = errorMessage;
                            providerRetryAfter = retryAfter;
                            break;
                        }

                        if (TryExtractChunk(data, out string? chunk) && !string.IsNullOrEmpty(chunk))
                        {
                            translatedText.Append(chunk);

                            var now = DateTime.UtcNow;
                            if (now - lastUiUpdate >= debounceInterval)
                            {
                                lastUiUpdate = now;
                                onChunkReceived?.Invoke(translatedText.ToString());
                            }
                        }
                    }

                    if (rateLimited)
                        continue;

                    if (!string.IsNullOrWhiteSpace(providerErrorMessage))
                    {
                        providerErrorCount++;
                        var delay = providerRetryAfter ??
                                    TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, providerErrorCount - 1) * 2));
                        Log($"GPT4Free provider error: {providerErrorMessage}. Retrying in {FormatWait(delay)}.");
                        await Task.Delay(delay, externalCancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (translatedText.Length > 0)
                    {
                        providerErrorCount = 0;
                        return translatedText.ToString();
                    }
                }
                catch (OperationCanceledException) when (!externalCancellationToken.IsCancellationRequested)
                {
                    Log("GPT4Free request timeout. Retrying...");
                }
                catch (HttpRequestException ex)
                {
                    Log($"Network error: {ex.Message}. Retrying in 10s...");
                    await Task.Delay(TimeSpan.FromSeconds(10), externalCancellationToken);
                }
                catch (Exception ex)
                {
                    Log($"Unexpected error: {ex.Message}. Retrying in 5s...");
                    await Task.Delay(TimeSpan.FromSeconds(5), externalCancellationToken);
                }
            }
        }

        private static bool TryGetErrorMessage(string data, out string errorMessage, out TimeSpan? retryAfter)
        {
            errorMessage = string.Empty;
            retryAfter = null;

            try
            {
                using var doc = JsonDocument.Parse(data);
                if (doc.RootElement.TryGetProperty("error", out var error) &&
                    error.TryGetProperty("message", out var message))
                {
                    errorMessage = message.GetString() ?? string.Empty;
                    if (error.TryGetProperty("retry_after", out var retryAfterProp) &&
                        TryParseRetryAfterSeconds(retryAfterProp, out var seconds))
                    {
                        retryAfter = TimeSpan.FromSeconds(seconds);
                    }

                    return !string.IsNullOrWhiteSpace(errorMessage);
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool TryClassifyRateLimit(string text, out RateLimitType type, out bool isExplicit429)
        {
            type = RateLimitType.None;
            isExplicit429 = false;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (text.Contains("per minute"))
                type = RateLimitType.PerMinute;
            else if (text.Contains("per hour"))
                type = RateLimitType.PerHour;
            else if (text.Contains("rate limit") || text.Contains("too many requests") || text.Contains("request limit"))
                type = RateLimitType.Generic;
            else
                return false;

            if (text.Contains("429"))
                isExplicit429 = true;

            return true;
        }

        private static bool TryExtractChunk(string data, out string? chunk)
        {
            chunk = null;

            try
            {
                using var jsonDoc = JsonDocument.Parse(data);
                if (!jsonDoc.RootElement.TryGetProperty("choices", out var choices))
                    return false;

                foreach (var choice in choices.EnumerateArray())
                {
                    if (choice.TryGetProperty("delta", out var delta) &&
                        delta.TryGetProperty("content", out var contentProp))
                    {
                        chunk = contentProp.GetString();
                        if (!string.IsNullOrEmpty(chunk))
                            return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private static string BuildGpt4FreeRequestPayload(string sourceText)
        {
            string targetLanguage = Properties.Settings.Default.TargetLanguage;
            string prompt = string.Format(Properties.Settings.Default.Prompt, targetLanguage);
            string? provider = ResolveGpt4FreeProvider();
            string? model = ResolveGpt4FreeModel();
            double temperature = ResolveGpt4FreeTemperature();

            var requestBody = new
            {
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = $"{sourceText}\n\n{prompt}"
                    }
                },
                provider,
                model,
                temperature,
                stream = true
            };

            return JsonSerializer.Serialize(requestBody);
        }

        private static string? ResolveGpt4FreeApiKey()
        {
            string savedKey = Properties.Settings.Default.Gpt4FreeApiKey;
            return string.IsNullOrWhiteSpace(savedKey) ? null : savedKey;
        }

        private static string? ResolveGpt4FreeProvider()
        {
            string provider = Properties.Settings.Default.Gpt4FreeProviderId;
            return string.IsNullOrWhiteSpace(provider) ? null : provider;
        }

        private static string? ResolveGpt4FreeModel()
        {
            string model = Properties.Settings.Default.Gpt4FreeModelId;
            return string.IsNullOrWhiteSpace(model) ? null : model;
        }

        private static double ResolveGpt4FreeTemperature()
        {
            double temperature = Properties.Settings.Default.Gpt4FreeTemperature;
            return temperature is < 0.1 or > 1.5 ? 0.5 : temperature;
        }
    }
}
