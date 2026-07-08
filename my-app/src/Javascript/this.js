// =============================================================================
//   THE COMPLETE GUIDE TO `this` IN JAVASCRIPT
//   Covers: All binding rules + 20 Interview Questions (Easy → Very Hard)
// =============================================================================


// =============================================================================
// SECTION 1 — WHAT IS `this`?
// =============================================================================
//
// `this` is a special keyword in JavaScript that refers to the object that is
// currently executing the function. The value of `this` is NOT fixed — it
// depends entirely on HOW the function is called, not WHERE it is written.
//
// There are 5 binding rules (in priority order):
//   1. new binding        → `this` = newly created object
//   2. Explicit binding   → `this` = object passed to call/apply/bind
//   3. Implicit binding   → `this` = object to the left of the dot
//   4. Default binding    → `this` = window (non-strict) or undefined (strict)
//   5. Arrow functions    → `this` = inherited from surrounding lexical scope
//
// =============================================================================


// =============================================================================
// SECTION 2 — RULE 1: DEFAULT BINDING (Global / Standalone call)
// =============================================================================
//
// When a regular function is called without any object context,
// `this` falls back to the global object (window in browser, global in Node.js).
// In STRICT MODE, it becomes `undefined` instead.

// --- Non-strict mode ---
function showThisNonStrict() {
  console.log(this); // window (in browser) / global (in Node)
}
showThisNonStrict();

// --- Strict mode ---
"use strict";
function showThisStrict() {
  console.log(this); // undefined
}
showThisStrict();

// --- Key insight ---
// Modern JS (ES modules, class bodies) ALWAYS runs in strict mode,
// so default binding gives `undefined`, not window.

// Example demonstrating the difference:
var globalVar = "I am global"; // `var` attaches to window

function readGlobal() {
  console.log(this.globalVar); // "I am global" (non-strict)
}
// readGlobal(); // works in non-strict, throws in strict


// =============================================================================
// SECTION 3 — RULE 2: IMPLICIT BINDING (Object method call)
// =============================================================================
//
// When a function is invoked as a method on an object,
// `this` is set to the object to the LEFT of the dot at call time.

const user = {
  name: "Rahul",
  greet() {
    console.log("Hello, " + this.name); // "Hello, Rahul"
  }
};
user.greet(); // `this` = user ✅

// --- Chaining works the same way ---
const company = {
  name: "TechCorp",
  hr: {
    name: "Priya",
    introduce() {
      console.log("I work at... wait, I'm " + this.name);
      // `this` = company.hr, not company!
    }
  }
};
company.hr.introduce(); // "I'm Priya" — the LAST object before the dot wins


// ⚠️  THE CLASSIC TRAP: Implicit Binding Loss
// When you detach a method from its object, `this` is lost.

const dog = {
  name: "Bruno",
  bark() {
    console.log(this.name + " says Woof!");
  }
};

dog.bark(); // ✅ "Bruno says Woof!" — called on dog

const detachedBark = dog.bark;
// detachedBark(); // ❌ undefined / TypeError — `this` is window or undefined


// ⚠️  CALLBACK TRAP: Passing a method as a callback also loses `this`

const timer = {
  message: "Time's up!",
  start() {
    // The callback is called by setTimeout, not by `timer`
    setTimeout(function () {
      console.log(this.message); // ❌ undefined
    }, 1000);
  }
};
// timer.start(); // logs undefined after 1 second


// =============================================================================
// SECTION 4 — RULE 3: ARROW FUNCTIONS (Lexical `this`)
// =============================================================================
//
// Arrow functions do NOT have their own `this`.
// They inherit `this` from the surrounding scope at DEFINITION time.
// This is called "lexical binding".

const timerFixed = {
  message: "Time's up!",
  start() {
    // Arrow captures `this` from start() → timerFixed
    setTimeout(() => {
      console.log(this.message); // ✅ "Time's up!"
    }, 1000);
  }
};
// timerFixed.start(); // works correctly!


// --- When arrow functions BACKFIRE ---
// Never use an arrow function as an object method directly.

