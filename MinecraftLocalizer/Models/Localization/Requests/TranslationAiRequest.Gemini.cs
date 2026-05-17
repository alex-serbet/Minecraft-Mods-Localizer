namespace MinecraftLocalizer.Models.Localization.Requests
{
    public partial class TranslationAiRequest
    {
        private async Task<string> TranslateWithGeminiAsync(
            string sourceText,
            CancellationToken cancellationToken,
            Action<string>? onChunkReceived)
        {
            string systemPrompt = BuildSystemPrompt();
            string model = ResolveGeminiModel();
            double temperature = ResolveGeminiTemperature();
            bool isThinkingEnabled = !Properties.Settings.Default.GeminiDisableThinking;

            const int maxDelaySec = 60;
            int retrySec = 5;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Log($"Sending request to Gemini ({model}, thinking={isThinkingEnabled})...");

                try
                {
                    var result = await _geminiClient!
                        .StreamGenerateAsync(systemPrompt, sourceText, model, temperature, isSearchingEnabled: false, isThinkingEnabled, cancellationToken, onChunkReceived)
                        .ConfigureAwait(false);

                    return result;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Log("Gemini request canceled by user.");
                    throw;
                }
                catch (Exception ex) when (IsRateLimitError(ex))
                {
                    var delay = ParseRetryDelay(ex.Message);
                    Log($"Rate limit hit. Waiting {delay.TotalSeconds:F0}s...");
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log($"Request failed: {ex.Message}. Retrying in {retrySec}s...");
                    await Task.Delay(TimeSpan.FromSeconds(retrySec), cancellationToken).ConfigureAwait(false);
                    retrySec = Math.Min(retrySec * 2, maxDelaySec);
                }
            }
        }

        private static string ResolveGeminiModel()
        {
            string model = Properties.Settings.Default.GeminiModelId;
            return string.IsNullOrWhiteSpace(model) ? "gemini-flash-latest" : model;
        }

        private static double ResolveGeminiTemperature()
        {
            double temperature = Properties.Settings.Default.GeminiTemperature;
            return temperature is < 0.1 or > 1.5 ? 0.5 : temperature;
        }

        private static bool IsRateLimitError(Exception ex)
        {
            return ex.Message.Contains("429", StringComparison.Ordinal)
                || ex.Message.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase);
        }

        private static TimeSpan ParseRetryDelay(string message)
        {
            // Try to extract "retry in Xs" or "retryDelay": "Xs"
            var match = System.Text.RegularExpressions.Regex.Match(
                message, @"(\d+\.?\d*)s", System.Text.RegularExpressions.RegexOptions.RightToLeft);

            if (match.Success && double.TryParse(match.Groups[1].Value,
                System.Globalization.CultureInfo.InvariantCulture, out double seconds))
            {
                return TimeSpan.FromSeconds(Math.Ceiling(seconds));
            }

            return TimeSpan.FromSeconds(15);
        }
    }
}
