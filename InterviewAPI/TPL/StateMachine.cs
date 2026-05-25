
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// ================================================================
//  ORIGINAL ASYNC CODE  — exactly what you write
// ================================================================
class OrderService
{
    public async Task<string> PlaceOrderAsync()
    {
        Console.WriteLine("  [PlaceOrder]  Starting...");
        string user = await GetUserAsync();
        Console.WriteLine($"  [PlaceOrder]  Got user    → {user}");
        string payment = await ChargePaymentAsync();
        Console.WriteLine($"  [PlaceOrder]  Got payment → {payment}");
        return user + "  |  " + payment;
    }

    public async Task<string> GetUserAsync()
    {
        Console.WriteLine("    [GetUser]   Querying DB...");
        await Task.Delay(800);                   // simulate DB latency
        Console.WriteLine("    [GetUser]   DB responded");
        return "User{John}";
    }

    public async Task<string> ChargePaymentAsync()
    {
        Console.WriteLine("    [Charge]    Calling payment API...");
        await Task.Delay(400);                   // simulate API latency
        Console.WriteLine("    [Charge]    Payment API responded");
        return "Payment{Success}";
    }
}

// ================================================================
//  MANUAL STATE MACHINES  — what the compiler generates for you
// ================================================================

// ── State Machine 1: GetUserAsync ────────────────────────────────
//
//  States:
//   -1 → start, kick off Tasks.Delay, suspend
//    0 → resume after Delay, return result
//   -2 → terminal (done or faulted)
//
struct GetUserAsync_StateMachine : IAsyncStateMachine
{
    public int _state;
    public AsyncTaskMethodBuilder<string> _builder;
    private TaskAwaiter _awaiter;   // Tasks.Delay → non-generic

    public void MoveNext()
    {
        try
        {
            // Jump to the right state on re-entry
            if (_state == 0) goto RESUME_STATE_0;

            //──── STATE -1 : First entry ────────────────────────
            Console.WriteLine("    [GetUser SM]   Querying DB...");

            var delayTask = Task.Delay(800);
            _awaiter = delayTask.GetAwaiter();

            if (!_awaiter.IsCompleted)
            {
                _state = 0;                                     // remember resume point
                _builder.AwaitUnsafeOnCompleted(ref _awaiter, ref this);
                return;                                         // ← suspend, free thread
            }

            //──── STATE 0 : Resume after Tasks.Delay ─────────────
        RESUME_STATE_0:
            _awaiter.GetResult();   // void — only here to surface exceptions
            Console.WriteLine("    [GetUser SM]   DB responded");
            _builder.SetResult("User{John}");                   // complete outer Tasks
        }
        catch (Exception ex)
        {
            _state = -2;
            _builder.SetException(ex);
        }
    }

    public void SetStateMachine(IAsyncStateMachine sm) => _builder.SetStateMachine(sm);
}

// ── State Machine 2: ChargePaymentAsync ──────────────────────────
//
//  States:
//   -1 → start, kick off Tasks.Delay, suspend
//    0 → resume after Delay, return result
//
struct ChargePaymentAsync_StateMachine : IAsyncStateMachine
{
    public int _state;
    public AsyncTaskMethodBuilder<string> _builder;
    private TaskAwaiter _awaiter;

    public void MoveNext()
    {
        try
        {
            if (_state == 0) goto RESUME_STATE_0;

            //──── STATE -1 ───────────────────────────────────────
            Console.WriteLine("    [Charge SM]    Calling payment API...");

            var delayTask = Task.Delay(400);
            _awaiter = delayTask.GetAwaiter();

            if (!_awaiter.IsCompleted)
            {
                _state = 0;
                _builder.AwaitUnsafeOnCompleted(ref _awaiter, ref this);
                return;
            }

            //──── STATE 0 ────────────────────────────────────────
        RESUME_STATE_0:
            _awaiter.GetResult();
            Console.WriteLine("    [Charge SM]    Payment API responded");
            _builder.SetResult("Payment{Success}");
        }
        catch (Exception ex)
        {
            _state = -2;
            _builder.SetException(ex);
        }
    }

