using System.Text;

namespace ASPNETCONCEPTS.TPL
{
    public static class Tasks
    {
        // A Task is a promise that some work will complete in the future.It is the core abstraction for async programming in C# — not a thread, but something that runs on the ThreadPool 
        // Task is not a thread. It is scheduled on the ThreadPool. One thread can run many tasks over time.

        // A method that returns Tasks (no return value)
        public static async Task DoWorkAsync()
        {
            Console.WriteLine("Starting work...");
            await Task.Delay(2000); // simulates async I/O
            Console.WriteLine("Work done!");
        }

        // Calling it
        // await DoWorkAsync();
        //  The await keyword suspends the method without blocking the thread. The thread is returned to the pool while waiting, then a thread resumes the method when the awaited task finishes.

        // Task t = DoWorkAsync();

        // Console.WriteLine(t.IsCompleted);   // true when done
        // Console.WriteLine(t.IsFaulted);     // true if exception thrown
        // Console.WriteLine(t.IsCanceled);    // true if canceled
        // Console.WriteLine(t.Status);        // TaskStatus enum

        // -------- Task.Run() --------
        // Two types of work:
        // CPU-bound(calculations, loops) → needs a real thread
        // I / O - bound(network, file, DB) → no thread needed, OS handles the wait

        // Task.Run(() => HeavyWork()) Schedules HeavyWork on a ThreadPool thread.Your current thread is NOT blocked, it continues immediately.Two threads are now running.
        // Task.Run is for CPU-bound work that needs a thread. For I/O-bound work, just use async methods that return Tasks without blocking threads.

        // await Task.Run(() => HeavyWork()) Same as above, but your current thread suspends and waits for HeavyWork to finish before continuing.Thread is free(not blocked) during the wait.Code after the await only runs when HeavyWork is done.Exceptions surface normally.

        // Task.Run(() => HeavyWork()) — no await HeavyWork runs on a background thread, your thread continues immediately.You lose all control — no way to know when it finishes, exceptions are silently swallowed, app can exit before it completes.

        // ======== Pure async/await (no Task.Run) ==========
        // await httpClient.GetAsync(...);
        // Only 1 thread. Your thread suspends, OS registers a callback, thread goes free.
        // When response arrives, a ThreadPool thread picks up the continuation. No extra thread held during the wait.

        // Wrapping I/O in Task.Run is wrong
        // await Task.Run(() => File.ReadAllText("file.txt")); // bad 
        // Wastes a ThreadPool thread just to sit and wait. I/O doesn't need a thread, so this buys you nothing.


        // ===== You write Task.Run → you're asking for a new thread ==
        // You await pure I/O → OS handles it, no extra thread
        // Inside the method matters, not just how you call it

        // ======== Fire and forget — if intentional ========
        // _ = Task.Run(() => SendAnalytics()); // signals intent, suppresses warning
        // Only do this when you genuinely don't care about the result or errors.



    }
}
