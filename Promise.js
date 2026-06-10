// ============================================================
//         ASYNC JAVASCRIPT & PROMISES — COMPLETE GUIDE
//         Covers everything discussed in this session
// ============================================================




// ============================================================
// SECTION 1 — SYNCHRONOUS VS ASYNCHRONOUS JAVASCRIPT
// ============================================================

// JavaScript is single-threaded — one operation at a time.
// Synchronous code blocks the thread until the operation finishes.

console.log("-- SECTION 1: Sync vs Async --");

// Synchronous — runs in order, blocks until done
console.log("First");
console.log("Second");
console.log("Third");
// Output: First → Second → Third

// Asynchronous — starts an operation, moves on, handles result later
console.log("First");
setTimeout(() => console.log("Second (async)"), 1000);
console.log("Third");
// Output: First → Third → Second (after 1s)




// ============================================================
// SECTION 2 — THE EVENT LOOP & TASK QUEUES
// ============================================================

// JavaScript uses an Event Loop to handle async operations.
//
// Components:
//   - Call Stack        : where currently executing functions live
//   - Microtask Queue   : Promise callbacks (.then/.catch) — HIGH priority
//   - Macrotask Queue   : setTimeout, setInterval, DOM events — LOW priority
//
// Priority order: Call Stack → Microtask Queue → Macrotask Queue
//
// The Event Loop rule:
//   After every macrotask, drain the ENTIRE microtask queue before
//   picking the next macrotask.

console.log("\n-- SECTION 2: Event Loop Priority --");

console.log("1 - sync");                                      // call stack
setTimeout(() => console.log("2 - macrotask"), 0);           // macrotask queue
Promise.resolve().then(() => console.log("3 - microtask"));  // microtask queue
console.log("4 - sync");                                      // call stack

// Output:
// 1 - sync
// 4 - sync
// 3 - microtask   ← microtask queue drains BEFORE macrotask
// 2 - macrotask




// ============================================================
// SECTION 3 — WHO PUSHES TO EACH QUEUE?
// ============================================================

// This is the most important distinction most developers miss.
//
// MICROTASK QUEUE — pushed by the JavaScript ENGINE itself (V8, SpiderMonkey)
//   - Promise resolve() / reject() internally schedule .then()/.catch() callbacks
//   - queueMicrotask() lets you push manually
//   - MutationObserver callbacks
//   - These are defined in the ECMAScript specification itself
//
// MACROTASK QUEUE — pushed by the HOST ENVIRONMENT (Browser or Node.js)
//   - setTimeout / setInterval  → Browser's Timer API (C++ thread outside JS)
//   - fetch / XHR               → Browser's Networking Layer (C++)
//   - DOM events (click, etc.)  → Browser's Event System
//   - File I/O in Node.js       → libuv library (C thread pool)
//   - These are NOT in the ECMAScript spec — they are Web APIs / Node APIs
//
// Key mental model:
//   JS Engine world   = call stack + microtask queue + Promises
//   Host Env world    = timers, network, I/O, user input
//   Macrotask queue   = the BRIDGE between host environment and JS engine
//
// When setTimeout fires:
//   1. You call setTimeout(cb, 1000)
//   2. JS engine hands the callback to the browser's Timer API
//   3. Browser's C++ timer counts 1000ms INDEPENDENTLY (JS engine is free)
//   4. After 1000ms, Browser pushes the callback to the macrotask queue
//   5. Event loop picks it up when call stack is empty

console.log("\n-- SECTION 3: Who Pushes to Queues --");

// Manual microtask push — done by the JS engine
queueMicrotask(() => console.log("manually pushed to microtask queue"));

// Macrotask push — done by the browser's Timer API after delay
setTimeout(() => console.log("pushed to macrotask queue by browser Timer API"), 0);

// Promise microtask — pushed by V8 engine internally when resolved
Promise.resolve().then(() => console.log("pushed to microtask queue by V8 via Promise"));

// Output:
// pushed to microtask queue by V8 via Promise
// manually pushed to microtask queue
// pushed to macrotask queue by browser Timer API




// ============================================================
// SECTION 4 — CALLBACKS & CALLBACK HELL
// ============================================================

// A callback is a function passed as an argument, to be called later.
// It was the original way to handle async in JavaScript.

console.log("\n-- SECTION 4: Callbacks --");

