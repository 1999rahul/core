using Microsoft.AspNetCore.Http.HttpResults;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Security.Claims;
using static ASPNETCONCEPTS.DEPENDENCY_INJECTION;

namespace ASPNETCONCEPTS
{
    public class DI_EXAMPLES
    {
        // ============================================================
        //   DI LIFETIMES — REAL WORLD EXAMPLES
        //   Singleton, Scoped, Transient + Captive Dependency Problem
        //   ASP.NET Core — Interview Notes
        // ============================================================

        // ============================================================
        // SINGLETON EXAMPLES
        // ============================================================
        //
        // Rule: ONE instance for the ENTIRE application lifetime
        // Use when:
        //   - Service is stateless (no per-request data)
        //   - OR state is intentionally shared across ALL requests
        //   - Service is expensive to create (create once, reuse forever)
        //   - Service is thread-safe (MUST be — multiple threads hit it)


        // ----------------------------------------------------------
        // SINGLETON EXAMPLE 1 — In-Memory Cache
        // ----------------------------------------------------------
        // WHY SINGLETON?
        //   You WANT all requests sharing the same cache.
        //   If Scoped, each request gets its own empty cache — pointless.
        //   If Transient, each class gets its own empty cache — even more pointless.
        //   The whole purpose of a cache is that it is shared.
        //   Must use ConcurrentDictionary because multiple threads write to it.

        interface ICacheService { }
        public class InMemoryCacheService : ICacheService
        {
            // ConcurrentDictionary — thread-safe, no manual locking needed
            // Shared across ALL requests — this is exactly the point
            private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

            public void Set<T>(string key, T value, TimeSpan duration)
            {
                _cache[key] = new CacheEntry
                {
                    Value = value,
                    ExpiresAt = DateTime.UtcNow.Add(duration)
                };
                // No lock needed — ConcurrentDictionary handles thread safety
            }

            public T? Get<T>(string key)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    if (entry.ExpiresAt > DateTime.UtcNow)
                        return (T?)entry.Value; // cache hit

                    _cache.TryRemove(key, out _); // expired, remove it
                }
                return default; // cache miss
            }

            public void Invalidate(string key) => _cache.TryRemove(key, out _);

