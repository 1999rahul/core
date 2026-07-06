// =============================================================================
//         ASP.NET CORE FILTERS — COMPLETE GUIDE (Interview Ready)
// =============================================================================
//
// TABLE OF CONTENTS
// -----------------
//  1.  What Are Filters?
//  2.  Filter vs Middleware
//  3.  The 5 Filter Types
//       3a. Authorization Filter
//       3b. Resource Filter
//       3c. Action Filter
//       3d. Exception Filter
//       3e. Result Filter
//  4.  Built-in Filters
//  5.  Custom Filters
//       5a. Implementing IActionFilter (with DI)
//       5b. Inheriting ActionFilterAttribute (no DI)
//       5c. Async Filters
//  6.  IActionFilter vs ActionFilterAttribute
//  7.  ServiceFilter vs TypeFilter
//  8.  Registering Filters (Global / Controller / Action)
//  9.  IOrderedFilter — Controlling Execution Order
// 10.  Order Resolution Rules
// 11.  Written Attribute Order — Does It Matter?
// 12.  Real-World Production Example
//
// =============================================================================


using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;


// =============================================================================
// 1. WHAT ARE FILTERS?
// =============================================================================
//
// Filters let you run code BEFORE or AFTER specific stages in the request
// processing pipeline. They are ideal for cross-cutting concerns — things you
// don't want to repeat in every controller action.
//
// Cross-cutting concern examples:
//   - Logging
//   - Authentication / Authorization
//   - Model validation
//   - Exception handling
//   - Response caching
//   - Performance timing
//
// Filters live inside the CONTROLLER PIPELINE (also loosely called the
// "MVC pipeline"). This pipeline is shared by both ASP.NET Core MVC and
// Web API — they are built on the same MVC kernel (Microsoft.AspNetCore.Mvc.Core).
//
// MVC    = MVC kernel + Views (Razor, ViewBag, ViewResult...)
// Web API = MVC kernel only (no view engine — returns JSON/XML)
//
// The filter pipeline, model binding, and IActionResult all live in that
// shared kernel, which is why filters work identically in both MVC and Web API.


// =============================================================================
// 2. FILTER vs MIDDLEWARE
// =============================================================================
//
// A common interview question — they are NOT the same thing.
//
// ┌──────────────────────────────────────────────────────────────────────────┐
// │  MIDDLEWARE                       │  FILTER                              │
// ├──────────────────────────────────────────────────────────────────────────┤
// │  Entire HTTP pipeline             │  Controller pipeline only            │
// │  All requests (static, health...) │  Only controller-bound requests      │
// │  Raw HttpContext only             │  ActionContext, ModelState, args...   │
// │  Defined in Program.cs            │  Attribute / global options          │
// │  No access to action result       │  Full access via Result filters       │
// │  Doesn't know route/controller    │  Full ActionDescriptor access        │
// └──────────────────────────────────────────────────────────────────────────┘
//
// Rule of thumb:
//   If it needs to know about controllers, actions, or model state → FILTER
//   If it doesn't care about MVC context → MIDDLEWARE
//
// Example: Authentication belongs in middleware (all requests must pass
// through it, including non-MVC endpoints). Model validation belongs in
// a filter (only meaningful inside the controller pipeline).


// =============================================================================
// 3. THE 5 FILTER TYPES  (execution order is fixed by the framework)
// =============================================================================
//
// Order is always:
//   Authorization → Resource → Action → Exception → Result
//
// You CANNOT swap the order of filter types. The type determines the stage.


// -----------------------------------------------------------------------------
// 3a. AUTHORIZATION FILTER
// -----------------------------------------------------------------------------
// Runs FIRST — before anything else, including model binding.
// If it short-circuits (sets context.Result), nothing else runs.
// Use for: API key checks, token validation, IP whitelisting.

public class ApiKeyAuthFilter : IAuthorizationFilter
{
    private readonly IConfiguration _config;

    // DI works fine — this is a plain class, not an attribute
    public ApiKeyAuthFilter(IConfiguration config)
    {
        _config = config;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var providedKey = context.HttpContext.Request.Headers["X-Api-Key"]
                                                     .FirstOrDefault();
        var validKey = _config["ApiSettings:Key"];

        if (string.IsNullOrEmpty(providedKey) || providedKey != validKey)
        {
            // Short-circuit: sets Result → nothing else in the pipeline runs
            context.Result = new UnauthorizedObjectResult(new
            {
                error = "Invalid or missing API key."
            });
        }
    }
}


