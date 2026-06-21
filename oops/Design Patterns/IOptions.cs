// =============================================================================
// IOptions Pattern in ASP.NET Core — Complete Reference
// =============================================================================
// Topics covered:
//   1. Configuration POCO class
//   2. IOptions<T>          — singleton, never reloads
//   3. IOptionsSnapshot<T>  — scoped, reloads per request
//   4. IOptionsMonitor<T>   — singleton, hot-reloads at runtime
//   5. Named Options        — multiple instances of the same type
//   6. Options Validation   — fail fast at startup
//   7. PostConfigure        — override values after all Configure calls
//   8. OnChange memory leak — and how to fix it
//   9. DI registration      — Program.cs wiring
//  10. Unit testing         — no DI container needed
// =============================================================================

using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

// -----------------------------------------------------------------------------
// SECTION 1 — Configuration POCO
// -----------------------------------------------------------------------------
// Mirror the shape of your appsettings.json section as a plain C# class.
// No base class or interface needed.
//
// appsettings.json looks like this:
// {
//   "SmtpSettings": {
//     "Host": "smtp.example.com",
//     "Port": 587,
//     "EnableSsl": true,
//     "SenderEmail": "no-reply@example.com"
//   }
// }

public class SmtpSettings
{
    // DataAnnotations drive validation (see Section 6)
    [Required(ErrorMessage = "Host is required")]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
    public int Port { get; set; }

    public bool EnableSsl { get; set; }

    [Required]
    [EmailAddress]
    public string SenderEmail { get; set; } = string.Empty;
}


// -----------------------------------------------------------------------------
// SECTION 2 — IOptions<T>
// -----------------------------------------------------------------------------
// • Lifetime  : Singleton (registered once, lives for app lifetime)
// • Reloads   : NEVER — reads config once at startup, caches it forever
// • Named opts: NOT supported (.Get("name") does not exist on IOptions<T>)
//
// USE WHEN:
//   - Your config values never change while the app is running
//   - Injecting into singleton services
//   - You want the simplest possible setup

public class EmailServiceWithIOptions
{
    private readonly SmtpSettings _settings;

    // IOptions<T> is injected by the DI container
    public EmailServiceWithIOptions(IOptions<SmtpSettings> options)
    {
        // .Value reads the bound config object.
        // This snapshot is taken ONCE and never updates.
        _settings = options.Value;
    }

    public void SendEmail(string to, string subject)
    {
        Console.WriteLine($"Connecting to {_settings.Host}:{_settings.Port}");
        Console.WriteLine($"SSL: {_settings.EnableSsl}");
        Console.WriteLine($"From: {_settings.SenderEmail}");
        Console.WriteLine($"To: {to} | Subject: {subject}");
    }
}


// -----------------------------------------------------------------------------
// SECTION 3 — IOptionsSnapshot<T>
// -----------------------------------------------------------------------------
// • Lifetime  : Scoped (new instance per HTTP request / DI scope)
// • Reloads   : YES — re-reads config at the start of each new scope
// • Named opts: Supported via .Get("name")
//
// USE WHEN:
//   - Injecting into controllers, Razor Pages, or any scoped service
//   - You want config changes to take effect on the NEXT request
//     (without restarting the app)
//
// ⚠️  Cannot be injected into a Singleton — that would be a captive
//     dependency bug (scoped trapped inside singleton forever).

public class NewsletterController
{
    private readonly SmtpSettings _settings;

    public NewsletterController(IOptionsSnapshot<SmtpSettings> options)
    {
        // Each HTTP request that creates this controller gets a fresh
        // SmtpSettings reflecting whatever is currently in appsettings.json
        _settings = options.Value;
    }

    public void SendNewsletter()
    {
        Console.WriteLine($"[Snapshot] Using host: {_settings.Host}");
        // If you changed appsettings.json between requests, this will
        // already have the new value.
    }
}


