// What is State in a Function Component

/**
 * State is data a component owns and manages internally. 
 * When state changes, React calls your component function again and re-renders the output
 * You never manually update the DOM — you update state and React handles the rest.
 * ======= The rule for what belongs in state: ===========
 * If a value affects what the component renders and it can change over time, it is state.
 * If it can be computed from existing state or props, it is derived and should not be stored separately.
 * If it needs to persist between renders but does not affect the UI, use a ref.
 */

// =======================  useState — the complete picture ===========================

/**
 * useState is the primary hook for state in function components. 
 * It returns a pair — the current value and a setter function.
 *  You call it once per independent piece of data. 
 *  React tracks all useState calls in the order they are called, which is why hooks must never be called inside conditions, loops, or nested functions.
 */

import { useState } from "react";

function Profile() {
  const [name, setName] = useState("Rahul");       // string
  const [age, setAge] = useState(28);               // number
  const [isActive, setIsActive] = useState(true);  // boolean
  const [tags, setTags] = useState([]);             // array
  const [address, setAddress] = useState({         // object
    city: "Patna",
    pin: "800001",
  });

  return <div>{name}, {age}</div>;
}

// The initial value is only used on the very first render. Every subsequent render ignores it — React stores the current value internally. This is why useState(someExpression) is fine for cheap values but wasteful for expensive ones.

// ==== The setter function — replaces, does not merge ====

// The setter returned by useState replaces the entire value. 
// It does not merge. For primitive values this does not matter. For objects it matters a lot.

const [user, setUser] = useState({ name: "Rahul", age: 28, city: "Patna" });

// WRONG — replaces the entire object
setUser({ age: 29 });
// State is now: { age: 29 }
// name and city are GONE

// CORRECT — spread to preserve other fields
setUser(prev => ({ ...prev, age: 29 }));
// State is now: { name: "Rahul", age: 29, city: "Patna" }

// This is why it is generally better to have separate useState calls for independent data rather than grouping everything into one object. 
// Only group values into a single state object when they are always updated together and it makes logical sense to treat them as one unit.

// Better — independent, no spread needed
const [name, setName] = useState("Rahul");
const [age, setAge] = useState(28);
const [city, setCity] = useState("Patna");

// vs. one object where you must always spread
const [user, setUser] = useState({ name: "Rahul", age: 28, city: "Patna" });

// ========================================================================================================================================================

// The functional updater — the right way when new value depends on old

// When the new state value depends on the previous value, always pass a function to the setter instead of a value. The function receives the guaranteed latest state as its argument.

// Object form — reads count from closure (potentially stale)
setCount(count + 1);

// Functional form — receives guaranteed latest value
setCount(prevCount => prevCount + 1);

/**
 * The difference matters because React batches multiple state updates together into one re-render pass. 
 * When your event handler calls the setter three times in a row, all three closures see the same snapshot of count. 
 * The functional form works correctly because React queues the functions and chains them — each receives the output of the previous one.
 */

function tripleIncrement() {
  // All three read count=0 from the closure
  setCount(count + 1); // schedules 0+1 = 1
  setCount(count + 1); // schedules 0+1 = 1 again
  setCount(count + 1); // schedules 0+1 = 1 again
  // Result: count = 1. Expected 3. Bug.

  // Each function receives the output of the last
  setCount(c => c + 1); // queued: 0 → 1
  setCount(c => c + 1); // queued: 1 → 2
  setCount(c => c + 1); // queued: 2 → 3
  // Result: count = 3. Correct.
}

// The rule: use the functional form whenever the new value depends on the previous value. Use the direct form only when the new value is completely independent of the previous state, like setName("Priya").

// ================================================================================================================================================================================================================
// ============= Immutability — never mutate state directly ===================

/**
 * React determines whether to re-render by comparing the old reference with the new reference using Object.is(). 
 * This is a shallow reference comparison, not a deep value comparison. 
 * If you mutate an object or array in place and pass the same reference to the setter, Object.is(old, new) is true — React skips the re-render entirely.
 */

const [items, setItems] = useState(["Mango", "Banana"]);

// WRONG — mutates the array, same reference
function addItem() {
  items.push("Cherry");  // mutates in place
  setItems(items);       // same array reference
  // React: old === new → skip re-render → UI never updates
}

// CORRECT — new array reference
function addItem() {
  setItems([...items, "Cherry"]); // new array → React re-renders
}

// The three array patterns you must know for interviews:

// Adding an item — spread into a new array:
setItems(prev => [...prev, newItem]);

// Removing an item — filter into a new array:
setItems(prev => prev.filter(item => item.id !== targetId));

