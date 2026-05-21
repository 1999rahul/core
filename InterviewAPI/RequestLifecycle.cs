// ============================================================
//   FULL REQUEST LIFECYCLE IN ASP.NET CORE
//   What happens internally when a request hits your API
//   Interview Notes — Detailed
// ============================================================

// ----------------------------------------------------------
// THE COMPLETE FLOW (overview)
// ----------------------------------------------------------
//
// Client (browser / mobile / Postman)
//     ↓  HTTP request over TCP
// Network (DNS resolution, TCP handshake, TLS handshake)
//     ↓
// Kestrel (web server — receives raw bytes from socket)
//     ↓
// HTTP parsing (turns raw bytes into HttpContext)
//     ↓
// Middleware pipeline (runs in registration order)
//     ↓  ExceptionHandler → HttpsRedirection → StaticFiles
//     ↓  → Routing → Authentication → Authorization
//     ↓
// Endpoint execution
//     ↓  Resource Filters → Model Binding → Model Validation
//     ↓  → Action Filters (before) → Action Method
//     ↓  → Action Filters (after) → Result Filters → Result Execution
//     ↓
// Response travels BACK through middleware in reverse order
//     ↓
// Kestrel writes response bytes to socket
//     ↓
// Client receives response
//     ↓
// Request scope disposed (DbContext, Scoped services cleaned up)


// ============================================================
// LAYER 1 — NETWORK AND TCP (outside ASP.NET)
// ============================================================
//
// Before your code is involved, the OS handles:
//
//   1. DNS resolution
//      Client asks DNS server: "what is the IP of api.myapp.com?"
//      DNS returns: "1.2.3.4"
//
//   2. TCP Three-way handshake
//      Client → Server : SYN  (I want to connect)
//      Server → Client : SYN-ACK  (OK, acknowledged)
//      Client → Server : ACK  (confirmed, connection open)
//
//   3. TLS Handshake (for HTTPS)
//      Client → Server : ClientHello (supported TLS versions, cipher suites)
//      Server → Client : ServerHello + Certificate
//      Client verifies certificate is trusted (signed by known CA)
//      Both sides derive shared encryption keys
//      All further communication is encrypted
//
// Result: open encrypted socket between client and server.
// ASP.NET Core knows nothing about this — the OS handled it.
// Kestrel just reads bytes from the open socket.


// ============================================================
// LAYER 2 — KESTREL RECEIVES THE REQUEST
// ============================================================
//
// Kestrel is ASP.NET Core's built-in cross-platform web server.
// It is the first piece of your application code that sees the request.
//
// What Kestrel does:
//   1. Listens on a socket (port 5000 HTTP, 5001 HTTPS by default)
//   2. Reads raw bytes arriving on the socket
//   3. Parses the HTTP protocol from those bytes
//   4. Creates HttpContext representing the request/response
//   5. Hands HttpContext to the middleware pipeline
//
// Raw bytes arriving at Kestrel look like this:
//
//   "GET /products/42 HTTP/1.1\r\n
//    Host: api.myapp.com\r\n
//    Authorization: Bearer eyJhbGci...\r\n
//    Accept: application/json\r\n
//    Content-Type: application/json\r\n
//    \r\n
//    { "name": "Laptop" }"
//
// Why Kestrel is fast:
//   - Built on System.IO.Pipelines — a low-allocation I/O API
//   - Reuses memory buffers instead of allocating new byte arrays per request
//   - Supports HTTP/1.1, HTTP/2, and HTTP/3
//   - Runs entirely in managed code, no IIS dependency
//   - Uses async I/O throughout — no threads blocked waiting for I/O
//
// Kestrel configuration example:
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000);                          // HTTP
    options.ListenLocalhost(5001, o => o.UseHttps());       // HTTPS
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;  // 10MB max body
    options.Limits.MaxConcurrentConnections = 100;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
});