    public void SetStateMachine(IAsyncStateMachine sm) => _builder.SetStateMachine(sm);
}

// ── State Machine 3: PlaceOrderAsync ─────────────────────────────
//
//  States:
//   -1 → start, call GetUserAsync, suspend
//    0 → resume with user, call ChargePaymentAsync, suspend
//    1 → resume with payment, return combined result
//
struct PlaceOrderAsync_StateMachine : IAsyncStateMachine
{
    public int _state;
    public AsyncTaskMethodBuilder<string> _builder;
    public OrderServiceManual _this;      // ref to service (captured variable)

    // Both awaits return Tasks<string>, so one shared awaiter field is enough
    private TaskAwaiter<string> _awaiter;

    // Local variables that must survive across suspension points
    private string _user;
    private string _payment;

    public void MoveNext()
    {
        try
        {
            // Dispatch to the correct resume point
            if (_state == 0) goto RESUME_STATE_0;
            if (_state == 1) goto RESUME_STATE_1;

            //──── STATE -1 : First entry ────────────────────────
            Console.WriteLine("  [PlaceOrder SM] Starting...");

            Task<string> userTask = _this.GetUserAsync();   // creates GetUserAsync_SM
            _awaiter = userTask.GetAwaiter();

            if (!_awaiter.IsCompleted)
            {
                _state = 0;                                 // next MoveNext → state 0
                _builder.AwaitUnsafeOnCompleted(ref _awaiter, ref this);
                return;                                     // ← suspend
            }

            //──── STATE 0 : Back from GetUserAsync ──────────────
        RESUME_STATE_0:
            _user = _awaiter.GetResult();                   // "User{John}" — saved on struct
            Console.WriteLine($"  [PlaceOrder SM] Got user    → {_user}");

            Task<string> paymentTask = _this.ChargePaymentAsync(); // creates ChargePaymentAsync_SM
            _awaiter = paymentTask.GetAwaiter();

            if (!_awaiter.IsCompleted)
            {
                _state = 1;                                 // next MoveNext → state 1
                _builder.AwaitUnsafeOnCompleted(ref _awaiter, ref this);
                return;                                     // ← suspend
            }

            //──── STATE 1 : Back from ChargePaymentAsync ────────
        RESUME_STATE_1:
            _payment = _awaiter.GetResult();                // "Payment{Success}"
            Console.WriteLine($"  [PlaceOrder SM] Got payment → {_payment}");

            _builder.SetResult(_user + "  |  " + _payment); // complete the outer Tasks
        }
        catch (Exception ex)
        {
            _state = -2;
            _builder.SetException(ex);
        }
    }

    public void SetStateMachine(IAsyncStateMachine sm) => _builder.SetStateMachine(sm);
}

// ================================================================
//  BOOTSTRAPPER CLASS  — replaces the async keyword on each method
//  Creates the struct, wires up the builder, kicks off MoveNext()
// ================================================================
class OrderServiceManual
{
    // Bootstrapper for GetUserAsync
    public Task<string> GetUserAsync()
    {
        var sm = new GetUserAsync_StateMachine();
        sm._state = -1;
        sm._builder = AsyncTaskMethodBuilder<string>.Create();
        sm._builder.Start(ref sm);          // calls MoveNext() once immediately
        return sm._builder.Task;            // return the Tasks to the caller
    }

    // Bootstrapper for ChargePaymentAsync
    public Task<string> ChargePaymentAsync()
    {
        var sm = new ChargePaymentAsync_StateMachine();
        sm._state = -1;
        sm._builder = AsyncTaskMethodBuilder<string>.Create();
        sm._builder.Start(ref sm);
        return sm._builder.Task;
    }

    // Bootstrapper for PlaceOrderAsync
    public Task<string> PlaceOrderAsync()
    {
        var sm = new PlaceOrderAsync_StateMachine();
        sm._state = -1;
        sm._this = this;                 // capture 'this' — same as a closure
        sm._builder = AsyncTaskMethodBuilder<string>.Create();
        sm._builder.Start(ref sm);
        return sm._builder.Task;
    }
}