function fetchUser(id, callback) {
  setTimeout(() => callback(null, { id, name: "Alice" }), 500);
}

fetchUser(1, (err, user) => {
  if (err) return console.log("Error:", err);
  console.log("User:", user.name);
});

// CALLBACK HELL — deeply nested, hard to read, poor error handling
//
// getUser(userId, (user) => {
//   getOrders(user.id, (orders) => {
//     getOrderDetails(orders[0].id, (details) => {
//       getInvoice(details.invoiceId, (invoice) => {
//         console.log(invoice); // 4 levels deep — and it gets worse
//       });
//     });
//   });
// });
//
// Problems: unreadable, hard to debug, error handling at every level,
// tightly coupled logic. Promises were created to solve this.




// ============================================================
// SECTION 5 — THE PROMISE CONSTRUCTOR
// ============================================================

// A Promise is an object representing the eventual completion or failure
// of an async operation. It is a placeholder for a future value.
//
// 3 States:
//   pending   → initial state, not yet settled
//   fulfilled → resolve() was called, has a value
//   rejected  → reject() was called, has a reason/error
//
// The Promise constructor takes one argument — the executor function.
// The executor runs IMMEDIATELY and SYNCHRONOUSLY when the Promise is created.
// The executor receives two arguments from the JS engine: resolve and reject.
// You do NOT define these — JS gives them to you.

console.log("\n-- SECTION 5: Promise Constructor --");

const myPromise = new Promise((resolve, reject) => {
  console.log("Executor runs immediately (synchronous)");

  const success = true;

  if (success) {
    resolve("Operation successful!");
  } else {
    reject(new Error("Operation failed!"));
  }
});

console.log("This runs after the constructor, before .then()");

myPromise
  .then((result) => console.log(".then():", result))
  .catch((err) => console.log(".catch():", err.message))
  .finally(() => console.log(".finally(): always runs"));

// Output:
// "Executor runs immediately (synchronous)"  ← executor is sync
// "This runs after constructor..."           ← still sync
// ".then(): Operation successful!"           ← microtask, runs after
// ".finally(): always runs"                  ← microtask, runs after




// ============================================================
// SECTION 6 — THE resolve() FUNCTION
// ============================================================

// resolve(value) transitions the promise: pending → fulfilled
// Key behaviors:

console.log("\n-- SECTION 6: resolve() --");

// 1. You can resolve with any value
new Promise((resolve) => resolve(42))
  .then((v) => console.log("Resolved with number:", v));

new Promise((resolve) => resolve({ name: "Alice" }))
  .then((v) => console.log("Resolved with object:", v.name));

new Promise((resolve) => resolve())
  .then((v) => console.log("Resolved with undefined:", v));

// 2. Resolving with another Promise — promise assimilation
//    The outer promise WAITS for and ADOPTS the inner promise's state
const inner = new Promise((resolve) => {
  setTimeout(() => resolve("inner value"), 300);
});

const outer = new Promise((resolve) => {
  resolve(inner); // outer adopts inner's state — waits for inner
});

outer.then((val) => console.log("Promise assimilation — outer got:", val));
// "inner value" after 300ms

// 3. Calling resolve multiple times — only the FIRST call matters
//    A promise can only be settled once. It is frozen after that.
new Promise((resolve) => {
  resolve("first");
  resolve("second"); // ignored
  resolve("third");  // ignored
}).then((v) => console.log("Only first resolve counts:", v));
// "first"




// ============================================================
// SECTION 7 — THE reject() FUNCTION
// ============================================================

// reject(reason) transitions the promise: pending → rejected
// Key behaviors:

console.log("\n-- SECTION 7: reject() --");

// 1. Always reject with an Error object, not a plain string
//    Error objects give you .message, .stack, .name for debugging

// BAD — plain string, no stack trace
// reject("something went wrong");

// GOOD — Error object with full debug info
new Promise((_, reject) => {
  reject(new Error("Something went wrong"));
}).catch((err) => console.log("Proper rejection:", err.message));

// 2. Like resolve, only the first reject call matters
new Promise((_, reject) => {
  reject(new Error("First error"));
  reject(new Error("Second error")); // ignored
}).catch((err) => console.log("Only first reject counts:", err.message));

// 3. Always handle rejections with .catch() or try/catch
//    Unhandled rejections cause warnings in Node.js and errors in browsers




// ============================================================
// SECTION 8 — THROWING INSIDE THE EXECUTOR
// ============================================================

