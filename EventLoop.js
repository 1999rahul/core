// =============================================================================
// JAVASCRIPT EVENT LOOP — COMPLETE GUIDE
// Topics: Call Stack, Event Loop, Microtasks vs Macrotasks
// =============================================================================
// HOW TO USE THIS FILE:
//   Run it with Node.js:  node event_loop_explained.js
//   Each section is separated and labelled. Comment/uncomment sections
//   as needed to run examples individually.
// =============================================================================




// =============================================================================
// SECTION 1: THE CALL STACK
// =============================================================================
// The call stack is a LIFO (Last In, First Out) structure.
// Every time a function is called, it is PUSHED onto the stack.
// When it returns, it is POPPED off.
// JavaScript is single-threaded — only one function runs at a time.
// =============================================================================

console.log("============ SECTION 1: CALL STACK ============");

function c() {
  console.log("  c() is running");
  // At this point, the call stack looks like:
  // [ main() → a() → b() → c() ]  ← top of stack
}

function b() {
  console.log("  b() called c()");
  c();
  // c() is now popped, back to b()
}

function a() {
  console.log("  a() called b()");
  b();
  // b() is now popped, back to a()
}

a();
// a() is now popped. Stack is empty.

// Output:
//   a() called b()
//   b() called c()
//   c() is running




// =============================================================================
// SECTION 2: THE EVENT LOOP — WHAT IT ACTUALLY IS
// =============================================================================
// The event loop is just a continuous check:
//   "Is the call stack empty? If yes, is there anything in a queue? Push it."
//
// It follows this priority order every single tick:
//   1. Run current synchronous code (call stack)
//   2. Drain entire microtask queue
//   3. Run ONE macrotask
//   4. Drain entire microtask queue again
//   5. Repeat
// =============================================================================

console.log("\n============ SECTION 2: EVENT LOOP BASICS ============");

console.log("  [1] sync — runs immediately on the call stack");

setTimeout(() => {
  console.log("  [3] macrotask — setTimeout with 0ms delay");
}, 0);
// Even with 0ms, this goes to the macrotask queue.
// It only runs AFTER the current script and all microtasks are done.

Promise.resolve().then(() => {
  console.log("  [2] microtask — Promise.then");
});
// This goes to the microtask queue.
// Runs before the setTimeout above.

// Output:
//   [1] sync — runs immediately on the call stack
//   [2] microtask — Promise.then
//   [3] macrotask — setTimeout with 0ms delay




// =============================================================================
// SECTION 3: MACROTASKS
// =============================================================================
// Macrotasks (also called "tasks") represent work that is scheduled
// for a FUTURE, INDEPENDENT turn of the event loop.
//
// APIs that queue macrotasks:
//   - setTimeout(fn, delay)
//   - setInterval(fn, delay)
//   - setImmediate(fn)         [Node.js only]
//   - I/O callbacks            [file reads, network in Node.js]
//   - UI event handlers        [click, keydown in browsers]
//
// Rule: Only ONE macrotask is picked per event loop iteration.
// =============================================================================

console.log("\n============ SECTION 3: MACROTASKS ============");

console.log("  start");

setTimeout(() => console.log("  timeout 1 (100ms)"), 100);
setTimeout(() => console.log("  timeout 2 (0ms)"),   0);
setTimeout(() => console.log("  timeout 3 (50ms)"),  50);

console.log("  end");

// Output order:
//   start
//   end
//   timeout 2 (0ms)    ← shortest delay runs first
//   timeout 3 (50ms)
//   timeout 1 (100ms)


// =============================================================================
// SECTION 4: MICROTASKS
// =============================================================================
// Microtasks are for reacting to something that ALREADY happened in the
// current execution — like a promise that just resolved.
//
// APIs that queue microtasks:
//   - Promise.then / .catch / .finally
//   - async/await  (code after "await" is a microtask continuation)
//   - queueMicrotask(fn)
//   - MutationObserver   [browsers only]
//
// Rule: ALL microtasks are drained after every task (including after the
//       main script), before the next macrotask is picked.
//       Microtasks can queue MORE microtasks and they still all run first.
// =============================================================================

console.log("\n============ SECTION 4: MICROTASKS ============");

console.log("  start");

queueMicrotask(() => console.log("  microtask via queueMicrotask"));
Promise.resolve().then(() => console.log("  microtask via Promise.then"));
setTimeout(() => console.log("  macrotask via setTimeout"), 0);

console.log("  end");

// Output:
//   start
//   end
//   microtask via queueMicrotask    ← queued first, runs first
//   microtask via Promise.then
//   macrotask via setTimeout        ← runs last