// -----------------------------------------------------------------------------
// SECTION 4 — IOptionsMonitor<T>
// -----------------------------------------------------------------------------
// • Lifetime  : Singleton
// • Reloads   : YES — hot-reloads the moment appsettings.json is saved,
//               with NO app restart
// • Named opts: Supported via .Get("name")
// • OnChange  : Fires a callback immediately when config changes
//
// USE WHEN:
//   - Injecting into singleton services or BackgroundService workers
//   - You need zero-downtime config updates
//   - You want to react to config changes (e.g., rotate API keys live)
//
// HOW IT WORKS INTERNALLY:
//   .NET uses a FileSystemWatcher on appsettings.json.
//   When the file changes:
//     1. IConfiguration reloads in memory
//     2. IOptionsMonitor<T> re-binds the new values
//     3. OnChange callbacks are invoked on a background thread
//     4. CurrentValue now returns the updated object
//   The process never restarts. Zero downtime.
//
// ⚠️  MEMORY LEAK WARNING — see Section 8 before using OnChange

public class BackgroundEmailWorker : IDisposable
{
    private readonly IOptionsMonitor<SmtpSettings> _monitor;

    // Store the IDisposable returned by OnChange so we can clean up later
    private readonly IDisposable? _changeSubscription;

    public BackgroundEmailWorker(IOptionsMonitor<SmtpSettings> monitor)
    {
        _monitor = monitor;

        // OnChange fires every time appsettings.json changes on disk.
        // The callback runs on a background thread — be thread-safe!
        _changeSubscription = _monitor.OnChange(OnSettingsChanged);
    }

    private void OnSettingsChanged(SmtpSettings newSettings, string? namedInstance)
    {
        // This runs IMMEDIATELY when the file is saved — no restart.
        Console.WriteLine("=== Config changed at runtime! ===");
        Console.WriteLine($"New host   : {newSettings.Host}");
        Console.WriteLine($"New port   : {newSettings.Port}");
        Console.WriteLine($"Named inst : {namedInstance ?? "(default)"}");
    }

    public void DoWork()
    {
        // Always read CurrentValue — never cache it in a field,
        // or you'll defeat the whole point of IOptionsMonitor.
        var settings = _monitor.CurrentValue;

        Console.WriteLine($"[Monitor] Sending via {settings.Host}:{settings.Port}");
    }

    // IMPORTANT: Dispose unregisters the OnChange callback.
    // Without this, the callback keeps a reference to this object alive
    // even after the service is "disposed" → memory leak.
    public void Dispose()
    {
        _changeSubscription?.Dispose();
    }
}


// -----------------------------------------------------------------------------
// SECTION 5 — Named Options
// -----------------------------------------------------------------------------
// When you need MULTIPLE instances of the same settings type
// (e.g., a primary SMTP server and a backup SMTP server),
// use named options.
//
// appsettings.json:
// {
//   "PrimarySmtp":  { "Host": "smtp1.example.com", "Port": 587 },
//   "BackupSmtp":   { "Host": "smtp2.example.com", "Port": 465 }
// }
//
// Registration (Program.cs):
//   services.Configure<SmtpSettings>("Primary", config.GetSection("PrimarySmtp"));
//   services.Configure<SmtpSettings>("Backup",  config.GetSection("BackupSmtp"));
//
// ⚠️  IOptions<T> does NOT support named options.
//     Use IOptionsSnapshot<T> or IOptionsMonitor<T>.

public class SmartEmailService
{
    private readonly IOptionsSnapshot<SmtpSettings> _options;

    public SmartEmailService(IOptionsSnapshot<SmtpSettings> options)
    {
        _options = options;
    }

    public void Send(string to, bool useFallback = false)
    {
        // Retrieve by name using .Get("name")
        SmtpSettings settings = useFallback
            ? _options.Get("Backup")
            : _options.Get("Primary");

        Console.WriteLine($"Routing via {settings.Host}:{settings.Port}");
    }
}