const cat = {
  name: "Whiskers",
  // ❌ Arrow as object method — `this` is outer scope, NOT cat
  meow: () => {
    console.log(this.name); // undefined (outer `this`)
  },
  // ✅ Regular function as object method
  purr() {
    console.log(this.name); // "Whiskers"
  }
};
cat.meow();  // undefined ❌
cat.purr();  // "Whiskers" ✅


// --- Nested arrow functions all share the same outer `this` ---
const obj = {
  val: 100,
  outer() {
    const inner = () => {
      const deepInner = () => {
        return this.val; // still `obj`!
      };
      return deepInner();
    };
    return inner();
  }
};
console.log(obj.outer()); // 100 ✅


// =============================================================================
// SECTION 5 — RULE 4: EXPLICIT BINDING (call / apply / bind)
// =============================================================================
//
// You can manually set `this` using three methods:
//   call(ctx, arg1, arg2, ...)  → invokes immediately, args one by one
//   apply(ctx, [arg1, arg2])    → invokes immediately, args as array
//   bind(ctx, arg1, ...)        → returns a NEW permanently bound function

function introduce(greeting, punctuation) {
  console.log(`${greeting}, I'm ${this.name}${punctuation}`);
}

const person = { name: "Arjun" };

introduce.call(person, "Hi", "!");         // "Hi, I'm Arjun!"
introduce.apply(person, ["Hello", "."]);   // "Hello, I'm Arjun."
const boundIntro = introduce.bind(person, "Hey");
boundIntro("?");                           // "Hey, I'm Arjun?"


// --- call vs apply: just argument style differs ---
const nums = [5, 6, 2, 3, 7];
const max = Math.max.apply(null, nums); // classic trick to spread array
console.log(max); // 7   (same as Math.max(...nums) in modern JS)


// --- bind: returns a new function with locked `this` ---
const logUser = function () {
  console.log("User: " + this.name);
};
const logRahul = logUser.bind({ name: "Rahul" });
logRahul(); // "User: Rahul" — works even when passed as callback

setTimeout(logRahul, 500); // still "User: Rahul" — `this` is locked ✅


// --- bind also supports partial application ---
function multiply(a, b) {
  return a * b;
}
const double = multiply.bind(null, 2); // `this` = null, `a` pre-set to 2
console.log(double(5));  // 10
console.log(double(9));  // 18


// ⚠️  bind wins over call/apply — you CANNOT override a bound function's `this`
function fn() { return this.n; }
const a = { n: 1 };
const b = { n: 2 };
const bound = fn.bind(a);
console.log(bound.call(b)); // 1 — bind wins! call(b) is ignored


// =============================================================================
// SECTION 6 — RULE 5: NEW BINDING (Constructor / class)
// =============================================================================
//
// When a function is called with `new`, JavaScript:
//   1. Creates a brand new empty object
//   2. Sets `this` to that new object
//   3. Executes the function body
//   4. Returns the new object (unless the function returns another object)

function Car(make, model) {
  this.make = make;   // `this` = the new Car instance
  this.model = model;
  this.describe = function () {
    return `${this.make} ${this.model}`;
  };
}

const myCar = new Car("Toyota", "Camry");
console.log(myCar.describe()); // "Toyota Camry"


// --- Classes work identically under the hood ---
class Counter {
  constructor(start = 0) {
    this.count = start;
  }

  increment() {
    this.count++;
    return this; // enables method chaining
  }

  decrement() {
    this.count--;
    return this;
  }

  value() {
    return this.count;
  }
}

const c = new Counter(5);
console.log(c.increment().increment().decrement().value()); // 6


// ⚠️  The common class callback trap
class App {
  constructor() {
    this.name = "MyApp";
    // Without binding, `this` is lost when handleClick is passed as callback
    this.handleClick = this.handleClick.bind(this); // Fix: Option 1
  }

  handleClick() {
    console.log(this.name); // "MyApp" ✅ (because of bind above)
  }
}

// Fix Option 2: Class field arrow (ES2022) — preferred in React
class AppModern {
  name = "MyApp";
  handleClick = () => {   // Arrow class field — `this` always = instance
    console.log(this.name); // "MyApp" ✅
  };
}