// -----------------------------------------------------------------------------
// 3b. RESOURCE FILTER
// -----------------------------------------------------------------------------
// Runs AFTER authorization but BEFORE model binding.
// Short-circuiting here skips model binding AND the action — very efficient.
// Use for: response caching, blocking expensive operations early.

public class SimpleCacheFilter : IResourceFilter
{
    private static readonly Dictionary<string, IActionResult> _cache = new();

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var cacheKey = context.HttpContext.Request.Path.ToString();

        if (_cache.TryGetValue(cacheKey, out var cachedResult))
        {
            // Short-circuit — action never runs, model binding never runs
            context.Result = cachedResult;
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
        var cacheKey = context.HttpContext.Request.Path.ToString();

        if (context.Result != null && !_cache.ContainsKey(cacheKey))
        {
            _cache[cacheKey] = context.Result;
        }
    }
}


// -----------------------------------------------------------------------------
// 3c. ACTION FILTER  (most commonly used — ⭐ most common in interviews)
// -----------------------------------------------------------------------------
// Wraps the controller action method.
// OnActionExecuting  → runs BEFORE the action
// OnActionExecuted   → runs AFTER the action
// Use for: logging, model state validation, timing, argument modification.

public class LoggingActionFilter : IActionFilter
{
    private readonly ILogger<LoggingActionFilter> _logger;

    public LoggingActionFilter(ILogger<LoggingActionFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Access to action name, arguments, model state
        _logger.LogInformation(
            "Executing: {Action} | Args: {@Args}",
            context.ActionDescriptor.DisplayName,
            context.ActionArguments);

        // Short-circuit example: fail fast on invalid model state
        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(context.ModelState);
            return; // action never runs
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        _logger.LogInformation(
            "Executed: {Action} | Result: {Result}",
            context.ActionDescriptor.DisplayName,
            context.Result?.GetType().Name);
    }
}

// ASYNC version (preferred when doing async operations inside the filter)
public class TimingFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)  // next() = execute the action
    {
        var stopwatch = Stopwatch.StartNew();

        // Everything BEFORE next() runs before the action
        var executedContext = await next();
        // Everything AFTER next() runs after the action

        stopwatch.Stop();

        // executedContext.Exception != null if the action threw
        if (executedContext.Exception == null)
        {
            Console.WriteLine($"Action took {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}


// -----------------------------------------------------------------------------
// 3d. EXCEPTION FILTER
// -----------------------------------------------------------------------------
// Handles UNHANDLED exceptions thrown by action methods or other filters.
// Does NOT catch exceptions from Result filters or middleware.
// Set context.ExceptionHandled = true to mark the exception as resolved.
// Use for: global error responses, domain exception mapping.

public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "Unhandled exception");

        // Map exception types to HTTP status codes
        var statusCode = context.Exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            ArgumentException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Result = new ObjectResult(new
        {
            error = context.Exception.Message,
            type = context.Exception.GetType().Name,
            traceId = context.HttpContext.TraceIdentifier
        })
        { StatusCode = statusCode };

        // IMPORTANT: must mark as handled, otherwise the exception propagates
        context.ExceptionHandled = true;
    }
}


// -----------------------------------------------------------------------------
// 3e. RESULT FILTER
// -----------------------------------------------------------------------------
// Wraps the EXECUTION of IActionResult (e.g., writing JSON to the response).
// Runs only if no exception was thrown (or exception was handled).
// OnResultExecuting  → before the result is written to the response
// OnResultExecuted   → after the result is written to the response
// Use for: adding response headers, response transformation.

public class AddHeaderResultFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        // Modify the response before it is written
        context.HttpContext.Response.Headers.Append("X-Api-Version", "2.0");
        context.HttpContext.Response.Headers.Append("X-Powered-By", "MyAPI");
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
        // Runs after the response body is written
        // Useful for cleanup or post-response logging
    }
}


