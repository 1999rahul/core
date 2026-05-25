// =============================================================================
// ASP.NET Core Threading Model — Study Notes
// =============================================================================
// Topics covered:
//   1. CLR Thread Pool
//   2. Request Lifecycle
//   3. async/await — State Machines
//   4. Memory Layout (Heap vs Stack)
//   5. Request Queuing
//   6. CLR Internal Threads
//   7. Process Memory Model
// =============================================================================


// -----------------------------------------------------------------------------
// 1. CLR THREAD POOL
// -----------------------------------------------------------------------------
//
// - The CLR (Common Language Runtime) manages a thread pool inside your process.
// - You do NOT create threads manually per request.
// - At startup: pre-creates threads roughly equal to CPU core count.
// - Under load: injects ~1 new thread per second (deliberate, cautious).
// - After load drops: retires idle threads back to baseline.
//
// Why slow injection?
//   Avoids "over-subscription" — too many threads competing for CPU causes
//   excessive context switching, which wastes CPU time doing bookkeeping
//   instead of real work.
//
//   8 cores + 16 threads  => minimal context switching, efficient
//   8 cores + 1000 threads => constant context switching, CPU wastes cycles
//
// Configure thread pool limits (use carefully):
//
//   ThreadPool.SetMinThreads(workerThreads: 16, completionPortThreads: 16);
//   ThreadPool.SetMaxThreads(workerThreads: 100, completionPortThreads: 100);


// -----------------------------------------------------------------------------
// 2. REQUEST LIFECYCLE (Synchronous View)
// -----------------------------------------------------------------------------
//
// When an HTTP request arrives:
//
//   OS Network Buffer (kernel)
//       └── TCP packets sit here until runtime reads them
//
//   Kestrel (ASP.NET's built-in web server)
//       └── Reads OS buffer, parses raw TCP bytes into HTTP request
//       └── Posts a work item to the CLR Thread Pool Queue
//
//   CLR Thread Pool Queue
//       └── Lightweight work items (pointers/callbacks) waiting for a free thread
//
//   Thread Pool Thread
//       └── Picks up work item
//       └── Runs middleware pipeline: Auth → Routing → Controller → Response
//       └── Returns to pool after response is written
//
//
// What if all threads are busy and a new request arrives?
//
//   1. Request is queued in the CLR thread pool queue.
//   2. CLR waits briefly to see if a thread frees up.
//   3. If no thread frees up, CLR injects a NEW thread (~1 per second cadence).
//   4. If queue fills up entirely => Kestrel returns 503 Service Unavailable.


// -----------------------------------------------------------------------------
// 3. async/await — THE STATE MACHINE
// -----------------------------------------------------------------------------
//
// The C# compiler transforms every async method into a STATE MACHINE at
// compile time. The state machine is a plain C# object allocated on the heap.
//
// Example:
//
//   public async Tasks<User> GetUserAsync(int id)
//   {
//       var user   = await db.GetAsync(id);        // suspension point 1
//       var orders = await cache.GetAsync(user.Id); // suspension point 2
//       return user;
//   }
//
// Compiler rewrites this roughly as:
//
//   State 0: Call db.GetAsync(id), save [id], advance to State 1, RELEASE THREAD
//   State 1: Call cache.GetAsync(user.Id), save [user], advance to State 2, RELEASE THREAD
//   State 2: Return user, state machine eligible for GC
//
// Key insight:
//   When a thread hits an await, the thread is RELEASED back to the pool.
//   The state machine object stays alive on the heap, holding all local variables.
//   When I/O completes, ANY free thread picks up the continuation.
//
// Execution flow with async/await:
//
//   Request arrives
//       └── Thread picked from pool
//       └── Hits `await db.GetAsync()`
//       └── State machine created on heap, thread RELEASED
//
//       [Thread is free — handles other requests]
//
//       └── DB responds, I/O completes
//       └── CLR picks any free thread from pool
//       └── Thread resumes from State 1
//       └── Hits `await cache.GetAsync()`
//       └── Thread RELEASED again
//
//       [Thread is free again]
//
//       └── Cache responds
//       └── Thread resumes, method completes
//       └── State machine object eligible for GC


// -----------------------------------------------------------------------------
// 4. MEMORY COMPARISON — Threads vs State Machines
// -----------------------------------------------------------------------------
//
// Thread stack:       ~1 MB  (native OS memory, outside GC heap, fixed address)
// State machine obj:  ~few hundred bytes to a few KB (managed heap, GC controlled)
//
// 10,000 concurrent requests comparison:
//
//   Thread-per-request:  10,000 × 1 MB  = ~10 GB  ← not viable
//   Async state machines: 10,000 × ~1 KB = ~10 MB  ← very manageable
//
// async/await trades expensive thread stacks for tiny heap objects.
// This is the core scalability win.
//
// Why thread stacks are in native memory (not GC heap):
//   The GC compacts the heap — it moves objects around to reduce fragmentation.
//   If a thread stack moved mid-execution, every return address and pointer
//   on it would become invalid => instant crash.
//   Thread stacks need fixed, stable memory addresses, so they live outside GC.