// Throwing inside the executor automatically becomes a rejection.
// The Promise constructor catches synchronous throws for you.
// IMPORTANT: this only works for SYNCHRONOUS throws in the executor.

console.log("\n-- SECTION 8: Throwing in Executor --");

// Synchronous throw → auto-caught → becomes rejection
new Promise(() => {
  throw new Error("Sync throw in executor");
}).catch((err) => console.log("Auto-caught sync throw:", err.message));

// Async throw inside setTimeout → NOT caught by the constructor
// This would CRASH the process — you must manually call reject
new Promise((_, reject) => {
  setTimeout(() => {
    try {
      throw new Error("Async throw");
    } catch (err) {
      reject(err); // must manually catch and call reject
    }
  }, 100);
}).catch((err) => console.log("Manually caught async throw:", err.message));




// ============================================================
// SECTION 9 — WHEN resolve/reject IS CALLED IMMEDIATELY
//             Do we force-push to the microtask queue?
// ============================================================

// YES — but with a critical distinction:
//   - The promise STATE changes synchronously when resolve/reject is called
//   - The .then()/.catch() CALLBACK is what gets pushed to the microtask queue
//   - .then() callbacks are ALWAYS asynchronous — guaranteed by the spec
//   - This is true even if the promise was already settled when .then() is attached

console.log("\n-- SECTION 9: Immediate resolve & microtask queue --");

const p = new Promise((resolve) => {
  resolve("done"); // state becomes 'fulfilled' RIGHT NOW, synchronously
});
// p is already fulfilled here, but .then() hasn't run yet

p.then((val) => console.log(".then() ran via microtask:", val));

console.log("This runs before .then() — even though promise is already settled");

// Output:
// "This runs before .then()..."
// ".then() ran via microtask: done"
//
// The Promises/A+ spec guarantees .then() callbacks are ALWAYS async.
// This ensures consistent behavior — .then() can never sometimes be sync.

// Same behavior with Promise.resolve()
Promise.resolve(42).then((v) => console.log("Promise.resolve microtask:", v));
console.log("Still runs before the .then() above");




// ============================================================
// SECTION 10 — DOES setTimeout RETURN A PROMISE?
// ============================================================

// NO — setTimeout returns a numeric timer ID (an integer).
// That ID is used only if you want to cancel the timer with clearTimeout().
// There is no promise involved in setTimeout at all.
//
// Why does its callback go to the MACROTASK queue?
//   - setTimeout is a Web API, not a JavaScript feature
//   - When called, JS hands the callback to the browser's Timer API (C++)
//   - The browser's timer thread counts the delay independently
//   - After the delay, the BROWSER pushes the callback to the macrotask queue
//   - The JS engine was completely uninvolved during the delay
//   - This is why it's a macrotask — host environment pushed it, not the engine

console.log("\n-- SECTION 10: setTimeout & Promises --");

const timerId = setTimeout(() => {}, 0);
console.log("setTimeout returns a timer ID (number):", timerId); // e.g. 1
clearTimeout(timerId);

// To make setTimeout work with async/await, wrap it in a Promise yourself
function delay(ms) {
  return new Promise((resolve) => {
    setTimeout(resolve, ms); // resolve is passed as the callback
  });
}

async function useDelay() {
  console.log("Before delay");
  await delay(500);
  console.log("After 500ms delay — now setTimeout works with await");
}

useDelay();




// ============================================================
// SECTION 11 — WAYS TO CREATE ALREADY-SETTLED PROMISES
// ============================================================

console.log("\n-- SECTION 11: Already-Settled Promises --");

// WAY 1 — Promise.resolve() and Promise.reject() static methods
//          Most common, clearest signal of intent
const p1 = Promise.resolve("static resolve");
const p2 = Promise.reject(new Error("static reject"));

p1.then((v) => console.log("Way 1 - resolve:", v));
p2.catch((e) => console.log("Way 1 - reject:", e.message));

// WAY 2 — new Promise() with immediate resolve/reject call
//          Use when you need some sync logic before settling
const p3 = new Promise((resolve) => resolve("constructor resolve"));
const p4 = new Promise((_, reject) => reject(new Error("constructor reject")));

p3.then((v) => console.log("Way 2 - resolve:", v));
p4.catch((e) => console.log("Way 2 - reject:", e.message));

