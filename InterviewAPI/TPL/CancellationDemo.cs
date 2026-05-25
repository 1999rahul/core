// ============================================================
//  CancellationDemo.cs
//  Topic: Cancellation in Async/Await — C#
//  The core problem: stopping stale work when you no longer
//  need the result (e.g. a live search box).
// ============================================================

class CancellationDemo
{
    // ─────────────────────────────────────────────────────────
    // PART 1 — THE PROBLEM (no cancellation)
    // All 3 searches run to completion even though only the
    // last result matters. Older results can overwrite newer
    // ones if network latency varies.
    // ─────────────────────────────────────────────────────────
    static async Task SearchWithoutCancellation()
    {
        Console.WriteLine("=== PART 1: WITHOUT Cancellation (broken) ===\n");

        // Fire all three — no way to stop the earlier ones
        Task t1 = FakeApiCallNoCancellation("c");
        Task t2 = FakeApiCallNoCancellation("c#");
        Task t3 = FakeApiCallNoCancellation("c# cancellation");

        await Task.WhenAll(t1, t2, t3);

        Console.WriteLine("\n↑ All 3 ran. We only wanted the last one.\n");
    }

    static async Task FakeApiCallNoCancellation(string query)
    {
        Console.WriteLine($"  [{query}] API call started...");
        await Task.Delay(1000);                          // simulate network
        Console.WriteLine($"  [{query}] ✅ Result arrived — showing UI");
        // Older slower results can arrive AFTER newer ones → stale UI bug
    }


    // ─────────────────────────────────────────────────────────
    // PART 2 — THE FIX (with cancellation)
    // Before each new search, cancel the previous one.
    // Only the last search ever shows a result.
    // ─────────────────────────────────────────────────────────
    static async Task SearchWithCancellation()
    {
        Console.WriteLine("=== PART 2: WITH Cancellation (correct) ===\n");

        CancellationTokenSource cts = null;

        // Helper: called every time user types a character
        async Task OnUserTyped(string query, int typingDelayMs)
        {
            await Task.Delay(typingDelayMs);   // simulate user typing speed

            // ── KEY STEP: cancel whatever was running before ──
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();
            // ─────────────────────────────────────────────────

            try
            {
                await FakeApiCallWithCancellation(query, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // This is EXPECTED, not an error.
                // It means the user kept typing — discard this result.
                Console.WriteLine($"  [{query}] ⛔ Cancelled — user kept typing\n");
            }
        }

        // Simulate user typing quickly: "c" → "c#" → "c# cancellation"
        await OnUserTyped("c", typingDelayMs: 0);
        await OnUserTyped("c#", typingDelayMs: 300);   // types next char after 300ms
        await OnUserTyped("c# cancellation", typingDelayMs: 300);   // types next char after 300ms

        Console.WriteLine("↑ Only the last search showed a result.\n");

        cts?.Dispose();
    }

    static async Task FakeApiCallWithCancellation(string query, CancellationToken token)
    {
        Console.WriteLine($"  [{query}] API call started...");

        // Task.Delay is cancellation-aware:
        // if token fires before 1000ms, it throws OperationCanceledException immediately
        await Task.Delay(1000, token);

        // This line only runs if nobody cancelled us
        Console.WriteLine($"  [{query}] ✅ Result arrived — showing UI");
    }


    // ─────────────────────────────────────────────────────────
    // PART 3 — MANUAL CHECKPOINT (ThrowIfCancellationRequested)
    // Use this when YOU are doing the work in a loop,
    // not delegating to a cancellation-aware method.
    // ─────────────────────────────────────────────────────────
    static async Task ManualCheckpointDemo()
    {
        Console.WriteLine("=== PART 3: Manual Checkpoints in a Loop ===\n");

        var cts = new CancellationTokenSource();

        // Cancel after 1.2 seconds from another thread
        _ = Task.Run(async () =>
        {
            await Task.Delay(1200);
            Console.WriteLine("  [Main] Cancelling now...");
            cts.Cancel();
        });

        try
        {
            await ProcessRowsAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("  Processing stopped mid-way. Partial work discarded.\n");
        }

        cts.Dispose();
    }

    static async Task ProcessRowsAsync(CancellationToken token)
    {
        for (int i = 1; i <= 10; i++)
        {
            // ── Cooperative check at the top of every loop iteration ──
            token.ThrowIfCancellationRequested();
            // ─────────────────────────────────────────────────────────

            Console.WriteLine($"  Processing row {i}...");
            await Task.Delay(400); // simulate per-row work
        }

        Console.WriteLine("  All rows processed.");
    }


    // ─────────────────────────────────────────────────────────
    // PART 4 — TIMEOUT (auto-cancel after N seconds)
    // You don't always need a human to cancel — set a deadline.
    // ─────────────────────────────────────────────────────────
    static async Task TimeoutDemo()
    {
        Console.WriteLine("=== PART 4: Timeout-Based Cancellation ===\n");

        // Auto-cancel if work takes longer than 1.5 seconds
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1.5));

