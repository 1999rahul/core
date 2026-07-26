// ============================================================
//   REACT MEMOIZATION DEEP DIVE
//   useMemo | useCallback | memo
// ============================================================
//
//  Core idea: React re-renders components whenever state or
//  props change. Memoization lets you SKIP expensive work or
//  unnecessary re-renders by caching previous results.
//
//  Three tools, three jobs:
//   - memo()        → memoize a COMPONENT (skip re-render)
//   - useMemo()     → memoize a COMPUTED VALUE
//   - useCallback() → memoize a FUNCTION REFERENCE
// ============================================================

import React, { useState, useMemo, useCallback, memo } from "react";


// ============================================================
// 1. React.memo()
// ============================================================
//
//  What it does:
//    Wraps a component so React skips re-rendering it if its
//    props haven't changed (shallow comparison by default).
//
//  When to use:
//    - A "pure" child component that renders the same output
//      for the same props.
//    - The parent re-renders often but the child's props rarely
//      change.
//
//  Syntax:
//    const MemoizedComponent = memo(Component);
//    const MemoizedComponent = memo(Component, arePropsEqual); // custom comparator
// ============================================================

// ❌ Without memo — re-renders every time Parent re-renders,
//    even if `name` prop didn't change.
function GreetingWithoutMemo({ name }) {
  console.log("GreetingWithoutMemo rendered");
  return <p>Hello, {name}!</p>;
}

// ✅ With memo — only re-renders when `name` actually changes.
const Greeting = memo(function Greeting({ name }) {
  console.log("Greeting rendered"); // you'll see this fires less
  return <p>Hello, {name}!</p>;
});

// Custom comparator — memo with fine-grained control
// Return true  → props are "equal" → skip re-render
// Return false → props changed    → re-render
const ExpensiveList = memo(
  function ExpensiveList({ items, theme }) {
    return <ul>{items.map((i) => <li key={i}>{i}</li>)}</ul>;
  },
  (prevProps, nextProps) => {
    // Only re-render when `items` array reference changes.
    // Ignore `theme` changes entirely (intentional for this example).
    return prevProps.items === nextProps.items;
  }
);

// ⚠️  memo TRAP — passing a new object/array literal each render
//    breaks memoization because {} !== {} (different reference).
function ParentBad() {
  const [count, setCount] = useState(0);

  // ❌ New array created on every render → memo is useless
  return <ExpensiveList items={["a", "b", "c"]} />;
}

function ParentGood() {
  const [count, setCount] = useState(0);
  // ✅ Stable reference with useMemo (see section 2)
  const items = useMemo(() => ["a", "b", "c"], []);
  return <ExpensiveList items={items} />;
}


// ============================================================
// 2. useMemo()
// ============================================================
//
//  What it does:
//    Caches the RETURN VALUE of a function between renders.
//    Re-runs the function only when listed dependencies change.
//
//  Syntax:
//    const value = useMemo(() => computeExpensiveValue(a, b), [a, b]);
//
//  When to use:
//    - Expensive calculations (sorting, filtering, heavy math).
//    - Creating stable object/array references to avoid breaking
//      memo() on child components.
//    - Derived data that depends on props or state.
//
//  When NOT to use:
//    - Simple expressions (adding two numbers — not worth it).
//    - Every computation by default (premature optimisation).
// ============================================================

function ProductList({ products, filterText }) {
  // ❌ Without useMemo — filters the entire list on every render,
  //    even when filterText hasn't changed.
  const filteredBad = products.filter((p) =>
    p.name.toLowerCase().includes(filterText.toLowerCase())
  );

  // ✅ With useMemo — only re-filters when products or filterText changes.
  const filteredGood = useMemo(() => {
    console.log("Filtering products..."); // only logs when deps change
    return products.filter((p) =>
      p.name.toLowerCase().includes(filterText.toLowerCase())
    );
  }, [products, filterText]); // dependency array

  return (
    <ul>
      {filteredGood.map((p) => (
        <li key={p.id}>{p.name}</li>
      ))}
    </ul>
  );
}

// ── Stable object reference example ──────────────────────────
//
//  Every render creates a new object literal → child sees new
//  props even if values are identical → memo() is bypassed.

function ParentWithStyle() {
  const [count, setCount] = useState(0);

  // ❌ New object on every render — breaks memo on StyledBox
  const styleBad = { color: "red", fontSize: 16 };

  // ✅ Same reference until deps change
  const styleGood = useMemo(() => ({ color: "red", fontSize: 16 }), []);

  return (
    <>
      <button onClick={() => setCount(c => c + 1)}>Count: {count}</button>
      <StyledBox style={styleGood} />
    </>
  );
}