// ============================================================
// LAYER 3 — HttpContext IS CREATED
// ============================================================
//
// After parsing the raw HTTP bytes, Kestrel creates an HttpContext.
// This is the CENTRAL object that flows through your entire pipeline.
// Every middleware, every filter, every action method works with it.

// Simplified view of what HttpContext contains:
public class HttpContextOverview
{
    // The incoming request — everything the client sent
    HttpRequest Request { get; }
    // Request.Method       → "GET", "POST", "PUT", "DELETE"
    // Request.Path         → "/products/42"
    // Request.QueryString  → "?search=laptop&page=1"
    // Request.Headers      → all HTTP headers as key/value pairs
    // Request.Body         → the raw request body stream
    // Request.ContentType  → "application/json"
    // Request.Cookies      → cookie collection

    // The outgoing response — you write to this
    HttpResponse Response { get; }
    // Response.StatusCode  → set this to 200, 404, 401, etc.
    // Response.Headers     → add response headers
    // Response.Body        → write response body here
    // Response.ContentType → "application/json"
    // Response.Cookies     → set cookies on the client

    // Per-request DI scope — created fresh for this request
    // Disposed at the end of the request
    // Use this to resolve Scoped services (DbContext, etc.)
    IServiceProvider RequestServices { get; }

    // Populated by Authentication middleware
    // Contains claims (userId, email, roles) from JWT token
    ClaimsPrincipal User { get; set; }

    // Share data between middleware components in the same request
    // Like a request-scoped dictionary
    IDictionary<object, object?> Items { get; }

    // Session data (if sessions are enabled)
    ISession Session { get; }

    // Information about the TCP connection
    ConnectionInfo Connection { get; }
    // Connection.RemoteIpAddress → client's IP address
    // Connection.LocalPort       → port the server is listening on

    // Fires if the client disconnects before the response is sent
    // Pass this to async methods so work stops if client is gone
    CancellationToken RequestAborted { get; }

    // Matched route data — populated after UseRouting() runs
    // Contains extracted route values like { id = 42 }
    RouteData GetRouteData() => null;

    // The matched endpoint — populated after UseRouting() runs
    // Contains the action descriptor and metadata ([Authorize], etc.)
    Endpoint GetEndpoint() => null;
}


// ============================================================
// LAYER 4 — MIDDLEWARE PIPELINE
// ============================================================
//
// HttpContext now enters the middleware pipeline.
// Middleware runs in the EXACT order registered in Program.cs.
// Each middleware can inspect/modify the request, call next,
// then inspect/modify the response on the way back.
//
// The pipeline is bidirectional:
//
//  Request  →  [MW1]  →  [MW2]  →  [MW3]  →  Endpoint
//  Response ←  [MW1]  ←  [MW2]  ←  [MW3]  ←  Endpoint
//
// Code after "await next(context)" runs on the response path.

// Correct middleware order in Program.cs:
var app = builder.Build();

app.UseExceptionHandler();   // 1. Catch ALL unhandled exceptions — must be FIRST
app.UseHttpsRedirection();   // 2. Redirect HTTP → HTTPS
app.UseStaticFiles();        // 3. Serve static files, skip rest of pipeline
app.UseRouting();            // 4. MATCH route, store endpoint in HttpContext
app.UseAuthentication();     // 5. WHO are you? — populate context.User
app.UseAuthorization();      // 6. WHAT can you do? — check [Authorize]
app.MapControllers();        // 7. EXECUTE the matched endpoint


// ----------------------------------------------------------
// 4a — Exception Handler Middleware (MUST BE FIRST)
// ----------------------------------------------------------
//
// Wraps the entire remaining pipeline in a try/catch.
// If ANY middleware or endpoint throws an unhandled exception,
// this catches it and returns a structured error response.
//
// Why first?
//   Exceptions bubble UP through the call stack.
//   If exception handler is not first, exceptions from middleware
//   registered BEFORE it will be completely uncaught.

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        context.Response.StatusCode = exception switch
        {
            NotFoundException => 404,
            ValidationException => 400,
            UnauthorizedAccessException => 401,
            _ => 500
        };

        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = context.Response.StatusCode,
            Title = "An error occurred",
            Detail = exception?.Message
        };

        await context.Response.WriteAsJsonAsync(problem);
    });
});