// ================================================================
//  ENTRY POINT
// ================================================================
class Program
{
    static async Task Main()
    {
        // ── Run 1: normal async/await ────────────────────────────
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║   ORIGINAL  async / await            ║");
        Console.WriteLine("╚══════════════════════════════════════╝");

        var original = new OrderService();
        string r1 = await original.PlaceOrderAsync();
        Console.WriteLine($"\n  ✅ Final result : {r1}");

        Console.WriteLine();

        // ── Run 2: manual state machines ─────────────────────────
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║   MANUAL  State Machines             ║");
        Console.WriteLine("╚══════════════════════════════════════╝");

        var manual = new OrderServiceManual();
        string r2 = await manual.PlaceOrderAsync();
        Console.WriteLine($"\n  ✅ Final result : {r2}");

        Console.WriteLine();
        Console.WriteLine("Both results are identical — manual SM is exactly what compiler generates.");
    }
}

// ══════════════════════════════════════════════════════════════════════════════
//  C# ASYNC / AWAIT — COMPLETE THEORY NOTES
//  Topics covered:
//   1. What is async/await
//   2. What the compiler generates (state machine)
//   3. State machines — theory of computation vs compiler generated
//   4. How the thread is freed and who does the I/O
//   5. OS level mechanics (IOCP)
//   6. TaskAwaiter and AsyncTaskMethodBuilder
//   7. Multiple async methods and their state machines
//   8. Interview cheat sheet
// ══════════════════════════════════════════════════════════════════════════════


// ─────────────────────────────────────────────────────────────────────────────
//  SECTION 1 — WHAT IS ASYNC / AWAIT
// ─────────────────────────────────────────────────────────────────────────────

// async/await is a compiler feature introduced in C# 5.
// It allows you to write asynchronous code that looks synchronous.
// The async keyword on a method tells the compiler to transform that method
// into a state machine.
// The await keyword marks a suspension point — where the method can pause
// and free the thread without blocking it.

// async alone does NOT make code run concurrently.
// You need actual async I/O (network, disk, DB) or Tasks.Run for concurrency.

// An async method always returns:
//   Tasks        — for void async methods
//   Tasks<T>     — for async methods that return a value
//   ValueTask   — performance optimized version of Tasks
//   void        — only for event handlers (dangerous, avoid)


// ─────────────────────────────────────────────────────────────────────────────
//  SECTION 2 — WHAT THE COMPILER GENERATES (STATE MACHINE)
// ─────────────────────────────────────────────────────────────────────────────

// When you write an async method, the compiler does NOT keep it as is.
// It rewrites the entire method into two things:
//
//   1. A BOOTSTRAPPER METHOD
//      — has the same name and signature as your original method
//      — creates the state machine struct
//      — sets _state = -1 (not started)
//      — creates the AsyncTaskMethodBuilder
//      — calls builder.Start() which triggers MoveNext() once
//      — returns builder.Tasks to the caller
//
//   2. A STATE MACHINE STRUCT
//      — implements IAsyncStateMachine
//      — has a MoveNext() method containing all your original code
//      — has an integer _state field to track where to resume
//      — stores all local variables as fields (so they survive suspension)
//      — has a TaskAwaiter field for each await point
//      — has an AsyncTaskMethodBuilder field to control the returned Tasks

// ONE state machine is generated per async method — not per await.
// Multiple awaits inside one method = multiple states inside one state machine.

// State numbering:
//   -1  →  initial state, method has not started yet
//    0  →  suspended at first await
//    1  →  suspended at second await
//    2  →  suspended at third await
//   -2  →  terminal state, method is done or faulted

// MoveNext() is the heart of the state machine.
// Every time it is called, it checks _state and jumps to the right place.
// It runs until it hits an incomplete await, then suspends (returns).
// When the awaited task completes, MoveNext() is called again.


// ─────────────────────────────────────────────────────────────────────────────
//  SECTION 3 — STATE MACHINES: THEORY OF COMPUTATION vs COMPILER GENERATED
// ─────────────────────────────────────────────────────────────────────────────