// WAY 3 — async function returning a value or throwing
//          An async function always returns a Promise automatically
async function getResolved() {
  return "async return value"; // becomes Promise.resolve("async return value")
}

async function getRejected() {
  throw new Error("async throw"); // becomes Promise.reject(new Error(...))
}

getResolved().then((v) => console.log("Way 3 - resolve:", v));
getRejected().catch((e) => console.log("Way 3 - reject:", e.message));

// WAY 4 — Promise.all / Promise.allSettled with already-settled promises
//          Resolves immediately since all inputs are already settled
Promise.all([
  Promise.resolve(1),
  Promise.resolve(2),
  Promise.resolve(3)
]).then((vals) => console.log("Way 4 - Promise.all:", vals));




// ============================================================
// SECTION 12 — PROMISE CHAINING
// ============================================================

// .then() always returns a NEW promise, enabling chaining.
// Always return a promise inside .then() to keep the chain going.
// A single .catch() at the end handles errors from any step.

console.log("\n-- SECTION 12: Promise Chaining --");

function getUser(id) {
  return new Promise((resolve) => {
    setTimeout(() => resolve({ id, name: "Alice" }), 200);
  });
}

function getOrders(user) {
  return new Promise((resolve) => {
    setTimeout(() => resolve(["Laptop", "Phone"]), 200);
  });
}

getUser(1)
  .then((user) => {
    console.log("Chain - User:", user.name);
    return getOrders(user); // return the next promise to chain
  })
  .then((orders) => {
    console.log("Chain - Orders:", orders);
  })
  .catch((err) => {
    console.log("Chain - Error:", err); // catches error from ANY step above
  });




// ============================================================
// SECTION 13 — PROMISE STATIC METHODS
// ============================================================

console.log("\n-- SECTION 13: Static Methods --");

// Promise.all() — all must resolve; fail-fast on first rejection
// Use when operations are INDEPENDENT and ALL results are needed
Promise.all([
  Promise.resolve(10),
  Promise.resolve(20),
  Promise.resolve(30)
]).then((vals) => console.log("Promise.all:", vals)); // [10, 20, 30]

// Promise.all() fail-fast behavior
Promise.all([
  Promise.resolve("ok"),
  Promise.reject(new Error("one failed")),
  Promise.resolve("ok2")
]).catch((err) => console.log("Promise.all fail-fast:", err.message));

// Promise.allSettled() — waits for ALL, never short-circuits
// Use when you want every result, regardless of success or failure
Promise.allSettled([
  Promise.resolve("success"),
  Promise.reject(new Error("failed")),
  Promise.resolve("another success")
]).then((results) => {
  results.forEach((r) => {
    if (r.status === "fulfilled") console.log("allSettled fulfilled:", r.value);
    else console.log("allSettled rejected:", r.reason.message);
  });
});

// Promise.race() — first to SETTLE wins (resolve OR reject)
// Use for timeout patterns
const slow = new Promise((resolve) => setTimeout(() => resolve("slow"), 1000));
const fast = new Promise((resolve) => setTimeout(() => resolve("fast"), 100));

Promise.race([slow, fast]).then((winner) => console.log("Promise.race:", winner));
// "fast"

// Promise.any() — first to RESOLVE wins, ignores rejections
// Only rejects if ALL promises reject (AggregateError)
Promise.any([
  Promise.reject(new Error("err1")),
  Promise.resolve("first success"),
  Promise.reject(new Error("err3"))
]).then((val) => console.log("Promise.any:", val)); // "first success"




// ============================================================
// SECTION 14 — async / await
// ============================================================

// async/await is syntactic sugar over Promises.
// It makes async code look and read like synchronous code.
// Under the hood, it compiles to .then() chains — behavior is identical.
//
// Rules:
//   - async before a function makes it always return a Promise
//   - await can only be used inside an async function
//   - await pauses execution INSIDE the async function until the promise settles
//   - Other code outside the async function continues running normally

console.log("\n-- SECTION 14: async/await --");

async function loadDashboard() {
  try {
    const user = await getUser(1);             // waits here, then continues
    console.log("async/await - User:", user.name);

    const orders = await getOrders(user);      // waits here, then continues
    console.log("async/await - Orders:", orders);
  } catch (err) {
    console.log("async/await - Error:", err.message);
  } finally {
    console.log("async/await - Finally: always runs");
  }
}

loadDashboard();

