using System.Text;

namespace MinecraftLocalizer.Models.Localization.Requests
{
    public partial class TranslationAiRequest
    {
        private async Task<string> TranslateWithDeepSeekAsync(
            string sourceText,
            CancellationToken cancellationToken,
            Action<string> onChunkReceived)
        {
            string targetLanguage = Properties.Settings.Default.TargetLanguage;
            string prompt = string.Format(Properties.Settings.Default.Prompt, targetLanguage);
            string message = $"{sourceText}\n\n{prompt}";

            var builder = new StringBuilder();
            EventHandler<string>? handler = (_, chunk) =>
            {
                builder.Append(chunk);
                onChunkReceived(builder.ToString());
            };

            _deepSeekClient!.OnStreamChunkReceived += handler;

            try
            {
                var result = await _deepSeekClient
                    .SendMessageOnceAsync(message, stream: true, cancellationToken)
                    .ConfigureAwait(false);

                return result.Content;
            }
            finally
            {
                _deepSeekClient.OnStreamChunkReceived -= handler;
            }
        }

        private static double ResolveDeepSeekTemperature()
        {
            double temperature = Properties.Settings.Default.DeepSeekTemperature;
            return temperature is < 0.1 or > 1.5 ? 0.5 : temperature;
        }
    }
}