// THEORY OF COMPUTATION — Finite State Machine (FSM)
// Formally defined as a 5-tuple: M = (Q, Σ, δ, q₀, F)
//
//   Q   →  finite set of states
//   Σ   →  input alphabet (set of valid inputs)
//   δ   →  transition function — δ(state, input) → next state
//   q₀  →  start state
//   F   →  set of accepting / final states
//
// A pure FSM:
//   — reads input symbols one at a time from a tape
//   — has NO memory beyond what the state label encodes
//   — only says YES or NO (accept or reject a string)
//   — is a purely mathematical model

// COMPILER GENERATED STATE MACHINE
// Follows the same fundamental principle but is NOT a pure FSM:
//
//   Q   →  _state values (-1, 0, 1, 2 ... -2)
//   Σ   →  "task completed" or "exception thrown"
//   δ   →  MoveNext() — given state + event → next state
//   q₀  →  _state = -1
//   F   →  _state = -2 (done or faulted)

// KEY DIFFERENCES — where they diverge:
//
//   1. MEMORY
//      Pure FSM has no variables — only a state label.
//      Async state machine stores local variables on the struct (_user, _payment etc.)
//      This makes it closer to a Pushdown Automaton or Turing Machine in theory.
//
//   2. EVENT DRIVEN, NOT TAPE DRIVEN
//      FSM reads from an input tape left to right.
//      Async state machine is event driven — MoveNext() is called by the
//      runtime when an I/O completion event fires. No tape involved.
//
//   3. PRODUCES OUTPUT
//      Pure acceptor FSMs only say yes/no.
//      Async state machine produces a value (Tasks<T> result).
//      This makes it closer to a Mealy Machine (output depends on state + input).

// TRUE THEORETICAL ANCESTOR — Coroutines
// The real lineage of async/await is:
//   Coroutines (Conway, 1963)
//     → Communicating Sequential Processes (Hoare, 1978)
//       → Generators
//         → Async / Await
//
// Core coroutine idea: a routine can suspend itself mid-execution,
// transfer control elsewhere, then resume from the same point later.
// The state machine is how compilers implement this on a call stack
// that does not natively support suspension.


// ─────────────────────────────────────────────────────────────────────────────
//  SECTION 4 — HOW THE THREAD IS FREED AND WHO DOES THE I/O
// ─────────────────────────────────────────────────────────────────────────────

// BIGGEST MISCONCEPTION TO KILL:
// The thread is NOT waiting in the background during an async I/O call.
// There is NO thread blocked or sleeping. Zero.

// WHAT YOUR THREAD ACTUALLY DOES when GetStringAsync(url) is called:
//   1. Opens a socket
//   2. Tells the OS — "Send this HTTP request. Notify me when response comes."
//   3. Returns immediately — thread is done in microseconds

// WHO DOES THE ACTUAL WORK:
//   — The OS kernel handles TCP/IP
//   — The Network Interface Card (NIC) physically sends and receives packets
//   — This happens at the hardware level — no .NET thread is involved

// THE THREAD FLOW:
//   Thread calls GetStringAsync()
//     → makes a syscall to the OS
//     → OS takes over the I/O completely
//     → thread hits return in MoveNext()
//     → thread goes BACK TO THE THREAD POOL
//     → thread is now free to handle other requests
//
//   [200ms later — NIC receives response]
//     → NIC fires a hardware interrupt
//     → OS kernel processes TCP packets
//     → OS signals .NET via I/O Completion Port (Windows) or epoll (Linux)
//     → .NET background listener thread sees the signal
//     → posts MoveNext() to the Thread Pool queue
//     → a thread pool thread picks it up
//     → MoveNext() runs again, state machine resumes

// BLOCKING vs ASYNC comparison at scale:
//   Blocking:  10,000 requests needs 10,000 threads × 1MB stack = ~10GB RAM
//   Async:     10,000 requests needs ~20 threads + Tasks objects on heap = few MB


// ─────────────────────────────────────────────────────────────────────────────
//  SECTION 5 — OS LEVEL MECHANICS (IOCP)
// ─────────────────────────────────────────────────────────────────────────────

// On Windows the .NET runtime uses I/O Completion Ports (IOCP).
// On Linux it uses epoll (or io_uring on newer kernels).

