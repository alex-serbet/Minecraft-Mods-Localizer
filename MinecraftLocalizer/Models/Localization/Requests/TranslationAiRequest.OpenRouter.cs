using System.Net;
using MinecraftLocalizer.Models.Ai.OpenRouter;

namespace MinecraftLocalizer.Models.Localization.Requests
{
    public partial class TranslationAiRequest
    {
        private async Task<string> TranslateWithOpenRouterAsync(
            string sourceText,
            CancellationToken cancellationToken,
            Action<string>? onChunkReceived)
        {
            string systemPrompt = BuildSystemPrompt();
            string model = ResolveOpenRouterModel();
            double temperature = ResolveOpenRouterTemperature();
            bool reasoningEnabled = !Properties.Settings.Default.OpenRouterDisableReasoning;

            const int maxDelaySec = 60;
            int retrySec = 5;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Log($"Sending request to OpenRouter ({model})...");

                try
                {
                    return await _openRouterClient!
                        .StreamChatAsync(systemPrompt, sourceText, model, temperature, reasoningEnabled, cancellationToken, onChunkReceived)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Log("OpenRouter request canceled by user.");
                    throw;
                }
                catch (OpenRouterApiException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    Log($"Rate limit (429): {ex.Message}. Retrying in {retrySec}s...");
                    await Task.Delay(TimeSpan.FromSeconds(retrySec), cancellationToken).ConfigureAwait(false);
                    retrySec = Math.Min(retrySec * 2, maxDelaySec);
                }
                catch (OpenRouterApiException ex)
                {
                    Log($"OpenRouter API error ({(int)ex.StatusCode}): {ex.Message}. Retrying in {retrySec}s...");
                    await Task.Delay(TimeSpan.FromSeconds(retrySec), cancellationToken).ConfigureAwait(false);
                    retrySec = Math.Min(retrySec * 2, maxDelaySec);
                }
                catch (Exception ex)
                {
                    Log($"Request failed: {ex.Message}. Retrying in {retrySec}s...");
                    await Task.Delay(TimeSpan.FromSeconds(retrySec), cancellationToken).ConfigureAwait(false);
                    retrySec = Math.Min(retrySec * 2, maxDelaySec);
                }
            }
        }

        private static string ResolveOpenRouterModel()
        {
            string model = Properties.Settings.Default.OpenRouterModelId;
            return string.IsNullOrWhiteSpace(model) ? "openai/gpt-4o-mini" : model;
        }

        private static double ResolveOpenRouterTemperature()
        {
            double temperature = Properties.Settings.Default.OpenRouterTemperature;
            return temperature is < 0.1 or > 1.5 ? 0.5 : temperature;
        }
    }
}
