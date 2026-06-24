// ============================================================
//  BUILDER PATTERN — COMPLETE INTERVIEW GUIDE
//  Covers:
//    1. What is the Builder pattern & its components
//    2. Where to write validation logic
//    3. Why use an Interface for the builder
//    4. How ASP.NET Core uses the Builder pattern in Program.cs
//    5. What happens after builder.Build() — pipeline vs DI
//    6. What app.Run() actually does
// ============================================================


// ─────────────────────────────────────────────────────────────
// SECTION 1: THE PRODUCT
// ─────────────────────────────────────────────────────────────
// The complex object we want to build.
// - Constructor is PRIVATE → only the builder can create it.
// - All properties are read-only → the object is IMMUTABLE once built.
// ─────────────────────────────────────────────────────────────

public class Pizza
{
    public string Size { get; }
    public string Crust { get; }
    public IReadOnlyList<string> Toppings { get; }

    // OPTION C VALIDATION: last line of defence inside the product.
    // Even if someone bypasses the builder, this object cannot be
    // created in an invalid state.
    private Pizza(string size, string crust, IReadOnlyList<string> toppings)
    {
        Size = size ?? throw new ArgumentNullException(nameof(size));
        Crust = crust ?? throw new ArgumentNullException(nameof(crust));
        Toppings = toppings;
    }

    public override string ToString() =>
        $"{Size} pizza | Crust: {Crust} | Toppings: {string.Join(", ", Toppings)}";


    // ─────────────────────────────────────────────────────────
    // SECTION 2: THE BUILDER INTERFACE
    // ─────────────────────────────────────────────────────────
    // WHY AN INTERFACE?
    //   - Allows multiple builder implementations (prod, test, etc.)
    //   - Director depends on the abstraction, not a concrete class
    //   - Enables mocking in unit tests (e.g. Moq)
    //   - Follows Open/Closed Principle — add new builders without
    //     changing the Director or any existing code
    // ─────────────────────────────────────────────────────────

    public interface IPizzaBuilder
    {
        IPizzaBuilder WithCrust(string crust);
        IPizzaBuilder WithTopping(string topping);
        Pizza Build();
    }


    // ─────────────────────────────────────────────────────────
    // SECTION 3: CONCRETE BUILDER #1 — Production
    // ─────────────────────────────────────────────────────────
    // WHERE TO WRITE VALIDATION LOGIC:
    //
    //   OPTION A — Build() method (recommended for cross-field rules)
    //     Use when: fields depend on each other, or final completeness
    //     checks are needed before the object is created.
    //
    //   OPTION B — With...() setter methods (eager / per-field)
    //     Use when: a single field's value is invalid on its own,
    //     regardless of other fields. Fails fast with a clear message.
    //
    //   OPTION C — Product constructor (defensive guard)
    //     Use when: you want the product itself to be self-protecting,
    //     even if someone creates it outside the builder.
    // ─────────────────────────────────────────────────────────

    public class PizzaBuilder : IPizzaBuilder
    {
        private string _size;
        private string _crust = "thin";                  // sensible default
        private readonly List<string> _toppings = new();

        // Required parameter goes in the constructor — enforced at compile time
        public PizzaBuilder(string size)
        {
            // OPTION B VALIDATION: reject obviously wrong input immediately
            if (string.IsNullOrWhiteSpace(size))
                throw new ArgumentException("Size cannot be empty.", nameof(size));

            _size = size;
        }

        // OPTION B VALIDATION: crust type is invalid on its own
        public IPizzaBuilder WithCrust(string crust)
        {
            var validCrusts = new[] { "thin", "thick", "stuffed" };
            if (!validCrusts.Contains(crust))
                throw new ArgumentException($"'{crust}' is not a valid crust. Choose: thin, thick, stuffed.");

            _crust = crust;
            return this;   // return this → enables fluent chaining
        }

        // OPTION B VALIDATION: topping name cannot be blank
        public IPizzaBuilder WithTopping(string topping)
        {
            if (string.IsNullOrWhiteSpace(topping))
                throw new ArgumentException("Topping name cannot be empty.");

            _toppings.Add(topping);
            return this;
        }

        // OPTION A VALIDATION: cross-field rules checked just before creation
        public Pizza Build()
        {
            // Rule 1: business constraint combining two fields
            if (_crust == "stuffed" && _size == "Small")
                throw new InvalidOperationException("Stuffed crust is not available for Small pizzas.");

            // Rule 2: aggregate rule (cannot check this inside WithTopping)
            if (_toppings.Count > 10)
                throw new InvalidOperationException("Maximum 10 toppings allowed.");

            // Only if ALL validations pass do we create the product
            return new Pizza(_size, _crust, _toppings.AsReadOnly());
        }
    }


    // ─────────────────────────────────────────────────────────
    // SECTION 4: CONCRETE BUILDER #2 — Testing
    // ─────────────────────────────────────────────────────────
    // Same interface, completely different behaviour.
    // - Pre-filled sensible defaults → tests get a valid object fast
    // - No validation noise → tests focus on the logic being tested
    // - The Director and any class depending on IPizzaBuilder
    //   never need to change when you swap builders
    // ─────────────────────────────────────────────────────────