// IOCP FLOW:
//   1. httpClient.GetStringAsync() internally calls WSARecv() — Windows async socket API
//   2. Passes an IOCP handle to the kernel
//      "When data arrives, post a completion packet to THIS port"
//   3. Thread returns. Goes back to thread pool. Done.
//   4. [I/O completes] NIC fires hardware interrupt
//   5. OS kernel processes TCP packets, posts completion packet to IOCP queue
//   6. .NET's dedicated IOCP listener thread (NOT from pool) sees the packet
//   7. It finds the callback stored in the Tasks
//   8. Posts it to the ThreadPool queue
//   9. A thread pool thread picks it up and calls MoveNext()

// IOCP LISTENER THREAD:
//   — This is a special background thread managed by .NET
//   — It is NOT from the thread pool
//   — Its only job is to sit blocked on the IOCP queue
//   — It wakes when OS signals completion
//   — It does NOT do your work — it only dispatches it to the thread pool

// THE STATE MACHINE STRUCT ON THE HEAP:
//   When AwaitUnsafeOnCompleted() is called, the struct is BOXED onto the heap.
//   This is critical — the struct now lives independently of any thread stack.
//   No thread owns it. It just sits in memory holding all the state.
//   When MoveNext() is called again (possibly by a different thread),
//   it reads the state and local variables from this heap object.
//   Thread A might suspend it. Thread B might resume it.
//   The heap object is the handoff point between threads.


// ─────────────────────────────────────────────────────────────────────────────
//  SECTION 6 — TaskAwaiter AND AsyncTaskMethodBuilder
// ─────────────────────────────────────────────────────────────────────────────

// TASKAWAITER<T>
// A wrapper around the Tasks you are WAITING FOR (the input side).
// Obtained by calling task.GetAwaiter().
//
// It gives you exactly 3 things:
//   awaiter.IsCompleted    — is the task already done? (fast path check)
//   awaiter.GetResult()    — get the result, or rethrow exception properly
//   awaiter.OnCompleted()  — register a callback to run when task finishes
//
// Why not use the Tasks directly?
//   Tasks.Result blocks the thread — it is dangerous.
//   TaskAwaiter.GetResult() is designed for the non-blocking callback pattern.
//   It also handles exception propagation correctly (preserves stack trace).
//
// TaskAwaiter is the BRIDGE between the Tasks and the state machine.
// It is stored as a field on the state machine struct so it survives suspension.
//
// If the awaited task returns void (like Tasks.Delay), use TaskAwaiter (non-generic).
// If the awaited task returns a value, use TaskAwaiter<T>.

// ASYNCTASKMETHODBUILDER<T>
// Owns and controls the Tasks that YOUR method returns to the caller (the output side).
//
// It has two jobs:
//
//   JOB 1 — Creates the Tasks that is returned to the caller
//     builder = AsyncTaskMethodBuilder<string>.Create();
//     return builder.Tasks;   ← caller gets this, starts awaiting it
//
//   JOB 2 — Completes or faults that Tasks when the method finishes
//     builder.SetResult("done");     ← marks Tasks completed with value
//                                       triggers any continuations waiting on it
//     builder.SetException(ex);      ← marks Tasks faulted
//                                       exception rethrown at caller's await point

// THINK OF THEM AS TWO ENDS OF A PIPE:
//   TaskAwaiter     = watching the task you DEPEND ON   (input)
//   MethodBuilder   = controlling the task YOU OWN      (output)
//
//   Caller awaits YOUR Tasks (builder.Tasks)
//   You await THEIR Tasks (via awaiter)
//   When their Tasks completes → awaiter fires → your code runs → builder.SetResult()
//   → your Tasks completes → caller resumes


// ─────────────────────────────────────────────────────────────────────────────
//  SECTION 7 — MULTIPLE ASYNC METHODS AND THEIR STATE MACHINES
// ─────────────────────────────────────────────────────────────────────────────

// ONE STATE MACHINE PER METHOD:
//   Each async method gets exactly one state machine struct.
//   3 async methods = 3 structs, regardless of how many awaits each has.

