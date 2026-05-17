using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MinecraftLocalizer.Models.Ai.OpenRouter
{
    /// <summary>
    /// Lightweight streaming client for the OpenRouter API (OpenAI-compatible).
    /// Handles both regular content and extended-thinking (reasoning) chunks.
    /// </summary>
    public sealed class OpenRouterClient : IDisposable
    {
        private const string ChatCompletionsUrl = "https://openrouter.ai/api/v1/chat/completions";

        private readonly HttpClient _httpClient;
        private bool _disposed;

        public OpenRouterClient(string apiKey, TimeSpan? timeout = null)
        {
            _httpClient = new HttpClient { Timeout = timeout ?? TimeSpan.FromSeconds(120) };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/Alex-Serbet/Minecraft-Mods-Localizer");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "Minecraft Localizer");
        }

        /// <summary>
        /// Sends a streaming chat completion request and returns the accumulated text.
        /// Reasoning chunks are displayed in the UI but excluded from the final result.
        /// </summary>
        /// <param name="reasoningEnabled">
        /// When true, reasoning tokens are allowed and shown in UI.
        /// When false, sends <c>"reasoning": {"effort": "none"}</c> to disable reasoning.
        /// </param>
        public async Task<string> StreamChatAsync(
            string systemPrompt,
            string userText,
            string model,
            double temperature,
            bool reasoningEnabled,
            CancellationToken cancellationToken,
            Action<string>? onChunkReceived = null)
        {
            var payload = new JsonObject
            {
                ["model"] = model,
                ["messages"] = new JsonArray
                {
                    new JsonObject { ["role"] = "system", ["content"] = systemPrompt },
                    new JsonObject { ["role"] = "user",   ["content"] = userText }
                },
                ["temperature"] = temperature,
                ["stream"] = true
            };

            if (!reasoningEnabled)
            {
                payload["reasoning"] = new JsonObject { ["effort"] = "none" };
            }

            string json = payload.ToJsonString();
            using var request = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                string detail = TryExtractErrorMessage(errorBody) ?? errorBody;
                throw new OpenRouterApiException(response.StatusCode, detail);
            }

            var accumulated = new StringBuilder();
            var display = new StringBuilder();
            bool wasReasoning = false;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!line.StartsWith("data: ", StringComparison.Ordinal))
                    continue;

                string data = line[6..];
                if (data == "[DONE]")
                    break;

                var chunk = ParseChunk(data);

                if (!string.IsNullOrEmpty(chunk.Reasoning))
                {
                    if (!wasReasoning)
                    {
                        display.Append("💭 Thinking...\n");
                        wasReasoning = true;
                    }
                    display.Append(chunk.Reasoning);
                    onChunkReceived?.Invoke(display.ToString());
                }

                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    if (wasReasoning)
                    {
                        display.Append("\n\n📝 Answer:\n");
                        wasReasoning = false;
                    }

                    accumulated.Append(chunk.Content);
                    display.Append(chunk.Content);
                    onChunkReceived?.Invoke(display.ToString());
                }
            }

            return accumulated.ToString();
        }

        private readonly record struct ChunkResult(string? Content, string? Reasoning);

        private static ChunkResult ParseChunk(string json)
        {
            string? content = null;
            string? reasoning = null;

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("choices", out var choices))
                    return default;

                foreach (var choice in choices.EnumerateArray())
                {
                    if (!choice.TryGetProperty("delta", out var delta))
                        continue;

                    // Regular content
                    if (delta.TryGetProperty("content", out var c) &&
                        c.ValueKind == JsonValueKind.String)
                    {
                        string? val = c.GetString();
                        if (!string.IsNullOrEmpty(val))
                            content = val;
                    }

                    // Reasoning text (extended thinking models)
                    if (delta.TryGetProperty("reasoning", out var r) &&
                        r.ValueKind == JsonValueKind.String)
                    {
                        string? val = r.GetString();
                        if (!string.IsNullOrEmpty(val))
                            reasoning = val;
                    }
                }
            }
            catch { /* malformed chunk — skip */ }

            return new ChunkResult(content, reasoning);
        }

        private static string? TryExtractErrorMessage(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("error", out var error))
                {
                    if (error.TryGetProperty("message", out var msg))
                        return msg.GetString();
                    if (error.ValueKind == JsonValueKind.String)
                        return error.GetString();
                }
            }
            catch { /* not JSON */ }
            return null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _httpClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Carries HTTP status code and the API error message for proper handling upstream.
    /// </summary>
    public sealed class OpenRouterApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public OpenRouterApiException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
