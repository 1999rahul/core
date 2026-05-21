using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static ASPNETCONCEPTS.DEPENDENCY_INJECTION;
using static System.Net.Mime.MediaTypeNames;

namespace ASPNETCONCEPTS
{
    public class DEPENDENCY_INJECTION
    {
        // ============================================================
        //   DEPENDENCY INJECTION — LIFETIMES
        //   Singleton, Scoped, Transient
        //   ASP.NET Core — Interview Notes (Detailed)
        // ============================================================

        // ============================================================
        // WHAT IS DEPENDENCY INJECTION AND WHY IT EXISTS
        // ============================================================
        //
        // Without DI — classes create their own dependencies (tightly coupled)
        // Hard to test, hard to swap implementations, hard to maintain

        public class EmailService { }
        public class AppDbContext { }

        public class OrderServiceWithoutDI
        {
            private readonly EmailService _emailService;
            private readonly AppDbContext _db;

            public OrderServiceWithoutDI()
            {
                _emailService = new EmailService();   // tightly coupled to EmailService
                _db = new AppDbContext();             // tightly coupled to AppDbContext
                                                      // Problems:
                                                      //   1. Cannot swap EmailService for FakeEmailService in tests
                                                      //   2. Cannot change implementation without modifying this class
                                                      //   3. This class controls the lifetime of its dependencies
                                                      //   4. Cannot share the same DbContext instance across services
            }
        }

        public interface IEmailService { }

        // With DI — dependencies are injected from outside (loosely coupled)
        public class OrderServiceWithDI
        {
            private readonly IEmailService _emailService;  // depends on abstraction
            private readonly AppDbContext _db;

            // Dependencies come IN — this class does not create them
            public OrderServiceWithDI(IEmailService emailService, AppDbContext db)
            {
                _emailService = emailService;
                _db = db;
                // Benefits:
                //   1. Inject FakeEmailService in tests — no real emails sent
                //   2. Swap SmtpEmailService for SendGridEmailService without touching this class
                //   3. DI container controls lifetime — shared DbContext within a request
                //   4. Clean, testable, maintainable code
            }
        }

        // The DI container is essentially a dictionary:
        //   IEmailService  →  SmtpEmailService  (create how? Singleton/Scoped/Transient)
        //   AppDbContext   →  AppDbContext       (Scoped)
        //   ICacheService  →  MemoryCacheService (Singleton)
        // When something needs IEmailService, the container creates/returns one
        // based on the registered lifetime

        // ============================================================
        // REGISTERING SERVICES — BASIC SYNTAX
        // ============================================================