// Updating one item — map into a new array:
setItems(prev =>
  prev.map(item =>
    item.id === targetId ? { ...item, done: !item.done } : item
  )
);

// For nested objects, you must spread at every level that changes:
const [profile, setProfile] = useState({
  name: "Rahul",
  address: { city: "Patna", pin: "800001" },
});

// ================================================================================================================================================================================================================

// Lazy initialization — for expensive initial values

// When the initial state requires an expensive computation — reading from localStorage, parsing a large JSON string, filtering a large dataset — you should not pass the value directly.
// The expression would run on every render even though it is only used once.

// WRONG — JSON.parse runs on every single render
const [data, setData] = useState(
  JSON.parse(localStorage.getItem("savedData") || "[]")
);

// CORRECT — pass a function, called only on first render
const [data, setData] = useState(
  () => JSON.parse(localStorage.getItem("savedData") || "[]")
);

// The function you pass is called a lazy initializer. React calls it once on mount and never again.

// ======================== Batching — how multiple setState calls work =====================================

/**
 * React groups multiple state updates into a single re-render pass. This is called batching
 * It means calling the setter three times inside one event handler does not cause three re-renders — it causes one.
 */

// In React 18, batching is automatic everywhere — inside event handlers, setTimeout, Promise.then, fetch callbacks, and native DOM event listeners.

function handleSubmit() {
  setLoading(true);       // not a re-render yet
  setError(null);         // not a re-render yet
  setResults([]);         // not a re-render yet
  // React processes all three → ONE re-render
}

// React 18 — even in async contexts
setTimeout(() => {
  setCount(c => c + 1);  // batched
  setName("Priya");       // batched
  // ONE re-render, not two
}, 1000);

// If you explicitly need to force separate re-renders between two updates (rare), use flushSync from react-dom:

import { flushSync } from "react-dom";

flushSync(() => setCount(c => c + 1)); // re-render happens here
flushSync(() => setName("Priya"));     // second re-render happens here

// ========================================================================================================================================

// Derived state — what should not be state

/**
 * If a value can be computed from existing state or props, computing it during render is always better than storing it as separate state
 * Stored derived state creates two sources of truth that can get out of sync
 */

// WRONG — three state variables, one is derived
const [items, setItems] = useState([...]);
const [filter, setFilter] = useState("all");
const [filteredItems, setFilteredItems] = useState([]); // derived!

function changeFilter(f) {
  setFilter(f);
  setFilteredItems(items.filter(i => i.status === f)); // easy to forget
}

// CORRECT — two state variables, one derived value
const [items, setItems] = useState([...]);
const [filter, setFilter] = useState("all");

// Computed during render — always in sync
const filteredItems = filter === "all"
  ? items
  : items.filter(i => i.status === filter);

// When derivation is expensive (sorting or filtering thousands of items), memoize it with useMemo — do not store it in state:

import { useMemo } from "react";

const sortedItems = useMemo(
  () => [...items].sort((a, b) => a.name.localeCompare(b.name)),
  [items] // only recomputes when items changes
);

// =========================================================================================================================================

// useReducer — for complex state logic

// When a component has several related state values that update together based on complex logic, useReducer gives a cleaner model than multiple useState calls.
// You define a pure reducer function — it takes the current state and an action object and returns the next state. The component dispatches action objects and the reducer handles the logic.

import { useReducer } from "react";

const initialState = { count: 0, step: 1, history: [] };

function reducer(state, action) {
  switch (action.type) {
    case "INCREMENT":
      return {
        ...state,
        count: state.count + state.step,
        history: [...state.history, state.count + state.step],
      };
    case "DECREMENT":
      return { ...state, count: state.count - state.step };
    case "SET_STEP":
      return { ...state, step: action.payload };
    case "RESET":
      return initialState;
    default:
      return state;
  }
}

function Counter() {
  const [state, dispatch] = useReducer(reducer, initialState);

  return (
    <div>
      <p>Count: {state.count} — Step: {state.step}</p>
      <p>History: {state.history.join(" → ")}</p>
      <button onClick={() => dispatch({ type: "INCREMENT" })}>+</button>
      <button onClick={() => dispatch({ type: "DECREMENT" })}>−</button>
      <button onClick={() => dispatch({ type: "SET_STEP", payload: 5 })}>Step 5</button>
      <button onClick={() => dispatch({ type: "RESET" })}>Reset</button>
    </div>
  );
}

// The reducer is a pure function — no side effects, no API calls, no timers. It takes state and an action and returns a new state object. This makes it trivially testable without rendering anything:
// Use useReducer over useState when state has multiple sub-values that change together, update logic has branching conditions, or you want to keep update logic in one testable place separate from the component.