// .NET 8 introduced IExceptionHandler — cleaner approach:
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred");

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = exception.Message
        };

        context.Response.StatusCode = problem.Status.Value;
        await context.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true; // true = exception is handled, don't rethrow
    }
}

// Register in Program.cs:
// builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// builder.Services.AddProblemDetails();
// app.UseExceptionHandler();


// ----------------------------------------------------------
// 4b — HTTPS Redirection Middleware
// ----------------------------------------------------------
//
// Checks if request came in on HTTP.
// If yes → short-circuits pipeline, returns 301 redirect to HTTPS.
// If no  → calls next, does nothing.

app.UseHttpsRedirection();
// Internally does this:
app.Use(async (context, next) =>
{
    if (!context.Request.IsHttps)
    {
        var httpsUrl = $"https://{context.Request.Host}{context.Request.Path}";
        context.Response.Redirect(httpsUrl, permanent: true); // 301
        return; // short-circuit — next is NOT called
    }
    await next(context);
});


// ----------------------------------------------------------
// 4c — Static Files Middleware
// ----------------------------------------------------------
//
// Checks if the request path matches a file in wwwroot/.
// If yes → short-circuits, serves the file directly.
//          Skips routing, auth, controllers — pure performance.
// If no  → calls next.
//
// Place this EARLY so static files bypass expensive middleware.

app.UseStaticFiles();
// Request: GET /css/styles.css
//   → Finds wwwroot/css/styles.css
//   → Returns file with correct Content-Type: text/css
//   → Pipeline stops here — no auth, no routing, no controller
//
// Request: GET /api/products
//   → No matching file in wwwroot
//   → Calls next middleware


// ----------------------------------------------------------
// 4d — Routing Middleware
// ----------------------------------------------------------
//
// CRITICAL CONCEPT: UseRouting does NOT execute the endpoint.
// It ONLY matches the incoming URL+method to a registered endpoint
// and stores the result in HttpContext.
//
// The separation exists so Authorization can inspect the
// matched endpoint's metadata BEFORE execution happens.

app.UseRouting();

// What UseRouting does internally:
app.Use(async (context, next) =>
{
    // Inspects: context.Request.Method + context.Request.Path
    // Finds matching endpoint from all registered endpoints
    // Stores result in context for downstream middleware to read

    // After this runs:
    var endpoint = context.GetEndpoint();
    // endpoint.Metadata → contains [Authorize], [AllowAnonymous], route info
    // endpoint.DisplayName → "MyApp.Controllers.ProductsController.GetProduct"

    var routeData = context.GetRouteData();
    // routeData.Values → { "controller": "Products", "action": "GetProduct", "id": "42" }

    await next(context);
});

// Example: Request comes in as GET /products/42
// UseRouting finds this matches:
//   [HttpGet("{id}")]
//   public IActionResult GetProduct(int id) { }
// Stores the match. Does NOT call the method yet.


// ----------------------------------------------------------
// 4e — Authentication Middleware
// ----------------------------------------------------------
//
// Reads the request and tries to establish WHO the caller is.
// For JWT: reads Authorization header, validates token, extracts claims.
// Populates context.User with the identity.
//
// Important: Does NOT reject requests here.
// Even if token is missing/invalid, request continues.
// Rejection is Authorization's job (next middleware).

app.UseAuthentication();