            private class CacheEntry
            {
                public object? Value { get; set; }
                public DateTime ExpiresAt { get; set; }
            }
        }

        // Registration:
        // builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
        // Usage — any service can inject this, they all share the same cache

        // ----------------------------------------------------------
        // SINGLETON EXAMPLE 2 — Configuration/Settings Reader
        // ----------------------------------------------------------
        // WHY SINGLETON?
        //   Settings are loaded once at startup and never change at runtime.
        //   Expensive to read from environment/files repeatedly.
        //   No per-request state — same settings for every request.
        //   Thread-safe because it is read-only after construction.

        public interface IAppSettingsService { }
        public class AppSettingsService : IAppSettingsService
        {
            // All values loaded ONCE at construction time
            public string DatabaseConnectionString { get; }
            public string JwtSecretKey { get; }
            public int JwtExpiryMinutes { get; }
            public string SendGridApiKey { get; }
            public bool IsMaintenanceModeEnabled { get; }

            // IConfiguration is itself Singleton — safe to inject here
            public AppSettingsService(IConfiguration configuration)
            {
                DatabaseConnectionString = configuration
                    .GetConnectionString("Default")
                    ?? throw new InvalidOperationException("DB connection string missing");

                JwtSecretKey = configuration["Jwt:SecretKey"]
                    ?? throw new InvalidOperationException("JWT secret key missing");

                JwtExpiryMinutes = configuration.GetValue<int>("Jwt:ExpiryMinutes", 60);
                SendGridApiKey = configuration["SendGrid:ApiKey"] ?? string.Empty;
                IsMaintenanceModeEnabled = configuration.GetValue<bool>("MaintenanceMode", false);

                // All values are read once — object is immutable after construction
                // 100% thread-safe — no mutable state
            }
        }

        // Registration:
        // builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>();

        // ----------------------------------------------------------
        // SINGLETON EXAMPLE 3 — HttpClient / External API Client
        // ----------------------------------------------------------
        // WHY SINGLETON?
        //   HttpClient is designed to be reused across requests.
        //   Creating a new HttpClient per request causes socket exhaustion —
        //   too many TCP connections opened and not properly closed.
        //   DNS changes are handled by IHttpClientFactory internally.
        //   Connection pooling works correctly with long-lived HttpClient.

        public interface IPaymentGatewayClient { }
        public class PaymentResult { }

        public class PaymentGatewayClient : IPaymentGatewayClient
        {
            private readonly HttpClient _httpClient;
            private readonly IAppSettingsService _settings;
            private readonly ILogger<PaymentGatewayClient> _logger;

            public PaymentGatewayClient(
                HttpClient httpClient, // injected by IHttpClientFactory
                IAppSettingsService settings,
                ILogger<PaymentGatewayClient> logger)
            {
                _httpClient = httpClient;
                _settings = settings;
                _logger = logger;
                _httpClient.BaseAddress = new Uri("https://api.paymentgateway.com");
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", settings.SendGridApiKey);
            }

            public async Task<PaymentResult> ChargeAsync(decimal amount, string cardToken)
            {
                var payload = new { amount, cardToken };
                var response = await _httpClient.PostAsJsonAsync("/v1/charge", payload);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Payment failed with status {Status}", response.StatusCode);
                    return PaymentResult.Failed("Payment gateway returned an error");
                }

                return await response.Content.ReadFromJsonAsync<PaymentResult>();
            }
        }

        // Registration using typed HttpClient (manages lifetime correctly):
        // builder.Services.AddHttpClient<IPaymentGatewayClient, PaymentGatewayClient>();


        // ----------------------------------------------------------
        // SINGLETON EXAMPLE 4 — Request Counter (intentional shared state)
        // ----------------------------------------------------------

        // WHY SINGLETON?
        //   Counting total requests across the application lifetime.
        //   State is INTENTIONALLY shared — you want a global counter.
        //   Uses Interlocked for thread-safe increment without locks.

        public interface IRequestMetricsService { }
        public class MetricsSummary
        {
            public long TotalRequests { get; set; }
            public long FailedRequests { get; set; }
            public long AverageResponseTimeMs { get; set; }
        }

        public class RequestMetricsService : IRequestMetricsService
        {
            // These fields are shared across ALL requests — intentional
            private long _totalRequests = 0;
            private long _failedRequests = 0;
            private long _totalResponseTimeMs = 0;

            // Interlocked ensures atomic operations — no race conditions
            public void RecordRequest() => Interlocked.Increment(ref _totalRequests);
            public void RecordFailure() => Interlocked.Increment(ref _failedRequests);

            public void RecordResponseTime(long ms)
                => Interlocked.Add(ref _totalResponseTimeMs, ms);

            public MetricsSummary GetSummary() => new()
            {
                TotalRequests = Interlocked.Read(ref _totalRequests),
                FailedRequests = Interlocked.Read(ref _failedRequests),
                AverageResponseTimeMs = _totalRequests > 0
                    ? _totalResponseTimeMs / _totalRequests : 0
            };
        }

        // ============================================================
        // SCOPED EXAMPLES
        // ============================================================
        //
        // Rule: ONE instance per HTTP REQUEST
        // Use when:
        //   - Service holds per-request state (current user, request ID)
        //   - Multiple services in same request must share the same instance
        //   - Service needs to be fresh per request (DbContext)
        //   - Service aggregates work across a request (Unit of Work)

        // ----------------------------------------------------------
        // SCOPED EXAMPLE 1 — DbContext (most classic example)
        // ----------------------------------------------------------
        // WHY SCOPED?
        //   All DB operations in one request must share the same DbContext
        //   so they share the same change tracker and can be committed
        //   in a single atomic transaction.
        //
        //   If SINGLETON: same DbContext across all requests forever
        //     - Stale data (entities tracked from old requests)
        //     - Thread safety issues (DbContext is NOT thread-safe)
        //     - Memory leaks (change tracker grows forever)
        //     - Connection held open forever
        //
        //   If TRANSIENT: new DbContext for every injection
        //     - OrderService has DbContext A, InventoryService has DbContext B
        //     - Cannot commit both in one transaction
        //     - Change tracking is isolated — services cannot see each other's changes


        // ----------------------------------------------------------
        // SCOPED EXAMPLE 2 — Current User Service
        // ----------------------------------------------------------
        // WHY SCOPED?
        //   Reads the authenticated user from JWT once per request.
        //   Caches the result so multiple services in the same request
        //   don't all hit the database to fetch the same user.
        //   Must be Scoped so the cache resets for each new request.
        //
        //   If SINGLETON: _currentUser cached forever after first request
        //     - Every subsequent request thinks it is the first user who logged in
        //     - Catastrophic security bug
        //
        //   If TRANSIENT: each service gets its own CurrentUserService
        //     - OrderService fetches user from DB
        //     - InventoryService fetches the same user from DB again
        //     - Wasteful — multiple DB hits for the same data in one request

        public interface ICurrentUserService { }

        public class UserProfile
        {
            public string Id { get; set; } = default!;
            public string Email { get; set; } = default!;
            public string FullName { get; set; } = default!;
            public string[] Roles { get; set; } = Array.Empty<string>();
        }

        public class CurrentUserService : ICurrentUserService
        {
            private readonly IHttpContextAccessor _httpContextAccessor;
            private readonly AppDbContext _db;

            // Lazy cache — fetched from DB once per request, then reused
            private UserProfile? _cachedUser;
            private bool _fetched = false;

            public CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext db)
            {
                _httpContextAccessor = httpContextAccessor;
                _db = db;
            }

            public async Task<UserProfile?> GetCurrentUserAsync()
            {
                // Already fetched in this request — return cached value
                if (_fetched) return _cachedUser;

                var userId = _httpContextAccessor.HttpContext?
                    .User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userId == null)
                {
                    _fetched = true;
                    return null;
                }

                // Fetch from DB only ONCE per request
                _cachedUser = await _db.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new UserProfile
                    {
                        Id = u.Id,
                        Email = u.Email,
                        FullName = u.FullName,
                        Roles = u.Roles
                    })
                    .FirstOrDefaultAsync();

                _fetched = true;
                return _cachedUser;

                // Multiple services calling GetCurrentUserAsync in same request
                // → only ONE DB hit → _cachedUser returned for subsequent calls
                // New request → new CurrentUserService instance → _cachedUser = null → fresh fetch
            }

            public string? GetCurrentUserId()
                => _httpContextAccessor.HttpContext?
                    .User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            public bool IsInRole(string role)
                => _httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
        }

        // Registration:
        // builder.Services.AddHttpContextAccessor(); // needed for IHttpContextAccessor
        // builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        // ----------------------------------------------------------
        // SCOPED EXAMPLE 3 — Unit of Work
        // ----------------------------------------------------------
        // WHY SCOPED?
        //   Coordinates multiple repository operations in one transaction.
        //   All repositories in the same request must share the same
        //   Unit of Work so they all commit/rollback together.
        //   Resets for each new request — each request is its own unit of work.

        public class UnitOfWork : IUnitOfWork
        {
            private readonly AppDbContext _db;
            private IDbContextTransaction? _transaction;

            public UnitOfWork(AppDbContext db) => _db = db;

            public IOrderRepository Orders => new OrderRepository(_db);
            public IProductRepository Products => new ProductRepository(_db);
            public ICustomerRepository Customers => new CustomerRepository(_db);

            public async Task BeginTransactionAsync()
            {
                _transaction = await _db.Database.BeginTransactionAsync();
            }

            public async Task CommitAsync()
            {
                await _db.SaveChangesAsync();
                if (_transaction != null)
                    await _transaction.CommitAsync();
            }

            public async Task RollbackAsync()
            {
                if (_transaction != null)
                    await _transaction.RollbackAsync();
            }

            public void Dispose()
            {
                _transaction?.Dispose();
                _db.Dispose();
            }
        }

        // Registration:
        // builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();


        // ----------------------------------------------------------
        // SCOPED EXAMPLE 4 — Request Correlation / Audit Context
        // ----------------------------------------------------------
        // WHY SCOPED?
        //   Each request gets a unique correlation ID for distributed tracing.
        //   All logging/auditing in the same request uses the same correlation ID.
        //   Resets for each new request — each request has its own trace.

        public interface IRequestContext { }
        public class RequestContext : IRequestContext
        {
            // Set once at the start of the request
            // Shared across ALL services in the same request
            public string CorrelationId { get; } = Guid.NewGuid().ToString();
            public DateTime RequestStartedAt { get; } = DateTime.UtcNow;
            public string? UserId { get; set; }
            public string? IpAddress { get; set; }

            // All services injecting IRequestContext get the SAME instance
            // So they all log with the SAME CorrelationId
            // Making it easy to trace a single request across many log entries
        }

        // Registration:
        // builder.Services.AddScoped<IRequestContext, RequestContext>();

        // Middleware that initializes it at the start of each request:
        public class RequestContextMiddleware
        {
            private readonly RequestDelegate _next;

            public RequestContextMiddleware(RequestDelegate next) => _next = next;

            public async Task InvokeAsync(HttpContext context, IRequestContext requestContext)
            {
                // IRequestContext is injected via InvokeAsync — fresh Scoped instance
                requestContext.IpAddress = context.Connection.RemoteIpAddress?.ToString();
                requestContext.UserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Add correlation ID to response headers for client-side tracing
                context.Response.Headers["X-Correlation-Id"] = requestContext.CorrelationId;

                await _next(context);
            }
        }

        // ============================================================
        // TRANSIENT EXAMPLES
        // ============================================================
        //
        // Rule: NEW instance EVERY TIME resolved
        // Use when:
        //   - Service is stateless (holds no state between calls)
        //   - Service is very cheap to create
        //   - Each caller must have complete isolation from other callers
        //   - Sharing an instance would cause bugs

        // ----------------------------------------------------------
        // TRANSIENT EXAMPLE 1 — Validator
        // ----------------------------------------------------------
        // WHY TRANSIENT?
        //   Each validation call must be completely independent.
        //   No state should leak between different objects being validated.
        //   Cheap to create — just method calls, no resources.
        //   Callers need isolation — validating Order A should not
        //   affect validating Order B happening in the same request.

        public class CreateOrderRequestValidator : IValidator<CreateOrderRequest>
        {
            // No constructor dependencies — pure logic
            // No shared state — each instance is a clean slate

            public ValidationResult Validate(CreateOrderRequest request)
            {
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(request.CustomerEmail))
                    errors.Add("Customer email is required");

                if (!request.CustomerEmail.Contains('@'))
                    errors.Add("Customer email is not valid");

                if (request.Items == null || !request.Items.Any())
                    errors.Add("Order must contain at least one item");

                if (request.Items?.Any(i => i.Quantity <= 0) == true)
                    errors.Add("All item quantities must be greater than zero");

                if (request.Items?.Any(i => i.UnitPrice <= 0) == true)
                    errors.Add("All item prices must be greater than zero");

                return errors.Any()
                    ? ValidationResult.Failure(errors)
                    : ValidationResult.Success();
            }
        }

        // Registration:
        // builder.Services.AddTransient<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();

        // ----------------------------------------------------------
        // TRANSIENT EXAMPLE 2 — Email Builder (stateful per use, not shared)
        // ----------------------------------------------------------
        // WHY TRANSIENT?
        //   Each email being built is completely independent.
        //   If two services build different emails at the same time
        //   and share the same EmailBuilder instance, they would
        //   overwrite each other's subject, body, recipients.
        //   Each caller MUST have their own isolated instance.

        public interface IEmailBuilder { }
        public class EmailMessage
        {
            public string To { get; set; } = default!;
            public string Subject { get; set; } = default!;
            public string Body { get; set; } = default!;
            public List<string> Cc { get; set; } = new();
            public List<string> Attachments { get; set; } = new();
        }
        public class EmailBuilder : IEmailBuilder
        {
            // Mutable state — each instance builds ONE email independently
            private string _to = string.Empty;
            private string _subject = string.Empty;
            private string _body = string.Empty;
            private readonly List<string> _cc = new();
            private readonly List<string> _attachments = new();

            // Fluent API — each method returns 'this' for chaining
            public IEmailBuilder To(string email) { _to = email; return this; }
            public IEmailBuilder Subject(string subject) { _subject = subject; return this; }
            public IEmailBuilder Body(string body) { _body = body; return this; }
            public IEmailBuilder Cc(string email) { _cc.Add(email); return this; }
            public IEmailBuilder AttachFile(string path) { _attachments.Add(path); return this; }

            public EmailMessage Build()
            {
                if (string.IsNullOrEmpty(_to))
                    throw new InvalidOperationException("Recipient is required");

                return new EmailMessage
                {
                    To = _to,
                    Subject = _subject,
                    Body = _body,
                    Cc = _cc.ToList(),
                    Attachments = _attachments.ToList()
                };
            }
        }

        // Registration:
        // builder.Services.AddTransient<IEmailBuilder, EmailBuilder>();

        // ============================================================
        // CAPTIVE DEPENDENCY PROBLEM — IN DETAIL
        // ============================================================
        //
        // DEFINITION:
        //   A captive dependency is when a LONGER-LIVED service captures a
        //   SHORTER-LIVED service in its constructor, trapping it for
        //   longer than it was designed to live.
        //
        // ANALOGY:
        //   Imagine hiring a temporary worker (Scoped/Transient) for one day.
        //   But instead of letting them go at the end of the day,
        //   you lock them in the office permanently (Singleton captures them).
        //   Now your "temporary" worker is permanent, exhausted, and shared
        //   with everyone — causing chaos.

        // ============================================================
        // CAPTIVE DEPENDENCY CASE 1 — Singleton capturing Scoped
        // ============================================================
        //
        // THE MOST DANGEROUS AND COMMON MISTAKE
        // Most common real-world case: Singleton service injecting DbContext

        public class WrongReportService_Singleton_Captures_Scoped
        {
            private readonly AppDbContext _db; // DbContext is Scoped — WRONG here

            public WrongReportService_Singleton_Captures_Scoped(AppDbContext db)
            {
                _db = db;

                // WHAT ACTUALLY HAPPENS:
                //
                // Step 1: App starts. ReportService is Singleton.
                //         First time ReportService is needed, DI creates it.
                //         DI also creates a DbContext (Scoped) to inject.
                //         This DbContext was created OUTSIDE of any real request scope.
                //         It is stored in _db FOREVER inside the Singleton.
                //
                // Step 2: Request 1 comes in.
                //         ReportService is reused (Singleton).
                //         _db is the SAME DbContext from Step 1.
                //         Request 1 fetches orders, changes are tracked in _db.
                //
                // Step 3: Request 2 comes in simultaneously (different user).
                //         ReportService is reused again (Singleton).
                //         _db is STILL the SAME DbContext.
                //         Request 2 fetches products, changes tracked in SAME _db.
                //         Now _db has both Request 1 AND Request 2 changes mixed together.
                //
                // CONSEQUENCES:
                //   1. DATA CORRUPTION:
                //      DbContext is NOT thread-safe. Two threads writing to the
                //      same DbContext simultaneously causes internal state corruption.
                //      Exception: "A second operation started on this context before
                //      a previous operation completed."
                //
                //   2. STALE DATA:
                //      DbContext has a first-level cache (identity map).
                //      After Request 1 loads a product, _db caches it internally.
                //      Request 2 loads the same product — gets the CACHED version
                //      from Request 1, even if the DB has newer data.
                //      User sees outdated data.
                //
                //   3. MEMORY LEAK:
                //      DbContext's change tracker accumulates every entity ever
                //      loaded or modified. Since this DbContext is never disposed,
                //      the change tracker grows indefinitely → memory leak.
                //
                //   4. CONNECTION POOL STARVATION:
                //      DbContext holds onto a database connection.
                //      Since it is never disposed, the connection is never returned
                //      to the connection pool. Other requests cannot get a connection.
                //      App becomes unresponsive under load.
            }

            public async Task<List<Order>> GetRecentOrdersAsync()
            {
                return await _db.Orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .ToListAsync();
                // This might throw: "A second operation started on this context"
                // Or return stale data cached from a previous request
            }
        }

        // ASP.NET Core DETECTS this at startup in Development:
        // InvalidOperationException:
        // "Cannot consume scoped service 'AppDbContext' from singleton
        //  'WrongReportService_Singleton_Captures_Scoped'"
        // This validation saves you in development. It will NOT throw in Production
        // if scope validation is disabled — so the bug would silently corrupt data.

        // THE FIX — Use IServiceScopeFactory to create a fresh scope per operation
        public class CorrectReportService
        {
            private readonly IServiceScopeFactory _scopeFactory;
            // IServiceScopeFactory is itself Singleton — safe to inject

            public CorrectReportService(IServiceScopeFactory scopeFactory)
                => _scopeFactory = scopeFactory;

            public async Task<List<Order>> GetRecentOrdersAsync()
            {
                // Create a fresh scope just for this operation
                using var scope = _scopeFactory.CreateScope();

                // Resolve a fresh DbContext from the new scope
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // db is a brand new DbContext — no stale state, not shared
                var orders = await db.Orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                return orders;
            }
            // using block ends → scope disposed → db disposed → connection returned to pool
            // Next call to GetRecentOrdersAsync creates a FRESH scope and FRESH db
            // No shared state, no thread safety issues, no memory leaks
        }

        // Registration:
        // builder.Services.AddSingleton<CorrectReportService>();

        // ============================================================
        // CAPTIVE DEPENDENCY CASE 2 — Singleton capturing Transient
        // ============================================================
        //
        // The Transient service loses its "new every time" nature.
        // It effectively becomes Singleton because the Singleton holds
        // a permanent reference to it.

        public class TransientEmailBuilder_WhenCapturedBySingleton : IEmailBuilder
        {
            // Mutable state — intended to be fresh for each use
            private string _to = string.Empty;
            private string _subject = string.Empty;

            public IEmailBuilder To(string email) { _to = email; return this; }
            public IEmailBuilder Subject(string subject) { _subject = subject; return this; }
            public EmailMessage Build() => new() { To = _to, Subject = _subject };
        }

        // WRONG — Singleton capturing Transient
        public class WrongNotificationService_Singleton_Captures_Transient
        {
            private readonly IEmailBuilder _emailBuilder; // Transient captured in Singleton

            public WrongNotificationService_Singleton_Captures_Transient(IEmailBuilder emailBuilder)
            {
                _emailBuilder = emailBuilder;

                // WHAT ACTUALLY HAPPENS:
                //
                // IEmailBuilder is registered as Transient.
                // You expect a new instance every time.
                // But this Singleton was created ONCE, and _emailBuilder was
                // injected ONCE at that moment.
                //
                // Now ALL requests share the SAME _emailBuilder instance.
                // The Transient lifetime is completely ignored.
                //
                // CONSEQUENCES:
                //   Request 1 calls: _emailBuilder.To("user1@email.com").Subject("Welcome")
                //   Request 2 calls: _emailBuilder.To("user2@email.com").Subject("Invoice")
                //   Request 1 builds the email — gets "user2@email.com" and "Invoice"
                //   because Request 2 overwrote the state!
                //   User 1 gets User 2's email content.
                //   User 2 gets User 1's email content.
                //   This is a SECURITY and DATA INTEGRITY bug.
            }
        }

        // THE FIX — Resolve Transient inside the method using IServiceProvider
        public class CorrectNotificationService
        {
            private readonly IServiceScopeFactory _scopeFactory;

            public CorrectNotificationService(IServiceScopeFactory scopeFactory)
                => _scopeFactory = scopeFactory;

            public async Task SendWelcomeEmailAsync(string userEmail)
            {
                using var scope = _scopeFactory.CreateScope();

                // Fresh IEmailBuilder for this specific email
                var emailBuilder = scope.ServiceProvider.GetRequiredService<IEmailBuilder>();

                var email = emailBuilder
                    .To(userEmail)
                    .Subject("Welcome to our platform!")
                    .Body("Thank you for signing up.")
                    .Build();

                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                await emailSender.SendAsync(email);
            }
            // Each call gets a fresh IEmailBuilder — Transient behavior restored
        }

        // ============================================================
        // CAPTIVE DEPENDENCY CASE 3 — Scoped capturing Transient
        // ============================================================
        //
        // Less dangerous than Singleton capturing Scoped,
        // but still breaks the Transient promise.
        // The Transient service becomes effectively Scoped —
        // same instance for the entire request.

        public interface IOperationLogger { }
        public class TransientOperationLogger : IOperationLogger
        {
            // Transient — intended to be a fresh logger for each operation
            public Guid OperationId { get; } = Guid.NewGuid();

            public void Log(string message)
                => Console.WriteLine($"[{OperationId}] {message}");
        }

        // WRONG — Scoped capturing Transient
        public class WrongOrderProcessor_Scoped_Captures_Transient
        {
            private readonly IOperationLogger _logger; // Transient captured in Scoped

            public WrongOrderProcessor_Scoped_Captures_Transient(IOperationLogger logger)
            {
                _logger = logger;

                // WHAT ACTUALLY HAPPENS:
                // IOperationLogger is Transient — should be new for every resolution.
                // But WrongOrderProcessor is Scoped — created once per request.
                // _logger was injected once when WrongOrderProcessor was created.
                //
                // Now ALL operations within this request share the SAME logger instance.
                // OperationId is the same for ALL operations in the request.
                // You cannot distinguish individual operations by their OperationId.
                //
                // This is a MINOR bug — at least it resets per request.
                // But if you intended each operation to have its own unique ID,
                // that intention is broken.
            }
        }

        // THE FIX — Inject IServiceProvider and resolve Transient per operation
        public class CorrectOrderProcessor
        {
            private readonly IServiceProvider _serviceProvider;

            public CorrectOrderProcessor(IServiceProvider serviceProvider)
                => _serviceProvider = serviceProvider;

            public async Task ProcessOrderAsync(Order order)
            {
                // Fresh logger for THIS specific operation
                var logger = _serviceProvider.GetRequiredService<IOperationLogger>();
                logger.Log($"Starting order {order.Id}"); // unique OperationId

                await ProcessPaymentAsync(order, logger);
                await UpdateInventoryAsync(order, logger);
            }

            private async Task ProcessPaymentAsync(Order order, IOperationLogger logger)
            {
                logger.Log("Processing payment"); // same OperationId as above — correct
            }

            private async Task UpdateInventoryAsync(Order order, IOperationLogger logger)
            {
                logger.Log("Updating inventory"); // same OperationId — correct
            }
        }

        // Can IServiceScopeFactory always be used to safely use lower lifetime services in higher lifetime services?
        // Yes, always.That is exactly its purpose.
        // IServiceScopeFactory is itself registered as Singleton by the framework.It is always safe to inject into any lifetime.
        // When you call _scopeFactory.CreateScope(), it creates a completely independent child DI scope.Any service resolved from that scope gets a fresh instance with the correct lifetime behavior restored.

        // You should never register HttpClient directly with any scope because Singleton causes DNS staleness and Scoped/Transient causes socket exhaustion.
        // Always use builder.Services.AddHttpClient() which internally separates the lightweight HttpClient wrapper (Transient — fresh per use) from the HttpMessageHandler connection pool (long-lived — reused, rotated every 2 minutes for DNS refresh).
        // The typed client approach is the cleanest — it hides IHttpClientFactory entirely and lets you inject your own strongly typed client directly.
    }
}
