// ============================================================
//  CLOSURES IN C# — Complete Reference
//  Topics covered:
//    1. What is a Closure?
//    2. How the Compiler Handles Closures
//    3. Multiple Closures Sharing a Variable
//    4. The Classic Loop Trap (Interview Favourite)
//    5. Real-World Use Cases
//       5a. UI / Event Handlers
//       5b. LINQ Queries
//       5c. Async / Task Continuations
//       5d. Factory Methods & Strategy Pattern
//       5e. ASP.NET Core Middleware
//       5f. Caching & Retry Logic
// ============================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ClosuresDemo
{
    // =========================================================
    // SECTION 1 — What is a Closure?
    // =========================================================
    // A closure is a lambda or delegate that CAPTURES variables
    // from its enclosing (outer) scope and keeps them alive even
    // after that scope has finished executing.
    //
    // Key rule: capture is BY REFERENCE — all closures that share
    // a variable see the SAME value, not a snapshot copy.
    // =========================================================

    public class Section1_BasicClosure
    {
        public static void Demo()
        {
            Console.WriteLine("=== SECTION 1: Basic Closure ===\n");

            Action counter = CreateCounter();
            counter(); // Count: 1
            counter(); // Count: 2
            counter(); // Count: 3

            Console.WriteLine();
        }

        // 'count' is a local variable that normally dies when
        // CreateCounter() returns. The closure keeps it alive.
        static Action CreateCounter()
        {
            int count = 0; // captured variable

            Action increment = () =>
            {
                count++;                            // closes over 'count'
                Console.WriteLine($"Count: {count}");
            };

            return increment; // method ends, but 'count' lives on the heap
        }
    }


    // =========================================================
    // SECTION 2 — What the Compiler Actually Does
    // =========================================================
    // The C# compiler rewrites the lambda into a hidden class
    // (called a "display class"). The captured variable becomes
    // a FIELD on that class, which lives on the heap — not the
    // stack — so it outlives the method frame.
    //
    // Equivalent generated code looks like this:
    // =========================================================

    // --- What YOU write ---
    //
    //   static Action CreateCounter()
    //   {
    //       int count = 0;
    //       return () => { count++; Console.WriteLine(count); };
    //   }
    //
    // --- What the COMPILER generates (simplified) ---
    //
    //   private class <>c__DisplayClass0
    //   {
    //       public int count;           // your local → a heap field
    //
    //       public void Lambda()
    //       {
    //           count++;
    //           Console.WriteLine(count);
    //       }
    //   }
    //
    //   static Action CreateCounter()
    //   {
    //       var obj = new <>c__DisplayClass0(); // allocated on heap
    //       obj.count = 0;
    //       return obj.Lambda;
    //   }
    //
    // This is why 'count' survives after CreateCounter() returns.


    // =========================================================
    // SECTION 3 — Multiple Closures Sharing One Variable
    // =========================================================
    // All lambdas that close over the SAME variable share the
    // same display-class field. Modifying it from one closure
    // is visible to all others.
    // =========================================================

    public class Section3_SharedVariable
    {
        public static void Demo()
        {
            Console.WriteLine("=== SECTION 3: Shared Variable ===\n");

            var (inc, dec, get) = CreateSharedCounter();

            inc(); inc(); inc(); // count = 3
            dec();               // count = 2

            Console.WriteLine($"Final count: {get()}"); // 2
            Console.WriteLine();
        }

        static (Action increment, Action decrement, Func<int> get) CreateSharedCounter()
        {
            int count = 0; // ONE variable shared by all three closures

            Action increment = () => count++;
            Action decrement = () => count--;
            Func<int> get = () => count;

            return (increment, decrement, get);
        }
    }


    // =========================================================
    // SECTION 4 — The Classic Loop Trap (Top Interview Question)
    // =========================================================
    // All loop lambdas capture the SAME loop variable 'i'.
    // By the time they run, the loop has finished and 'i' == 5.
    //
    // Fix: introduce a local copy inside each iteration so each
    // lambda gets its own display-class instance.
    // =========================================================

    public class Section4_LoopTrap
    {
        public static void Demo()
        {
            Console.WriteLine("=== SECTION 4: Loop Trap ===\n");

            // ❌ WRONG — all lambdas share the same 'i'
            var wrong = new List<Action>();
            for (int i = 0; i < 5; i++)
            {
                wrong.Add(() => Console.Write(i + " "));
            }
            Console.Write("Wrong output: ");
            wrong.ForEach(a => a()); // prints: 5 5 5 5 5
            Console.WriteLine();

            // ✅ CORRECT — each iteration captures its own copy
            var correct = new List<Action>();
            for (int i = 0; i < 5; i++)
            {
                int localCopy = i; // new variable each iteration = new display class
                correct.Add(() => Console.Write(localCopy + " "));
            }
            Console.Write("Correct output: ");
            correct.ForEach(a => a()); // prints: 0 1 2 3 4
            Console.WriteLine("\n");
        }
    }


    // =========================================================
    // SECTION 5a — Real World: UI / Event Handlers
    // =========================================================
    // Closures capture form-level state (current user, context)
    // so you don't need to store it as a class field just to
    // access it inside a button-click handler.
    // =========================================================

    public class Section5a_EventHandlers
    {
        // Simulated button class for demo purposes
        class Button { public event EventHandler? Click; public void SimulateClick() => Click?.Invoke(this, EventArgs.Empty); }
        class Label { public string Text { get; set; } = ""; }

        public void SetupForm(string userName, int userId)
        {
            Console.WriteLine("=== SECTION 5a: Event Handlers ===\n");

            string welcomeMsg = $"Hello, {userName}!";
            var btnSave = new Button();
            var btnDelete = new Button();
            var lblStatus = new Label();

            // Both handlers close over userName, userId, welcomeMsg, lblStatus
            btnSave.Click += (sender, e) =>
            {
                lblStatus.Text = welcomeMsg + " Data saved.";
                Console.WriteLine($"Save clicked for user {userId}: {lblStatus.Text}");
            };

            btnDelete.Click += (sender, e) =>
            {
                Console.WriteLine($"Delete clicked — removing user {userId} ({userName})");
            };

            btnSave.SimulateClick();
            btnDelete.SimulateClick();
            Console.WriteLine();
        }
    }


    // =========================================================
    // SECTION 5b — Real World: LINQ Queries
    // =========================================================
    // Every LINQ predicate you write (Where, Select, OrderBy…)
    // is a closure. Parameters from the calling method are
    // captured and used to build the query.
    // Entity Framework translates these closures into SQL.
    // =========================================================

    public class Section5b_LINQ
    {
        record Order(int Id, DateTime CreatedAt, decimal TotalAmount, string Status);

        public static void Demo()
        {
            Console.WriteLine("=== SECTION 5b: LINQ Closures ===\n");

            var orders = new List<Order>
            {
                new(1, DateTime.Now.AddDays(-1), 120m, "Completed"),
                new(2, DateTime.Now.AddDays(-2), 45m,  "Pending"),
                new(3, DateTime.Now.AddDays(-3), 300m, "Completed"),
                new(4, DateTime.Now.AddDays(-10), 80m, "Completed"),
            };

            // from, to, minAmount, status are ALL captured by the lambdas below
            var results = GetFilteredOrders(orders,
                from: DateTime.Now.AddDays(-5),
                to: DateTime.Now,
                minAmount: 100m,
                status: "Completed");

            Console.WriteLine("Filtered orders:");
            results.ForEach(o => Console.WriteLine($"  Order #{o.Id}  {o.TotalAmount:C}  {o.Status}"));

            // Projection closure — captures a discount rate
            decimal discountRate = 0.10m;
            var discounted = orders
                .Select(o => new
                {
                    o.Id,
                    Original = o.TotalAmount,
                    Discount = o.TotalAmount * discountRate,  // captures discountRate
                    FinalPrice = o.TotalAmount * (1 - discountRate)
                });

            Console.WriteLine("\nDiscounted prices:");
            foreach (var item in discounted)
                Console.WriteLine($"  Order #{item.Id}  Original:{item.Original:C}  After discount:{item.FinalPrice:C}");

            Console.WriteLine();
        }

        static List<Order> GetFilteredOrders(
            List<Order> source,
            DateTime from, DateTime to, decimal minAmount, string status)
        {
            return source
                .Where(o => o.CreatedAt >= from && o.CreatedAt <= to) // captures from, to
                .Where(o => o.TotalAmount >= minAmount)               // captures minAmount
                .Where(o => o.Status == status)                       // captures status
                .OrderByDescending(o => o.CreatedAt)
                .ToList();
        }
    }


    // =========================================================
    // SECTION 5c — Real World: Async / Task Continuations
    // =========================================================
    // Closures carry context ACROSS await boundaries. Variables
    // captured before an await are still accessible after it —
    // the compiler wires this up via a state machine that holds
    // references to the display class.
    // =========================================================

    public class Section5c_Async
    {
        public static async Task Demo()
        {
            Console.WriteLine("=== SECTION 5c: Async Continuations ===\n");

            await ProcessOrderAsync(orderId: 42, userId: "user-99");

            // ContinueWith example
            string jobName = "ReportGeneration";
            await Task.Run(() =>
            {
                Console.WriteLine($"  Running job: {jobName}");
                Task.Delay(10).Wait();
            })
            .ContinueWith(t =>
            {
                // jobName is still accessible here via closure
                if (t.IsCompletedSuccessfully)
                    Console.WriteLine($"  Job '{jobName}' completed successfully.");
                else
                    Console.WriteLine($"  Job '{jobName}' failed.");
            });

            Console.WriteLine();
        }

        static async Task ProcessOrderAsync(int orderId, string userId)
        {
            // Simulate async work
            await Task.Delay(1);

            // orderId and userId are captured — alive across all three awaits
            Console.WriteLine($"  Processing order #{orderId} for user {userId}...");

            await Task.Delay(1);
            Console.WriteLine($"  Payment charged for user {userId}.");

            await Task.Delay(1);
            // Still accessible after multiple awaits
            Console.WriteLine($"  Confirmation email sent: Order #{orderId} confirmed.");
        }
    }


    // =========================================================
    // SECTION 5d — Real World: Factory Methods & Strategy Pattern
    // =========================================================
    // Closures let you "bake in" configuration at factory time.
    // The returned delegate remembers its captured values —
    // every call uses them without re-reading config.
    // =========================================================

    public class Section5d_Factory
    {
        public static void Demo()
        {
            Console.WriteLine("=== SECTION 5d: Factory / Strategy Pattern ===\n");

            // minLength and requireSpecialChar are baked into each validator
            var strictValidator = CreatePasswordValidator(minLength: 12, requireSpecialChar: true);
            var simpleValidator = CreatePasswordValidator(minLength: 6, requireSpecialChar: false);

            string[] passwords = { "hi", "hello1", "MyP@ssword123", "NoSpecial123456" };

            foreach (var pw in passwords)
            {
                Console.WriteLine($"  \"{pw}\"");
                Console.WriteLine($"    Strict: {(strictValidator(pw) ? "pass" : "fail")}");
                Console.WriteLine($"    Simple: {(simpleValidator(pw) ? "pass" : "fail")}");
            }

            // Multiplier factory — partial application via closure
            Console.WriteLine();
            var triple = Multiplier(3);
            var double_ = Multiplier(2);
            Console.WriteLine($"  triple(5)  = {triple(5)}");
            Console.WriteLine($"  double_(5) = {double_(5)}");
            Console.WriteLine();
        }

        // Each call to CreatePasswordValidator returns a NEW closure
        // with its own captured copy of minLength and requireSpecialChar
        static Func<string, bool> CreatePasswordValidator(int minLength, bool requireSpecialChar)
        {
            return password =>
                password.Length >= minLength &&
                (!requireSpecialChar || password.Any(c => !char.IsLetterOrDigit(c)));
        }

        static Func<int, int> Multiplier(int factor)
        {
            return x => x * factor; // 'factor' is captured at creation time
        }
    }


    // =========================================================
    // SECTION 5e — Real World: Middleware / Pipeline Pattern
    // =========================================================
    // ASP.NET Core's entire pipeline is built with closures.
    // Each middleware captures 'next' (the rest of the pipeline)
    // and config values from startup scope.
    //
    // Shown here as a simplified pipeline to make it runnable
    // without the ASP.NET runtime.
    // =========================================================

    public class Section5e_Middleware
    {
        // Simplified middleware: a function that takes a context string
        // and a 'next' delegate, then optionally calls next
        delegate Task MiddlewareDelegate(string context, Func<Task> next);

        public static async Task Demo()
        {
            Console.WriteLine("=== SECTION 5e: Middleware / Pipeline ===\n");

            // Simulating app.Use(async (context, next) => { ... })
            var pipeline = new List<MiddlewareDelegate>();

            // Middleware 1 — timing (captures stopwatch)
            pipeline.Add(async (ctx, next) =>
            {
                var watch = Stopwatch.StartNew();
                Console.WriteLine($"  [{ctx}] Request started");
                await next(); // captured 'next' = rest of pipeline
                watch.Stop();
                Console.WriteLine($"  [{ctx}] Request finished in {watch.ElapsedMilliseconds}ms");
            });

            // Middleware 2 — auth (captures jwtSecret from "config")
            string jwtSecret = "super-secret-key-12345";
            pipeline.Add(async (ctx, next) =>
            {
                bool isAuthenticated = ctx.Contains("auth-token"); // simplified check
                if (isAuthenticated)
                {
                    Console.WriteLine($"  [{ctx}] Auth passed (secret: {jwtSecret[..8]}...)");
                    await next();
                }
                else
                {
                    Console.WriteLine($"  [{ctx}] Auth FAILED — 401");
                }
            });

            // Middleware 3 — final handler
            pipeline.Add(async (ctx, next) =>
            {
                Console.WriteLine($"  [{ctx}] Handler executed — 200 OK");
                await Task.CompletedTask;
            });

            // Execute the pipeline
            await RunPipeline(pipeline, "GET /api/orders auth-token");
            Console.WriteLine();
            await RunPipeline(pipeline, "GET /api/secret NO-TOKEN");
            Console.WriteLine();
        }

        static Task RunPipeline(List<MiddlewareDelegate> pipeline, string context)
        {
            int index = 0;
            Func<Task> next = null!;
            next = async () =>
            {
                if (index < pipeline.Count)
                    await pipeline[index++](context, next); // next captured recursively
            };
            return next();
        }
    }


    // =========================================================
    // SECTION 5f — Real World: Caching & Retry Logic
    // =========================================================
    // GetOrCreate-style caching: the value-factory lambda runs
    // only on cache miss and captures the key/ID it needs.
    //
    // Retry policies (Polly) use closures to capture logger and
    // service name in onRetry callbacks.
    // =========================================================

    public class Section5f_CachingAndRetry
    {
        // Simple in-memory cache simulation
        static readonly Dictionary<string, object> _cache = new();

        public static async Task Demo()
        {
            Console.WriteLine("=== SECTION 5f: Caching & Retry Logic ===\n");

            // GetOrCreate pattern — factory lambda captures userId
            int userId = 42;
            var profile = await GetOrCreateAsync($"profile:{userId}", async () =>
            {
                Console.WriteLine($"  Cache miss — fetching profile for user {userId} from DB...");
                await Task.Delay(5); // simulate DB call
                return new { Name = "Alice", Email = "alice@example.com", UserId = userId };
            });
            Console.WriteLine($"  Profile: {profile}");

            // Second call — served from cache (factory NOT called again)
            var profile2 = await GetOrCreateAsync($"profile:{userId}", async () =>
            {
                Console.WriteLine("  (this should NOT print — cache hit)");
                return new { Name = "Should not appear", Email = "", UserId = 0 };
            });
            Console.WriteLine($"  Profile (cached): {profile2}");
            Console.WriteLine();

            // Retry logic — captures logger and serviceName
            string serviceName = "PaymentService";
            int attempt = 0;

            await RetryAsync(maxRetries: 3, onRetry: (ex, retryNum) =>
            {
                // Captures: serviceName — baked in at registration time
                Console.WriteLine($"  [{serviceName}] Retry {retryNum}: {ex.Message}");
            },
            action: () =>
            {
                attempt++;
                if (attempt < 3)
                    throw new Exception($"Connection timeout (attempt {attempt})");
                Console.WriteLine($"  [{serviceName}] Succeeded on attempt {attempt}");
                return Task.CompletedTask;
            });

            Console.WriteLine();
        }

        static async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
        {
            if (_cache.TryGetValue(key, out var cached))
                return (T)cached;

            var value = await factory(); // closure called on cache miss
            _cache[key] = value!;
            return value;
        }

        static async Task RetryAsync(int maxRetries, Action<Exception, int> onRetry, Func<Task> action)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try { await action(); return; }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    onRetry(ex, attempt); // onRetry is itself a closure
                    await Task.Delay(10);
                }
            }
        }
    }


    // =========================================================
    // PROGRAM ENTRY POINT — Run all sections
    // =========================================================

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║      CLOSURES IN C# — DEMO           ║");
            Console.WriteLine("╚══════════════════════════════════════╝\n");

            // Section 1 — Basic closure
            Section1_BasicClosure.Demo();

            // Section 2 — Compiler output is shown as comments above
            Console.WriteLine("=== SECTION 2: Compiler Output ===");
            Console.WriteLine("See comments in source — compiler generates a hidden");
            Console.WriteLine("display class with captured variables as fields.\n");

            // Section 3 — Shared variable
            Section3_SharedVariable.Demo();

            // Section 4 — Loop trap
            Section4_LoopTrap.Demo();

            // Section 5a — Event handlers
            var form = new Section5a_EventHandlers();
            form.SetupForm("Alice", userId: 7);

            // Section 5b — LINQ
            Section5b_LINQ.Demo();

            // Section 5c — Async
            await Section5c_Async.Demo();

            // Section 5d — Factory / Strategy
            Section5d_Factory.Demo();

            // Section 5e — Middleware
            await Section5e_Middleware.Demo();

            // Section 5f — Caching & Retry
            await Section5f_CachingAndRetry.Demo();

            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║  KEY INTERVIEW POINTS                 ║");
            Console.WriteLine("╠══════════════════════════════════════╣");
            Console.WriteLine("║  • Captures by REFERENCE not value    ║");
            Console.WriteLine("║  • Compiler generates a display class ║");
            Console.WriteLine("║  • Variable moves from stack to heap  ║");
            Console.WriteLine("║  • Loop trap: use a local copy        ║");
            Console.WriteLine("║  • Allocates memory — avoid hot paths ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
        }
    }
}