// =============================================================================
// SECTION 5: MICROTASKS DRAINING — THE COMPLETE FLUSH RULE
// =============================================================================
// Microtasks can add MORE microtasks. They ALL run before macrotasks.
// This is called "draining" the microtask queue.
// =============================================================================

console.log("\n============ SECTION 5: MICROTASK DRAINING ============");

Promise.resolve()
  .then(() => {
    console.log("  micro 1");
    // Queuing a new microtask from inside a microtask
    return Promise.resolve();
  })
  .then(() => console.log("  micro 2"))  // still runs before macrotask
  .then(() => console.log("  micro 3")); // still runs before macrotask

setTimeout(() => console.log("  macro — runs after ALL microtasks"), 0);

// Output:
//   micro 1
//   micro 2
//   micro 3
//   macro — runs after ALL microtasks




// =============================================================================
// SECTION 6: PROMISE CONSTRUCTOR TRAP
// =============================================================================
// The Promise executor function (the callback passed to `new Promise`)
// runs SYNCHRONOUSLY. Only .then() / .catch() is async.
// This trips up many candidates in interviews.
// =============================================================================

console.log("\n============ SECTION 6: PROMISE EXECUTOR IS SYNC ============");

console.log("  1");

new Promise((resolve) => {
  console.log("  2 — executor runs synchronously!");
  resolve();
}).then(() => console.log("  4 — .then is async (microtask)"));

console.log("  3");

// Output:
//   1
//   2 — executor runs synchronously!
//   3
//   4 — .then is async (microtask)




// =============================================================================
// SECTION 7: async / await UNDER THE HOOD
// =============================================================================
// `async/await` is syntactic sugar over Promises.
// Everything BEFORE the first `await` runs synchronously.
// Everything AFTER `await` is scheduled as a microtask continuation.
// The function suspends at `await` and control returns to the caller.
// =============================================================================

console.log("\n============ SECTION 7: ASYNC / AWAIT ============");

async function fetchData() {
  console.log("  [fetchData] A — sync, before await");
  await Promise.resolve();               // suspends here
  console.log("  [fetchData] B — microtask, after await");
  await Promise.resolve();               // suspends again
  console.log("  [fetchData] C — microtask, after second await");
}

console.log("  start");
fetchData();                             // call does not block
console.log("  end — fetchData is suspended at await");
setTimeout(() => console.log("  macrotask — runs after all microtasks"), 0);

// Output:
//   start
//   [fetchData] A — sync, before await
//   end — fetchData is suspended at await
//   [fetchData] B — microtask, after await
//   [fetchData] C — microtask, after second await
//   macrotask — runs after all microtasks




// =============================================================================
// SECTION 8: PROMISE CHAINING TICK COST
// =============================================================================
// When you return a new Promise (or Promise.resolve()) inside .then(),
// it costs TWO microtask ticks to unwrap — not one.
// This means other queued microtasks can sneak in between chained steps.
// =============================================================================

console.log("\n============ SECTION 8: PROMISE CHAIN TICK COST ============");

Promise.resolve()
  .then(() => {
    console.log("  chain A — step 1");
    return Promise.resolve(); // returning a new promise = 2 extra ticks to settle
  })
  .then(() => console.log("  chain A — step 2 (delayed by 2 ticks)"));

Promise.resolve()
  .then(() => console.log("  chain B — sneaks in before chain A step 2"));

// Output:
//   chain A — step 1
//   chain B — sneaks in before chain A step 2
//   chain A — step 2 (delayed by 2 ticks)




// =============================================================================
// SECTION 9: setTimeout INSIDE A PROMISE (Queue Interleaving)
// =============================================================================
// When a macrotask is registered INSIDE a microtask, it goes behind
// any macrotasks already waiting in the queue.
// =============================================================================

console.log("\n============ SECTION 9: QUEUE INTERLEAVING ============");

Promise.resolve().then(() => {
  console.log("  micro 1");
  setTimeout(() => console.log("  macro registered INSIDE micro (runs last)"), 0);
});

setTimeout(() => console.log("  macro registered BEFORE micro (runs first)"), 0);

// Output:
//   micro 1
//   macro registered BEFORE micro (runs first)
//   macro registered INSIDE micro (runs last)




// =============================================================================
// SECTION 10: INTERVIEW LEVEL EXAMPLES (Easy → Hard)
// =============================================================================

console.log("\n============ SECTION 10: INTERVIEW EXAMPLES ============");


// --- Easy ---

console.log("\n  [Easy 1] Basic sync + setTimeout + Promise");

console.log("  1");
setTimeout(() => console.log("  2 — macro"), 0);
Promise.resolve().then(() => console.log("  3 — micro"));
console.log("  4");
// Output: 1 → 4 → 3 → 2


// --- Easy-Medium ---

console.log("\n  [Easy-Medium 2] Multiple queued Promises");