        public void RegisterServices()
        {
            var builder = WebApplication.CreateBuilder();

            // Three ways to register each lifetime:

            // 1. Interface → Implementation (most common)
            // builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
            // builder.Services.AddScoped<IOrderService, OrderService>();
            // builder.Services.AddTransient<IEmailSender, SmtpEmailSender>()

            // 2. Concrete type only (no interface)
            builder.Services.AddScoped<AppDbContext>();

            // 3. Factory — when creation needs custom logic
            //builder.Services.AddSingleton<IConfigService>(provider =>
            //{
            //    var config = provider.GetRequiredService<IConfiguration>();
            //    return new ConfigService(config["ApiKey"]);
            //});

            // 4. Pre-created instance (always Singleton by nature)
            //builder.Services.AddSingleton<IAppSettings>(new AppSettings
            //{
            //    MaxRetries = 3,
            //    TimeoutSeconds = 30
            //});


            // ============================================================
            // LIFETIME 1 — SINGLETON
            // ============================================================
            //
            // One single instance is created the FIRST TIME it is requested.
            // That same instance is reused for EVERY request from EVERY user
            // for the ENTIRE lifetime of the application.
            // Disposed only when the application shuts down.
            //
            // Timeline:
            //   App starts
            //       │
            //       ├── Request 1: User A → creates Instance A
            //       │                       returns Instance A
            //       ├── Request 2: User B → returns Instance A  (same!)
            //       ├── Request 3: User C → returns Instance A  (same!)
            //       │
            //   App shuts down → Instance A disposed

            // builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
            // builder.Services.AddSingleton<IConfiguration>(_ => configuration);
            // builder.Services.AddSingleton<IHttpClientFactory, HttpClientFactory>();

            // RULE: Singleton services MUST be thread-safe
            // They are shared across concurrent requests on multiple threads
            // Any shared mutable state must be protected with locks or
            // use thread-safe collections like ConcurrentDictionary

            // ============================================================
            // LIFETIME 2 — SCOPED
            // ============================================================
            //
            // One instance is created PER HTTP REQUEST.
            // Every class that resolves this service within the SAME request
            // gets the EXACT same instance.
            // When the request ends, the instance is disposed.
            //
            // Timeline:
            //   Request 1 starts → creates Instance B
            //       ├── OrderService    gets Instance B
            //       ├── InventoryService gets Instance B  (same!)
            //       └── CheckoutController gets Instance B  (same!)
            //   Request 1 ends → Instance B disposed
            //
            //   Request 2 starts → creates Instance C (brand new)
            //       ├── OrderService    gets Instance C
            //       └── ...
            //   Request 2 ends → Instance C disposed
            //
            //   Request 1 and 2 NEVER share the same instance

            //builder.Services.AddScoped<AppDbContext>();
            //builder.Services.AddScoped<IOrderService, OrderService>();
            //builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            //builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // RULE: Scoped services do NOT need to be thread-safe
            // Each request runs on its own scope with its own instance
            // Two concurrent requests never share a Scoped instance

            // ============================================================
            // LIFETIME 3 — TRANSIENT
            // ============================================================
            //
            // A BRAND NEW instance is created EVERY TIME the service is resolved.
            // If three classes in the same request all inject a Transient service,
            // each gets a completely separate, independent instance.
            //
            // Timeline:
            //   Request 1:
            //       OrderService    resolves IEmailSender → Instance X (new)
            //       PaymentService  resolves IEmailSender → Instance Y (new, different!)
            //       NotifyService   resolves IEmailSender → Instance Z (new, different!)
            //
            //   Request 2:
            //       OrderService    resolves IEmailSender → Instance P (new)
            //       (and so on...)
            //
            //   Every resolution = new instance. Always.

            //builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
            //builder.Services.AddTransient<IValidator<CreateOrderRequest>, OrderValidator>();
            //builder.Services.AddTransient<IGuidGenerator, GuidGenerator>();
            //builder.Services.AddTransient<IPasswordHasher, PasswordHasher>();


            // WARNING — Transient + IDisposable
            // DI container TRACKS disposable Transient services
            // and disposes them when the SCOPE (request) ends
            // They stay in memory for the ENTIRE request even if done early

            // builder.Services.AddTransient<DatabaseConnection>();
            // If 10 classes in one request inject this:
            //   → 10 open SQL connections at the same time
            //   → all stay open until request ends
            //   → Use Scoped instead — one connection shared within the request
        }

        // ============================================================
        // SIDE BY SIDE — SAME REQUEST, SAME SERVICE, DIFFERENT LIFETIMES
        // ============================================================

        // Register three versions:
        // builder.Services.AddSingleton<SingletonDemo>();
        // builder.Services.AddScoped<ScopedDemo>();
        // builder.Services.AddTransient<TransientDemo>();

        [ApiController]
        [Route("api/lifetime")]
        public class LifetimeDemoController : ControllerBase
        {
            private readonly SingletonDemo _singleton1;
            private readonly SingletonDemo _singleton2;
            private readonly ScopedDemo _scoped1;
            private readonly ScopedDemo _scoped2;
            private readonly TransientDemo _transient1;
            private readonly TransientDemo _transient2;

            public LifetimeDemoController(
                SingletonDemo singleton1, SingletonDemo singleton2,
                ScopedDemo scoped1, ScopedDemo scoped2,
                TransientDemo transient1, TransientDemo transient2)
            {
                _singleton1 = singleton1; _singleton2 = singleton2;
                _scoped1 = scoped1; _scoped2 = scoped2;
                _transient1 = transient1; _transient2 = transient2;
            }

            [HttpGet]
            public IActionResult Get()
            {
                return Ok(new
                {
                    // Both singleton IDs are the same (always — across all requests)
                    Singleton1 = _singleton1.InstanceId,  // e.g. 1
                    Singleton2 = _singleton2.InstanceId,  // e.g. 1 (same!)

                    // Both scoped IDs are the same (same request)
                    // But different from another request's scoped IDs
                    Scoped1 = _scoped1.InstanceId,        // e.g. 2
                    Scoped2 = _scoped2.InstanceId,         // e.g. 2 (same!)

                    // Transient IDs are ALWAYS different
                    Transient1 = _transient1.InstanceId,  // e.g. 3
                    Transient2 = _transient2.InstanceId   // e.g. 4 (different!)
                });
            }
        }

        // Request 1 result: { Singleton1:1, Singleton2:1, Scoped1:2, Scoped2:2, Transient1:3, Transient2:4 }
        // Request 2 result: { Singleton1:1, Singleton2:1, Scoped1:5, Scoped2:5, Transient1:6, Transient2:7 }
        //                                   ^^^^ same      ^^^^ new               ^^^^ always new