// =============================================================================
// SECTION 7 — `this` IN EVENT LISTENERS
// =============================================================================
//
// In browser DOM event handlers:
//   Regular function → `this` = the DOM element that fired the event
//   Arrow function   → `this` = the surrounding scope (usually window/module)

/*
  // Regular function — `this` is the button
  btn.addEventListener("click", function () {
    console.log(this); // <button> element ✅
    this.style.color = "red";
  });

  // Arrow function — `this` is NOT the button
  btn.addEventListener("click", () => {
    console.log(this); // window / undefined ❌
  });
*/


// =============================================================================
// SECTION 8 — PRIORITY ORDER (cheat sheet)
// =============================================================================
//
//  HIGHEST  →  new binding         : new Fn()
//           →  Explicit binding    : fn.call(ctx) / fn.apply(ctx) / fn.bind(ctx)
//           →  Implicit binding    : obj.method()
//  LOWEST   →  Default binding     : fn()  →  window (non-strict) / undefined (strict)
//
//  ⚡ Arrow functions bypass ALL rules — they always use lexical `this`
//
// Quick reference table:
//  ┌──────────────────────────────┬────────────────────────────┐
//  │ How it's called              │ `this` equals              │
//  ├──────────────────────────────┼────────────────────────────┤
//  │ fn()                         │ window / undefined         │
//  │ obj.fn()                     │ obj                        │
//  │ fn.call(ctx)                 │ ctx                        │
//  │ fn.apply(ctx)                │ ctx                        │
//  │ fn.bind(ctx)()               │ ctx (locked permanently)   │
//  │ new fn()                     │ new instance               │
//  │ Arrow function               │ outer lexical scope        │
//  │ DOM handler (regular fn)     │ the DOM element            │
//  │ DOM handler (arrow fn)       │ window / undefined         │
//  └──────────────────────────────┴────────────────────────────┘


// =============================================================================
// SECTION 9 — INTERVIEW QUESTIONS WITH ANSWERS
// =============================================================================


// ─────────────────────────────────────────────────────────────────
// EASY (Q1–Q5) — fundamental binding rules
// ─────────────────────────────────────────────────────────────────

// Q1. What does `this` refer to inside a method called on an object?
// ------------------------------------------------------------------
const q1 = {
  name: "Bruno",
  bark() {
    console.log(this.name);
  }
};
q1.bark();
// Answer: "Bruno"
// Reason: Called with dot notation on `q1`, so `this` = q1.


// Q2. What is the output in non-strict mode?
// ------------------------------------------------------------------
function q2() {
  console.log(this === globalThis);
}
// q2();
// Answer: true
// Reason: Default binding in non-strict mode → `this` = global object (window/globalThis).


// Q3. What does `this` log in strict mode?
// ------------------------------------------------------------------
function q3() {
  "use strict";
  console.log(this);
}
// q3();
// Answer: undefined
// Reason: In strict mode, default binding gives `undefined`, not the global object.


// Q4. What is logged?
// ------------------------------------------------------------------
const q4obj = { x: 42 };
function q4() { return this.x; }
console.log(q4.call(q4obj));
// Answer: 42
// Reason: call(q4obj) explicitly sets `this` = q4obj for this invocation.


// Q5. What is printed?
// ------------------------------------------------------------------
function Q5Person(name) { this.name = name; }
const q5p = new Q5Person("Arjun");
console.log(q5p.name);
// Answer: "Arjun"
// Reason: `new` creates a fresh object, sets `this` to it, assigns this.name = "Arjun".


// ─────────────────────────────────────────────────────────────────
// MEDIUM (Q6–Q10) — binding loss and arrow traps
// ─────────────────────────────────────────────────────────────────

// Q6. What is logged? (non-strict)
// ------------------------------------------------------------------
const q6obj = {
  val: 10,
  getVal: function () { return this.val; }
};
const q6fn = q6obj.getVal;
// console.log(q6fn());
// Answer: undefined
// Reason: Detaching the method strips its context. `fn()` is a plain call
//         so `this` = window. window.val is undefined.


// Q7. What does the arrow function log? (module / strict context)
// ------------------------------------------------------------------
const q7obj = {
  name: "Riya",
  greet: () => { console.log(this.name); }
};
// q7obj.greet();
// Answer: undefined
// Reason: Arrow function captures `this` from the enclosing scope (module level),
//         where `this` is undefined. It does NOT use q7obj as `this`.