// What happens internally for JWT:
app.Use(async (context, next) =>
{
    // 1. Read Authorization header
    var authHeader = context.Request.Headers["Authorization"].ToString();
    // Value: "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

    // 2. Extract token
    var token = authHeader.StartsWith("Bearer ") ? authHeader[7..] : null;

    if (token != null)
    {
        // 3. Validate token signature using the secret key
        // 4. Check token expiry (exp claim)
        // 5. Check issuer and audience
        // 6. Extract all claims from payload:
        //      sub  → user ID
        //      email → user's email
        //      role  → user's roles
        //      any custom claims

        // 7. Create ClaimsPrincipal and assign to context.User
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }
    // If token missing or invalid:
    //   context.User = unauthenticated ClaimsPrincipal
    //   context.User.Identity.IsAuthenticated = false
    //   Request CONTINUES — authorization will reject it if needed

    await next(context);
});

// After UseAuthentication:
//   context.User.Identity.IsAuthenticated  → true / false
//   context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value  → "user-123"
//   context.User.IsInRole("Admin")  → true / false
//   context.User.Claims  → all claims from the JWT token


// ----------------------------------------------------------
// 4f — Authorization Middleware
// ----------------------------------------------------------
//
// Now that we know WHO the user is (Authentication)
// AND which endpoint matched (Routing),
// check if the user is ALLOWED to access it.
//
// Reads endpoint metadata ([Authorize], policies, roles).
// Evaluates context.User against those requirements.
// Short-circuits with 401 or 403 if not authorized.

app.UseAuthorization();

// What happens internally:
app.Use(async (context, next) =>
{
    var endpoint = context.GetEndpoint();

    // Check if endpoint has [Authorize] attribute
    var authorizeMetadata = endpoint?.Metadata.GetMetadata<IAuthorizeData>();

    if (authorizeMetadata != null)
    {
        if (!context.User.Identity.IsAuthenticated)
        {
            // Not logged in at all
            context.Response.StatusCode = 401; // Unauthorized
            return; // short-circuit
        }

        // Check roles if specified: [Authorize(Roles = "Admin")]
        // Check policies if specified: [Authorize(Policy = "CanEditProducts")]
        // If any check fails:
        context.Response.StatusCode = 403; // Forbidden — logged in but not allowed
        return;
    }

    // [AllowAnonymous] → skip all checks regardless of auth state
    // No [Authorize] → endpoint is public, continue

    await next(context);
});

// Why UseRouting MUST come before UseAuthorization:
//   Authorization reads endpoint.Metadata ([Authorize] attributes)
//   If UseRouting hasn't run yet, context.GetEndpoint() returns null
//   Authorization would have nothing to check against


// ============================================================
// LAYER 5 — ENDPOINT EXECUTION (Controller-based)
// ============================================================
//
// The request has passed all middleware.
// Now the matched controller action executes.
// But before your action method runs, several sub-layers fire.


// ----------------------------------------------------------
// 5a — Resource Filters
// ----------------------------------------------------------
//
// Run BEFORE model binding — the earliest MVC-level hook.
// Rarely used. Main use case: short-circuit with a cached response
// before the expensive work of model binding and action execution.

public class CacheResourceFilter : IResourceFilter
{
    private readonly IMemoryCache _cache;

    public CacheResourceFilter(IMemoryCache cache) => _cache = cache;

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var cacheKey = context.HttpContext.Request.Path;

        if (_cache.TryGetValue(cacheKey, out var cachedResult))
        {
            // Short-circuit entire pipeline — action method never runs
            context.Result = new OkObjectResult(cachedResult);
        }
        // If not cached, continue to model binding and action execution
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
        // Cache the result for next time
        var cacheKey = context.HttpContext.Request.Path;
        _cache.Set(cacheKey, context.Result, TimeSpan.FromMinutes(5));
    }
}


// ----------------------------------------------------------
// 5b — Model Binding
// ----------------------------------------------------------
//
// Reads data from the request and maps it to your action's parameters.
// ASP.NET looks at parameter names and [From*] attributes to know
// where to read each value from.

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult GetProduct(
        int id,                             // from route:  /products/42
        [FromQuery] string? search,         // from query:  ?search=laptop
        [FromQuery] int page = 1,           // from query:  ?page=2
        [FromHeader] string? apiVersion,    // from header: X-Api-Version: 2
        [FromServices] ILogger<ProductsController> logger) // from DI container
    {
        return Ok();
    }

    [HttpPost]
    public IActionResult CreateProduct(
        [FromBody] CreateProductRequest request, // from JSON request body
        [FromQuery] bool dryRun = false)         // from query string
    {
        return Ok();
    }
}