        // ============================================================
        // THE CAPTIVE DEPENDENCY PROBLEM — MOST IMPORTANT INTERVIEW TOPIC
        // ============================================================
        //
        // A captive dependency is when a LONGER-LIVED service holds a reference
        // to a SHORTER-LIVED service, trapping it for longer than intended.
        //
        // The rule: you can inject EQUAL or LONGER-lived services into a service.
        //           You CANNOT safely inject SHORTER-lived services into constructor.
        //

        // Safe combinations:
        //   Singleton  ← Singleton   
        //   Scoped     ← Singleton    (Scoped can use Singleton)
        //   Scoped     ← Scoped      
        //   Transient  ← Singleton    (Transient can use Singleton)
        //   Transient  ← Scoped       (Transient can use Scoped — it lives shorter)
        //   Transient  ← Transient
        //   
        // DANGEROUS combinations (captive dependency):
        //   Singleton  ← Scoped       Scoped captured forever
        //   Singleton  ← Transient    Transient captured forever (becomes Singleton)
        //   Scoped     ← Transient    Transient captured for request duration (minor issue

        // ============================================================
        // RESOLVING SERVICES MANUALLY
        // ============================================================
        //
        // Sometimes constructor injection is not possible.
        // You can resolve services manually from IServiceProvider.

        // From root provider (app.Services) — Singleton services ONLY
        // var app = builder.Build();
        // var cache = app.Services.GetRequiredService<ICacheService>();// Singleton — OK
        // var db = app.Services.GetRequiredService<AppDbContext>(); // Scoped — THROWS

        // Creating a manual scope for Scoped services
        //using (var scope = app.Services.CreateScope())
        //{
        //    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        //    // db is properly scoped
        //    await db.Database.MigrateAsync(); // run migrations at startup
        //} // scope disposed here → db disposed

        // Inside middleware — use context.RequestServices (per-request scope)
        // app.Use(async (context, next) =>
        //{
        //    // context.RequestServices IS the request scope
        //    // Safe to resolve any lifetime from here
        //    var db = context.RequestServices.GetRequiredService<AppDbContext>();
        //        var cache = context.RequestServices.GetRequiredService<ICacheService>();
        //        await next(context);
        //    });


        // ============================================================
        // REGISTERING MULTIPLE IMPLEMENTATIONS OF SAME INTERFACE
        // ============================================================

        //builder.Services.AddScoped<INotificationService, EmailNotificationService>();
        //builder.Services.AddScoped<INotificationService, SmsNotificationService>();
        //builder.Services.AddScoped<INotificationService, PushNotificationService>();

        // Inject ALL implementations via IEnumerable
        public class NotificationDispatcher
        {
            private readonly IEnumerable<INotificationService> _services;

            // DI injects ALL registered implementations of INotificationService
            public NotificationDispatcher(IEnumerable<INotificationService> services)
                => _services = services;

            public async Task NotifyAllAsync(string message)
            {
                // Sends via Email, SMS, and Push simultaneously
                var tasks = _services.Select(s => s.SendAsync(message));
                await Task.WhenAll(tasks);
            }
        }

        // IMPORTANT:
        //   GetRequiredService<INotificationService>() → returns the LAST registered one
        //   GetRequiredService<IEnumerable<INotificationService>>() → returns ALL of them

        // ============================================================
        // DI IN BACKGROUND SERVICES — IHostedService
        // ============================================================
        //
        // Background services (IHostedService) are Singleton by nature.
        // They cannot inject Scoped services in constructor.
        // Must use IServiceScopeFactory.

        public class OrderProcessingBackgroundService : BackgroundService
        {
            private readonly IServiceScopeFactory _scopeFactory;
            private readonly ILogger<OrderProcessingBackgroundService> _logger;

            // Only Singleton services in constructor
            public OrderProcessingBackgroundService(
                IServiceScopeFactory scopeFactory,
                ILogger<OrderProcessingBackgroundService> logger)
            {
                _scopeFactory = scopeFactory;
                _logger = logger;
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await ProcessPendingOrdersAsync();
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            private async Task ProcessPendingOrdersAsync()
            {
                // Create a fresh scope for each processing cycle
                using var scope = _scopeFactory.CreateScope();

                // Now safely resolve Scoped services
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

                var pendingOrders = await db.Orders
                    .Where(o => o.Status == OrderStatus.Pending)
                    .ToListAsync();

                foreach (var order in pendingOrders)
                {
                    await orderService.ProcessAsync(order);
                }

                await db.SaveChangesAsync();
            }
            // scope disposed → db and orderService disposed → connection returned to pool
        }


    }
}