    public class TestPizzaBuilder : IPizzaBuilder
    {
        private string _size = "Medium";
        private string _crust = "thin";
        private readonly List<string> _toppings = new() { "cheese" };

        public IPizzaBuilder WithCrust(string crust) { _crust = crust; return this; }
        public IPizzaBuilder WithTopping(string topping) { _toppings.Add(topping); return this; }

        // No validation — tests just need a valid object fast
        public Pizza Build() => new Pizza(_size, _crust, _toppings.AsReadOnly());
    }


    // ─────────────────────────────────────────────────────────
    // SECTION 5: THE DIRECTOR (optional)
    // ─────────────────────────────────────────────────────────
    // - Knows common "recipes" (step sequences) for building products
    // - Depends ONLY on IPizzaBuilder → works with any concrete builder
    // - Client code can bypass the Director and call the builder directly
    //   when it needs a custom configuration
    // ─────────────────────────────────────────────────────────

    public class PizzaDirector
    {
        private readonly IPizzaBuilder _builder;

        // Director receives the builder via constructor injection
        // → same Dependency Inversion used everywhere in ASP.NET Core
        public PizzaDirector(IPizzaBuilder builder)
        {
            _builder = builder;
        }

        public Pizza BuildMargherita() =>
            _builder
                .WithCrust("thin")
                .WithTopping("tomato sauce")
                .WithTopping("mozzarella")
                .WithTopping("basil")
                .Build();

        public Pizza BuildPepperoni() =>
            _builder
                .WithCrust("thick")
                .WithTopping("tomato sauce")
                .WithTopping("pepperoni")
                .Build();
    }
}


// ─────────────────────────────────────────────────────────────
// SECTION 6: ASP.NET CORE — BUILDER PATTERN IN PROGRAM.CS
// ─────────────────────────────────────────────────────────────
// ASP.NET Core uses the Builder pattern at framework level.
// The three phases below map exactly to the pattern above.
//
//   WebApplicationBuilder  =  ConcreteBuilder
//   builder.Build()        =  Build() method
//   WebApplication         =  Product
//   Program.cs             =  Director
// ─────────────────────────────────────────────────────────────

