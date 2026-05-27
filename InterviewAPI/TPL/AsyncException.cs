namespace ASPNETCONCEPTS.TPL
{
    public class AsyncException
    {
        // In synchronous code, exceptions bubble up the call stack immediately.
        // In async code, exceptions are captured and stored inside the Task, then re-thrown when you await it.

        // Task task = DoWorkAsync();  // exception happens here, but you won't see it yet until you await it
        // await task; // ← exception is RE-THROWN here

        // This is the root of all the differences below.

        // Part 1 — await vs .Wait() / .Result

        // This is the most common interview question on this topic.
        // await — unwraps the original exception cleanly

        static async Task ThrowingMethodAsync()
        {
            await Task.Delay(100);
            throw new InvalidOperationException("Something went wrong.");
        }

        // await — you get the ORIGINAL exception, clean stack trace
        //try
        //{
        //    await ThrowingMethodAsync();
        //    }
        //catch (InvalidOperationException ex)
        //{
        //    Console.WriteLine(ex.Message); // "Something went wrong."
        //    // Stack trace points directly to the throw line
        //}

        // .Wait() / .Result — wraps it in AggregateException

        // ❌ .Wait() — exception is WRAPPED in AggregateException

        //try
        //{
        //    ThrowingMethodAsync().Wait();   // or .Result
        //    }
        //catch (AggregateException ae)
        //{
        //    Console.WriteLine(ae.Message);
        //    // "One or more errors occurred. (Something went wrong.)"
        //    // You have to dig into .InnerExceptions to find the real one

        //    Console.WriteLine(ae.InnerExceptions[0].Message);
        //    // "Something went wrong." ← had to unwrap manually
        //}


        // Part 2 — AggregateException and Unwrapping
        // AggregateException is a container that holds one or more exceptions. You see it when:

        // Using.Wait() / .Result
        // Using Task.WhenAll() where multiple tasks fail
        // Using Parallel.ForEach / Task.Factory.StartNew

        // Multiple tasks failing simultaneously
        Task t1 = Task.Run(() => throw new InvalidOperationException("Error in T1"));
        Task t2 = Task.Run(() => throw new ArgumentNullException("Error in T2"));
        Task t3 = Task.Run(() => throw new TimeoutException("Error in T3"));

        //try
        //{
        //    Task.WaitAll(t1, t2, t3);   // blocking — all 3 run, all 3 fail
        //}
        //catch (AggregateException ae)
        //{
        //    Console.WriteLine($"Total exceptions: {ae.InnerExceptions.Count}"); // 3

        //    foreach (var ex in ae.InnerExceptions)
        //        Console.WriteLine($"  {ex.GetType().Name}: {ex.Message}");
        //}

        // Output:
        // Total exceptions: 3
        //   InvalidOperationException: Error in T1
        //   ArgumentNullException: Error in T2
        //   TimeoutException: Error in T3


        // Part 3 — await Task.WhenAll vs Task.WaitAll

        // Task.WaitAll — blocks + throws AggregateException with ALL 3 exceptions
        //try
        //{
        //    Task.WaitAll(t1, t2, t3);
        //}
        //catch (AggregateException ae)
        //{
        //    Console.WriteLine(ae.InnerExceptions.Count); // 3 — all exceptions captured
        //}

        // await Task.WhenAll — async + throws only the FIRST exception
        //try
        //{
        //    await Task.WhenAll(t1, t2, t3);
        //    }
        //catch (InvalidOperationException ex)
        //{
        //    // Only the first exception is re-thrown by await
        //    Console.WriteLine(ex.Message); // "T1 failed"
        //    // T2 and T3 exceptions are LOST silently!
        //}

        // Part 4 — Unobserved Exceptions (Silent Failures)

        static async Task Main()
        {
            _ = Task.Run(() => throw new Exception("Nobody sees this"));
            await Task.Delay(1000);
            Console.WriteLine("App ends — exception was silently swallowed");
        }

        // The fix — if you genuinely need fire-and-forget, wrap it:

        // ✅ Safe fire-and-forget with logging
        static void FireAndForget(Task task, ILogger logger)
        {
            task.ContinueWith(
                t => logger.LogError(t.Exception, "Background task failed"),
                TaskContinuationOptions.OnlyOnFaulted
            );
        }


        // Part 5 — async void — the Danger Zone
        // async void methods are the most dangerous pattern — their exceptions cannot be caught by the caller:
        // async void — exception crashes the app, can't be caught
        static async void DangerousMethod()
        {
            await Task.Delay(100);
            throw new Exception("This will crash the process!");
        }

        //try
        //{
        //    DangerousMethod();   // no await possible — returns void
        //    }
        //catch (Exception ex)
        //{
        //    // This catch block NEVER fires
        //    Console.WriteLine("Never reached.");
        //}
        //// App crashes with unhandled exception
        ///The async method must return a Task or Task<T> to allow the caller to await it and catch exceptions. async void should only be used for event handlers where there's no caller to await, and even then, you should handle exceptions inside the method itself to prevent crashes.
        /// 

        
    }
}
