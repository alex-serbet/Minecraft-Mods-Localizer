using MinecraftLocalizer.Models.Ai.DeepSeek;
using MinecraftLocalizer.Models.Ai.Gemini;
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
        private readonly GeminiClient? _geminiClient;
        private readonly TranslationProvider _provider;
        private readonly Action<string>? _onLog;
        private readonly string? _modContext;

        public TranslationAiRequest(TranslationProvider provider, Action<string>? onLog = null, string? modContext = null)
        {
            _provider = provider;
            _onLog = onLog;
            _modContext = string.IsNullOrWhiteSpace(modContext) ? null : modContext;

            switch (_provider)
            {
                case TranslationProvider.Gpt4Free:
                    _httpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
                    break;

                case TranslationProvider.Gemini:
                {
                    string apiKey = Properties.Settings.Default.GeminiApiKey;
                    if (string.IsNullOrWhiteSpace(apiKey))
                        throw new InvalidOperationException("Gemini API key is not configured. Please set it in settings.");
                    _geminiClient = new GeminiClient(apiKey);
                    break;
                }

                case TranslationProvider.DeepSeek:
                default:
                {
                    string apiKey = Properties.Settings.Default.DeepSeekApiKey;
                    if (string.IsNullOrWhiteSpace(apiKey))
                        throw new InvalidOperationException("DeepSeek API key is not configured. Please set it in settings.");

                    var options = new DeepSeekClientOptions
                    {
                        Model = "deepseek-chat",
                        MaxTokens = 1000,
                        Temperature = ResolveDeepSeekTemperature(),
                        Timeout = TimeSpan.FromSeconds(120),
                        ShowDebugInfo = false,
                        ShowTokenUsage = false,
                    };

                    _deepSeekClient = new DeepSeekClient(apiKey, options);
                    _deepSeekClient.OnError += (_, ex) => Debug.WriteLine($"DeepSeek API Error: {ex.Message}");
                    break;
                }
            }
        }

        public Task<string> TranslateTextWithStreamingUIAsync(
            string sourceText,
            CancellationToken cancellationToken,
            Action<string> onChunkReceived)
        {
            if (string.IsNullOrWhiteSpace(sourceText))
                return Task.FromResult(string.Empty);

            return _provider switch
            {
                TranslationProvider.Gpt4Free => TranslateWithGpt4FreeAsync(sourceText, cancellationToken, onChunkReceived),
                TranslationProvider.Gemini => TranslateWithGeminiAsync(sourceText, cancellationToken, onChunkReceived),
                _ => TranslateWithDeepSeekAsync(sourceText, cancellationToken, onChunkReceived),
            };
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
            _geminiClient?.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Builds the system prompt sent to the LLM: base translation rules + per-mod context block.
        /// User-role messages must NEVER contain anything except the source text to translate, so the
        /// mod context lives here and gets refreshed for every batch when the mod changes.
        /// </summary>
        private string BuildSystemPrompt()
        {
            string targetLanguage = Properties.Settings.Default.TargetLanguage;
            string basePrompt = string.Format(Properties.Settings.Default.Prompt, targetLanguage);

            if (string.IsNullOrWhiteSpace(_modContext))
                return basePrompt;

            return basePrompt + "\n\n" + _modContext;
        }
    }
}