// =============================================================================
// 4. BUILT-IN FILTERS
// =============================================================================
//
// ASP.NET Core ships with many built-in filters you can use directly.
//
// AUTHORIZATION FILTERS (built-in):
//   [Authorize]               — requires authenticated user (roles/policies)
//   [AllowAnonymous]          — bypasses [Authorize] on specific action
//   [RequireHttps]            — rejects non-HTTPS requests with 403
//
// RESOURCE FILTERS (built-in):
//   [RequestSizeLimit(bytes)] — caps request body size
//   [DisableRequestSizeLimit] — removes any size cap on an action
//
// ACTION FILTERS (built-in):
//   [ValidateAntiForgeryToken]  — CSRF protection for MVC forms
//   [IgnoreAntiforgeryToken]    — skips CSRF check on specific action
//   [Consumes("app/json")]      — restricts accepted Content-Type
//   [Produces("app/json")]      — declares response Content-Type
//
// RESULT FILTERS (built-in):
//   [ResponseCache(Duration=60)] — sets Cache-Control headers
//   [ProducesResponseType(200)]  — documents expected status codes (Swagger)
//   [FormatFilter]               — allows ?format=json in query string
//
// SPECIAL / COMPOUND (built-in):
//   [ApiController]  — enables auto-400 on invalid ModelState, inferred
//                      binding sources, and ProblemDetails responses
//   [NonAction]      — marks a public method so it is NOT treated as action


// =============================================================================
// 5. CUSTOM FILTERS — 3 APPROACHES
// =============================================================================


// -----------------------------------------------------------------------------
// 5a. IMPLEMENTING THE INTERFACE (with DI — most flexible)
// -----------------------------------------------------------------------------
// Use when your filter needs services from DI (logger, DbContext, etc.)
// Trade-off: cannot be used directly as [MyFilter] — needs [ServiceFilter] wrapper

public class AuditFilter : IAsyncActionFilter
{
    private readonly ILogger<AuditFilter> _logger;

    // ✅ Constructor injection works because this is a plain class
    public AuditFilter(ILogger<AuditFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var userId = context.HttpContext.User?.Identity?.Name ?? "anonymous";
        var action = context.ActionDescriptor.DisplayName;
        var startAt = DateTime.UtcNow;

        var result = await next(); // execute the action

        var success = result.Exception == null;

        _logger.LogInformation(
            "AUDIT | User: {User} | Action: {Action} | Success: {Success} | At: {Time}",
            userId, action, success, startAt);
    }
}

// Registration in Program.cs:
//   builder.Services.AddScoped<AuditFilter>();
//
// Applied with [ServiceFilter] — DI resolves it:
//   [ServiceFilter(typeof(AuditFilter))]
//   public class OrdersController : ControllerBase { }


// -----------------------------------------------------------------------------
// 5b. INHERITING ATTRIBUTE BASE CLASS (no DI — cleanest syntax)
// -----------------------------------------------------------------------------
// Use when your filter has NO external dependencies — pure logic only.
// Trade-off: no DI in constructor; instance is a singleton (watch for state!)
//
// Available base classes:
//   ActionFilterAttribute   → Action + Result filter
//   ExceptionFilterAttribute → Exception filter
//   ResultFilterAttribute   → Result filter
//   AuthorizeAttribute      → Authorization filter

public class ValidateModelAttribute : ActionFilterAttribute
{
    // ✅ Clean [ValidateModel] syntax — just like [Authorize]
    // ❌ Cannot inject ILogger here — attribute limitation

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(context.ModelState);
        }
    }
}

public class HandleExceptionAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        var statusCode = context.Exception switch
        {
            KeyNotFoundException => 404,
            UnauthorizedAccessException => 401,
            ArgumentException => 400,
            _ => 500
        };

        context.Result = new ObjectResult(new
        {
            error = context.Exception.Message
        })
        { StatusCode = statusCode };

        context.ExceptionHandled = true;
    }
}

// ⚠️ SINGLETON TRAP: attribute instances are created ONCE and REUSED.
// NEVER store request-specific state in fields of an attribute filter.

public class DangerousFilter : ActionFilterAttribute
{
    // ❌ WRONG: _requestId is shared across ALL concurrent requests!
    private string _requestId = string.Empty;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _requestId = context.HttpContext.TraceIdentifier; // RACE CONDITION BUG!
    }
}

