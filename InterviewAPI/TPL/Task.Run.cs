// ============================================================
//   C# TASK, ASYNC/AWAIT & THREADING — NOTES
// ============================================================


// ─────────────────────────────────────────────────────────────
// 1. TWO TYPES OF WORK
// ─────────────────────────────────────────────────────────────

// CPU-BOUND — your code is doing actual computation
//   examples: sorting, image processing, loops, algorithms
//   needs a real thread to execute

// I/O-BOUND — your code is waiting for something external
//   examples: network request, file read, database query, Task.Delay
//   does NOT need a thread — OS handles the wait via callbacks/interrupts


// ─────────────────────────────────────────────────────────────
// 2. Task.Run(() => HeavyWork())
// ─────────────────────────────────────────────────────────────

// - Requests a thread from the ThreadPool
// - Schedules HeavyWork on that thread
// - Returns a Task immediately to the caller
// - Caller thread is NOT blocked, it continues right away
// - Two threads are now running in parallel

Task t = Task.Run(() => HeavyWork());
Console.WriteLine("This runs immediately, HeavyWork is running in background");


// ─────────────────────────────────────────────────────────────
// 3. await Task.Run(() => HeavyWork())
// ─────────────────────────────────────────────────────────────

// Step 1 — Task.Run requests a ThreadPool thread, hands HeavyWork to it
// Step 2 — await sees task is not done, suspends the current method
//           and RELEASES the calling thread (it goes free, not blocked)
// Step 3 — HeavyWork runs on the ThreadPool thread
// Step 4 — when HeavyWork finishes, runtime picks a thread (usually pool)
//           and resumes your method from after the await

await Task.Run(() => HeavyWork());
Console.WriteLine("This runs ONLY after HeavyWork is done");

// NOTE: after await resumes, you are NOT guaranteed to be on the same
// thread as before. You get whatever thread the ThreadPool assigns.
// Exception: UI apps (WPF/WinForms) use SynchronizationContext to
// resume on the UI thread so you can safely touch UI elements.

Console.WriteLine($"Before: Thread {Thread.CurrentThread.ManagedThreadId}");

await Task.Run(() => {
    Console.WriteLine($"Inside Task.Run: Thread {Thread.CurrentThread.ManagedThreadId}"); // different thread
    HeavyWork();
});

Console.WriteLine($"After: Thread {Thread.CurrentThread.ManagedThreadId}"); // may differ from Before


// ─────────────────────────────────────────────────────────────
// 4. Task.Run() WITHOUT await — fire and forget
// ─────────────────────────────────────────────────────────────

// - HeavyWork still runs on a ThreadPool thread
// - Your calling thread continues immediately
// - You lose ALL control over the task

Task.Run(() => HeavyWork()); // dropped the task!

// Problems:
//   - No way to know when HeavyWork finishes
//   - Exceptions are silently swallowed — you'll never see them
//   - App can exit before HeavyWork completes (console apps)
//   - Compiler gives CS4014 warning: "call is not awaited"

// If fire-and-forget is intentional, signal it clearly:
_ = Task.Run(() => SendAnalytics()); // discard operator suppresses warning

// Better — at least handle exceptions:
_ = Task.Run(() => SendAnalytics())
        .ContinueWith(t => Console.WriteLine(t.Exception),
                      TaskContinuationOptions.OnlyOnFaulted);


// ─────────────────────────────────────────────────────────────
// 5. EXCEPTION HANDLING — await vs no await
// ─────────────────────────────────────────────────────────────

// WITH await — exception surfaces normally
try
{
    await Task.Run(() => throw new Exception("boom"));
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message); // "boom" — you catch it
}

// WITHOUT await — exception disappears silently
try
{
    Task.Run(() => throw new Exception("boom"));
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message); // NEVER reached
    // exception lives inside the dropped Task object and silently dies
}


// ─────────────────────────────────────────────────────────────
// 6. PURE async/await — I/O bound (no Task.Run)
// ─────────────────────────────────────────────────────────────

// Only 1 thread involved — no extra thread is spawned

async Task FetchDataAsync()
{
    // your thread suspends here
    // OS registers a network callback (not a thread — a hardware interrupt)
    // your thread goes FREE
    // when response arrives, a pool thread picks up and resumes
    var result = await httpClient.GetAsync("https://api.example.com");
}

// Thread count: 1 (no Task.Run = no extra thread)
// The OS and hardware handle the waiting, not a thread


// ─────────────────────────────────────────────────────────────
// 7. WRAPPING I/O IN Task.Run — wrong pattern
// ─────────────────────────────────────────────────────────────

// BAD — wastes a ThreadPool thread just to sit and wait
await Task.Run(() => File.ReadAllText("file.txt"));

// GOOD — I/O doesn't need a thread, let the OS handle it
await File.ReadAllTextAsync("file.txt");

// File.ReadAllText inside Task.Run holds a real thread doing nothing
// File.ReadAllTextAsync uses OS async I/O, no thread held during read


// ─────────────────────────────────────────────────────────────
// 8. WHAT DECIDES HOW MANY THREADS
// ─────────────────────────────────────────────────────────────

// You write Task.Run              → you're explicitly asking for a new thread
// You await pure I/O (no Task.Run)→ OS handles it, no extra thread
// What's INSIDE the method matters, not just how you call it

// Calling a method without await does NOT change how many threads it uses.
// If the method has Task.Run inside → 2 threads
// If the method is pure async I/O  → 1 thread
// The caller just decides whether to wait or not.


