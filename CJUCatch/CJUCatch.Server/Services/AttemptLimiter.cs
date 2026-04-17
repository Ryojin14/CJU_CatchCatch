using System.Collections.Concurrent;

namespace CJUCatch.Server.Services;

public sealed class AttemptLimiter
{
    private readonly ConcurrentDictionary<string, AttemptWindow> _windows = new(StringComparer.Ordinal);

    public bool TryConsume(string scope, string actorKey, int maxAttempts, TimeSpan window, out TimeSpan retryAfter)
    {
        retryAfter = TimeSpan.Zero;

        var now = DateTimeOffset.UtcNow;
        var key = $"{scope}:{actorKey}";
        var attemptWindow = _windows.GetOrAdd(key, static _ => new AttemptWindow());

        lock (attemptWindow.SyncRoot)
        {
            while (attemptWindow.Attempts.Count > 0 && now - attemptWindow.Attempts.Peek() >= window)
            {
                attemptWindow.Attempts.Dequeue();
            }

            if (attemptWindow.Attempts.Count >= maxAttempts)
            {
                retryAfter = window - (now - attemptWindow.Attempts.Peek());
                if (retryAfter < TimeSpan.Zero)
                {
                    retryAfter = TimeSpan.Zero;
                }

                return false;
            }

            attemptWindow.Attempts.Enqueue(now);
            return true;
        }
    }

    private sealed class AttemptWindow
    {
        public object SyncRoot { get; } = new();
        public Queue<DateTimeOffset> Attempts { get; } = new();
    }
}