public class SafeFilter : ActionFilterAttribute
{
    private const string TimestampKey = "filter_start_time";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // ✅ CORRECT: HttpContext.Items is scoped to this single request
        context.HttpContext.Items[TimestampKey] = DateTime.UtcNow;
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        var start = (DateTime)context.HttpContext.Items[TimestampKey]!;
        var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
        Console.WriteLine($"Request took {elapsed}ms");
    }
}


// -----------------------------------------------------------------------------
// 5c. ASYNC FILTERS
// -----------------------------------------------------------------------------
// Use whenever your filter logic involves async operations:
// database calls, HTTP client calls, file I/O, etc.
//
// Available async interfaces:
//   IAsyncAuthorizationFilter
//   IAsyncResourceFilter
//   IAsyncActionFilter        ← most common
//   IAsyncExceptionFilter
//   IAsyncResultFilter

public class AsyncValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _services;

    public AsyncValidationFilter(IServiceProvider services)
    {
        _services = services;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        // Async work BEFORE the action
        await Task.Delay(1); // placeholder for real async validation

        if (!context.ModelState.IsValid)
        {
            // Short-circuit — do NOT call next()
            context.Result = new BadRequestObjectResult(context.ModelState);
            return;
        }

        // Call next() to execute the action
        var executedContext = await next();

        // Async work AFTER the action
        if (executedContext.Exception != null)
        {
            // handle or log exception
        }
    }
}


// =============================================================================
// 6. IActionFilter vs ActionFilterAttribute — KEY DIFFERENCES
// =============================================================================
//
// The core C# limitation: attributes can only accept COMPILE-TIME CONSTANTS
// as constructor arguments — strings, numbers, typeof(), enums.
// They CANNOT accept objects from DI at runtime.
//
// This is why two approaches exist:
//
// ┌──────────────────────────────────────────────────────────────────────────┐
// │                    │ IActionFilter          │ ActionFilterAttribute      │
// ├──────────────────────────────────────────────────────────────────────────┤
// │ DI Support         │ ✅ Full (constructor)  │ ❌ None                   │
// │ Apply syntax       │ [ServiceFilter(...)]   │ [MyFilter] (clean!)       │
// │ Instance lifetime  │ Controlled by DI       │ Singleton (reused!)       │
// │ Store request state│ ✅ In fields (scoped)  │ ❌ Use HttpContext.Items  │
// │ DI registration    │ Required               │ Not needed                │
// │ Best for           │ Infrastructure (DB,log)│ Pure logic, validation    │
// └──────────────────────────────────────────────────────────────────────────┘


// =============================================================================
// 7. ServiceFilter vs TypeFilter
// =============================================================================
//
// Both are wrappers that let you apply an IActionFilter as an attribute.
// The difference is HOW they create the filter instance.
//
// [ServiceFilter(typeof(MyFilter))]
//   - Resolves the filter FROM the DI container
//   - Respects your registered lifetime (Scoped/Transient/Singleton)
//   - Filter MUST be pre-registered in DI
//
// [TypeFilter(typeof(MyFilter))]
//   - Creates a NEW instance itself using DI for constructor args
//   - No pre-registration needed
//   - Ignores your DI lifetime — always creates fresh
//   - Can pass EXTRA constructor arguments not in DI via Arguments property

// Example with TypeFilter arguments:
//   [TypeFilter(typeof(RateLimitFilter), Arguments = new object[] { 100, "per-minute" })]
//   public IActionResult Index() => Ok();


// =============================================================================
// 8. REGISTERING FILTERS (Global / Controller / Action)
// =============================================================================

// ----- GLOBAL (every controller and action in the app) -----
//
// builder.Services.AddScoped<LoggingActionFilter>();
// builder.Services.AddScoped<GlobalExceptionFilter>();
//
// builder.Services.AddControllers(options =>
// {
//     options.Filters.Add<LoggingActionFilter>();   // by type (resolved from DI)
//     options.Filters.Add<GlobalExceptionFilter>(); // by type
//     options.Filters.Add(new ValidateModelAttribute()); // by instance
// });