// SEQUENTIAL vs PARALLEL with async/await

async function sequential() {
  // SLOW — waits 200ms + 200ms = ~400ms total
  const user = await getUser(1);     // wait 200ms
  const orders = await getOrders(1); // then wait 200ms more
  return [user, orders];
}

async function parallel() {
  // FAST — both run at the same time, ~200ms total
  const [user, orders] = await Promise.all([getUser(1), getOrders(1)]);
  return [user, orders];
}

// Rule: if operations are INDEPENDENT of each other, always use Promise.all
// Awaiting sequentially when not needed is a common performance mistake




// ============================================================
// SECTION 15 — ERROR HANDLING
// ============================================================

console.log("\n-- SECTION 15: Error Handling --");

// With .catch() on promises
function riskyOperation() {
  return new Promise((_, reject) => {
    setTimeout(() => reject(new Error("Network timeout")), 100);
  });
}

riskyOperation()
  .then((val) => console.log("Success:", val))
  .catch((err) => console.log("Caught with .catch():", err.message));

// With try/catch in async functions
async function safeOperation() {
  try {
    const result = await riskyOperation();
    console.log("Success:", result);
  } catch (err) {
    console.log("Caught with try/catch:", err.message);
  }
}

safeOperation();

// Common mistake — forgetting await means no catch will work properly
async function badExample() {
  try {
    const result = riskyOperation(); // forgot await! result is a pending Promise
    console.log("Result:", result);  // Promise { <pending> } — not the value
  } catch (err) {
    // this catch will NEVER run because we didn't await
    console.log("This never runs");
  }
}




// ============================================================
// SECTION 16 — PROMISIFYING A CALLBACK-BASED FUNCTION
// ============================================================

// The most common real-world use of the Promise constructor.
// Wraps old callback-based APIs in a clean Promise interface.

console.log("\n-- SECTION 16: Promisification --");

// Old callback-based function (Node.js convention: callback(error, data))
function readFileCb(path, callback) {
  setTimeout(() => {
    if (path) {
      callback(null, `contents of ${path}`);
    } else {
      callback(new Error("Invalid path"), null);
    }
  }, 100);
}

// Wrap it in a Promise
function readFilePromise(path) {
  return new Promise((resolve, reject) => {
    readFileCb(path, (err, data) => {
      if (err) reject(err);   // error → reject
      else resolve(data);     // success → resolve
    });
  });
}

// Now usable with .then() or async/await
readFilePromise("/home/user/file.txt")
  .then((contents) => console.log("File contents:", contents))
  .catch((err) => console.log("File error:", err.message));

// Node.js has a built-in utility for this: util.promisify()
// const { promisify } = require("util");
// const readFileAsync = promisify(readFileCb);




// ============================================================
// SECTION 17 — QUICK REFERENCE CHEAT SHEET
// ============================================================

//
//  CONCEPT                  SUMMARY
//  ─────────────────────────────────────────────────────────────────
//  Single-threaded          JS runs one thing at a time
//  Call Stack               Where currently running functions live
//  Microtask Queue          Promise callbacks — pushed by JS ENGINE
//  Macrotask Queue          setTimeout etc  — pushed by HOST ENV
//  Event Loop               Pulls from queues when call stack is empty
//  Priority                 Microtasks drain fully before next macrotask
//  setTimeout return        Returns a timer ID (number), NOT a promise
//  Promise states           pending → fulfilled / rejected (one-way, once)
//  resolve()                Settles promise as fulfilled (sync state change)
//  reject()                 Settles promise as rejected (sync state change)
//  .then() timing           Always async (microtask), even if already settled
//  Promise assimilation     resolve(anotherPromise) adopts that promise's state
//  Throw in executor        Sync throws auto-become rejections
//  Throw in async callback  Must manually call reject() inside try/catch
//  Promise.resolve(v)       Already-fulfilled promise shorthand
//  Promise.reject(e)        Already-rejected promise shorthand
//  async function           Always returns a Promise
//  await                    Pauses inside async fn, resumes after promise settles
//  Promise.all              All must resolve, fail-fast
//  Promise.allSettled       All must settle, no fail-fast
//  Promise.race             First to settle (resolve OR reject) wins
//  Promise.any              First to resolve wins, ignores rejections
//  Sequential await         Adds latency if ops are independent — use Promise.all
//  Promisification          Wrapping callback APIs in Promises
//