// Model binding sources checked in order (if no [From*] attribute):
//   1. Route values        → {id} in [HttpGet("{id}")]
//   2. Query string        → ?page=1
//   3. Form data           → for form submissions
//
// For complex objects without [FromBody], binding tries route+query.
// [FromBody] explicitly reads and deserializes the JSON request body.
//
// The JSON deserializer used:
//   System.Text.Json by default (.NET 5+)
//   Newtonsoft.Json if you call builder.Services.AddNewtonsoftJson()


// ----------------------------------------------------------
// 5c — Model Validation
// ----------------------------------------------------------
//
// After model binding, DataAnnotations on your model are checked.
// With [ApiController], validation failures AUTO-RETURN 400.
// Your action method NEVER runs if validation fails.

public class CreateProductRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; }

    [Required]
    [Range(0.01, 99999.99, ErrorMessage = "Price must be between 0.01 and 99999.99")]
    public decimal Price { get; set; }

    [Required]
    public int CategoryId { get; set; }
}

// If Name is missing, ASP.NET automatically returns:
// HTTP 400 Bad Request
// {
//   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
//   "title": "One or more validation errors occurred.",
//   "status": 400,
//   "errors": {
//     "Name": ["Name is required"]
//   }
// }
// Your action method is NEVER called.

// Without [ApiController], you check manually:
[HttpPost]
public IActionResult CreateProduct([FromBody] CreateProductRequest request)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    // continue...
    return Ok();
}


// ----------------------------------------------------------
// 5d — Action Filters (Before execution)
// ----------------------------------------------------------
//
// Run after model binding and validation but BEFORE your action.
// Can read/modify bound parameters.
// Can short-circuit by setting context.Result.

public class RequestLoggingFilter : IActionFilter
{
    private readonly ILogger<RequestLoggingFilter> _logger;
    private Stopwatch _stopwatch;

    public RequestLoggingFilter(ILogger<RequestLoggingFilter> logger)
        => _logger = logger;

    // Runs BEFORE action method
    public void OnActionExecuting(ActionExecutingContext context)
    {
        _stopwatch = Stopwatch.StartNew();

        // context.ActionArguments contains the bound parameters
        // e.g. { "id": 42, "search": "laptop" }
        _logger.LogInformation(
            "Executing {Action} with args {@Args}",
            context.ActionDescriptor.DisplayName,
            context.ActionArguments);

        // To short-circuit (skip action method):
        // context.Result = new BadRequestObjectResult("Custom validation failed");
    }

    // Runs AFTER action method
    public void OnActionExecuted(ActionExecutedContext context)
    {
        _stopwatch.Stop();

        // context.Result contains the IActionResult returned by the action
        // context.Exception contains any exception thrown (if not handled)

        _logger.LogInformation(
            "Executed {Action} in {Ms}ms",
            context.ActionDescriptor.DisplayName,
            _stopwatch.ElapsedMilliseconds);
    }
}

// Register globally (applies to all actions):
// builder.Services.AddControllers(options =>
//     options.Filters.Add<RequestLoggingFilter>());
//
// Or per controller/action:
// [ServiceFilter(typeof(RequestLoggingFilter))]


// ----------------------------------------------------------
// 5e — Action Method Executes (YOUR code)
// ----------------------------------------------------------
//
// After all the layers above, your actual business logic finally runs.
// This is what most developers think of as "the API" —
// but it is actually the LAST step in a long chain.