// -----------------------------------------------------------------------------
// 5. THREAD POOL STARVATION (What goes wrong without async)
// -----------------------------------------------------------------------------
//
// BAD — blocks the thread during DB I/O, thread sits idle in pool:
//
//   public IActionResult GetUsers()
//   {
//       var users = db.Users.ToList(); // thread blocked here waiting for DB
//       return Ok(users);
//   }
//
// GOOD — releases thread back to pool during I/O wait:
//
//   public async Tasks<IActionResult> GetUsersAsync()
//   {
//       var users = await db.Users.ToListAsync(); // thread freed during DB wait
//       return Ok(users);
//   }
//
// Without async:
//   Threads block on I/O → pool exhausts → new requests queue →
//   CLR injects threads slowly (1/sec) → latency spikes → potential 503s
//
// With async:
//   Threads freed during I/O → pool stays available → CLR never needs
//   to inject new threads → handles high concurrency with small pool


// -----------------------------------------------------------------------------
// 6. PROCESS MEMORY LAYOUT
// -----------------------------------------------------------------------------
//
// When you run `dotnet MyApp.dll`, the OS creates ONE process.
// The CLR is loaded INTO that process. Everything lives in one memory space.
//
//   Process Memory Space
//   │
//   ├── Managed Heap (GC controlled)
//   │     ├── CLR thread pool manager object
//   │     ├── Work item queue (pending requests)
//   │     ├── Async state machine objects
//   │     ├── Your controllers, services, DbContext, etc.
//   │     └── All regular application objects
//   │
//   ├── Native Heap (outside GC)
//   │     ├── CLR internals
//   │     └── Unmanaged library allocations
//   │
//   └── Thread Stacks (fixed native memory, outside GC)
//         ├── Main thread stack
//         ├── Thread pool thread 1 stack (~1 MB)
//         ├── Thread pool thread 2 stack (~1 MB)
//         └── ... one per active thread
//
// Outside the process (kernel memory):
//   - OS TCP socket buffers (your process holds a handle/reference only)
//   - Other processes (completely separate memory spaces)


// -----------------------------------------------------------------------------
// 7. CLR INTERNAL THREADS
// -----------------------------------------------------------------------------
//
// The CLR spins up several hidden background threads inside your process.
// You cannot access or control them from application code.
//
//   Your Process Threads
//   │
//   ├── Main Thread
//   │     └── Runs Program.cs / app startup
//   │
//   ├── Thread Pool Threads (handle your requests)
//   │     ├── Thread 1 — running middleware pipeline
//   │     ├── Thread 2 — running middleware pipeline
//   │     └── ...
//   │
//   └── CLR Internal Threads (hidden, automatic)
//         ├── GC Thread         — runs garbage collection
//         ├── Finalizer Thread  — cleans up unmanaged resources
//         ├── Debugger Thread   — handles debug/profiler events
//         └── Thread Pool Manager Thread — monitors pool, injects threads
//
//
// The GC "Stop the World" pause:
//
//   Generation 0/1 GC  => fast, minor interruption
//   Generation 2 / Full GC => GC suspends ALL threads briefly to scan heap
//
//   Normal:   Thread 1 [running] Thread 2 [running] Thread 3 [running]
//   Full GC:  Thread 1 [FROZEN]  Thread 2 [FROZEN]  Thread 3 [FROZEN]
//                 └── GC scans entire heap
//             Thread 1 [resumed] Thread 2 [resumed] Thread 3 [resumed]
//
// This is why minimizing allocations matters in high-perf ASP.NET apps —
// every object is potential GC pressure and a potential "stop the world" pause.


// -----------------------------------------------------------------------------
// 8. IDLE THREADS — DO THEY STEAL CPU FROM OTHER PROCESSES?
// -----------------------------------------------------------------------------
//
// No. Idle threads consume NO CPU.
//
// The OS scheduler only gives CPU time to RUNNABLE threads.
// An idle thread pool thread is in a WAITING/SUSPENDED state — the scheduler
// simply skips it. No CPU cycles consumed.
//
// Cost of a pre-created idle thread:
//   CPU cost:    Zero (scheduler ignores waiting threads)
//   Memory cost: ~1 MB stack (native memory) — accepted trade-off for
//                zero thread-spawn latency when requests arrive
//
// Spawning a thread on demand = expensive OS kernel call = added latency.
// Pre-creating threads = pay tiny memory cost upfront, get instant availability.


// =============================================================================
// QUICK REFERENCE SUMMARY
// =============================================================================
//
//  Concept                  | Location          | Size        | GC managed?
//  -------------------------|-------------------|-------------|------------
//  Thread stack             | Native OS memory  | ~1 MB each  | No
//  Async state machine      | Managed heap      | ~few KB     | Yes
//  Thread pool manager      | Managed heap      | small obj   | Yes
//  Work item queue          | Managed heap      | small ptrs  | Yes
//  CLR itself               | Inside process    | —           | —
//  OS socket buffers        | Kernel memory     | —           | No (kernel)
//
// =============================================================================