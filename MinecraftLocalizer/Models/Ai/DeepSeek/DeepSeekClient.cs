using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MinecraftLocalizer.Models.Ai.DeepSeek
{
    /// <summary>
    /// Main client for interacting with DeepSeek AI API
    /// </summary>
    public class DeepSeekClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl = "https://api.deepseek.com/chat/completions";
        private readonly List<DeepSeekChatMessage> _conversationHistory = new();
        private bool _disposed;

        /// <summary>
        /// Configuration options for the client
        /// </summary>
        public DeepSeekClientOptions Options { get; }

        /// <summary>
        /// Event raised when a chunk of streamed content is received
        /// </summary>
        public event EventHandler<string>? OnStreamChunkReceived;

        /// <summary>
        /// Event raised when an error occurs
        /// </summary>
        public event EventHandler<Exception>? OnError;

        /// <summary>
        /// Event raised when debug information should be displayed
        /// </summary>
        public event EventHandler<string>? OnDebugInfo;

        /// <summary>
        /// Event raised when token usage should be displayed
        /// </summary>
        public event EventHandler<DeepSeekUsage>? OnTokenUsage;

        /// <summary>
        /// Event raised when a message is added to conversation history
        /// </summary>
        public event EventHandler<DeepSeekChatMessage>? OnMessageAdded;

        /// <summary>
        /// Event raised when conversation history is cleared
        /// </summary>
        public event EventHandler? OnHistoryCleared;

        /// <summary>
        /// Initializes a new instance of DeepSeekClient
        /// </summary>
        /// <param name="apiKey">DeepSeek API key</param>
        /// <param name="options">Client configuration options</param>
        public DeepSeekClient(string apiKey, DeepSeekClientOptions? options = null)
        {
            Options = options ?? new DeepSeekClientOptions();
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            _httpClient.Timeout = Options.Timeout;
        }

        /// <summary>
        /// Sends a chat message and automatically manages conversation history
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="stream">Whether to stream the response</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The chat result</returns>
        public async Task<DeepSeekChatResult> SendMessageAsync(
            string message,
            bool stream = true,
            CancellationToken cancellationToken = default)
        {
            // Add user message to history
            var userMessage = new DeepSeekChatMessage { Role = "user", Content = message };
            AddMessageToHistory(userMessage);

            // Send all messages from history
            var result = await SendMessagesAsync(_conversationHistory, stream, cancellationToken);

            // Add assistant response to history
            var assistantMessage = new DeepSeekChatMessage { Role = result.Role, Content = result.Content };
            AddMessageToHistory(assistantMessage);

            return result;
        }

        /// <summary>
        /// Sends a message without managing history (for custom history management)
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="stream">Whether to stream the response</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The chat result</returns>
        public async Task<DeepSeekChatResult> SendMessageOnceAsync(
            string message,
            bool stream = true,
            CancellationToken cancellationToken = default)
        {
            var messages = new List<DeepSeekChatMessage>
            {
                new DeepSeekChatMessage { Role = "user", Content = message }
            };

            return await SendMessagesAsync(messages, stream, cancellationToken);
        }

        /// <summary>
        /// Sends multiple messages in a conversation
        /// </summary>
        /// <param name="messages">List of messages</param>
        /// <param name="stream">Whether to stream the response</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The chat result</returns>
        public async Task<DeepSeekChatResult> SendMessagesAsync(
            IEnumerable<DeepSeekChatMessage> messages,
            bool stream = true,
            CancellationToken cancellationToken = default)
        {
            var request = new DeepSeekChatRequest
            {
                Model = Options.Model,
                Messages = messages.ToList(),
                Stream = stream,
                MaxTokens = Options.MaxTokens,
                Temperature = Options.Temperature
            };

            var jsonRequest = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
            httpRequest.Content = content;
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            using var response = await _httpClient.SendAsync(
                httpRequest,
                stream ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            if (stream)
            {
                return await ProcessStreamResponseAsync(response, cancellationToken);
            }
            else
            {
                return await ProcessRegularResponseAsync(response, cancellationToken);
            }
        }

        /// <summary>
        /// Sends a message and returns the response with debug info
        /// </summary>
        public async Task<DeepSeekChatResult> SendMessageWithDebugAsync(
            string message,
            bool stream = true,
            CancellationToken cancellationToken = default)
        {
            // Temporarily enable debug info
            var originalDebugSetting = Options.ShowDebugInfo;
            var originalTokenSetting = Options.ShowTokenUsage;

            Options.ShowDebugInfo = true;
            Options.ShowTokenUsage = true;

            try
            {
                return await SendMessageAsync(message, stream, cancellationToken);
            }
            finally
            {
                Options.ShowDebugInfo = originalDebugSetting;
                Options.ShowTokenUsage = originalTokenSetting;
            }
        }

        private async Task<DeepSeekChatResult> ProcessStreamResponseAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            var result = new DeepSeekChatResult { WasStreamed = true };
            var contentBuilder = new StringBuilder();

            // Show debug info if enabled
            if (Options.ShowDebugInfo)
            {
                var contentType = response.Content.Headers.ContentType?.MediaType;
                OnDebugInfo?.Invoke(this, $"Response Content-Type: {contentType}");
            }

            try
            {
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                string? line;
                int lineCount = 0;
            
                try
                {
                    while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                    {
                        lineCount++;

                        // Show first few lines for debugging if enabled
                        if (Options.ShowDebugInfo && lineCount <= 3)
                        {
                            OnDebugInfo?.Invoke(this, $"Stream line {lineCount}: {line}");
                        }

                        if (line.StartsWith("data: "))
                        {
                            var data = line.Substring(6);
                            if (data == "[DONE]")
                            {
                                if (Options.ShowDebugInfo)
                                {
                                    OnDebugInfo?.Invoke(this, "Stream ended with [DONE]");
                                }
                                result.FinishReason = "stop";
                                break;
                            }

                            try
                            {
                                var chatResponse = JsonSerializer.Deserialize<DeepSeekChatResponse>(data);
                                if (chatResponse?.Choices?.Count > 0)
                                {
                                    var choice = chatResponse.Choices[0];
                                    var deltaContent = choice.Delta?.Content;

                                    if (!string.IsNullOrEmpty(deltaContent))
                                    {
                                        contentBuilder.Append(deltaContent);
                                        OnStreamChunkReceived?.Invoke(this, deltaContent);
                                    }

                                    if (choice.Delta?.Role != null)
                                    {
                                        result.Role = choice.Delta.Role;
                                    }

                                    if (choice.FinishReason != null)
                                    {
                                        result.FinishReason = choice.FinishReason;
                                    }

                                    if (chatResponse.Usage != null)
                                    {
                                        result.Usage = chatResponse.Usage;
                                        if (Options.ShowTokenUsage)
                                        {
                                            OnTokenUsage?.Invoke(this, chatResponse.Usage);
                                        }
                                    }
                                }
                            }
                            catch (JsonException ex)
                            {
                                if (Options.ShowDebugInfo)
                                {
                                    OnDebugInfo?.Invoke(this, $"JSON parsing error: {ex.Message}");
                                }
                                OnError?.Invoke(this, ex);
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(line) && Options.ShowDebugInfo)
                        {
                            OnDebugInfo?.Invoke(this, $"Non-data line: {line}");
                        }
                    }
                }
                catch (IOException ioEx) when (ioEx.Message.Contains("Удаленный хост принудительно разорвал существующее подключение") ||
                                              ioEx.Message.Contains("Unable to read data from the transport connection"))
                {
                    // Handle connection reset gracefully
                    if (Options.ShowDebugInfo)
                    {
                        OnDebugInfo?.Invoke(this, $"Connection reset by remote host. Partial content received: {contentBuilder.Length} characters");
                    }
                
                    // Still return partial content if we have any
                    result.FinishReason = "connection_reset";
                }
                catch (OperationCanceledException)
                {
                    // Handle cancellation gracefully
                    if (Options.ShowDebugInfo)
                    {
                        OnDebugInfo?.Invoke(this, "Stream reading was cancelled");
                    }
                    result.FinishReason = "cancelled";
                    throw;
                }
                catch (Exception ex)
                {
                    // Handle other exceptions
                    if (Options.ShowDebugInfo)
                    {
                        OnDebugInfo?.Invoke(this, $"Error reading stream: {ex.Message}");
                    }
                    OnError?.Invoke(this, ex);
                    result.FinishReason = "error";
                }

                if (Options.ShowDebugInfo)
                {
                    OnDebugInfo?.Invoke(this, $"Total lines processed: {lineCount}");
                }
            }
            catch (Exception ex)
            {
                if (Options.ShowDebugInfo)
                {
                    OnDebugInfo?.Invoke(this, $"Error in ProcessStreamResponseAsync: {ex.Message}");
                }
                OnError?.Invoke(this, ex);
                result.FinishReason = "error";
            }

            result.Content = contentBuilder.ToString();
            return result;
        }

        private async Task<DeepSeekChatResult> ProcessRegularResponseAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                var chatResponse = JsonSerializer.Deserialize<DeepSeekChatResponse>(responseBody);
                if (chatResponse?.Choices?.Count > 0)
                {
                    var choice = chatResponse.Choices[0];
                    var result = new DeepSeekChatResult
                    {
                        Content = choice.Message?.Content ?? string.Empty,
                        Role = choice.Message?.Role ?? "assistant",
                        Usage = chatResponse.Usage,
                        WasStreamed = false,
                        FinishReason = choice.FinishReason
                    };

                    // Show token usage if enabled
                    if (Options.ShowTokenUsage && chatResponse.Usage != null)
                    {
                        OnTokenUsage?.Invoke(this, chatResponse.Usage);
                    }

                    return result;
                }
            }
            catch (JsonException ex)
            {
                if (Options.ShowDebugInfo)
                {
                    OnDebugInfo?.Invoke(this, $"Failed to parse response: {ex.Message}");
                }
                OnError?.Invoke(this, ex);
            }

            return new DeepSeekChatResult { Content = string.Empty };
        }

        /// <summary>
        /// Adds a message to the conversation history
        /// </summary>
        /// <param name="message">The message to add</param>
        private void AddMessageToHistory(DeepSeekChatMessage message)
        {
            _conversationHistory.Add(message);
            OnMessageAdded?.Invoke(this, message);

            // Limit history size if configured
            if (Options.MaxHistorySize > 0 && _conversationHistory.Count > Options.MaxHistorySize)
            {
                _conversationHistory.RemoveAt(0); // Remove oldest message
            }
        }

        /// <summary>
        /// Gets a copy of the current conversation history
        /// </summary>
        /// <returns>List of messages in conversation history</returns>
        public List<DeepSeekChatMessage> GetHistory()
        {
            return new List<DeepSeekChatMessage>(_conversationHistory);
        }

        /// <summary>
        /// Clears the conversation history
        /// </summary>
        public void ClearHistory()
        {
            _conversationHistory.Clear();
            OnHistoryCleared?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Gets the number of messages in conversation history
        /// </summary>
        public int HistoryCount => _conversationHistory.Count;

        /// <summary>
        /// Manually adds a message to history (for custom scenarios)
        /// </summary>
        /// <param name="role">Message role (user, assistant, system)</param>
        /// <param name="content">Message content</param>
        public void AddMessage(string role, string content)
        {
            var message = new DeepSeekChatMessage { Role = role, Content = content };
            AddMessageToHistory(message);
        }

        /// <summary>
        /// Disposes the client and underlying HttpClient
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Configuration options for DeepSeekClient
    /// </summary>
    public class DeepSeekClientOptions
    {
        /// <summary>
        /// The model to use for chat completions
        /// </summary>
        public string Model { get; set; } = "deepseek-chat";

        /// <summary>
        /// Maximum number of tokens to generate
        /// </summary>
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Controls randomness in the output (0.0 to 2.0)
        /// </summary>
        public double? Temperature { get; set; } = 0.7;

        /// <summary>
        /// Request timeout
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether to show debug information (Content-Type, stream lines, errors)
        /// </summary>
        public bool ShowDebugInfo { get; set; } = false;

        /// <summary>
        /// Whether to show token usage statistics
        /// </summary>
        public bool ShowTokenUsage { get; set; } = false;

        /// <summary>
        /// Maximum number of messages to keep in conversation history (0 = unlimited)
        /// </summary>
        public int MaxHistorySize { get; set; } = 50;
    }
}




