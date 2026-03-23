using System.Net.Http;
using System.Text.Json;

namespace MinecraftLocalizer.Models.Localization.Requests
{
    public partial class TranslationAiRequest
    {
        private static string FormatWait(TimeSpan wait)
        {
            int totalSeconds = Math.Max(1, (int)Math.Ceiling(wait.TotalSeconds));
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;

            if (hours > 0)
                return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";

            if (minutes > 0)
                return seconds > 0 ? $"{minutes}m {seconds}s" : $"{minutes}m";

            return $"{seconds}s";
        }

        private static string FormatLimitType(RateLimitType type)
        {
            return type switch
            {
                RateLimitType.PerMinute => "per minute",
                RateLimitType.PerHour => "per hour",
                RateLimitType.Generic => "unknown limit",
                _ => "unknown"
            };
        }

        private static TimeSpan? TryGetRetryAfter(HttpResponseMessage response)
        {
            var retryHeader = response.Headers.RetryAfter;
            if (retryHeader == null)
                return null;

            if (retryHeader.Delta.HasValue)
                return retryHeader.Delta.Value;

            if (retryHeader.Date.HasValue)
            {
                var delta = retryHeader.Date.Value - DateTimeOffset.UtcNow;
                return delta > TimeSpan.Zero ? delta : TimeSpan.Zero;
            }

            return null;
        }

        private static bool TryParseRetryAfterSeconds(JsonElement element, out double seconds)
        {
            seconds = 0;

            if (element.ValueKind == JsonValueKind.Number &&
                element.TryGetDouble(out var numericValue) &&
                numericValue > 0)
            {
                seconds = numericValue;
                return true;
            }

            if (element.ValueKind == JsonValueKind.String &&
                double.TryParse(element.GetString(), out var parsedValue) &&
                parsedValue > 0)
            {
                seconds = parsedValue;
                return true;
            }

            return false;
        }
    }
}
