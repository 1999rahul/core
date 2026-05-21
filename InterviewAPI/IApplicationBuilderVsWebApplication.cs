namespace ASPNETCONCEPTS
{
    public class IApplicationBuilderVsWebApplication
    {
        public IApplicationBuilderVsWebApplication()
        {
            // ============================================================
            //   IApplicationBuilder vs WebApplication
            //   ASP.NET Core — Interview Notes
            // ============================================================

            // Background — Why This Topic Matters
            // Before .NET 6, ASP.NET Core used a two-class model to bootstrap an application.
            // You had Program.cs which was responsible only for creating and running the host,
            // and Startup.cs which was responsible for everything else — registering services
            // and configuring the middleware pipeline. These two responsibilities were split across two files and two methods,
            // which was verbose and made simple things unnecessarily complicated.
            // In .NET 6, Microsoft introduced a new minimal hosting model with a single class called WebApplication that unified everything into one file and one linear flow.

            // The Old Model — Startup.cs and IApplicationBuilder
            // Program.cs in .NET 5
            /*
            public class Program
            {
                public static void Main(string[] args)
                {
                    Host.CreateDefaultBuilder(args)
                        .ConfigureWebHostDefaults(webBuilder =>
                        {
                            webBuilder.UseStartup<Startup>();
                        })
                        .Build()
                        .Run();
                }
            }
            This file did only one thing — create a generic host, point it at the Startup class, build it, and run it. 
            All the real configuration lived in Startup.cs.

            // Startup.cs in .NET 5

            public class Startup
            {
                public IConfiguration Configuration { get; }

                public Startup(IConfiguration configuration)
                {
                    Configuration = configuration;
                }

                // STEP 1 — Register services into the DI container
                public void ConfigureServices(IServiceCollection services)
                {
                    services.AddControllers();
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlServer(Configuration.GetConnectionString("Default")));
                    services.AddScoped<IProductService, ProductService>();
                    services.AddSingleton<ICacheService, CacheService>();
                }

                // STEP 2 — Configure the middleware pipeline
                // IApplicationBuilder is injected here by the framework
                public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
                {
                    if (env.IsDevelopment())
                    {
                        app.UseDeveloperExceptionPage();
                    }

                    app.UseHttpsRedirection();
                    app.UseStaticFiles();
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();

                    // In .NET 5, endpoint mapping was done inside UseEndpoints
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                        endpoints.MapHealthChecks("/health");
                    });
                }

            What is IApplicationBuilder exactly?
            IApplicationBuilder is an interface. Its sole responsibility is to let you compose middleware components into a request processing pipeline. It knows nothing about hosting, nothing about the DI container setup, and nothing about configuration. It just builds the pipeline.
            Internally it maintains a list of middleware components. When you call Use(), Run(), or Map(), you are adding to that list. When Build() is called on it, it chains all those components together into a single RequestDelegate — which is the final pipeline that processes every request.
            }

            -> Problems with the Startup.cs model

            -> Problem 1 — Artificial separation. ConfigureServices and Configure are two different methods but they configure one logical thing — your application. You cannot easily share a variable between them without making it a field on the Startup class.
            public class Startup
            {
                // Awkward — have to make it a field just to share between two methods
                private bool _isProduction;

                public void ConfigureServices(IServiceCollection services)
                {
                    _isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";
                    // use _isProduction here
                }

                public void Configure(IApplicationBuilder app)
                {
                    if (_isProduction)
                    {
                        // use _isProduction here
                    }
                }
            }

            -> Problem 4 — Two mental models. You had IServiceCollection for services (in ConfigureServices) and IApplicationBuilder for the pipeline (in Configure). Developers always had to remember which thing goes where.
            */

            // The New Model — WebApplication (.NET 6+)

            // Program.cs in .NET 6+
            /*
             ------------------------------------------------------------------------------------
            var builder = WebApplication.CreateBuilder(args);

            // Register services — replaces ConfigureServices
            builder.Services.AddControllers();
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
            builder.Services.AddScoped<IProductService, ProductService>();

            var app = builder.Build();

            // Configure middleware — replaces Configure(IApplicationBuilder app)
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHealthChecks("/health");

            app.Run();

            -> This is the complete application. No Startup.cs, no UseEndpoints, no separate host builder. Everything reads top to bottom.
            -------------------------------------------------------------------------------------
             What is WebApplication exactly?
                -> WebApplication is a sealed class introduced in .NET 6. 
                -> The key thing to understand is that it does NOT replace IApplicationBuilder — it implements it. 
                -> WebApplication is a superset that combines multiple interfaces into a single object.
             
            What WebApplication implements internally (simplified)
            public sealed class WebApplication :
                IHost,                  // Start(), StopAsync(), Dispose()
                IApplicationBuilder,    // Use(), Run(), Map() — the middleware pipeline
                IEndpointRouteBuilder,  // MapGet(), MapPost(), MapControllers()
                IAsyncDisposable
            {
                // Everything in one place
            }

            This means:

            -> When you call app.Use() — you are calling IApplicationBuilder.Use()
            -> When you call app.MapGet() — you are calling IEndpointRouteBuilder extension methods
            -> When you call app.Run() — you are calling IHost start (not IApplicationBuilder.Run() — different method, same name, different interface)
            -> When you call app.Services — you are accessing the built DI container
             
            What is WebApplicationBuilder?
                -> WebApplicationBuilder is the object returned by WebApplication.
                -> CreateBuilder(args). It is the setup/configuration phase before the app is built. 
                -> It exposes several sub-objects for configuring different aspects:

            var builder = WebApplication.CreateBuilder(args)
            builder.Services     → IServiceCollection — register DI services
            builder.Configuration → IConfigurationManager — add config sources
            builder.Logging      → ILoggingBuilder — configure logging
            builder.Host         → IHostBuilder — configure the generic host
            builder.WebHost      → IWebHostBuilder — configure the web server
            var app = builder.Build(); // locks the DI container, returns WebApplication

            builder.Services.AddScoped<IBar, Bar>(); // WRONG — throws exception
            // The DI container is compiled and frozen after Build()
            // You cannot register new services after this point

            app.UseRouting();        // OK — pipeline configuration is after Build()
            app.MapControllers();    // OK
            app.Run();
             */

            // ---------------------------------------------
            // Accessing Services — Both Models
            // ---------------------------------------------    

            /*
            // In Startup.Configure — IApplicationBuilder has ApplicationServices
            // but it's the root provider, not a scoped provider
            public void Configure(IApplicationBuilder app)
            {
                // Getting services from root — only safe for Singleton services
                var cache = app.ApplicationServices.GetRequiredService<ICacheService>();

                app.Use(async (context, next) =>
                {
                    // context.RequestServices is the per-request scoped container
                    // Safe for Scoped services like DbContext
                    var db = context.RequestServices.GetRequiredService<AppDbContext>();
                    await next(context);
                });
            }

            In the new model

            var app = builder.Build();

            // app.Services is the root DI container — Singleton services only
            var cache = app.Services.GetRequiredService<ICacheService>();

            // For Scoped services, always use a scope or context.RequestServices
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // use db here, scope disposes db when done

            app.Use(async (context, next) =>
            {
                // Per-request scope — safe for any lifetime
                var db = context.RequestServices.GetRequiredService<AppDbContext>();
                await next(context);
            });


            --------------------------------------------------------------------------------------
            The dotnet 5 and earlier model
            Host.CreateDefaultBuilder(args)
            │
            │  Creates IHostBuilder
            │  Sets up: config, logging, content root
            │
            .ConfigureWebHostDefaults(w => w.UseStartup<Startup>())
            │
            │  Adds HTTP/web capability on top of the generic host
            │  Sets up: Kestrel, IIS integration, web defaults
            │  Points to Startup class for service + pipeline config
            │
            .Build()
            │
            │  Calls Startup.ConfigureServices() → compiles DI container
            │  Calls Startup.Configure()        → builds middleware pipeline
            │  Returns a ready-but-idle IHost
            │
            .Run()
            │
            │  Starts Kestrel on port 5000/5001
            │  App is now live and handling requests
            │  Blocks until shutdown
            
            When you write the .NET 6+ version:

            var builder = WebApplication.CreateBuilder(args); // = CreateDefaultBuilder + ConfigureWebHostDefaults
            builder.Services.AddControllers();               // = Startup.ConfigureServices

            var app = builder.Build();                        // = .Build()

            app.UseRouting();                                 // = Startup.Configure
            app.MapControllers();

            app.Run();                                        // = .Run()
             */
        }
    }
}
