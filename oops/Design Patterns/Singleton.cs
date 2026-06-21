// =============================================================================
// SingletonPattern.cs
// Topic : Singleton Pattern — lock keyword, critical sections & thread safety
// =============================================================================
//
// TABLE OF CONTENTS
//  1. What is a Critical Section?
//  2. What does lock do internally?
//  3. Why lock needs a reference-type object
//  4. Implementation 1 — Basic (NOT thread-safe, broken)
//  5. Implementation 2 — Simple lock (thread-safe, but slow)
//  6. Implementation 3 — Double-Checked Locking (recommended)
//  7. Implementation 4 — Lazy<T> (modern, cleanest)
//  8. Implementation 5 — Static Initializer (eager, simplest)
//  9. Demo — runs all implementations and proves same reference
// =============================================================================

using System;
using System.Threading;

// ─────────────────────────────────────────────────────────────────────────────
// CONCEPT 1 — What is a Critical Section?
//
// A critical section is a block of code that:
//   • Accesses a shared resource (field, file, DB connection, etc.)
//   • Must NOT be executed by more than one thread at the same time
//
// Three properties every critical section must satisfy (OS theory):
//   1. MUTUAL EXCLUSION  — only one thread inside at a time
//   2. PROGRESS          — a waiting thread will eventually get in
//   3. BOUNDED WAITING   — no thread waits forever (no starvation)
//
// In C#, the lock keyword satisfies all three via Monitor.Enter / Monitor.Exit.
// ─────────────────────────────────────────────────────────────────────────────


// ─────────────────────────────────────────────────────────────────────────────
// CONCEPT 2 — What does lock compile to?
//
//   lock (_lock) { /* body */ }
//
// The C# compiler transforms this into:
//
//   bool lockTaken = false;
//   try
//   {
//       Monitor.Enter(_lock, ref lockTaken);   // acquire
//       /* body */
//   }
//   finally
//   {
//       if (lockTaken) Monitor.Exit(_lock);    // always release, even on exception
//   }
//
// The finally block is critical — without it a thrown exception would leave the
// lock held forever, freezing every other thread waiting on it.
// ─────────────────────────────────────────────────────────────────────────────


// ─────────────────────────────────────────────────────────────────────────────
// CONCEPT 3 — Why lock needs a reference-type object
//
// Every heap object in .NET has a hidden 8-byte "sync block" in its header:
//
//   ┌──────────────────┐
//   │  Object Header   │ ← sync block index lives here
//   ├──────────────────┤
//   │  Method Table    │
//   ├──────────────────┤
//   │  Fields...       │
//   └──────────────────┘
//
// Monitor.Enter reads/writes the sync block to track ownership.
// Value types (int, bool, struct) have NO object header → compile error.
// null has no object at all → NullReferenceException at runtime.
//
// VALID lock targets (all compile):
//   private static readonly object _lock = new object();  ✔ BEST CHOICE
//   lock (this)              → compiles, but dangerous (external code can lock it)
//   lock ("some string")     → compiles, but strings are interned globally (dangerous)
//   lock (typeof(MyClass))   → compiles, but Type is a globally shared object (dangerous)
//
// INVALID lock targets:
//   int x; lock (x)         → ❌ compile error (value type)
//   lock (null)             → ❌ NullReferenceException
//
// WHY private static readonly object is the gold standard:
//   private   → nothing outside this class can lock on it (no external deadlocks)
//   static    → one instance shared across all threads (required for static methods)
//   readonly  → reference cannot change after init (changing it would break the lock)
//   object    → lightest heap object, exists only to carry the sync block header
// ─────────────────────────────────────────────────────────────────────────────


namespace SingletonDemo
{
    // =========================================================================
    // IMPLEMENTATION 1 — Basic Singleton (NOT thread-safe — DO NOT use in prod)
    // =========================================================================
    // Problem: Two threads can both see _instance == null simultaneously,
    //          both call new Singleton(), and both create separate objects.
    //          Only one assignment survives, but:
    //            • Two constructors ran (side-effects duplicated)
    //            • The losing object is orphaned in heap memory
    //            • The Singleton contract is broken
    public class SingletonBasic
    {
        private static SingletonBasic _instance;  // shared state — unprotected

        private SingletonBasic()
        {
            Console.WriteLine($"[BasicSingleton] Constructor called on Thread {Thread.CurrentThread.ManagedThreadId}");
        }

