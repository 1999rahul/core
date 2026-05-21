using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.VisualBasic;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ASPNETCONCEPTS
{
    public class Middlewares
    {
        public Middlewares()
        {
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();

            // ----------------------------------------------------------
            // WHAT IS MIDDLEWARE?
            // ----------------------------------------------------------
            // Middleware is peice of code assembled into a pipeline to handle
            // requests and responses. Each component can:
            //   - Do work BEFORE the next component
            //   - Pass the request to the next component (await next)
            //   - Do work AFTER the next component returns
            //   - Short-circuit the pipeline (skip calling next)
            //
            // Request  → [MW1] → [MW2] → [MW3] → Endpoint
            // Response ← [MW1] ← [MW2] ← [MW3] ← Endpoint

            // Every middleware receives:
            //   - HttpContext  : the current request/response
            //   - RequestDelegate next : the next middleware in the chain

            // ----------------------------------------------------------
            // THREE WAYS TO ADD MIDDLEWARE
            // ----------------------------------------------------------

            // 1. Use — runs code BEFORE and AFTER the next middleware
            app.Use(async (context, next) =>
            {
                Console.WriteLine("Before next middleware");
                await next(context);                          // move forward
                Console.WriteLine("After next middleware");   // runs on the way back
            });

            // 2. Run — TERMINAL middleware, never calls next (short-circuits)
            app.Run(async context =>
            {
                await context.Response.WriteAsync("Hello from terminal middleware");
                // Nothing registered after this will run
            });

            // 3. Map — branches the pipeline based on path

            app.Map("/admin", adminApp =>
            {
                adminApp.Run(async context =>
                {
                    await context.Response.WriteAsync("Admin area");
                });
            });

            // ----------------------------------------------------------
            // CUSTOM MIDDLEWARE
            // ----------------------------------------------------------

            // Approach 1: Inline (quick, for simple cases)

            app.Use(async (context, next) =>
            {
                var start = DateTime.UtcNow;
                await next(context);
                var elapsed = DateTime.UtcNow - start;
                Console.WriteLine($"Request took {elapsed.TotalMilliseconds}ms");
            });

            // Approach 2: Class - based(recommended for real projects)

            /*
                public class RequestLoggingMiddleware
                {
                    private readonly RequestDelegate _next;
                    private readonly ILogger<RequestLoggingMiddleware> _logger;

                    // Dependencies injected via constructor (singleton lifetime)
                    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
                    {
                        _next = next;
                        _logger = logger;
                    }

                    // Must be named InvokeAsync or Invoke
                    // Scoped services injected here, not constructor
                    public async Task InvokeAsync(HttpContext context)
                    {
                        _logger.LogInformation("Incoming request: {Method} {Path}",
                            context.Request.Method,
                            context.Request.Path);

                        await _next(context); // call next middleware

                        _logger.LogInformation("Response status: {StatusCode}",
                            context.Response.StatusCode);
                    }
                }

            Then register it with an extension method (best practice):
            public static class RequestLoggingMiddlewareExtensions
            {
                public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
                {
                    return app.UseMiddleware<RequestLoggingMiddleware>();
                }
            }

            // In Program.cs — clean and readable
            app.UseRequestLogging();
             */

            // ----------------------------------------------------
            // Dependency Injection in Middleware — Important Gotcha
            // ----------------------------------------------------

            // Constructor injection — only works for Singleton services.
            // The middleware itself is instantiated once (singleton-like),
            // so if you inject a Scoped service in the constructor, i
            // it will be captured and reused across requests — this is the captive dependency problem.

            // InvokeAsync parameter injection — use this for Scoped services like DbContext, ICurrentUserService, etc.

            //----------------------------------------------------------
            // Middleware Order Matters
            // ----------------------------------------------------------

            app.UseExceptionHandler();     // 1. Catch all unhandled exceptions — must be first
            app.UseHttpsRedirection();     // 2. Redirect HTTP to HTTPS
            app.UseStaticFiles();          // 3. Short-circuit for static files early
            app.UseRouting();              // 4. Match routes
            app.UseAuthentication();       // 5. Who are you?
            app.UseAuthorization();        // 6. What are you allowed to do?
            // app.UseMiddleware<MyCustom>(); // 7. Your custom stuff
            app.MapControllers();

            // Key rules to remember:

            // Exception handler must be first so it can catch errors from everything below it
            // Authentication must come before authorization — you can't authorize an unknown user
            // Routing must come before authorization — authorization needs to know which endpoint was matched
            // Static files should come early to short-circuit before hitting auth / routing


            // Short-Circuiting the Pipeline
            // Sometimes you want to stop the pipeline entirely — for example in an IP blocklist middleware:

            /*
            public async Task InvokeAsync(HttpContext context)
            {
                var ip = context.Connection.RemoteIpAddress?.ToString();

                if (_blockedIps.Contains(ip))
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Forbidden");
                    return; // Don't call _next — pipeline stops here
                }

                await _next(context); // Allow through
            }
            */

            // IApplicationBuilder vs IMiddlewareFactory
            // There are actually two conventions for class-based middleware:

            // Convention-based (what we've been doing) — the class just needs InvokeAsync. No interface required. This is the most common approach.

            // IMiddleware interface — strongly typed, must be registered in DI as a service:

            /*
            public class StronglyTypedMiddleware : IMiddleware
            {
                // IMiddleware uses InvokeAsync with explicit signature
                public async Task InvokeAsync(HttpContext context, RequestDelegate next)
                {
                    // work here
                    await next(context);
                }
            }

            // Must register in DI — unlike convention-based middleware
            builder.Services.AddScoped<StronglyTypedMiddleware>();

            // Then use it
            app.UseMiddleware<StronglyTypedMiddleware>();
             */

            // The difference: IMiddleware is activated per-request via DI (so it supports Scoped lifetime naturally), while convention-based middleware is activated once at startup.

            // Best Practices Summary
            // Default choice is convention - based middleware.It's simpler and covers 90% of cases.
            // Switch to IMiddleware + AddScoped only when you need Scoped dependencies in the constructor(DbContext being the most common reason).
            // Never use AddTransient for middleware — no practical benefit over Scoped, and slightly more overhead.
            // If you find yourself registering IMiddleware as Singleton, ask yourself why you're not just using convention-based — the answer is almost always "I shouldn't be."

        }
    }
}