// Q8. What is logged after 100ms?
// ------------------------------------------------------------------
const q8 = {
  count: 0,
  start() {
    setTimeout(function () {
      this.count++;
      console.log(this.count);
    }, 100);
  }
};
// q8.start();
// Answer: NaN
// Reason: The callback's `this` = window (non-strict). window.count is undefined.
//         undefined++ = NaN.


// Q9. What is logged?
// ------------------------------------------------------------------
function q9foo() { console.log(this.x); }
const q9a = { x: 1 }, q9b = { x: 2 };
// q9foo.call(q9a);   // 1
// q9foo.apply(q9b);  // 2
// Answer: 1, then 2
// Reason: call and apply both set `this` explicitly for a single invocation.


// Q10. What is logged? (class body = always strict)
// ------------------------------------------------------------------
class Q10Animal {
  constructor(name) { this.name = name; }
  speak() { console.log(this.name + " speaks"); }
}
const q10a = new Q10Animal("Cat");
const q10fn = q10a.speak;
// q10fn();
// Answer: TypeError
// Reason: Class bodies are always strict. q10fn() is a plain call → this = undefined.
//         Accessing undefined.name throws TypeError.


// ─────────────────────────────────────────────────────────────────
// HARD (Q11–Q15) — nested functions, bind priority, prototypes
// ─────────────────────────────────────────────────────────────────

// Q11. What is logged? (non-strict)
// ------------------------------------------------------------------
const q11obj = {
  x: 1,
  outer() {
    const inner = function () { return this.x; };
    return inner();
  }
};
console.log(q11obj.outer());
// Answer: undefined
// Reason: outer() has this = q11obj, but inner() is a plain call inside outer.
//         inner's `this` = window (non-strict). window.x is undefined.
//         Fix: use an arrow function for inner, or `const self = this`.


// Q12. What is logged? (bind vs call)
// ------------------------------------------------------------------
function q12fn() { return this.n; }
const q12a = { n: 1 }, q12b = { n: 2 };
const q12bound = q12fn.bind(q12a);
console.log(q12bound.call(q12b));
// Answer: 1
// Reason: bind permanently locks `this`. call(q12b) cannot override a bound function.
//         bind > call in the priority order.


// Q13. What is logged after 2 seconds?
// ------------------------------------------------------------------
function Q13Timer() {
  this.seconds = 0;
  setInterval(() => {
    this.seconds++;
  }, 1000);
}
const q13t = new Q13Timer();
// After 2 seconds → q13t.seconds = 2
// Answer: 2
// Reason: Arrow in setInterval captures `this` from the constructor (the new instance).
//         So this.seconds correctly increments on q13t.


// Q14. What is logged? (prototype chain)
// ------------------------------------------------------------------
const q14base = {
  val: "base",
  getVal() { return this.val; }
};
const q14child = Object.create(q14base);
q14child.val = "child";
console.log(q14child.getVal());
// Answer: "child"
// Reason: getVal is inherited from q14base, but called on q14child.
//         `this` = the object to the LEFT of the dot = q14child.
//         So this.val = "child".


// Q15. What is logged? (closure + bind)
// ------------------------------------------------------------------
function q15makeAdder(x) {
  return function (y) {
    return this.base + x + y;
  };
}
const q15obj = { base: 10 };
const q15add5 = q15makeAdder(5).bind(q15obj);
console.log(q15add5(3));
// Answer: 18
// Reason: bind locks `this` = q15obj → this.base = 10.
//         x = 5 from closure. y = 3 from call. 10 + 5 + 3 = 18.


// ─────────────────────────────────────────────────────────────────
// VERY HARD (Q16–Q20) — inheritance, IIFEs, strict + map, chaining
// ─────────────────────────────────────────────────────────────────

