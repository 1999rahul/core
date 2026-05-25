/// <summary>
/// TOPIC: await vs .Wait() — Deadlock Risk
/// 
/// Key Rule: In older ASP.NET / WinForms / WPF, after an await completes,
/// the continuation MUST resume on the SAME thread that started it.
/// Blocking that thread with .Wait() creates a circular wait → DEADLOCK.
///
/// ASP.NET Core removed the per-request SynchronizationContext,
/// so continuations can run on ANY thread pool thread → no deadlock.
/// </summary>
namespace AwaitVsWaitDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== await vs .Wait() Demo ===\n");

            // ✅ Safe — await all the way
            Demo1_CorrectAwait();

            // ✅ Safe — ConfigureAwait(false) breaks the sync context dependency
            Demo2_ConfigureAwaitFalse();

            // ✅ Safe — console app has no SynchronizationContext
            Demo3_SafeInConsoleApp();

            // ⚠️  Dangerous — shows the pattern that causes deadlock in older ASP.NET
            Demo4_DeadlockPattern_DoNotUseInAspNet();

            // ✅ Safe — Task.Run offloads to thread pool, no sync context captured
            Demo5_TaskRunWorkaround();

            // ⚠️  Thread pool starvation — even in ASP.NET Core, heavy .Wait() is bad
            Demo6_ThreadPoolStarvationRisk();

            Console.WriteLine("\nAll demos completed.");
            await Task.CompletedTask;
        }

        // ============================================================
        // DEMO 1 — Correct Pattern: async/await all the way down
        // ============================================================
        static async void Demo1_CorrectAwait()
        {
            Console.WriteLine("--- Demo 1: Correct async/await ---");

            // The calling method is also async, so no thread is ever blocked.
            // When GetDataAsync hits 'await', the thread is RELEASED.
            // When the task completes, any available thread picks up the continuation.
            string result = await GetDataAsync();
            Console.WriteLine($"Result: {result}\n");
        }

        // ============================================================
        // DEMO 2 — ConfigureAwait(false): Safe to call with .Wait()
        // ============================================================
        static void Demo2_ConfigureAwaitFalse()
        {
            Console.WriteLine("--- Demo 2: ConfigureAwait(false) ---");

            // ConfigureAwait(false) tells the awaitable:
            // "Do NOT capture the current SynchronizationContext.
            //  Resume on any available thread pool thread."
            //
            // This breaks the circular dependency:
            // - Thread A blocks on .Wait()
            // - Continuation doesn't need Thread A → runs on Thread B
            // - No deadlock!
            //
            // This is why library authors ALWAYS use ConfigureAwait(false).
            string result = GetDataAsyncWithConfigureAwait().Result; // safe here
            Console.WriteLine($"Result: {result}\n");
        }

        // ============================================================
        // DEMO 3 — Console App: No SynchronizationContext, .Wait() is safe
        // ============================================================
        static void Demo3_SafeInConsoleApp()
        {
            Console.WriteLine("--- Demo 3: .Wait() safe in Console App ---");

            // Console apps have NO SynchronizationContext.
            // Continuations don't need a specific thread.
            // So .Wait() does NOT cause deadlock here.
            // However, it's still bad practice — prefer await.
            string result = GetDataAsync().Result; // works fine in console
            Console.WriteLine($"Result: {result}\n");
        }

        // ============================================================
        // DEMO 4 — The Deadlock Pattern (DO NOT use in older ASP.NET)
        // ============================================================
        static void Demo4_DeadlockPattern_DoNotUseInAspNet()
        {
            Console.WriteLine("--- Demo 4: Deadlock Pattern (safe here, deadly in old ASP.NET) ---");

            // In older ASP.NET, this would DEADLOCK:
            //
            // Step 1: Request arrives on Thread A (has SynchronizationContext)
            // Step 2: GetDataAsync() called → hits await → schedules continuation on Thread A
            // Step 3: .Result blocks Thread A — it just sits and waits
            // Step 4: Task completes → continuation tries to run on Thread A
            // Step 5: Thread A is blocked → continuation can't run
            // Step 6: DEADLOCK — both waiting on each other forever
            //
            // In console (this demo), it works because there's no SynchronizationContext.

            string result = GetDataAsync().Result; // ⚠️ NEVER do this in old ASP.NET controller
            Console.WriteLine($"Result (would deadlock in old ASP.NET): {result}\n");
        }

        // ============================================================
        // DEMO 5 — Task.Run workaround: offload to thread pool
        // ============================================================
        static void Demo5_TaskRunWorkaround()
        {
            Console.WriteLine("--- Demo 5: Task.Run() as a workaround ---");

            // Task.Run() queues work on the thread pool.
            // Thread pool threads have NO SynchronizationContext.
            // So the continuation after await inside Task.Run
            // does NOT need to return to the original thread.
            // This means .Wait() on the outside won't deadlock.
            //
            // BUT: This is a hack. Prefer async all the way.
            // Task.Run() wastes two threads (one blocked, one running).
            string result = Task.Run(() => GetDataAsync()).Result;
            Console.WriteLine($"Result: {result}\n");
        }

        // ============================================================
        // DEMO 6 — Thread Pool Starvation (ASP.NET Core concern)
        // ============================================================
        static void Demo6_ThreadPoolStarvationRisk()
        {
            Console.WriteLine("--- Demo 6: Thread Pool Starvation Risk ---");

            // ASP.NET Core has no SynchronizationContext, so .Wait() won't deadlock.
            // BUT: If many requests all call .Wait(), you block many thread pool threads.
            // The thread pool has a limited number of threads.
            // All threads are blocked → new requests queue up → app slows down / hangs.
            // This is called THREAD POOL STARVATION — not a deadlock, but equally bad.
            //
            // The fix: ALWAYS use await. Never block thread pool threads.

            Console.WriteLine("Heavy use of .Wait() in ASP.NET Core = thread pool starvation risk.");
            Console.WriteLine("Always use await — even in ASP.NET Core.\n");
        }

        // ============================================================
        // HELPER METHODS
        // ============================================================

        /// <summary>
        /// Simulates an async operation (e.g., DB call, HTTP call).
        /// After the await, continuation needs to resume on the original
        /// SynchronizationContext (if one exists).
        /// </summary>
        static async Task<string> GetDataAsync()
        {
            await Task.Delay(100); // simulates async I/O
            return "Data fetched successfully";
        }

        /// <summary>
        /// Same as GetDataAsync but uses ConfigureAwait(false).
        /// This tells the runtime: don't capture the SynchronizationContext.
        /// The continuation can run on ANY thread pool thread.
        /// Safe to call with .Wait() or .Result even in old ASP.NET.
        /// </summary>
        static async Task<string> GetDataAsyncWithConfigureAwait()
        {
            await Task.Delay(100).ConfigureAwait(false); // ← key difference
            return "Data fetched with ConfigureAwait(false)";
        }
    }

    // ============================================================
    // INTERVIEW CHEAT SHEET (as comments)
    // ============================================================

    /*
    INTERVIEW QUESTION: What is the difference between await and .Wait()?
    
    ANSWER:
    - await is NON-BLOCKING. It releases the current thread while waiting
      for the task, then resumes execution once the task completes.
    
    - .Wait() / .Result is BLOCKING. It freezes the current thread until
      the task completes. The thread does nothing but wait.
    
    ---
    
    INTERVIEW QUESTION: How does .Wait() cause a deadlock?
    
    ANSWER:
    In older ASP.NET (and WinForms/WPF), there is a single-threaded
    SynchronizationContext. After an await, the continuation must resume
    on the SAME original thread.
    
    Deadlock steps:
    1. Thread A starts the async method and hits await
    2. await schedules continuation to resume on Thread A
    3. Caller uses .Wait() → blocks Thread A
    4. Task completes → continuation needs Thread A to run
    5. Thread A is blocked waiting for the task to finish
    6. Task can't finish because it needs Thread A
    7. DEADLOCK — circular wait
    
    ---
    
    INTERVIEW QUESTION: Why doesn't this happen in ASP.NET Core?
    
    ANSWER:
    ASP.NET Core removed the per-request SynchronizationContext.
    Continuations don't need a specific thread — they run on any
    available thread pool thread. So no circular dependency exists.
    
    ---
    
    INTERVIEW QUESTION: What is ConfigureAwait(false)?
    
    ANSWER:
    It tells the awaitable to NOT capture the current SynchronizationContext.
    The continuation will run on any available thread pool thread.
    This makes .Wait() safe to use even in older ASP.NET.
    Library authors should ALWAYS use ConfigureAwait(false).
    
    ---
    
    INTERVIEW QUESTION: Can .Wait() cause problems in ASP.NET Core?
    
    ANSWER:
    Not a deadlock, but YES — thread pool starvation.
    If many requests call .Wait(), they all block thread pool threads.
    The thread pool runs out of threads. New requests queue up.
    The app slows or hangs. Always prefer await.
    
    ---
    
    RULE OF THUMB:
    async all the way down — never mix blocking and async code.
    */
}