const StyledBox = memo(function StyledBox({ style }) {
  console.log("StyledBox rendered");
  return <div style={style}>Box</div>;
});

// ── useMemo dependency rules ──────────────────────────────────
//
//  []          → runs once on mount, never again
//  [a, b]      → re-runs when a or b changes
//  (no array)  → runs on every render (same as no memo at all — avoid this!)
//
//  The function passed to useMemo must be PURE:
//   - No side effects (no fetch, no subscriptions, no DOM writes)
//   - Same inputs always produce the same output


// ============================================================
// 3. useCallback()
// ============================================================
//
//  What it does:
//    Caches a FUNCTION REFERENCE between renders.
//    Returns the same function object as long as deps don't change.
//
//  Syntax:
//    const fn = useCallback(() => doSomething(a, b), [a, b]);
//
//  Why does this matter?
//    In JavaScript, functions are objects.
//    () => {} !== () => {}   (different reference each render)
//    This breaks memo() on child components that receive functions
//    as props, because the prop "changed" even though the logic didn't.
//
//  When to use:
//    - Passing callbacks to memoized (memo()) child components.
//    - Functions used as useEffect dependencies.
//    - Event handlers in performance-critical lists.
// ============================================================

// ── Without useCallback ───────────────────────────────────────

function CounterBad() {
  const [count, setCount] = useState(0);
  const [other, setOther] = useState(0);

  //    New function reference created on every render.
  //    Even when `other` changes (nothing to do with increment),
  //    ButtonBad re-renders because it sees a "new" onClick prop.
  const increment = () => setCount(c => c + 1);

  return (
    <>
      <ButtonBad onClick={increment} label="Increment" />
      <button onClick={() => setOther(o => o + 1)}>Change Other: {other}</button>
    </>
  );
}

const ButtonBad = memo(function ButtonBad({ onClick, label }) {
  console.log("ButtonBad rendered"); // fires even for `other` changes
  return <button onClick={onClick}>{label}</button>;
});

// ── With useCallback ──────────────────────────────────────────

function CounterGood() {
  const [count, setCount] = useState(0);
  const [other, setOther] = useState(0);

  // ✅ Same function reference as long as deps don't change.
  //    Changing `other` no longer causes ButtonGood to re-render.
  const increment = useCallback(() => {
    setCount(c => c + 1);
  }, []); // no deps — setCount from useState is always stable

  return (
    <>
      <ButtonGood onClick={increment} label="Increment" />
      <button onClick={() => setOther(o => o + 1)}>Change Other: {other}</button>
      <p>Count: {count}</p>
    </>
  );
}

const ButtonGood = memo(function ButtonGood({ onClick, label }) {
  console.log("ButtonGood rendered"); // only fires when `increment` changes
  return <button onClick={onClick}>{label}</button>;
});

// ── useCallback with dependencies ─────────────────────────────

function SearchBar({ onSearch }) {
  const [query, setQuery] = useState("");

  // Re-creates the function only when `query` changes.
  const handleSearch = useCallback(() => {
    onSearch(query); // must include query in deps because it's used inside
  }, [query, onSearch]);

  return (
    <div>
      <input value={query} onChange={(e) => setQuery(e.target.value)} />
      <button onClick={handleSearch}>Search</button>
    </div>
  );
}

// ── useCallback as useEffect dependency ───────────────────────

function DataFetcher({ userId }) {
  const [data, setData] = useState(null);

  // ✅ Stable function reference → useEffect doesn't re-run
  //    on every render, only when userId changes.
  const fetchData = useCallback(async () => {
    const res = await fetch(`/api/users/${userId}`);
    const json = await res.json();
    setData(json);
  }, [userId]);

  // If fetchData wasn't memoized, this effect would re-run
  // on EVERY render (infinite loop risk).
  React.useEffect(() => {
    fetchData();
  }, [fetchData]);

  return <div>{data ? JSON.stringify(data) : "Loading..."}</div>;
}


// ============================================================
// 4. useMemo vs useCallback — the key distinction
// ============================================================
//
//  useCallback(fn, deps)      ≡   useMemo(() => fn, deps)
//
//  useCallback caches the function ITSELF.
//  useMemo      caches what the function RETURNS.
//
//  Example showing they're equivalent under the hood:

function EquivalenceDemo() {
  const [x, setX] = useState(1);

  // These two are identical in behaviour:
  const double_v1 = useCallback((n) => n * 2, []);
  const double_v2 = useMemo(() => (n) => n * 2, []);

  // Use useMemo when you want the computed value:
  const doubled = useMemo(() => x * 2, [x]);

  // Use useCallback when you want the function to call later:
  const handleDouble = useCallback(() => setX(x * 2), [x]);

  return (
    <div>
      <p>x = {x}, doubled = {doubled}</p>
      <button onClick={handleDouble}>Double x</button>
    </div>
  );
}


