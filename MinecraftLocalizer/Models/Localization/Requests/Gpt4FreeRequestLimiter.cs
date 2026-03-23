using MinecraftLocalizer.Models;

namespace MinecraftLocalizer.Models.Localization.Requests
{
    internal sealed class Gpt4FreeRequestLimiter
    {
        private static readonly TimeSpan MinuteWindow = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan HourWindow = TimeSpan.FromHours(1);

        private readonly Queue<DateTime> _minuteRequests = new();
        private readonly Queue<DateTime> _hourRequests = new();
        private readonly object _lock = new();

        private DateTime _minuteCooldownUntilUtc = DateTime.MinValue;
        private DateTime _hourCooldownUntilUtc = DateTime.MinValue;
        private DateTime _lastServerLimitUtc = DateTime.MinValue;
        private int _serverLimitHitCount;
        private DateTime _suppressServerCooldownLogUntilUtc = DateTime.MinValue;

        public static Gpt4FreeRequestLimiter Instance { get; } = new();

        private Gpt4FreeRequestLimiter() { }

        public async Task WaitForAvailabilityAsync(
            bool hasApiKey,
            CancellationToken cancellationToken,
            Action<string>? log)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                TimeSpan wait;
                string? reason;
                int perMinuteLimit;
                int perHourLimit;

                lock (_lock)
                {
                    var now = DateTime.UtcNow;
                    Cleanup(now);

                    (perMinuteLimit, perHourLimit) = GetLimits(hasApiKey);

                    bool minuteLimited = _minuteRequests.Count >= perMinuteLimit;
                    bool hourLimited = _hourRequests.Count >= perHourLimit;

                    var minuteWait = minuteLimited
                        ? _minuteRequests.Peek().Add(MinuteWindow) - now
                        : TimeSpan.Zero;
                    var hourWait = hourLimited
                        ? _hourRequests.Peek().Add(HourWindow) - now
                        : TimeSpan.Zero;

                    var cooldownWait = TimeSpan.Zero;
                    if (_minuteCooldownUntilUtc > now)
                        cooldownWait = _minuteCooldownUntilUtc - now;
                    if (_hourCooldownUntilUtc > now)
                        cooldownWait = Max(cooldownWait, _hourCooldownUntilUtc - now);

                    wait = Max(Max(minuteWait, hourWait), cooldownWait);

                    if (wait <= TimeSpan.Zero)
                    {
                        Register(now);
                        return;
                    }

                    reason = BuildReason(minuteLimited, hourLimited, cooldownWait > TimeSpan.Zero);
                }

                if (wait < TimeSpan.FromSeconds(1))
                    wait = TimeSpan.FromSeconds(1);

                bool suppressLog = false;
                lock (_lock)
                {
                    var now = DateTime.UtcNow;
                    if (_suppressServerCooldownLogUntilUtc > now && reason?.Contains("server cooldown", StringComparison.Ordinal) == true)
                    {
                        suppressLog = true;
                    }
                }

                if (!suppressLog)
                {
                    if (!string.IsNullOrWhiteSpace(reason))
                    {
                        log?.Invoke($"GPT4Free limiter active ({reason}). Waiting {FormatWait(wait)}.");
                    }
                    else
                    {
                        log?.Invoke($"GPT4Free limiter active. Waiting {FormatWait(wait)}.");
                    }
                }

                await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
            }
        }

        public TimeSpan RegisterServerLimit(RateLimitType type, TimeSpan? retryAfter = null)
        {
            TimeSpan wait;

            lock (_lock)
            {
                var now = DateTime.UtcNow;
                Cleanup(now);

                wait = retryAfter ?? GetLocalWait(type, now) ?? type switch
                {
                    RateLimitType.PerMinute => MinuteWindow,
                    RateLimitType.PerHour => HourWindow,
                    RateLimitType.Generic => MinuteWindow,
                    _ => TimeSpan.Zero
                };

                wait = ApplyServerBackoff(now, wait);

                if (wait <= TimeSpan.Zero)
                    return TimeSpan.Zero;

                if (type == RateLimitType.PerMinute)
                {
                    _minuteCooldownUntilUtc = Max(_minuteCooldownUntilUtc, now + wait);
                }
                else if (type == RateLimitType.PerHour)
                {
                    _hourCooldownUntilUtc = Max(_hourCooldownUntilUtc, now + wait);
                }
                else
                {
                    _minuteCooldownUntilUtc = Max(_minuteCooldownUntilUtc, now + wait);
                    _hourCooldownUntilUtc = Max(_hourCooldownUntilUtc, now + wait);
                }
            }

            return wait;
        }

        public void SuppressServerCooldownLog(TimeSpan wait)
        {
            if (wait <= TimeSpan.Zero)
                return;

            lock (_lock)
            {
                _suppressServerCooldownLogUntilUtc = DateTime.UtcNow + wait;
            }
        }

        private void Cleanup(DateTime now)
        {
            while (_minuteRequests.Count > 0 && now - _minuteRequests.Peek() >= MinuteWindow)
                _minuteRequests.Dequeue();

            while (_hourRequests.Count > 0 && now - _hourRequests.Peek() >= HourWindow)
                _hourRequests.Dequeue();
        }

        private void Register(DateTime now)
        {
            _minuteRequests.Enqueue(now);
            _hourRequests.Enqueue(now);
        }

        private TimeSpan? GetLocalWait(RateLimitType type, DateTime now)
        {
            if (type == RateLimitType.PerMinute && _minuteRequests.Count > 0)
            {
                var wait = _minuteRequests.Peek().Add(MinuteWindow) - now;
                return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
            }

            if (type == RateLimitType.PerHour && _hourRequests.Count > 0)
            {
                var wait = _hourRequests.Peek().Add(HourWindow) - now;
                return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
            }

            return null;
        }

        private TimeSpan ApplyServerBackoff(DateTime now, TimeSpan baseWait)
        {
            if (baseWait <= TimeSpan.Zero)
                return baseWait;

            if (now - _lastServerLimitUtc > TimeSpan.FromMinutes(10))
                _serverLimitHitCount = 0;

            _serverLimitHitCount++;
            _lastServerLimitUtc = now;

            if (baseWait >= TimeSpan.FromMinutes(30))
                return baseWait;

            double extraSeconds = Math.Min(120, Math.Pow(2, _serverLimitHitCount - 1) * 5);
            return baseWait + TimeSpan.FromSeconds(extraSeconds);
        }

        private static (int perMinute, int perHour) GetLimits(bool hasApiKey)
        {
            return hasApiKey ? (10, 100) : (5, 50);
        }

        private static string BuildReason(bool minuteLimited, bool hourLimited, bool cooldownLimited)
        {
            if (cooldownLimited && (minuteLimited || hourLimited))
                return "server cooldown + local limits";
            if (cooldownLimited)
                return "server cooldown";
            if (minuteLimited && hourLimited)
                return "minute/hour limit";
            if (minuteLimited)
                return "minute limit";
            if (hourLimited)
                return "hour limit";
            return string.Empty;
        }

        private static string FormatWait(TimeSpan wait)
        {
            int totalSeconds = Math.Max(1, (int)Math.Ceiling(wait.TotalSeconds));
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;

            if (hours > 0)
            {
                return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
            }

            if (minutes > 0)
            {
                return seconds > 0 ? $"{minutes}m {seconds}s" : $"{minutes}m";
            }

            return $"{seconds}s";
        }

        private static TimeSpan Max(TimeSpan a, TimeSpan b)
        {
            return a >= b ? a : b;
        }

        private static DateTime Max(DateTime a, DateTime b)
        {
            return a >= b ? a : b;
        }
    }
}