// MULTIPLE CALLS TO THE SAME METHOD:
//   Each call creates a NEW, INDEPENDENT instance of that state machine on the heap.
//   They do not share state. They do not interfere.
//   Each has its own _state, its own local variables, its own awaiter.
//   Each is cleaned up by the GC independently after it completes.

// NESTED ASYNC CALLS (method calls another async method):
//   Each method gets its own state machine.
//   They form a CHAIN on the heap.
//   The inner state machine's builder.Tasks is what the outer state machine awaits.
//   When inner SM completes its Tasks → outer SM's MoveNext() is triggered.
//   Chain propagates upward until the topmost caller's Tasks completes.
//
//   Example chain:
//     PlaceOrderAsync_SM awaits Tasks B (owned by GetUserAsync_SM)
//     GetUserAsync_SM awaits Tasks C (DB query, owned by OS)
//     OS completes Tasks C
//       → GetUserAsync_SM resumes → completes Tasks B
//         → PlaceOrderAsync_SM resumes → completes Tasks A
//           → HTTP response sent to client

// LOCAL VARIABLES ACROSS AWAIT POINTS:
//   In normal synchronous code, local variables live on the thread stack.
//   When a thread leaves, its stack is gone.
//   In async code, local variables that are used after an await point
//   are promoted to FIELDS on the state machine struct.
//   Since the struct is on the heap, they survive across suspension points
//   even if different threads execute different parts of the method.

// CAPTURED VARIABLES AND 'this':
//   If your async method uses 'this' (calls other methods on the same class)
//   or captures outer variables, these are stored as fields on the struct too.
//   Example: PlaceOrderAsync_SM stores _this = the service instance
//   so it can call GetUserAsync() and ChargePaymentAsync() when it resumes.

// LIFECYCLE OF A STATE MACHINE:
//   Created     → bootstrapper creates the struct and boxes it to heap
//   Suspended   → struct sits on heap, no thread running it
//   Resumed     → thread pool thread calls MoveNext()
//   Completed   → _state = -2, builder.SetResult() or SetException() called
//   Collected   → GC cleans it up after Tasks is no longer referenced


// ─────────────────────────────────────────────────────────────────────────────
//  SECTION 8 — OTHER IMPORTANT CONCEPTS
// ─────────────────────────────────────────────────────────────────────────────

// SYNCHRONIZATIONCONTEXT:
//   After I/O completes, the runtime checks: is there a SynchronizationContext?
//   In ASP.NET Core / Console: no context → resumes on any thread pool thread
//   In WPF / WinForms: has a UI context → marshals continuation back to UI thread
//   This ensures UI updates happen on the UI thread automatically.

// CONFIGUREAWAIT(FALSE):
//   await task.ConfigureAwait(false)
//   Tells the runtime: do NOT capture the SynchronizationContext.
//   Resume on any available thread pool thread.
//   Use this in library code to avoid deadlocks and improve performance.
//   Do NOT use in UI code where you need to update UI elements after await.

// CLASSIC DEADLOCK SCENARIO:
//   In WinForms or WPF, calling .Result or .Wait() on an async method DEADLOCKS.
//   Reason:
//     — .Result blocks the UI thread
//     — The async continuation needs to get back to the UI thread (SynchronizationContext)
//     — UI thread is blocked waiting for the task
//     — Tasks is waiting for the UI thread
//     — Neither side moves — deadlock
//   Fix: use ConfigureAwait(false) in the async method, or await properly end to end.

// ISCOMPETED FAST PATH:
//   Before suspending, the state machine always checks awaiter.IsCompleted.
//   If the task is already done (cached result, completed synchronously),
//   MoveNext() does NOT suspend — it falls through immediately.
//   No continuation is registered. No heap boxing. Much faster.
//   This is why methods that often return cached results use ValueTask.

// TASK vs VALUETASK:
//   Tasks<T>     — always allocates on the heap, even for synchronous results
//   ValueTask<T>— stack allocated (struct) when result is already available
//                 avoids heap allocation on the hot/sync path
//                 can only be awaited ONCE — do not store and await multiple times
//   Use Tasks<T> by default.
//   Use ValueTask<T> in high-performance code where the method often completes sync.