[HttpGet("{id}")]
public async Task<IActionResult> GetProduct(
    int id,
    [FromServices] IProductService productService)
{
    // By the time this runs:
    //   - TCP and TLS are already handled (Layer 1)
    //   - Kestrel parsed the HTTP request (Layer 2)
    //   - HttpContext was created (Layer 3)
    //   - Exception handler is wrapping everything (Layer 4a)
    //   - HTTPS was confirmed (Layer 4b)
    //   - Route was matched (Layer 4d)
    //   - User identity was established (Layer 4e)
    //   - User was authorized to access this endpoint (Layer 4f)
    //   - id parameter was bound from route values (Layer 5b)
    //   - Model validation passed (Layer 5c)
    //   - Action filters ran their OnActionExecuting (Layer 5d)
    //
    // Now your code actually runs:

    var product = await productService.GetByIdAsync(id);

    if (product is null)
        return NotFound(new ProblemDetails
        {
            Status = 404,
            Title = "Product not found",
            Detail = $"Product with id {id} does not exist"
        });

    return Ok(product); // returns IActionResult — NOT a response yet
}


// ----------------------------------------------------------
// 5f — Result Filters and Result Execution
// ----------------------------------------------------------
//
// The IActionResult returned from your action is NOT a response yet.
// It is just a C# object describing what the response SHOULD be.
// Result execution turns it into actual HTTP response bytes.
//
// Result filters run before and after this process.

public class ResponseEnrichmentFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        // Runs before result is executed (before response is written)
        // Good place to add response headers
        context.HttpContext.Response.Headers["X-Api-Version"] = "2.0";
        context.HttpContext.Response.Headers["X-Request-Id"] =
            context.HttpContext.TraceIdentifier;
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
        // Runs after result is executed (after response is written)
    }
}

// What happens during result execution for Ok(product):
//
// 1. Content Negotiation
//    Client sent: Accept: application/json
//    Server checks registered output formatters
//    Picks SystemTextJsonOutputFormatter
//
// 2. Serialization
//    product object → JSON bytes
//    using System.Text.Json serializer
//
// 3. Response written
//    context.Response.StatusCode = 200
//    context.Response.ContentType = "application/json; charset=utf-8"
//    context.Response.Body ← JSON bytes written here


// ============================================================
// LAYER 6 — RESPONSE TRAVELS BACK THROUGH MIDDLEWARE (REVERSE)
// ============================================================
//
// After the endpoint writes the response, execution returns
// back through each middleware in REVERSE order.
// Remember: code AFTER "await next(context)" runs on the way back.

// Example — response timing middleware shows bidirectional nature:
app.Use(async (context, next) =>
{
    // ↓ REQUEST PATH — runs before endpoint
    var stopwatch = Stopwatch.StartNew();
    var path = context.Request.Path;

    await next(context); // ← entire pipeline runs here

    // ↑ RESPONSE PATH — runs after endpoint on the way back
    stopwatch.Stop();

    // Response is being sent back to client
    // Can still add headers at this point (if not already sent)
    if (!context.Response.HasStarted)
    {
        context.Response.Headers["X-Response-Time-Ms"] =
            stopwatch.ElapsedMilliseconds.ToString();
    }

    Console.WriteLine($"{path} completed in {stopwatch.ElapsedMilliseconds}ms" +
                      $" with status {context.Response.StatusCode}");
});

// Response compression example — modifies response on the way back:
app.UseResponseCompression();
// Internally wraps context.Response.Body with a GZip/Brotli stream
// As response bytes flow back through this middleware,
// they are compressed before being sent to Kestrel


// ============================================================
// LAYER 7 — KESTREL WRITES RESPONSE TO SOCKET
// ============================================================
//
// The response headers and body have been written to HttpResponse.
// Kestrel serializes it back into raw HTTP bytes:
//
//   "HTTP/1.1 200 OK\r\n
//    Content-Type: application/json; charset=utf-8\r\n
//    Content-Length: 87\r\n
//    X-Response-Time-Ms: 12\r\n
//    \r\n
//    {"id":42,"name":"Laptop","price":999.99,"categoryId":3}"
//
// These bytes are:
//   1. Encrypted using the TLS session keys
//   2. Written to the TCP socket
//   3. Sent to the client
//
// HTTP Keep-Alive:
//   If the client sent "Connection: keep-alive" (default in HTTP/1.1),
//   the TCP connection stays open for subsequent requests.
//   No new TCP/TLS handshake needed for the next request.
//   Significant performance benefit for multiple requests to same server.


