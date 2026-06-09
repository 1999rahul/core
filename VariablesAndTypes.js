// ============================================================
//   var, let, const — Complete Guide with Examples
//   Topics: Scope · Hoisting · TDZ · Re-declaration · Mutation
// ============================================================

// ─────────────────────────────────────────────
//  1. OVERVIEW
// ─────────────────────────────────────────────

/*
  Keyword │ Scope            │ Hoisting        │ Re-declare │ Re-assign │ Must init
  ────────┼──────────────────┼─────────────────┼────────────┼───────────┼──────────
  var     │ Function/Global  │ Yes (undefined) │ ✅ Yes     │ ✅ Yes    │ No
  let     │ Block            │ Yes (TDZ)       │ ❌ No      │ ✅ Yes    │ No
  const   │ Block            │ Yes (TDZ)       │ ❌ No      │ ❌ No     │ ✅ Yes
*/




// ─────────────────────────────────────────────
//  2. var — FUNCTION / GLOBAL SCOPE
// ─────────────────────────────────────────────

// 2a. var leaks OUT of blocks
function demoVarScope() {
  if (true) {
    var message = "I am var inside an if-block";
  }
  console.log(message); // ✅ "I am var inside an if-block"
  // var is NOT block-scoped; it belongs to the nearest function (or global)
}
demoVarScope();


// 2b. var is function-scoped — invisible outside its function
function outer() {
  var localVar = "only inside outer()";
}
// console.log(localVar); // ❌ ReferenceError: localVar is not defined


// 2c. var at global level attaches to the window object (in browsers)
var globalVar = "I am global";
console.log(window?.globalVar); // "I am global" (browser only)

let globalLet = "I am also global but...";
console.log(window?.globalLet); // undefined — let does NOT attach to window


// 2d. var can be re-declared in the same scope (silent and dangerous!)
var city = "Delhi";
var city = "Mumbai"; // ✅ No error — silently overwrites
console.log(city);   // "Mumbai"




// ─────────────────────────────────────────────
//  3. let — BLOCK SCOPE
// ─────────────────────────────────────────────

// 3a. let stays inside the block it was declared in
function demoLetScope() {
  if (true) {
    let blockLet = "I am let inside an if-block";
    console.log(blockLet); // ✅ "I am let inside an if-block"
  }
  // console.log(blockLet); // ❌ ReferenceError: blockLet is not defined
}
demoLetScope();


// 3b. let cannot be re-declared in the same scope
let score = 10;
// let score = 20; // ❌ SyntaxError: Identifier 'score' has already been declared

// But re-assignment is fine
score = 20; // ✅
console.log(score); // 20


// 3c. let in different blocks = different variables
{
  let x = "block 1";
  console.log(x); // "block 1"
}
{
  let x = "block 2"; // ✅ completely separate variable
  console.log(x);    // "block 2"
}




// ─────────────────────────────────────────────
//  4. const — BLOCK SCOPE + NO RE-ASSIGNMENT
// ─────────────────────────────────────────────

// 4a. const must be initialized at declaration
// const empty; // ❌ SyntaxError: Missing initializer in const declaration
const PI = 3.14159;
console.log(PI); // 3.14159


// 4b. const cannot be re-assigned
const MAX_SIZE = 100;
// MAX_SIZE = 200; // ❌ TypeError: Assignment to constant variable


// 4c. const does NOT mean the value is immutable — objects/arrays can be mutated!
const person = { name: "Alice", age: 25 };
person.name = "Bob";   // ✅ mutating a property — allowed
person.city = "Pune";  // ✅ adding a property — allowed
console.log(person);   // { name: "Bob", age: 25, city: "Pune" }

// person = {}; // ❌ TypeError — you cannot rebind the variable itself


// 4d. Same with arrays
const fruits = ["apple", "banana"];
fruits.push("cherry");  // ✅ mutating the array — allowed
fruits[0] = "mango";    // ✅ modifying an element — allowed
console.log(fruits);    // ["mango", "banana", "cherry"]

// fruits = ["new"]; // ❌ TypeError


// 4e. To truly freeze an object, use Object.freeze()
const config = Object.freeze({ theme: "dark", lang: "en" });
config.theme = "light"; // silently ignored in non-strict mode (no error, no change)
console.log(config.theme); // "dark" — unchanged




// ─────────────────────────────────────────────
//  5. HOISTING
// ─────────────────────────────────────────────

// Hoisting = JS engine moves declarations to the top of their scope
// BEFORE executing any code. Initializations are NOT moved.


// 5a. var is hoisted and initialized as undefined
console.log(hoistedVar); // ✅ undefined (no error)
var hoistedVar = "hello";
console.log(hoistedVar); // "hello"

// What JS actually does internally:
// var hoistedVar;          ← declaration moved to top
// console.log(hoistedVar); ← undefined
// hoistedVar = "hello";    ← assignment stays here


// 5b. let and const are hoisted but NOT initialized → Temporal Dead Zone (TDZ)
// console.log(hoistedLet);   // ❌ ReferenceError: Cannot access 'hoistedLet' before initialization
let hoistedLet = "world";
console.log(hoistedLet);    // ✅ "world"

// console.log(hoistedConst); // ❌ ReferenceError
const hoistedConst = 42;
console.log(hoistedConst);  // ✅ 42


// 5c. Function declarations are fully hoisted (body included)
greet(); // ✅ "Hello!" — works before the declaration
function greet() {
  console.log("Hello!");
}

// Function expressions are NOT fully hoisted
// sayBye(); // ❌ TypeError: sayBye is not a function
var sayBye = function () {
  console.log("Bye!");
};
sayBye(); // ✅ works now