// ----- CONTROLLER (every action in this controller) -----
[ServiceFilter(typeof(AuditFilter))]       // DI-resolved — must be registered
[TypeFilter(typeof(LoggingActionFilter))]  // created fresh — no pre-registration
// public class ProductsController : ControllerBase { }

// ----- ACTION (this action only) -----
// [HandleException]
// [ValidateModel]
// [ServiceFilter(typeof(AuditFilter))]
// public IActionResult Create([FromBody] ProductDto dto) => Ok();


// =============================================================================
// 9. IOrderedFilter — CONTROLLING EXECUTION ORDER
// =============================================================================
//
// When you have multiple filters of the SAME TYPE, you control their order
// using the IOrderedFilter interface. Implement it on your filter class and
// return an integer from the Order property.
//
// Lower Order value  = runs EARLIER on the way IN
// Higher Order value = runs LATER on the way IN
// On the way OUT (response) — the order REVERSES automatically
//
// Default Order value = 0 for all filters that don't implement IOrderedFilter.
//
// RECOMMENDED BANDS (to avoid renumbering):
//   -3000 to -2000  →  Infrastructure (trace ID, correlation, timing)
//   -1000           →  Security & auth checks
//   0  (default)    →  Business logic (validation, rate limiting)
//   1000+           →  Response modification (headers, caching)

public class CorrelationIdFilter : IActionFilter, IOrderedFilter
{
    public int Order => -3000; // runs very first — stamps every request

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var traceId = Guid.NewGuid().ToString();
        context.HttpContext.Items["TraceId"] = traceId;
        context.HttpContext.Response.Headers.Append("X-Trace-Id", traceId);
        Console.WriteLine($"[{traceId}] CorrelationId - Executing");
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        var traceId = context.HttpContext.Items["TraceId"];
        Console.WriteLine($"[{traceId}] CorrelationId - Executed");
    }
}

public class SecurityCheckFilter : IActionFilter, IOrderedFilter
{
    public int Order => -1000; // security runs early

    public void OnActionExecuting(ActionExecutingContext context)
        => Console.WriteLine("SecurityCheck - Executing");

    public void OnActionExecuted(ActionExecutedContext context)
        => Console.WriteLine("SecurityCheck - Executed");
}

public class BusinessValidationFilter : IActionFilter, IOrderedFilter
{
    public int Order => 0; // default band — business logic

    public void OnActionExecuting(ActionExecutingContext context)
        => Console.WriteLine("BusinessValidation - Executing");

    public void OnActionExecuted(ActionExecutedContext context)
        => Console.WriteLine("BusinessValidation - Executed");
}

public class ResponseEnrichmentFilter : IActionFilter, IOrderedFilter
{
    public int Order => 1000; // runs last on way in, first on way out

    public void OnActionExecuting(ActionExecutingContext context)
        => Console.WriteLine("ResponseEnrichment - Executing");

    public void OnActionExecuted(ActionExecutedContext context)
    {
        context.HttpContext.Response.Headers.Append("X-Api-Version", "2.0");
        Console.WriteLine("ResponseEnrichment - Executed (headers added)");
    }
}

// Execution trace for a request (registration order irrelevant — Order wins):
//
// REQUEST  →  CorrelationId (-3000) → SecurityCheck (-1000) → Validation (0) → Enrichment (1000)
//                                      [ action method ]
// RESPONSE ←  Enrichment (1000)    → Validation (0)        → SecurityCheck (-1000) → CorrelationId (-3000)