// ============================================================
// LAYER 8 — CLEANUP AND DISPOSAL
// ============================================================
//
// After the response is sent, ASP.NET disposes the request scope.
// This triggers IDisposable.Dispose() on all Scoped services
// that were resolved during this request.

// What gets disposed:
//   - AppDbContext → returns database connection back to the pool
//   - Any IDisposable Scoped services
//   - Any IAsyncDisposable Scoped services

// The connection pool is key for performance:
//   Opening a real DB connection is expensive (~100ms+)
//   DbContext does NOT hold a real connection open the whole request
//   It borrows a connection from the pool when needed for a query
//   Returns it immediately after the query completes
//   When DbContext is disposed, any borrowed connection is returned to pool
//   Next request gets a connection from pool instantly (~1ms)

// This is why NEVER inject DbContext as Singleton:
public class WrongWayService  // DON'T DO THIS
{
    private readonly AppDbContext _db; // Singleton holds DbContext forever

    public WrongWayService(AppDbContext db) => _db = db;
    // _db is never disposed
    // Connection pool is starved
    // Stale data — same context tracks entities across all requests
    // Thread safety issues — DbContext is not thread-safe
}

public class RightWayService  // DO THIS
{
    private readonly IServiceScopeFactory _scopeFactory;

    public RightWayService(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public async Task DoWorkAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // db is fresh, will be disposed when scope is disposed
        var products = await db.Products.ToListAsync();
    } // scope disposed here → db disposed → connection returned to pool
}


// ============================================================
// PUTTING IT ALL TOGETHER — ANNOTATED PROGRAM.CS
// ============================================================

var builderFinal = WebApplication.CreateBuilder(args);

// --- SERVICE REGISTRATION (happens before Build()) ---
builderFinal.Services.AddControllers(options =>
{
    options.Filters.Add<RequestLoggingFilter>(); // global action filter
    options.Filters.Add<ResponseEnrichmentFilter>(); // global result filter
});
builderFinal.Services.AddDbContext<AppDbContext>();
builderFinal.Services.AddScoped<IProductService, ProductService>();
builderFinal.Services.AddAuthentication().AddJwtBearer();
builderFinal.Services.AddAuthorization();
builderFinal.Services.AddExceptionHandler<GlobalExceptionHandler>();
builderFinal.Services.AddProblemDetails();
builderFinal.Services.AddResponseCompression();

var appFinal = builderFinal.Build(); // DI container compiled, pipeline ready to configure

// --- MIDDLEWARE PIPELINE (happens after Build()) ---
// Each line below = one step in the request lifecycle

appFinal.UseExceptionHandler();      // Layer 4a — catch all unhandled exceptions
appFinal.UseResponseCompression();   // Layer 4b — compress responses on the way back
appFinal.UseHttpsRedirection();      // Layer 4c — redirect HTTP → HTTPS
appFinal.UseStaticFiles();           // Layer 4d — serve wwwroot files, skip rest
appFinal.UseRouting();               // Layer 4e — match URL to endpoint, store in context
appFinal.UseAuthentication();        // Layer 4f — establish identity (populate context.User)
appFinal.UseAuthorization();         // Layer 4g — check permissions against endpoint metadata
appFinal.MapControllers();           // Layer 5  — execute matched controller action

appFinal.Run(); // Start Kestrel, listen for requests, block until shutdown


// ============================================================
// INTERVIEW QUESTIONS AND ANSWERS
// ============================================================