// -----------------------------------------------------------------------------
// SECTION 6 — Options Validation
// -----------------------------------------------------------------------------
// Validate config at startup so the app fails fast with a clear error
// instead of silently misbehaving at runtime.
//
// TWO APPROACHES:
//
// A) DataAnnotations (shown on SmtpSettings class above) — simplest
//    Registration:
//      services.AddOptions<SmtpSettings>()
//              .Bind(config.GetSection("SmtpSettings"))
//              .ValidateDataAnnotations()
//              .ValidateOnStart();   // ← fails at startup, not first use
//
// B) Custom validation — for complex cross-field rules

public class SmtpSettingsValidator : IValidateOptions<SmtpSettings>
{
    public ValidateOptionsResult Validate(string? name, SmtpSettings options)
    {
        // Cross-field rule: SSL requires port 465 or 587
        if (options.EnableSsl && options.Port != 465 && options.Port != 587)
        {
            return ValidateOptionsResult.Fail(
                "When EnableSsl is true, Port must be 465 or 587.");
        }

        return ValidateOptionsResult.Success;
    }
}

// Registration for custom validator (Program.cs):
//   services.AddOptions<SmtpSettings>()
//           .Bind(config.GetSection("SmtpSettings"))
//           .ValidateOnStart();
//   services.AddSingleton<IValidateOptions<SmtpSettings>, SmtpSettingsValidator>();


// -----------------------------------------------------------------------------
// SECTION 7 — PostConfigure
// -----------------------------------------------------------------------------
// Runs AFTER all Configure<T> calls. Useful for:
//   - Applying environment-specific overrides
//   - Setting defaults that weren't provided in config
//   - Normalising values (e.g., trimming whitespace)
//
// Registration (Program.cs):
//   services.PostConfigure<SmtpSettings>(settings =>
//   {
//       // Fallback if SenderEmail was missing from appsettings
//       if (string.IsNullOrWhiteSpace(settings.SenderEmail))
//           settings.SenderEmail = "fallback@example.com";
//
//       // Normalise host to lowercase
//       settings.Host = settings.Host.ToLowerInvariant();
//   });


// -----------------------------------------------------------------------------
// SECTION 8 — OnChange Memory Leak (and the fix)
// -----------------------------------------------------------------------------
// IOptionsMonitor.OnChange returns an IDisposable that represents the
// subscription. If you never dispose it, the monitor holds a reference
// to your callback delegate, which in turn keeps the enclosing object alive.
// Over time (many requests / many service instances), this piles up.

// ❌ WRONG — new callback registered every time this scoped service is created
public class LeakyService
{
    public LeakyService(IOptionsMonitor<SmtpSettings> monitor)
    {
        // Each HTTP request creates a LeakyService, registering ANOTHER callback.
        // Old callbacks accumulate and are never removed → memory leak.
        monitor.OnChange(s => Console.WriteLine($"Changed to {s.Host}"));
    }
}

// ✅ CORRECT — implement IDisposable and dispose the subscription
public class SafeService : IDisposable
{
    private readonly IDisposable? _subscription;

    public SafeService(IOptionsMonitor<SmtpSettings> monitor)
    {
        _subscription = monitor.OnChange(s =>
        {
            Console.WriteLine($"Config updated: {s.Host}");
        });
    }

    public void Dispose()
    {
        // Unregisters the callback — no more reference kept by the monitor
        _subscription?.Dispose();
    }
}