        public static SingletonBasic GetInstance()
        {
            // ── RACE CONDITION ──────────────────────────────────────────────
            // Thread 1 checks: null? YES → enters if block
            // Thread 2 checks: null? YES → also enters if block  ← BUG
            // Both create an instance. One overwrites the other.
            // ────────────────────────────────────────────────────────────────
            if (_instance == null)
                _instance = new SingletonBasic();

            return _instance;
        }
    }


    // =========================================================================
    // IMPLEMENTATION 2 — Simple lock (thread-safe, but acquires lock every call)
    // =========================================================================
    // Correct: only one thread enters at a time.
    // Drawback: lock is acquired on EVERY call to GetInstance(), even after the
    //           instance exists. In high-throughput code this is wasteful.
    public class SingletonSimpleLock
    {
        private static SingletonSimpleLock _instance;
        private static readonly object _lock = new object(); // dedicated lock token

        private SingletonSimpleLock()
        {
            Console.WriteLine($"[SimpleLock] Constructor called on Thread {Thread.CurrentThread.ManagedThreadId}");
        }

        public static SingletonSimpleLock GetInstance()
        {
            // ── OUTSIDE critical section ────────────────────────────────────
            // Any thread can be here. No shared resource touched yet.

            lock (_lock) // Monitor.Enter — only ONE thread enters below
            {
                // ── INSIDE critical section ─────────────────────────────────
                // Shared resource (_instance) is safely read and written here.
                // All other threads wait at lock(_lock) until this thread exits.

                if (_instance == null)
                    _instance = new SingletonSimpleLock();

            } // Monitor.Exit — lock released, next waiting thread wakes up
            // ── OUTSIDE critical section ────────────────────────────────────

            return _instance;
        }
    }


    // =========================================================================
    // IMPLEMENTATION 3 — Double-Checked Locking (recommended for most cases)
    // =========================================================================
    // Combines speed of basic singleton (no lock after first creation) with
    // safety of simple lock (mutex during initialization).
    //
    // volatile keyword: prevents CPU/compiler from reordering the three steps
    // of object creation:
    //   Step 1 — allocate memory
    //   Step 2 — call constructor (initialize the object)
    //   Step 3 — assign reference to _instance
    //
    // Without volatile, a CPU may do step 3 before step 2 (reordering).
    // Another thread sees _instance != null but the object isn't ready yet.
    // volatile forces full memory barrier — no reordering allowed.
    public class SingletonDoubleChecked
    {
        private static volatile SingletonDoubleChecked _instance; // volatile is required
        private static readonly object _lock = new object();

        private SingletonDoubleChecked()
        {
            Console.WriteLine($"[DoubleChecked] Constructor called on Thread {Thread.CurrentThread.ManagedThreadId}");
        }

        public static SingletonDoubleChecked GetInstance()
        {
            if (_instance == null)              // OUTER check — no lock, fast path
            {                                   // (after first creation, we never enter here)
                lock (_lock)
                {
                    if (_instance == null)      // INNER check — inside lock, safe
                    {                           // Thread 2 sees _instance != null here
                        _instance = new SingletonDoubleChecked(); // and skips this
                    }
                }
            }
            return _instance;
        }

        // Why TWO null checks?
        // ─────────────────────────────────────────────────────────────────────
        // Thread 1 passes outer check, acquires lock, creates instance, exits.
        // Thread 2 was blocked on lock. Now it acquires the lock.
        // Without the inner check, Thread 2 would call new() again!
        // The inner check sees _instance is now set → skips creation.
        // Thread 3 arrives after Thread 1 is done: outer check fails → no lock.
        // ─────────────────────────────────────────────────────────────────────
    }


    // =========================================================================
    // IMPLEMENTATION 4 — Lazy<T> (modern, cleanest, thread-safe by default)
    // =========================================================================
    // Lazy<T> uses LazyThreadSafetyMode.ExecutionAndPublication by default,
    // which guarantees the factory lambda runs exactly once across all threads.
    // Internally it uses double-checked locking — you get it for free.
    public class SingletonLazy
    {
        private static readonly Lazy<SingletonLazy> _instance =
            new Lazy<SingletonLazy>(() => new SingletonLazy());
        //                                              ↑
        //                          factory runs exactly once, thread-safe

        private SingletonLazy()
        {
            Console.WriteLine($"[LazyT] Constructor called on Thread {Thread.CurrentThread.ManagedThreadId}");
        }

        // Property access — no lock code needed, Lazy<T> handles everything
        public static SingletonLazy Instance => _instance.Value;

        public void DoWork() =>
            Console.WriteLine($"[LazyT] DoWork() on Thread {Thread.CurrentThread.ManagedThreadId}");
    }