// Q16. What is logged? (bind in superclass, subclass overrides x)
// ------------------------------------------------------------------
class Q16Foo {
  constructor() {
    this.x = 1;
    this.bar = this.bar.bind(this); // `this` here is the actual instantiated object
  }
  bar() { return this.x; }
}
class Q16Baz extends Q16Foo {
  constructor() {
    super();        // runs Foo constructor with Baz instance as `this`
    this.x = 2;    // overrides x after super()
  }
}
const q16b = new Q16Baz();
const q16fn = q16b.bar;
console.log(q16fn());
// Answer: 2
// Reason: bar is bound to the Baz instance (the `this` when super() ran).
//         After super(), Baz sets this.x = 2. bar reads this.x = 2.
//         Detaching as q16fn still works because `this` is locked via bind.


// Q17. What is logged? (arrow IIFE inside object method)
// ------------------------------------------------------------------
const q17obj = {
  name: "outer",
  inner: {
    name: "inner",
    getName: function () {
      return (() => this.name)(); // IIFE arrow
    }
  }
};
console.log(q17obj.inner.getName());
// Answer: "inner"
// Reason: getName is called on q17obj.inner → this = q17obj.inner.
//         The arrow IIFE captures that `this` lexically.
//         So this.name = "inner".


// Q18. What is logged? (nested constructors + arrow closures)
// ------------------------------------------------------------------
function Q18Outer() {
  this.val = "outer";
  function Q18Inner() {
    this.val = "inner";
    this.getVal = () => {
      return (() => this.val)(); // nested arrow IIFE
    };
  }
  this.inner = new Q18Inner();
}
const q18o = new Q18Outer();
console.log(q18o.inner.getVal());
// Answer: "inner"
// Reason: new Q18Inner() makes `this` = the Inner instance (this.val = "inner").
//         getVal is an arrow capturing that `this`.
//         The arrow IIFE inside also inherits that same `this`.
//         → this.val = "inner".


// Q19. What is logged? (strict mode + Array.map callback)
// ------------------------------------------------------------------
const q19 = (function () {
  "use strict";
  return {
    x: 5,
    getX: function () {
      return [1].map(function () {
        return this.x; // regular function callback in strict mode
      })[0];
    }
  };
})();
// console.log(q19.getX());
// Answer: TypeError
// Reason: The regular function callback inside map runs in strict mode.
//         `this` = undefined. Accessing undefined.x throws TypeError.
// Fix options:
//   1. Arrow callback:        [1].map(() => this.x)
//   2. map's thisArg:         [1].map(function() { return this.x; }, this)
//   3. Saved reference:       const self = this; [1].map(function() { return self.x; })


// Q20. What is logged? (chained explicit binding across two functions)
// ------------------------------------------------------------------
function q20foo() {
  return q20bar.call(this);
}
function q20bar() {
  return this.x;
}
const q20obj = { x: 99 };
const q20bound = q20foo.bind(q20obj);
console.log(q20bound());
// Answer: 99
// Reason: q20bound() calls q20foo with this = q20obj.
//         Inside q20foo, q20bar.call(this) passes that same this (q20obj) to q20bar.
//         q20bar returns this.x = 99.


// =============================================================================
// SECTION 10 — COMMON FIXES CHEAT SHEET
// =============================================================================

// Problem: Losing `this` in callbacks
// Fix 1 — Arrow function (most modern)
// Fix 2 — .bind(this)
// Fix 3 — const self = this  (legacy)

const exampleObj = {
  name: "Example",
  run() {
    // Fix 1 — Arrow (preferred)
    [1].forEach(() => console.log(this.name));

    // Fix 2 — bind
    [1].forEach(function () {
      console.log(this.name);
    }.bind(this));

    // Fix 3 — self/that pattern
    const self = this;
    [1].forEach(function () {
      console.log(self.name);
    });
  }
};
exampleObj.run(); // "Example" three times


// =============================================================================
//  END OF GUIDE
//  Topics covered:
//    ✅ Default binding (global / strict)
//    ✅ Implicit binding (object method)
//    ✅ Implicit binding loss (detached method, callback trap)
//    ✅ Arrow functions (lexical `this`)
//    ✅ Explicit binding (call / apply / bind)
//    ✅ new binding (constructors / classes)
//    ✅ Event listener `this`
//    ✅ Priority order
//    ✅ 20 Interview Questions: Easy → Medium → Hard → Very Hard
// =============================================================================