// ─────────────────────────────────────────────────────────────
// 9. QUICK REFERENCE
// ─────────────────────────────────────────────────────────────

//  Scenario                              | Threads | Caller blocks?
//  --------------------------------------|---------|---------------
//  Normal method call                    |    1    |  YES
//  Task.Run (no await)                   |    2    |  NO (fire & forget)
//  await Task.Run                        |    2    |  NO (suspends, then resumes)
//  await pure I/O (no Task.Run)          |    1    |  NO (OS handles wait)
//  Task.Run wrapping I/O (bad pattern)   |    2    |  NO (but wastes a thread)


// ─────────────────────────────────────────────────────────────
// PLACEHOLDER METHODS USED IN EXAMPLES ABOVE
// ─────────────────────────────────────────less────────────────
static void HeavyWork() => Thread.Sleep(2000);
static void SendAnalytics() => Thread.Sleep(500);
static HttpClient httpClient = new HttpClient();



// =========================================================================================================


// ============================================================
//   Task.WhenAll() vs Task.WaitAll() — NOTES & EXAMPLES
// ============================================================

// ─────────────────────────────────────────────────────────────
// 1. CORE DIFFERENCE
// ─────────────────────────────────────────────────────────────

// Task.WhenAll  → async, calling thread is FREE while waiting
// Task.WaitAll  → sync,  calling thread is BLOCKED while waiting
// Both start all tasks in parallel — difference is only how they wait


// ─────────────────────────────────────────────────────────────
// 2. Task.WhenAll — async, non-blocking
// ─────────────────────────────────────────────────────────────
async Task WhenAllExample()
{
    Task t1 = Task.Run(() => HeavyWork1());
    Task t2 = Task.Run(() => HeavyWork2());
    Task t3 = Task.Run(() => HeavyWork3());

    // calling thread suspends here but is FREE
    // t1, t2, t3 all run in parallel on ThreadPool threads
    await Task.WhenAll(t1, t2, t3);

    Console.WriteLine("All done"); // only runs after all three finish
}

// ─────────────────────────────────────────────────────────────
// 3. Task.WaitAll — synchronous, BLOCKING
// ─────────────────────────────────────────────────────────────

void WaitAllExample()
{
    Task t1 = Task.Run(() => HeavyWork1());
    Task t2 = Task.Run(() => HeavyWork2());
    Task t3 = Task.Run(() => HeavyWork3());

    // calling thread is BLOCKED here — stuck, cannot do anything else
    Task.WaitAll(t1, t2, t3);

    Console.WriteLine("All done");
}

// ─────────────────────────────────────────────────────────────
// 4. GETTING RESULTS BACK
// ─────────────────────────────────────────────────────────────

async Task GettingResults()
{
    // WhenAll — cleanly returns an array of results
    Task<int> t1 = Task.Run(() => GetValue1());
    Task<int> t2 = Task.Run(() => GetValue2());
    Task<int> t3 = Task.Run(() => GetValue3());

    int[] results = await Task.WhenAll(t1, t2, t3);

    Console.WriteLine(results[0]); // result of t1
    Console.WriteLine(results[1]); // result of t2
    Console.WriteLine(results[2]); // result of t3


    // WaitAll — no built-in result collection
    // access results individually after waiting
    Task<int> a = Task.Run(() => GetValue1());
    Task<int> b = Task.Run(() => GetValue2());

    Task.WaitAll(a, b);

    Console.WriteLine(a.Result); // .Result is safe here since task is done
    Console.WriteLine(b.Result);
}

// ─────────────────────────────────────────────────────────────
// 5. EXCEPTION HANDLING
// ─────────────────────────────────────────────────────────────

async Task ExceptionHandling()
{
    // WhenAll with await — gives you first exception only
    try
    {
        await Task.WhenAll(
            Task.Run(() => throw new Exception("t1 failed")),
            Task.Run(() => throw new Exception("t2 failed"))
        );
    }
    catch (Exception ex)
    {
        // await unwraps AggregateException, surfaces only the FIRST exception
        Console.WriteLine(ex.Message); // "t1 failed"
    }


    // WhenAll — to see ALL exceptions, inspect the Task directly
    Task all = Task.WhenAll(
        Task.Run(() => throw new Exception("t1 failed")),
        Task.Run(() => throw new Exception("t2 failed"))
    );

    try { await all; }
    catch
    {
        foreach (var ex in all.Exception.InnerExceptions)
            Console.WriteLine(ex.Message); // "t1 failed", "t2 failed"
    }


    // WaitAll — always throws AggregateException with ALL exceptions
    try
    {
        Task.WaitAll(
            Task.Run(() => throw new Exception("t1 failed")),
            Task.Run(() => throw new Exception("t2 failed"))
        );
    }
    catch (AggregateException ae)
    {
        foreach (var ex in ae.InnerExceptions)
            Console.WriteLine(ex.Message); // "t1 failed", "t2 failed"
    }
}

// ContinueWith() -> is very similar to then() in the javascript world. It allows you to specify a callback that will run when the task completes, regardless of whether it completed successfully or with an exception. You can also specify options to control when the continuation runs (e.g., only on success, only on failure, etc.).

// ConfigureAwait(false) tells the awaiter not to marshal the continuation back to the original synchronization context. In library code this prevents deadlocks in environments like classic ASP.NET or WinForms, and in any code it avoids the overhead of context switching. In ASP.NET Core there's no sync context, so deadlocks aren't a concern — but you should still use it in library/service layers for portability and performance.