console.log("  start");
Promise.resolve().then(() => console.log("  p1"));
Promise.resolve().then(() => console.log("  p2"));
Promise.resolve().then(() => console.log("  p3"));
setTimeout(() => console.log("  macro"), 0);
console.log("  end");
// Output: start → end → p1 → p2 → p3 → macro


// --- Medium ---

console.log("\n  [Medium 3] Classic hard question — mixing queues");

console.log("  1");
setTimeout(() => console.log("  2 — macro"), 0);
Promise.resolve()
  .then(() => {
    console.log("  3 — micro");
    setTimeout(() => console.log("  4 — macro from inside micro"), 0);
  })
  .then(() => console.log("  5 — chained micro"));
console.log("  6");
// Output: 1 → 6 → 3 → 5 → 2 → 4


// --- Hard ---

console.log("\n  [Hard 4] async/await full mix — most common interview question");

async function async1() {
  console.log("  async1 start");  // sync
  await async2();                 // suspends, schedules continuation as microtask
  console.log("  async1 end");   // microtask
}

async function async2() {
  console.log("  async2");       // sync inside async2
}

console.log("  script start");
setTimeout(() => console.log("  setTimeout"), 0);
async1();
new Promise((resolve) => {
  console.log("  promise1");     // executor is sync
  resolve();
}).then(() => console.log("  promise2")); // microtask
console.log("  script end");

// Output:
//   script start
//   async1 start
//   async2
//   promise1
//   script end
//   async1 end      ← microtask, queued before promise2's .then
//   promise2        ← microtask
//   setTimeout      ← macrotask, runs last




// =============================================================================
// SECTION 11: REAL-WORLD USE CASES
// =============================================================================

console.log("\n============ SECTION 11: REAL-WORLD PATTERNS ============");


// Pattern 1: Using queueMicrotask for batching DOM updates
// (In a browser, you'd batch multiple state changes and apply once)
let pendingUpdates = [];

function scheduleUpdate(value) {
  pendingUpdates.push(value);
  queueMicrotask(flushUpdates); // deferred but still in current task
}

function flushUpdates() {
  if (pendingUpdates.length === 0) return;
  const batch = [...pendingUpdates];
  pendingUpdates = [];
  console.log("  Flushing batch:", batch);
}

scheduleUpdate("user_name");
scheduleUpdate("user_email");
scheduleUpdate("user_avatar");
// All three are batched and flushed together in one microtask
// Output: Flushing batch: [ 'user_name', 'user_email', 'user_avatar' ]


// Pattern 2: Promise.all — runs microtasks concurrently
console.log("\n  [Pattern 2] Promise.all");

async function runAll() {
  const [a, b, c] = await Promise.all([
    Promise.resolve("result A"),
    Promise.resolve("result B"),
    Promise.resolve("result C"),
  ]);
  console.log("  All resolved:", a, b, c);
}

runAll();
// All three promises settle in parallel.
// Promise.all waits for all, then gives results as an array.


// Pattern 3: Danger — infinite microtask loop (DO NOT RUN in production)
// function infiniteLoop() {
//   Promise.resolve().then(infiniteLoop); // never lets macrotasks run
// }
// infiniteLoop(); // freezes the event loop — browser tab becomes unresponsive




// =============================================================================
// SECTION 12: MENTAL CHECKLIST FOR INTERVIEWS
// =============================================================================
//
// When you see async code in an interview, do this mentally:
//
//  STEP 1 — Read top to bottom and sort each line into a bucket:
//
//    BUCKET 1 (runs immediately, in order):
//      - All regular statements
//      - Code inside Promise executor  (new Promise((resolve) => { ... }))
//      - Code inside async function BEFORE the first await
//      - console.log, assignments, function calls, etc.
//
//    BUCKET 2 — Microtask queue (runs after current task, in order queued):
//      - Promise.then / .catch / .finally callbacks
//      - Code after each `await` inside async functions
//      - queueMicrotask(fn) callbacks
//
//    BUCKET 3 — Macrotask queue (runs one per tick, after microtasks drain):
//      - setTimeout callbacks
//      - setInterval callbacks
//      - setImmediate callbacks (Node.js)
//      - I/O callbacks
//
//  STEP 2 — Read output from buckets in order: 1 → 2 → 3
//
//  STEP 3 — Watch for these traps:
//    - Promise executor is SYNC (goes in bucket 1, not 2)
//    - `await` splits the function: before = sync, after = microtask
//    - Returning Promise.resolve() inside .then() costs 2 extra ticks
//    - Macrotasks registered inside microtasks go BEHIND existing macrotasks
//    - Microtasks can queue more microtasks — all drain before any macrotask
//
// =============================================================================