        try
        {
            await SlowOperationAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("  ⏰ Operation timed out after 1.5s.\n");
        }
    }

    static async Task SlowOperationAsync(CancellationToken token)
    {
        Console.WriteLine("  Slow operation started (needs 3 seconds)...");
        await Task.Delay(3000, token);   // will be cancelled at 1.5s
        Console.WriteLine("  Slow operation finished.");
    }


    // ─────────────────────────────────────────────────────────
    // PART 5 — LINKED TOKENS
    // Real apps often need: cancel if user clicks "Stop" OR
    // if a timeout fires — whichever comes first.
    // ─────────────────────────────────────────────────────────
    static async Task LinkedTokenDemo()
    {
        Console.WriteLine("=== PART 5: Linked Tokens (user cancel + timeout) ===\n");

        var userCts = new CancellationTokenSource();          // user hits Cancel
        var timeoutCts = new CancellationTokenSource(2000);      // 2 second timeout

        // Fires when EITHER of the above fires
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            userCts.Token,
            timeoutCts.Token
        );

        // Simulate user clicking Cancel after 800ms
        _ = Task.Run(async () =>
        {
            await Task.Delay(800);
            Console.WriteLine("  [User] Clicked the Cancel button!");
            userCts.Cancel();
        });

        try
        {
            await Task.Delay(5000, linkedCts.Token);   // long running work
            Console.WriteLine("  Work completed.");
        }
        catch (OperationCanceledException)
        {
            // Inspect which one fired
            if (userCts.IsCancellationRequested)
                Console.WriteLine("  Stopped because: user clicked Cancel.\n");
            else if (timeoutCts.IsCancellationRequested)
                Console.WriteLine("  Stopped because: timeout.\n");
        }

        userCts.Dispose();
        timeoutCts.Dispose();
    }


    // ─────────────────────────────────────────────────────────
    // ENTRY POINT
    // ─────────────────────────────────────────────────────────
    static async Task Main()
    {
        await SearchWithoutCancellation();   // Part 1 — problem
        await SearchWithCancellation();      // Part 2 — fix
        await ManualCheckpointDemo();        // Part 3 — loops
        await TimeoutDemo();                 // Part 4 — timeout
        await LinkedTokenDemo();             // Part 5 — linked tokens

        Console.WriteLine("=== Done ===");
    }
}


// ============================================================
//  QUICK REFERENCE — Interview Cheat Sheet
// ============================================================
//
//  CancellationTokenSource  →  the OWNER.  Calls .Cancel()
//  CancellationToken        →  the LISTENER. Passed into methods.
//  OperationCanceledException → the SIGNAL that work stopped.
//
//  PATTERNS:
//
//  1. Cancel previous work before starting new:
//       cts?.Cancel(); cts = new CancellationTokenSource();
//
//  2. Timeout:
//       new CancellationTokenSource(TimeSpan.FromSeconds(5))
//
//  3. Manual check in a loop:
//       token.ThrowIfCancellationRequested();
//
//  4. Silent check (handle yourself):
//       if (token.IsCancellationRequested) return;
//
//  5. Combined cancel sources:
//       CancellationTokenSource.CreateLinkedTokenSource(t1, t2)
//
//  KEY FACTS:
//  - Cancellation is COOPERATIVE — worker must check the token
//  - OperationCanceledException is EXPECTED, not an error
//  - Task.Delay(ms, token) is cancellation-aware — use it
//  - Always pass CancellationToken as the LAST parameter (convention)
//  - CancellationToken.None = "I don't need cancellation here"
//  - Always Dispose() your CancellationTokenSource
//
// ============================================================