// ─────────────────────────────────────────────
//  6. TEMPORAL DEAD ZONE (TDZ)
// ─────────────────────────────────────────────

/*
  TDZ = the period between when a let/const variable is:
    1. HOISTED into scope (JS engine registers it)
    2. INITIALIZED (the actual declaration line is reached)

  Accessing a variable in its TDZ throws a ReferenceError.
  This is intentional — it catches bugs that var's "undefined" silently hides.
*/

function tdzExample() {
  // TDZ for 'value' starts here ──────────────────────────┐
  //                                                        │
  // console.log(value); // ❌ ReferenceError (in TDZ)    │
  //                                                        │
  let value = 50;  // ← TDZ ends here. 'value' is initialized ┘

  console.log(value); // ✅ 50
}
tdzExample();


// TDZ in a class context
// class-level fields also follow TDZ rules
class Counter {
  count = 0; // class field — initialized when instance is created

  increment() {
    this.count++;
  }
}
const c = new Counter();
c.increment();
console.log(c.count); // 1




// ─────────────────────────────────────────────
//  7. THE CLASSIC LOOP BUG — var vs let
// ─────────────────────────────────────────────

// 7a. Using var — all callbacks share the SAME 'i'
console.log("\n--- Loop with var ---");
for (var i = 0; i < 3; i++) {
  setTimeout(function () {
    console.log("var i =", i); // 3, 3, 3  ← BUG
  }, 100);
}
// After the loop, i === 3. All closures read the same 'i'.


// 7b. Using let — each iteration gets its OWN block-scoped 'i'
console.log("\n--- Loop with let ---");
for (let j = 0; j < 3; j++) {
  setTimeout(function () {
    console.log("let j =", j); // 0, 1, 2  ✅
  }, 200);
}
// Each iteration captures a fresh binding of 'j'.


// 7c. The old fix using an IIFE (before ES6 let was available)
console.log("\n--- Loop with IIFE fix ---");
for (var k = 0; k < 3; k++) {
  (function (capturedK) {
    setTimeout(function () {
      console.log("IIFE k =", capturedK); // 0, 1, 2 ✅
    }, 300);
  })(k);
}




// ─────────────────────────────────────────────
//  8. SCOPE CHAIN EXAMPLE
// ─────────────────────────────────────────────

var globalA = "global var";
let globalB = "global let";

function outerFunc() {
  var outerA = "outer var";
  let outerB = "outer let";

  function innerFunc() {
    var innerA = "inner var";
    let innerB = "inner let";

    // Can access everything up the chain
    console.log(globalA); // ✅ "global var"
    console.log(globalB); // ✅ "global let"
    console.log(outerA);  // ✅ "outer var"
    console.log(outerB);  // ✅ "outer let"
    console.log(innerA);  // ✅ "inner var"
    console.log(innerB);  // ✅ "inner let"
  }

  innerFunc();
  // console.log(innerA); // ❌ ReferenceError — innerA not visible here
}
outerFunc();




// ─────────────────────────────────────────────
//  9. SWITCH STATEMENT GOTCHA
// ─────────────────────────────────────────────

/*
  All case clauses in a switch share ONE block scope.
  Declaring let/const with the same name in two cases = SyntaxError.
*/

const action = "A";

// ❌ WRONG — both cases share the same scope
// switch (action) {
//   case "A":
//     let result = "from A"; // SyntaxError if case B also declares 'result'
//     break;
//   case "B":
//     let result = "from B"; // ❌ Identifier 'result' already declared
// }

// ✅ CORRECT — wrap each case in its own block {}
switch (action) {
  case "A": {
    let result = "from A"; // isolated scope
    console.log(result);   // "from A"
    break;
  }
  case "B": {
    let result = "from B"; // completely separate scope
    console.log(result);
    break;
  }
}




// ─────────────────────────────────────────────
//  10. BEST PRACTICES (Modern JS)
// ─────────────────────────────────────────────

// ✅ Use const by default — signals the value won't be rebound
const API_URL = "https://api.example.com/v1";
const MAX_RETRIES = 3;
const userProfile = { id: 1, name: "Riya" }; // object contents can still change

// ✅ Use let when you know the value will change
let isLoading = false;
let retryCount = 0;
let currentPage = 1;

// ❌ Avoid var in new code — unpredictable scope, accidental globals, re-declaration bugs
// var old = "legacy code only";


// Summary rule:
// 1st choice → const  (use this most of the time)
// 2nd choice → let    (only when re-assignment is needed)
// Last resort → var   (only for legacy browser targets or old codebases)




// ─────────────────────────────────────────────
//  11. QUICK INTERVIEW CHEAT SHEET
// ─────────────────────────────────────────────

/*
  Q: What does var print here?
     for (var i = 0; i < 3; i++) setTimeout(() => console.log(i), 0)
  A: 3, 3, 3 — all closures share the same function-scoped 'i'.
     Fix: use let → prints 0, 1, 2.

  Q: What is the Temporal Dead Zone?
  A: The period between hoisting and initialization of let/const.
     Accessing the variable in this zone throws a ReferenceError.

  Q: Can you change a const object's property?
  A: Yes — const prevents rebinding the variable, not mutation.
     Use Object.freeze() to prevent mutation too.

  Q: Difference between var and let in a for loop?
  A: var is function-scoped (one shared binding for all iterations).
     let is block-scoped (a fresh binding per iteration).

  Q: What does hoisting mean for var?
  A: Declaration is moved to the top of the function/global scope,
     initialized as undefined. So reading it before the line = undefined, not error.

  Q: What does hoisting mean for let/const?
  A: They are hoisted (registered in scope) but NOT initialized.
     Accessing before the declaration = ReferenceError (TDZ).
*/