    // =========================================================================
    // IMPLEMENTATION 5 — Static Initializer (eager loading, simplest)
    // =========================================================================
    // The CLR guarantees static field initializers run exactly once,
    // before any thread accesses the class. Thread-safe by CLR contract.
    //
    // Downside: eager loading — instance is created even if GetInstance()
    //           is never called. For heavy objects this wastes resources.
    public class SingletonEager
    {
        // Initialized at class load time — CLR handles thread safety
        private static readonly SingletonEager _instance = new SingletonEager();

        private SingletonEager()
        {
            Console.WriteLine($"[EagerSingleton] Constructor called (class load time)");
        }

        public static SingletonEager Instance => _instance;
    }


    // =========================================================================
    // DEMO — Run all implementations and verify same reference
    // =========================================================================
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Singleton Pattern Demo ===\n");

            // ── Test 1: Simple Lock ──────────────────────────────────────────
            Console.WriteLine("--- Simple Lock ---");
            var s1 = SingletonSimpleLock.GetInstance();
            var s2 = SingletonSimpleLock.GetInstance();
            Console.WriteLine($"Same instance? {object.ReferenceEquals(s1, s2)}\n"); // True

            // ── Test 2: Double-Checked Locking ───────────────────────────────
            Console.WriteLine("--- Double-Checked Locking ---");
            var d1 = SingletonDoubleChecked.GetInstance();
            var d2 = SingletonDoubleChecked.GetInstance();
            Console.WriteLine($"Same instance? {object.ReferenceEquals(d1, d2)}\n"); // True

            // ── Test 3: Lazy<T> ──────────────────────────────────────────────
            Console.WriteLine("--- Lazy<T> ---");
            var l1 = SingletonLazy.Instance;
            var l2 = SingletonLazy.Instance;
            Console.WriteLine($"Same instance? {object.ReferenceEquals(l1, l2)}\n"); // True

            // ── Test 4: Eager Static Initializer ─────────────────────────────
            Console.WriteLine("--- Eager Static Initializer ---");
            var e1 = SingletonEager.Instance;
            var e2 = SingletonEager.Instance;
            Console.WriteLine($"Same instance? {object.ReferenceEquals(e1, e2)}\n"); // True

            // ── Test 5: Multi-threaded stress test (Double-Checked) ───────────
            Console.WriteLine("--- Multi-threaded stress test (10 threads) ---");
            SingletonDoubleChecked[] results = new SingletonDoubleChecked[10];

            Thread[] threads = new Thread[10];
            for (int i = 0; i < 10; i++)
            {
                int idx = i;
                threads[idx] = new Thread(() =>
                {
                    results[idx] = SingletonDoubleChecked.GetInstance();
                });
            }

            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();

            // All 10 threads must hold the same reference
            bool allSame = true;
            for (int i = 1; i < results.Length; i++)
                if (!object.ReferenceEquals(results[0], results[i]))
                    allSame = false;

            Console.WriteLine($"All 10 threads got the same instance? {allSame}\n");

            // ── Summary ──────────────────────────────────────────────────────
            Console.WriteLine("=== Summary ===");
            Console.WriteLine("lock(_lock) defines a CRITICAL SECTION.");
            Console.WriteLine("Only ONE thread executes the body at a time.");
            Console.WriteLine("All other threads wait — suspended, not spinning.");
            Console.WriteLine("The lock object must be: private, static, readonly, non-null.");
            Console.WriteLine("Keep the critical section as SMALL as possible.");
        }
    }
}

// =============================================================================
// QUICK REFERENCE — Critical Section Rules
// =============================================================================
//
//  ✔  Wrap ONLY lines that read/write shared state
//  ✔  Keep the critical section as small as possible
//  ✔  Always use a private static readonly object as the lock token
//  ✔  Prefer Lazy<T> in modern code — it's concise and provably correct
//  ✗  Never lock on: this, string literals, typeof(T), null, or value types
//  ✗  Never put heavy computation inside a lock block
//
// Comparison table:
// ┌────────────────────────┬──────────────┬──────────┬──────────────┐
// │ Implementation         │ Thread-Safe  │ Lazy     │ Complexity   │
// ├────────────────────────┼──────────────┼──────────┼──────────────┤
// │ Basic (no lock)        │      ✗       │    ✔     │ Low          │
// │ Simple lock            │      ✔       │    ✔     │ Low          │
// │ Double-checked lock    │      ✔       │    ✔     │ Medium       │
// │ Lazy<T>                │      ✔       │    ✔     │ Low          │
// │ Static initializer     │      ✔       │    ✗     │ Very Low     │
// └────────────────────────┴──────────────┴──────────┴──────────────┘
// =============================================================================