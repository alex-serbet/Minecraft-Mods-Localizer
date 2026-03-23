using System.Text.Json.Serialization;

namespace MinecraftLocalizer.Models.Ai.DeepSeek
{
    /// <summary>
    /// Represents a chat message for DeepSeek API
    /// </summary>
    public class DeepSeekChatMessage
    {
        /// <summary>
        /// The role of the message author (user, assistant, system)
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// The content of the message
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for DeepSeek chat completions
    /// </summary>
    public class DeepSeekChatRequest
    {
        /// <summary>
        /// The model to use for generation
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = "deepseek-chat";

        /// <summary>
        /// A list of messages comprising the conversation
        /// </summary>
        [JsonPropertyName("messages")]
        public List<DeepSeekChatMessage> Messages { get; set; } = new();

        /// <summary>
        /// Whether to stream the response
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = true;

        /// <summary>
        /// Maximum number of tokens to generate
        /// </summary>
        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Controls randomness in the output (0.0 to 2.0)
        /// </summary>
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }
    }

    /// <summary>
    /// Response model for DeepSeek chat completions
    /// </summary>
    public class DeepSeekChatResponse
    {
        /// <summary>
        /// Unique identifier for the response
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Object type (always "chat.completion")
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        /// <summary>
        /// Unix timestamp of when the response was created
        /// </summary>
        [JsonPropertyName("created")]
        public long Created { get; set; }

        /// <summary>
        /// The model used for generation
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// List of completion choices
        /// </summary>
        [JsonPropertyName("choices")]
        public List<DeepSeekChoice> Choices { get; set; } = new();

        /// <summary>
        /// Usage statistics
        /// </summary>
        [JsonPropertyName("usage")]
        public DeepSeekUsage? Usage { get; set; }
    }

    /// <summary>
    /// Represents a DeepSeek completion choice
    /// </summary>
    public class DeepSeekChoice
    {
        /// <summary>
        /// The index of this choice
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// The message content (for non-stream responses)
        /// </summary>
        [JsonPropertyName("message")]
        public DeepSeekChatMessage? Message { get; set; }

        /// <summary>
        /// The delta content (for stream responses)
        /// </summary>
        [JsonPropertyName("delta")]
        public DeepSeekDelta? Delta { get; set; }

        /// <summary>
        /// The reason the completion finished
        /// </summary>
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    /// <summary>
    /// Represents a delta in DeepSeek streaming response
    /// </summary>
    public class DeepSeekDelta
    {
        /// <summary>
        /// The role of the message author
        /// </summary>
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        /// <summary>
        /// The content delta
        /// </summary>
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    /// <summary>
    /// DeepSeek usage statistics for the request
    /// </summary>
    public class DeepSeekUsage
    {
        /// <summary>
        /// Number of tokens in the prompt
        /// </summary>
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        /// <summary>
        /// Number of tokens in the completion
        /// </summary>
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        /// <summary>
        /// Total number of tokens used
        /// </summary>
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    /// <summary>
    /// Represents the result of a DeepSeek chat completion
    /// </summary>
    public class DeepSeekChatResult
    {
        /// <summary>
        /// The generated content
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// The role of the response
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Usage statistics
        /// </summary>
        public DeepSeekUsage? Usage { get; set; }

        /// <summary>
        /// Whether the response was streamed
        /// </summary>
        public bool WasStreamed { get; set; }

        /// <summary>
        /// The finish reason
        /// </summary>
        public string? FinishReason { get; set; }
    }
}




