using System;
using System.Net.Http;
using System.Threading.Tasks;


namespace oops.Design_Patterns
{
    // Closed - The circuit is closed, and requests are allowed to flow through.
    // Open - The circuit is open, and requests are blocked to prevent further failures.
    // Half-Open - The circuit is half-open, allowing a limited number of requests to test if the underlying issue has been resolved.
    public enum CircuitState { Closed, Open, HalfOpen }

    internal class CircuitBreaker
    {
        private readonly int _failureThreshold;
        private readonly TimeSpan _openTimeout;
        private readonly object _lock = new();
        private CircuitState _state = CircuitState.Closed;
        private int _failureCount = 0;
        private DateTime _openedAt;

        public CircuitBreaker(int failureThreshold = 3, int openTimeoutSeconds = 15)
        {
            _failureThreshold = failureThreshold;
            _openTimeout = TimeSpan.FromSeconds(openTimeoutSeconds);
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, Func<T> fallback = null)
        {
            lock (_lock)
            {
                if (_state == CircuitState.Open)
                {
                    if (DateTime.UtcNow - _openedAt >= _openTimeout)
                    {
                        _state = CircuitState.HalfOpen;
                        Console.WriteLine("  [CB] Moving to Half-Open — sending probe...");
                    }
                    else
                    {
                        Console.WriteLine("  [CB] Circuit OPEN — fast fail, no retries attempted.");
                        if (fallback != null) return Task.FromResult(fallback()).Result;
                        throw new CircuitBreakerOpenException("Circuit open.");
                    }
                }
            }

            try
            {
                T result = await action();   // <-- action here is already wrapped by retry
                OnSuccess();
                return result;
            }
            catch (Exception ex)
            {
                OnFailure(ex);
                if (fallback != null) return fallback();
                throw;
            }
        }

        private void OnSuccess()
        {
            lock (_lock) { _failureCount = 0; _state = CircuitState.Closed; }
        }

        private void OnFailure(Exception ex)
        {
            lock (_lock)
            {
                _failureCount++;
                if (_state == CircuitState.HalfOpen || _failureCount >= _failureThreshold)
                {
                    _state = CircuitState.Open;
                    _openedAt = DateTime.UtcNow;
                    Console.WriteLine($"  [CB] TRIPPED to Open after {_failureCount} failure(s).");
                }
            }
        }

        public class CircuitBreakerOpenException : Exception
        {
            public CircuitBreakerOpenException(string msg) : base(msg) { }
        }



        public class RetryPolicy
        {
            private readonly int _maxRetries;
            private readonly TimeSpan _delay;

            public RetryPolicy(int maxRetries = 2, int delayMs = 200)
            {
                _maxRetries = maxRetries;
                _delay = TimeSpan.FromMilliseconds(delayMs);
            }

            public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
            {
                int attempt = 0;
                while (true)
                {
                    try
                    {
                        attempt++;
                        Console.WriteLine($"    [Retry] Attempt {attempt}...");
                        return await action();
                    }
                    catch (Exception ex) when (attempt <= _maxRetries)
                    {
                        Console.WriteLine($"    [Retry] Attempt {attempt} failed: {ex.Message}. Waiting {_delay.TotalMilliseconds}ms...");
                        await Task.Delay(_delay * attempt); // exponential-ish backoff
                    }
                }
            }
        }

        // ── 3. Payment Service — combines both ────────────────────────────────────

        public class PaymentService
        {
            private readonly CircuitBreaker _breaker = new(failureThreshold: 3, openTimeoutSeconds: 10);
            private readonly RetryPolicy _retry = new(maxRetries: 2, delayMs: 300);
            private static int _callCount = 0;

            public async Task<string> ChargeAsync(decimal amount)
            {
                // Circuit Breaker wraps Retry
                return await _breaker.ExecuteAsync(

                    action: () => _retry.ExecuteAsync(async () =>
                    {
                        _callCount++;
                        await Task.Delay(50); // simulate network

                        // Simulate: first 7 calls fail, then recover
                        if (_callCount <= 7)
                            throw new HttpRequestException("Payment gateway unavailable.");

                        return $"Charged {amount:C} successfully.";
                    }),

                    fallback: () => $"Payment of {amount:C} queued for later processing."
                );
            }
        }

        // ── 4. Demo ────────────────────────────────────────────────────────────────

        class Program
        {
            static async Task Main()
            {
                var svc = new PaymentService();

                for (int i = 1; i <= 12; i++)
                {
                    Console.WriteLine($"\nRequest #{i}:");
                    try
                    {
                        var result = await svc.ChargeAsync(49.99m);
                        Console.WriteLine($"  Result: {result}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  FAILED: {ex.Message}");
                    }

                    await Task.Delay(800);
                }
            }
        }

    }
}
