using Google.GenAI;
using Google.GenAI.Types;

namespace MinecraftLocalizer.Models.Ai.Gemini
{
    /// <summary>
    /// Wrapper around Google.GenAI SDK for streaming translation requests.
    /// Supports system instructions, Google Search grounding, and thinking control.
    /// </summary>
    public sealed class GeminiClient : IDisposable
    {
        private readonly Client _client;
        private bool _disposed;

        /// <summary>
        /// Indicates whether the last response contained grounding metadata.
        /// </summary>
        public bool GroundingUsed { get; private set; }

        public GeminiClient(string apiKey)
        {
            _client = new Client(apiKey: apiKey);
        }

        /// <summary>
        /// Streams content generation and returns the complete text.
        /// Chunks are piped to <paramref name="onChunkReceived"/> as they arrive.
        /// </summary>
        public async Task<string> StreamGenerateAsync(
            string systemPrompt,
            string userText,
            string model,
            double temperature,
            bool isSearchingEnabled,
            bool isThinkingEnabled,
            CancellationToken cancellationToken,
            Action<string>? onChunkReceived)
        {
            var config = BuildConfig(systemPrompt, temperature, model, isSearchingEnabled, isThinkingEnabled);

            string accumulated = string.Empty;
            string display = string.Empty;
            bool wasThinking = false;
            GroundingUsed = false;

            await foreach (var chunk in _client.Models.GenerateContentStreamAsync(
                model: model,
                contents: userText,
                config: config).WithCancellation(cancellationToken))
            {
                if (chunk?.Candidates == null || chunk.Candidates.Count == 0)
                    continue;

                var candidate = chunk.Candidates[0];

                // Detect grounding
                if (!GroundingUsed && candidate.GroundingMetadata != null)
                    GroundingUsed = true;

                if (candidate.Content?.Parts == null)
                    continue;

                foreach (var part in candidate.Content.Parts)
                {
                    if (string.IsNullOrEmpty(part.Text))
                        continue;

                    if (part.Thought == true)
                    {
                        // Show thinking in UI with a visual separator
                        if (!wasThinking)
                        {
                            display += "💭 Thinking...\n";
                            wasThinking = true;
                        }
                        display += part.Text;
                        onChunkReceived?.Invoke(display);
                    }
                    else
                    {
                        // Transition from thinking to answer
                        if (wasThinking)
                        {
                            display += "\n\n📝 Answer:\n";
                            wasThinking = false;
                        }

                        accumulated += part.Text;
                        display += part.Text;
                        onChunkReceived?.Invoke(display);
                    }
                }
            }

            return accumulated;
        }

        private static GenerateContentConfig BuildConfig(
            string systemPrompt, double temperature, string model,
            bool isSearchingEnabled, bool isThinkingEnabled)
        {
            var config = new GenerateContentConfig
            {
                Temperature = temperature,
            };

            // System instruction
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                config.SystemInstruction = new Content
                {
                    Parts = new List<Part> { new Part { Text = systemPrompt } }
                };
            }

            // Google Search grounding
            if (isSearchingEnabled)
            {
                config.Tools = new List<Tool>
                {
                    new Tool { GoogleSearch = new GoogleSearch() }
                };
            }

            // Thinking control
            if (!isThinkingEnabled)
            {
                config.ThinkingConfig = ResolveThinkingConfig(model);
            }

            return config;
        }

        /// <summary>
        /// Gemini 2.5/2.0 use thinkingBudget=0 to disable.
        /// Gemini 3+, Gemma, and others use thinkingLevel="minimal".
        /// </summary>
        private static ThinkingConfig ResolveThinkingConfig(string model)
        {
            bool usesBudget = model.Contains("2.5", StringComparison.OrdinalIgnoreCase)
                           || model.Contains("2.0", StringComparison.OrdinalIgnoreCase);

            if (usesBudget)
                return new ThinkingConfig { ThinkingBudget = 0 };

            return new ThinkingConfig { ThinkingLevel = ThinkingLevel.Minimal };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
