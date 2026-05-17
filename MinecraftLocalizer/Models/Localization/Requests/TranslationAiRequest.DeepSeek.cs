using MinecraftLocalizer.Models.Ai.DeepSeek;
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
            string systemPrompt = BuildSystemPrompt();

            var messages = new List<DeepSeekChatMessage>
            {
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = sourceText },
            };

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
                    .SendMessagesAsync(messages, stream: true, cancellationToken)
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
