using MinecraftLocalizer.Models.Ai.DeepSeek;
using System.Diagnostics;
using System.Net.Http;

namespace MinecraftLocalizer.Models.Localization.Requests
{
    public partial class TranslationAiRequest : IDisposable
    {
        private const string Gpt4FreeApiUrl = "http://localhost:1337/v1/chat/completions";
        private const int MaxAttempts = 3;
        private const int BaseRetryDelayMs = 1000;

        private readonly DeepSeekClient? _deepSeekClient;
        private readonly HttpClient? _httpClient;
        private readonly bool _useGpt4Free;
        private readonly Action<string>? _onLog;

        public TranslationAiRequest(bool useGpt4Free = false, Action<string>? onLog = null)
        {
            _useGpt4Free = useGpt4Free;
            _onLog = onLog;

            if (_useGpt4Free)
            {
                _httpClient = new HttpClient
                {
                    Timeout = Timeout.InfiniteTimeSpan
                };

                return;
            }

            string apiKey = Properties.Settings.Default.DeepSeekApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("DeepSeek API key is not configured. Please set it in settings.");
            }

            var options = new DeepSeekClientOptions
            {
                Model = "deepseek-chat",
                MaxTokens = 1000,
                Temperature = ResolveDeepSeekTemperature(),
                Timeout = TimeSpan.FromSeconds(120),
                ShowDebugInfo = false,
                ShowTokenUsage = false
            };

            _deepSeekClient = new DeepSeekClient(apiKey, options);
            _deepSeekClient.OnError += (_, ex) => Debug.WriteLine($"DeepSeek API Error: {ex.Message}");
        }

        public Task<string> TranslateTextWithStreamingUIAsync(
            string sourceText,
            CancellationToken cancellationToken,
            Action<string> onChunkReceived)
        {
            if (string.IsNullOrWhiteSpace(sourceText))
                return Task.FromResult(string.Empty);

            return _useGpt4Free
                ? TranslateWithGpt4FreeAsync(sourceText, cancellationToken, onChunkReceived)
                : TranslateWithDeepSeekAsync(sourceText, cancellationToken, onChunkReceived);
        }

        private void Log(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                _onLog?.Invoke(message);
        }

        public void Dispose()
        {
            _deepSeekClient?.Dispose();
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