// ASYNC VOID — WHY IT IS DANGEROUS:
//   async void methods cannot be awaited.
//   Exceptions thrown inside them cannot be caught by the caller.
//   They can crash the entire process if an unhandled exception escapes.
//   Only valid use case: event handlers (Button.Click etc.)
//   Always use async Tasks instead of async void everywhere else.

// TASK.WHENALL vs TASK.WHENANY:
//   Tasks.WhenAll(t1, t2, t3)
//     — waits for ALL tasks to complete
//     — collects ALL exceptions into AggregateException
//     — use when you need results from all tasks
//
//   Tasks.WhenAny(t1, t2, t3)
//     — returns when the FIRST task completes
//     — other tasks keep running unless you cancel them
//     — use for timeouts or race conditions

// CANCELLATIONTOKEN:
//   Pass a CancellationToken to async methods to support cancellation.
//   The token is checked at await points — if cancelled, throws OperationCanceledException.
//   Always accept and pass CancellationToken in library/service code.
//   Use CancellationTokenSource to create and trigger cancellation.

// EXCEPTION HANDLING:
//   Exceptions in async methods are caught by the state machine
//   and stored inside the Tasks (via builder.SetException).
//   They are NOT thrown immediately when the async method is called.
//   They are rethrown at the await point in the caller when GetResult() is called.
//   With Tasks.WhenAll, all exceptions are wrapped in AggregateException.


// ─────────────────────────────────────────────────────────────────────────────
//  SECTION 9 — INTERVIEW CHEAT SHEET
// ─────────────────────────────────────────────────────────────────────────────

// Q: What does async do to a method?
// A: Triggers the compiler to rewrite it as a state machine struct.

// Q: Does async alone make things concurrent?
// A: No. You need actual async I/O or Tasks.Run for concurrency.

// Q: What does await do?
// A: Checks if the task is done. If not, saves state, registers MoveNext as
//    a callback inside the Tasks, and returns — freeing the thread.

// Q: Who actually does the I/O work (network, disk)?
// A: The OS and Network Card. No .NET thread is involved during the I/O itself.

// Q: Where does execution resume after await?
// A: Depends on SynchronizationContext. Thread pool by default (ASP.NET Core).
//    UI thread if there is a UI context (WPF/WinForms).

// Q: What is ConfigureAwait(false)?
// A: Skip context capture — resume on any thread pool thread.
//    Use in library code to avoid deadlocks and improve performance.

// Q: What causes a deadlock in async code?
// A: Calling .Result or .Wait() on an async method in a context that has a
//    SynchronizationContext (like WinForms/WPF UI thread).

// Q: What is GetAwaiter().IsCompleted?
// A: Fast path — if already done, skip suspension entirely. No allocation.

// Q: Why is async void dangerous?
// A: Cannot be awaited. Exceptions cannot be caught. Can crash the process.

// Q: Difference between Tasks and ValueTask?
// A: Tasks always heap-allocates. ValueTask is a struct — avoids allocation
//    when the result is already available synchronously.

// Q: How many state machines are generated for one async method?
// A: Exactly one per method. Multiple awaits = multiple states in one machine.

// Q: What happens to local variables across await points?
// A: They are promoted to fields on the state machine struct on the heap.
//    They survive suspension because the struct lives independently of any thread.

// Q: What is AsyncTaskMethodBuilder?
// A: Owns the Tasks returned to the caller. Calls SetResult or SetException
//    to complete that Tasks when the async method finishes.

// Q: What is TaskAwaiter?
// A: Wrapper around the Tasks being awaited. Provides IsCompleted, GetResult,
//    and OnCompleted — the bridge between the Tasks and the state machine.

// WHAT MATTERS MOST BY LEVEL:
//   Junior/Mid  : async/await basics, Tasks, deadlock, ConfigureAwait(false)
//   Senior      : state machine concept, ValueTask, WhenAll/WhenAny, cancellation
//   Principal+  : IOCP, boxing of state machine, SynchronizationContext internals