// =============================================================================
// 10. ORDER RESOLUTION RULES (Complete Decision Chain)
// =============================================================================
//
// When the framework sorts filters of the same TYPE, it applies these rules
// in order — stopping as soon as a rule produces a winner:
//
// STEP 1 — Different Order values?
//   YES → lower Order runs first on the way IN. Done. Scope is ignored.
//
// STEP 2 — Same Order, different scopes?
//   YES → Global first, Controller second, Action last. Done.
//
// STEP 3 — Same Order AND same scope?
//   → Registration order (order of options.Filters.Add() calls, or
//     top-to-bottom written order for attributes on the same target)
//
// ⚠️ IMPORTANT: Order ALWAYS beats Scope.
//   An action-level filter with Order = -2000 runs BEFORE a global filter
//   with Order = 1000, even though global scope normally runs first.
//
// SCENARIO A — Same scope, different Order:
//   [Security Order=-1000] [Logging Order=0] [Validation Order=500]
//   IN:  Security → Logging → Validation
//   OUT: Validation → Logging → Security
//
// SCENARIO B — Different scopes, same Order (all Order=0):
//   Global: Security | Controller: Logging | Action: Validation
//   IN:  Security (global) → Logging (controller) → Validation (action)
//   OUT: Validation → Logging → Security
//
// SCENARIO C — Order overrides scope:
//   Global: Security (Order=1000) | Controller: Logging (Order=0) | Action: Validation (Order=-2000)
//   IN:  Validation (action, -2000) → Logging (controller, 0) → Security (global, 1000)
//   OUT: Security → Logging → Validation
//   ⭐ Global filter with Order=1000 runs LAST despite being global scope!


// =============================================================================
// 11. WRITTEN ATTRIBUTE ORDER — DOES IT MATTER?
// =============================================================================
//
// When all filters on the SAME target (same action or same controller) have
// the SAME Order value (or no Order at all), they execute in written order.
//
// This works because:
//   - All have same Order → falls through to scope as tiebreaker
//   - All at same scope   → falls through to registration order as tiebreaker
//   - C# reflection returns attributes top-to-bottom → written order wins
//
// [FilterA]   ← runs 1st on the way IN
// [FilterB]   ← runs 2nd on the way IN
// [FilterC]   ← runs 3rd on the way IN
// public IActionResult Get() => Ok();
//
// Output:
//   FilterA - Executing
//   FilterB - Executing
//   FilterC - Executing
//   [action]
//   FilterC - Executed   ← reversed on way OUT
//   FilterB - Executed
//   FilterA - Executed
//
// ⚠️ WHEN WRITTEN ORDER BREAKS:
//
//   Gotcha 1 — Any filter gets a different Order value:
//     [FilterA]              // Order = 0
//     [FilterB]              // Order = 0
//     [FilterC(Order = -1)]  // jumps to front! — written last but runs first
//
//   Gotcha 2 — Mixing scopes:
//     [FilterB]              // on controller — runs before action-level filters
//     public class MyController ...
//     {
//         [FilterA]          // on action — written "first" visually but runs AFTER FilterB
//         [FilterC]          // on action
//         public IActionResult Get() => Ok();
//     }
//     Output: FilterB → FilterA → FilterC (scope wins over visual order)
//
//   Gotcha 3 — ServiceFilter with IOrderedFilter:
//     [FilterA]                            // Order = 0
//     [ServiceFilter(typeof(FilterB))]     // FilterB implements IOrderedFilter with Order = -500
//     [FilterC]                            // Order = 0
//     FilterB jumps to front regardless of where it is written.
//
// ✅ BEST PRACTICE: Don't rely on written order for anything critical.
//    Set explicit Order values when execution sequence matters.
//
//    // Fragile:
//    [FilterA] [FilterB] [FilterC]
//
//    // Explicit and safe:
//    [TypeFilter(typeof(FilterA), Order = 1)]
//    [TypeFilter(typeof(FilterB), Order = 2)]
//    [TypeFilter(typeof(FilterC), Order = 3)]


// =============================================================================
// 12. REAL-WORLD PRODUCTION EXAMPLE — All Concepts Together
// =============================================================================

// --- Filter implementations ---

// Infrastructure: stamps every request with a correlation ID (Order -3000)
public class CorrelationFilter : IActionFilter, IOrderedFilter
{
    public int Order => -3000;

    public void OnActionExecuting(ActionExecutingContext context)
        => context.HttpContext.Items["TraceId"] = Guid.NewGuid().ToString();

    public void OnActionExecuted(ActionExecutedContext context) { }
}

// Infrastructure: measures total action execution time (Order -2000)
public class PerformanceFilter : IAsyncActionFilter, IOrderedFilter
{
    private readonly ILogger<PerformanceFilter> _logger;
    public int Order => -2000;

    public PerformanceFilter(ILogger<PerformanceFilter> logger)
        => _logger = logger;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var sw = Stopwatch.StartNew();
        await next();
        sw.Stop();