// Q: What is the first thing that happens when a request hits ASP.NET Core?
// A: The OS handles TCP and TLS handshake first. Then Kestrel reads raw bytes
//    from the socket, parses the HTTP protocol, and creates an HttpContext.
//    That HttpContext is then passed into the middleware pipeline.

// Q: What is HttpContext?
// A: The central object representing a single request/response cycle.
//    Contains HttpRequest, HttpResponse, context.User (identity),
//    RequestServices (per-request DI scope), route data, Items dictionary,
//    and RequestAborted CancellationToken. Everything in the pipeline
//    reads from and writes to this object.

// Q: What is the difference between UseRouting and MapControllers?
// A: UseRouting inspects the URL and HTTP method, finds the matching endpoint,
//    and stores it in HttpContext. It does NOT execute anything.
//    MapControllers registers endpoints and defines WHERE in the pipeline
//    they execute. The separation lets Authorization read the matched
//    endpoint's [Authorize] metadata BEFORE execution.

// Q: Why must UseAuthentication come before UseAuthorization?
// A: Authentication establishes WHO the user is by populating context.User.
//    Authorization checks WHAT that user can do. You cannot authorize
//    an unknown identity — the identity must be established first.

// Q: Why must UseRouting come before UseAuthorization?
// A: Authorization reads the matched endpoint's metadata ([Authorize] attributes,
//    policy names). If UseRouting hasn't run, context.GetEndpoint() is null
//    and Authorization has nothing to evaluate against.

// Q: What is model binding?
// A: The process of reading data from the request (route values, query string,
//    headers, body) and mapping it to action method parameters.
//    [FromRoute], [FromQuery], [FromHeader], [FromBody] control the source.
//    For [FromBody], a JSON input formatter deserializes the body.

// Q: When does model validation run and what happens on failure?
// A: After model binding, before the action method. DataAnnotations on your
//    model classes are checked. With [ApiController], validation failures
//    automatically return 400 ValidationProblemDetails. Without it,
//    you check ModelState.IsValid manually in your action.

// Q: What is the difference between action filters and middleware?
// A: Middleware runs at the HTTP pipeline level — it sees every request
//    regardless of routing, has no knowledge of controllers or actions.
//    Action filters run inside the MVC pipeline — only for requests that
//    reach a controller action, with access to action arguments, model state,
//    and ActionContext. Use middleware for cross-cutting concerns (logging,
//    CORS, auth). Use filters for MVC-specific concerns (validation, auditing).

// Q: When is DbContext disposed?
// A: At the end of the request when context.RequestServices (the request scope)
//    is disposed. DbContext is Scoped — created once per request, disposed
//    when the scope ends. Disposal returns the database connection back
//    to the connection pool for the next request to use.

// Q: What is content negotiation?
// A: The process of choosing the response format based on the client's
//    Accept header. Client sends "Accept: application/json" → server uses
//    JSON formatter. Client sends "Accept: application/xml" → server uses
//    XML formatter (if registered). If no match, defaults to JSON.

// Q: What does RequestAborted CancellationToken do?
// A: It fires if the client disconnects before the response is sent.
//    Pass it to async methods (database queries, HTTP calls) so they
//    stop doing work when the client is no longer waiting.
//    Prevents wasting server resources on abandoned requests.

[HttpGet("{id}")]
public async Task<IActionResult> GetProductWithCancellation(
    int id,
    CancellationToken cancellationToken) // ASP.NET binds this from HttpContext.RequestAborted
{
    // If client disconnects, db query is cancelled automatically
    var product = await _productService.GetByIdAsync(id, cancellationToken);
    return Ok(product);
}

// Q: are filters part of middleware?
// Fundamentally yes. MapControllers is itself a middleware,
// and everything inside the MVC pipeline — model binding, filters, action execution — is code running inside that middleware.
// The distinction people draw between "middleware pipeline" and "MVC pipeline" is really about levels of abstraction.
// Middleware gives you raw HTTP access via HttpContext. Filters give you richer MVC context like action arguments and model state.
// Same underlying mechanism, different abstraction level.