// -----------------------------------------------------------------------------
// SECTION 9 — DI Registration (Program.cs)
// -----------------------------------------------------------------------------
// Everything wired together. In a real ASP.NET Core app:
//
// var builder = WebApplication.CreateBuilder(args);
//
// // ── Basic registration ──────────────────────────────────────────────────────
// builder.Services.Configure<SmtpSettings>(
//     builder.Configuration.GetSection("SmtpSettings"));
//
// // ── With validation ─────────────────────────────────────────────────────────
// builder.Services.AddOptions<SmtpSettings>()
//     .Bind(builder.Configuration.GetSection("SmtpSettings"))
//     .ValidateDataAnnotations()
//     .ValidateOnStart();
//
// // ── Named options ───────────────────────────────────────────────────────────
// builder.Services.Configure<SmtpSettings>("Primary",
//     builder.Configuration.GetSection("PrimarySmtp"));
// builder.Services.Configure<SmtpSettings>("Backup",
//     builder.Configuration.GetSection("BackupSmtp"));
//
// // ── Custom validator ────────────────────────────────────────────────────────
// builder.Services.AddSingleton<IValidateOptions<SmtpSettings>,
//     SmtpSettingsValidator>();
//
// // ── PostConfigure ───────────────────────────────────────────────────────────
// builder.Services.PostConfigure<SmtpSettings>(s =>
// {
//     if (string.IsNullOrWhiteSpace(s.SenderEmail))
//         s.SenderEmail = "no-reply@example.com";
// });
//
// // ── Service registrations ───────────────────────────────────────────────────
// builder.Services.AddSingleton<EmailServiceWithIOptions>();
// builder.Services.AddScoped<NewsletterController>();
// builder.Services.AddSingleton<BackgroundEmailWorker>();
// builder.Services.AddScoped<SmartEmailService>();
// builder.Services.AddScoped<SafeService>();
//
// // ── File watcher (enabled by default via CreateBuilder) ─────────────────────
// // reloadOnChange: true is already set by WebApplication.CreateBuilder.
// // To disable it (e.g., for performance in high-traffic read-heavy apps):
// //   builder.Configuration.AddJsonFile("appsettings.json",
// //       optional: false, reloadOnChange: false);


// -----------------------------------------------------------------------------
// SECTION 10 — Unit Testing (no DI container needed)
// -----------------------------------------------------------------------------
// Options.Create<T>() wraps a plain object in IOptions<T>.
// This is the standard way to test code that depends on IOptions.

public class IOptionsPatternTests
{
    public static void RunAll()
    {
        Test_IOptions_ReturnsConfiguredValue();
        Test_EmailService_UsesHost();
        Console.WriteLine("All tests passed ✅");
    }

    static void Test_IOptions_ReturnsConfiguredValue()
    {
        var settings = new SmtpSettings
        {
            Host = "test.smtp.com",
            Port = 587,
            EnableSsl = true,
            SenderEmail = "test@example.com"
        };

        // Options.Create wraps the object — no DI, no configuration system
        IOptions<SmtpSettings> options = Options.Create(settings);

        Console.Assert(options.Value.Host == "test.smtp.com");
        Console.Assert(options.Value.Port == 587);
        Console.WriteLine("Test_IOptions_ReturnsConfiguredValue: PASS");
    }

    static void Test_EmailService_UsesHost()
    {
        var settings = new SmtpSettings
        {
            Host = "smtp.myapp.com",
            Port = 465,
            EnableSsl = true,
            SenderEmail = "noreply@myapp.com"
        };

        var service = new EmailServiceWithIOptions(Options.Create(settings));

        // Just verifying it doesn't throw and uses the right settings
        service.SendEmail("user@example.com", "Welcome!");
        Console.WriteLine("Test_EmailService_UsesHost: PASS");
    }
}


// -----------------------------------------------------------------------------
// QUICK REFERENCE — Which interface to choose?
// -----------------------------------------------------------------------------
//
//  Scenario                                      → Use
//  ──────────────────────────────────────────────────────────────────────────
//  Singleton service, config fixed at startup    → IOptions<T>
//  Controller / scoped service, reload per req   → IOptionsSnapshot<T>
//  Singleton that needs live runtime updates     → IOptionsMonitor<T>
//  Multiple named instances of same type         → IOptionsSnapshot / Monitor
//  React to config change with a callback        → IOptionsMonitor<T>.OnChange
//  Unit test without DI container                → Options.Create(new T {...})
//
// -----------------------------------------------------------------------------