/*

// ════════════════════════════════════════════════════════════
// PHASE 1 — BUILDER PHASE (before builder.Build())
// ════════════════════════════════════════════════════════════
// What happens here:
//   - Services are REGISTERED into the DI container
//   - Configuration sources are layered (json → env vars → secrets)
//   - Logging providers are wired up
//   - The DI container is still OPEN — you can add services freely
// What does NOT happen yet:
//   - No HTTP server is started
//   - No requests are accepted
//   - No middleware runs
// ════════════════════════════════════════════════════════════

var builder = WebApplication.CreateBuilder(args);

// ── builder.Services ──────────────────────────────────────────
// Registers what services EXIST in the app.
// Three lifetimes:
//   Singleton  → one instance for the whole app lifetime
//   Scoped     → one instance per HTTP request
//   Transient  → new instance every time it is requested

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<IEmailService,    SmtpEmailService>();   // one for app lifetime
builder.Services.AddScoped<IOrderService,        OrderService>();       // one per request
builder.Services.AddTransient<IReportGenerator,  PdfReportGenerator>(); // new every time

// ── builder.Configuration ─────────────────────────────────────
// Layers config sources — each one can override the previous.
// Environment variables beat appsettings.json → great for Docker/K8s.

builder.Configuration
    .AddJsonFile("appsettings.json",                        optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>();    // dev-only secrets, never committed to git

// ── builder.Logging ───────────────────────────────────────────
// Register logging providers. ClearProviders() removes the default
// console logger so you start with a clean slate.

builder.Logging
    .ClearProviders()
    .AddConsole()
    .AddDebug()
    .SetMinimumLevel(LogLevel.Information);

// ── Auth setup (still Phase 1 — service registration) ─────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer    = true,
            ValidateAudience  = true,
            ValidateLifetime  = true,
            ValidIssuer       = builder.Configuration["Jwt:Issuer"],
            ValidAudience     = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey  = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("https://myapp.com")
              .AllowAnyMethod()
              .AllowAnyHeader()));


// ════════════════════════════════════════════════════════════
// THE BUILD() CALL — the pivot point
// ════════════════════════════════════════════════════════════
// What builder.Build() does internally:
//   1. Compiles all registered services into a sealed IServiceProvider
//   2. Merges all configuration sources into one IConfiguration
//   3. Creates the Kestrel HTTP server instance (does NOT start it)
//   4. Returns a WebApplication object — fully constructed, but idle
//
// AFTER this line:
//   - DI container is FROZEN — adding services throws an exception
//   - app object exists in memory, server is ready but not listening
// ════════════════════════════════════════════════════════════

var app = builder.Build();

// This would throw InvalidOperationException — container is sealed:
// app.Services.AddScoped<IOrderService, OrderService>(); // ❌


// ════════════════════════════════════════════════════════════
// PHASE 2 — PIPELINE CONFIGURATION (after builder.Build())
// ════════════════════════════════════════════════════════════
// What happens here:
//   - Each app.Use...() call REGISTERS a middleware delegate into
//     an ordered linked list inside the WebApplication object
//   - Nothing RUNS yet — you are building a recipe, not cooking
//   - ORDER IS CRITICAL — request flows top to bottom through this list
//
// What app.Use...() does internally (simplified):
//   app.Use(async (HttpContext context, RequestDelegate next) =>
//   {
//       // do something BEFORE the next middleware
//       await next(context);   // hand off to the next delegate
//       // optionally do something AFTER (response on the way back up)
//   });
//
// Request flow direction:  top → bottom (into the pipeline)
// Response flow direction: bottom → top (back out of the pipeline)
// ════════════════════════════════════════════════════════════

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();   // detailed error pages in dev only
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/error"); // friendly error page in prod
    app.UseHsts();                     // strict transport security header
}

// Each middleware below calls next() to pass the request down the chain.
// If a middleware does NOT call next(), it short-circuits (e.g. 401/403).

app.UseHttpsRedirection();   // 1. redirect HTTP → HTTPS (short-circuits if needed)
app.UseStaticFiles();        // 2. serve files from wwwroot (short-circuits if file found)
app.UseCors("AllowFrontend"); // 3. add CORS headers
app.UseRouting();            // 4. match the URL to an endpoint
app.UseAuthentication();     // 5. parse token → set HttpContext.User (never short-circuits)
app.UseAuthorization();      // 6. check [Authorize] → 401/403 if denied (short-circuits)
app.MapControllers();        // 7. execute controller action → write response (end of chain)


// ════════════════════════════════════════════════════════════
// PHASE 3 — app.Run() — THE MOMENT EVERYTHING STARTS
// ════════════════════════════════════════════════════════════
// What app.Run() does:
//   1. Compiles the middleware linked list into a single RequestDelegate
//      pipeline — this compilation happens ONCE, not per request
//   2. Starts the Kestrel HTTP server and binds to configured ports
//      (typically 5000 for HTTP, 5001 for HTTPS in development)
//   3. Enters a BLOCKING loop — this line never returns while the app runs
//   4. For each incoming request: runs it through the compiled pipeline
//   5. Hooks into OS signals for graceful shutdown:
//        Ctrl+C            → Console.CancelKeyPress
//        Docker/K8s stop   → SIGTERM via AppDomain.ProcessExit
//      On shutdown: drains in-flight requests, then stops cleanly.
//
// Conceptually what Run() does internally:
//
//   var pipeline = BuildPipeline(middlewareList);   // compiled ONCE
//   await server.StartAsync();                      // Kestrel binds to port
//   while (!cancellationToken.IsCancellationRequested)
//   {
//       var context = await server.AcceptRequestAsync();
//       _ = pipeline(context);   // run pipeline for this request
//   }
//   await server.StopAsync();   // graceful drain on shutdown
//
// NOTHING below app.Run() executes while the app is running.
// ════════════════════════════════════════════════════════════

app.Run();   // ← blocks here forever until shutdown signal received

// This line is unreachable while the app is running:
Console.WriteLine("App has shut down.");

*/


// ─────────────────────────────────────────────────────────────
// SECTION 7: QUICK REFERENCE — BUILDER PATTERN SUMMARY
// ─────────────────────────────────────────────────────────────
//
//  Component          | Pizza Example          | ASP.NET Core Equivalent
//  ───────────────────|────────────────────────|──────────────────────────
//  Product            | Pizza                  | WebApplication
//  Builder Interface  | IPizzaBuilder          | (internal to framework)
//  Concrete Builder   | PizzaBuilder           | WebApplicationBuilder
//  Director           | PizzaDirector          | Program.cs itself
//  Build()            | PizzaBuilder.Build()   | builder.Build()
//
//
//  Validation placement:
//
//  Location           | When to use
//  ───────────────────|────────────────────────────────────────────────
//  With...() setter   | Field is invalid on its own (bad enum, null, empty)
//  Build() method     | Cross-field rules, aggregate rules, final checks
//  Product constructor| Defensive guard — object can never be invalid
//
//
//  Three phases in ASP.NET Core Program.cs:
//
//  Phase              | Key line               | What it does
//  ───────────────────|────────────────────────|──────────────────────────
//  1. Builder phase   | builder.Services.Add() | Register services into DI
//  Pivot point        | builder.Build()        | Seal DI container, create app
//  2. Pipeline config | app.Use...()           | Register middleware delegates
//  3. Runtime         | app.Run()              | Start server, block, serve traffic
//
//
//  Key insight:
//    builder.Services  → answers "what services EXIST?"
//    app.Use...()      → answers "in what ORDER do requests travel?"
//    app.Run()         → answers "start accepting traffic NOW"
//
//  After builder.Build(), the DI container is sealed.
//  app.Use...() does NOT modify the WebApplication object's structure —
//  it registers delegates into an ordered linked list.
//  app.Run() compiles that list once into a pipeline, starts Kestrel,
//  and blocks until a shutdown signal is received.
// ─────────────────────────────────────────────────────────────