// ============================================================
// 5. A realistic combined example
// ============================================================
//
//  Parent manages state.
//  Child is memoized with memo().
//  Callback is stabilized with useCallback().
//  Derived data is cached with useMemo().
// ============================================================

const TaskItem = memo(function TaskItem({ task, onToggle }) {
  console.log(`TaskItem "${task.title}" rendered`);
  return (
    <li
      style={{ textDecoration: task.done ? "line-through" : "none" }}
      onClick={() => onToggle(task.id)}
    >
      {task.title}
    </li>
  );
});

function TaskApp() {
  const [tasks, setTasks] = useState([
    { id: 1, title: "Buy groceries", done: false },
    { id: 2, title: "Write tests", done: true },
    { id: 3, title: "Fix the bug", done: false },
  ]);
  const [showDone, setShowDone] = useState(true);

  // useMemo — only recomputes when tasks or showDone changes
  const visibleTasks = useMemo(() => {
    console.log("Computing visibleTasks");
    return showDone ? tasks : tasks.filter((t) => !t.done);
  }, [tasks, showDone]);

  // useCallback — stable reference so TaskItem (memo'd) doesn't
  // re-render when unrelated state changes
  const handleToggle = useCallback((id) => {
    setTasks((prev) =>
      prev.map((t) => (t.id === id ? { ...t, done: !t.done } : t))
    );
  }, []); // no deps — setTasks setter is always stable

  return (
    <div>
      <label>
        <input
          type="checkbox"
          checked={showDone}
          onChange={(e) => setShowDone(e.target.checked)}
        />
        Show completed
      </label>
      <ul>
        {visibleTasks.map((task) => (
          <TaskItem key={task.id} task={task} onToggle={handleToggle} />
        ))}
      </ul>
    </div>
  );
}


// ============================================================
// 6. Common mistakes & gotchas
// ============================================================

// ── MISTAKE 1: Missing dependencies ──────────────────────────
//
//  Stale closure — the memoized function closes over an old
//  value of `count` because `count` is missing from deps.

function StaleClosure() {
  const [count, setCount] = useState(0);

  // ❌ `count` is stale — always logs 0
  const logCount = useCallback(() => {
    console.log(count); // captured at creation time
  }, []); // count missing from deps!

  // ✅ Add count to deps so closure is refreshed
  const logCountFixed = useCallback(() => {
    console.log(count);
  }, [count]);

  return <button onClick={logCountFixed}>Log count</button>;
}

// ── MISTAKE 2: Memoizing everything ──────────────────────────
//
//  Memoization has a cost: React stores the previous value in
//  memory and compares deps on every render.
//  For cheap calculations, this overhead can EXCEED the savings.

//  ❌ Overkill — adding two numbers is already instant
const sum = useMemo(() => a + b, [a, b]);

//  ✅ Only memoize genuinely expensive work
const sorted = useMemo(() => hugeArray.sort(complexComparator), [hugeArray]);

// ── MISTAKE 3: New references in deps break memoization ──────

function SearchPage({ config }) {
  // ❌ config is an object prop — if parent passes a new literal
  //    each render, this never benefits from memoization.
  const results = useMemo(() => search(config), [config]);

  // ✅ Destructure primitives instead
  const { query, page } = config;
  const resultsFixed = useMemo(() => search({ query, page }), [query, page]);
}

// ── MISTAKE 4: memo() doesn't deep-compare ───────────────────
//
//  memo() uses Object.is() (shallow equality).
//  { a: 1 } !== { a: 1 } → child re-renders every time.
//  Fix: memoize the prop upstream with useMemo / useCallback.


// ============================================================
// 7. Quick decision guide
// ============================================================
//
//  Q: Am I passing a COMPONENT to prevent unnecessary renders?
//     → memo()
//
//  Q: Am I computing a VALUE from existing data?
//     → useMemo()
//
//  Q: Am I passing a FUNCTION as a prop to a memo()'d child,
//     or using a function as a useEffect dependency?
//     → useCallback()
//
//  Q: Is my computation cheap (simple maths, short strings)?
//     → Skip memoization entirely. Keep it simple.
//
//  Rule of thumb: profile first, optimise second.
//  React DevTools Profiler shows which components re-render
//  and why — use it before reaching for these hooks.

export { Greeting, TaskApp, CounterGood, DataFetcher };