        var traceId = context.HttpContext.Items["TraceId"];
        _logger.LogInformation("[{TraceId}] {Action} completed in {Ms}ms",
            traceId,
            context.ActionDescriptor.DisplayName,
            sw.ElapsedMilliseconds);
    }
}

// Security: validates API key (Order -1000)
public class ApiKeyFilter : IAuthorizationFilter, IOrderedFilter
{
    private readonly IConfiguration _config;
    public int Order => -1000;

    public ApiKeyFilter(IConfiguration config) => _config = config;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var key = context.HttpContext.Request.Headers["X-Api-Key"].FirstOrDefault();
        var validKey = _config["ApiSettings:Key"];

        if (key != validKey)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                error = "Invalid API key",
                traceId = context.HttpContext.Items["TraceId"]
            });
        }
    }
}

// Business: validates model state (Order 0, attribute-based for clean syntax)
public class ValidateAttribute : ActionFilterAttribute
{
    // No Order property — defaults to 0
    // No DI needed — pure logic

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(new
            {
                errors = context.ModelState.Values
                                 .SelectMany(v => v.Errors)
                                 .Select(e => e.ErrorMessage),
                traceId = context.HttpContext.Items["TraceId"]
            });
        }
    }
}

// Response: adds standard headers (Order 1000)
public class StandardHeadersFilter : IResultFilter, IOrderedFilter
{
    public int Order => 1000;

    public void OnResultExecuting(ResultExecutingContext context)
    {
        context.HttpContext.Response.Headers.Append("X-Api-Version", "2.0");
        context.HttpContext.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}

// Exception: global handler for unhandled exceptions
public class ProductionExceptionFilter : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        var statusCode = context.Exception switch
        {
            KeyNotFoundException => 404,
            UnauthorizedAccessException => 401,
            ArgumentException => 400,
            _ => 500
        };

        context.Result = new ObjectResult(new
        {
            error = statusCode >= 500 ? "An unexpected error occurred." : context.Exception.Message,
            traceId = context.HttpContext.Items["TraceId"]
        })
        { StatusCode = statusCode };

        context.ExceptionHandled = true;
    }
}

// --- Program.cs registration ---
//
// builder.Services.AddScoped<CorrelationFilter>();
// builder.Services.AddScoped<PerformanceFilter>();
// builder.Services.AddScoped<ApiKeyFilter>();
// builder.Services.AddScoped<StandardHeadersFilter>();
//
// builder.Services.AddControllers(options =>
// {
//     options.Filters.Add<CorrelationFilter>();          // Order -3000
//     options.Filters.Add<PerformanceFilter>();          // Order -2000
//     options.Filters.Add<ApiKeyFilter>();               // Order -1000 (auth filter)
//     options.Filters.Add<ProductionExceptionFilter>();  // exception filter (global)
//     options.Filters.Add<StandardHeadersFilter>();      // Order 1000 (result filter)
// });

// --- Controller usage ---
//
// [ApiController]
// [Route("api/[controller]")]
// public class PaymentsController : ControllerBase
// {
//     [HttpPost]
//     [Validate]                               // clean attribute syntax, Order=0
//     public IActionResult Pay([FromBody] PaymentRequest request)
//     {
//         // Business logic here
//         return Ok(new { message = "Payment processed." });
//     }
// }

// --- Full execution trace for POST /api/payments ---
//
// REQUEST  IN:
//   1. CorrelationFilter    (Order -3000, global)  → stamps TraceId
//   2. PerformanceFilter    (Order -2000, global)  → starts stopwatch
//   3. ApiKeyFilter         (Order -1000, global)  → validates API key
//   4. ValidateAttribute    (Order 0,     action)  → checks ModelState
//      [ PaymentsController.Pay() executes ]
//
// EXCEPTION (if thrown):
//   ProductionExceptionFilter → maps exception to HTTP response
//
// RESPONSE OUT:
//   5. StandardHeadersFilter (Order 1000, global) → adds X-Api-Version header
//   6. PerformanceFilter     (Order -2000)        → stops stopwatch, logs time
//   7. CorrelationFilter     (Order -3000)        → (nothing on way out)

// =============================================================================
// END OF GUIDE
// =============================================================================