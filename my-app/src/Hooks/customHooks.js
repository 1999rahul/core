// ============================================================
//   CUSTOM HOOKS IN REACT — Complete Interview Guide
// ============================================================
//
//  A custom hook is a JavaScript function whose name starts
//  with "use" and that can call other React hooks inside it.
//
//  Core purpose: EXTRACT and REUSE stateful logic across
//  multiple components without changing component hierarchy.
//
//  Key facts:
//   - NOT a new React feature — built entirely on existing hooks
//   - The "use" prefix is a CONVENTION (enforced by lint rules)
//   - Each component that calls a custom hook gets its OWN
//     isolated state — hooks don't share state, they share logic
//   - Can call other custom hooks inside them
// ============================================================

import { useState, useEffect, useCallback, useRef, useReducer } from "react";


// ============================================================
// 1. WHY CUSTOM HOOKS EXIST — The problem they solve
// ============================================================
//
//  Before hooks, sharing stateful logic required:
//   - Higher Order Components (HOC)  → wrapper hell
//   - Render props                   → deeply nested JSX
//
//  Custom hooks solve this cleanly — just call a function.

// ── The problem: duplicated logic ────────────────────────────

// Component A — needs window width
function ComponentA() {
  const [width, setWidth] = useState(window.innerWidth);
  useEffect(() => {
    const handler = () => setWidth(window.innerWidth);
    window.addEventListener("resize", handler);
    return () => window.removeEventListener("resize", handler);
  }, []);
  return <p>Width in A: {width}</p>;
}

// Component B — needs the same logic, copy-pasted!
function ComponentB() {
  const [width, setWidth] = useState(window.innerWidth);
  useEffect(() => {
    const handler = () => setWidth(window.innerWidth);
    window.addEventListener("resize", handler);
    return () => window.removeEventListener("resize", handler);
  }, []);
  return <p>Width in B: {width}</p>;
}

// ── The solution: extract into a custom hook ──────────────────

function useWindowWidth() {
  const [width, setWidth] = useState(window.innerWidth);

  useEffect(() => {
    const handler = () => setWidth(window.innerWidth);
    window.addEventListener("resize", handler);
    return () => window.removeEventListener("resize", handler); // cleanup
  }, []);

  return width; // expose only what consumers need
}

// Now both components are clean and DRY
function ComponentAClean() {
  const width = useWindowWidth();
  return <p>Width in A: {width}</p>;
}

function ComponentBClean() {
  const width = useWindowWidth();
  return <p>Width in B: {width}</p>;
}

// ⚠️  INTERVIEW KEY POINT:
//  ComponentAClean and ComponentBClean each have their OWN
//  width state. Custom hooks share LOGIC, not STATE.


// ============================================================
// 2. ANATOMY OF A CUSTOM HOOK
// ============================================================
//
//  Rules (same as built-in hooks):
//  1. Name MUST start with "use"
//  2. Only call hooks at the TOP LEVEL (no conditions/loops)
//  3. Only call hooks from React functions or other hooks
//  4. Can accept arguments, can return anything

// Minimal structure:
function useCounter(initialValue = 0) {          // 1. Accepts args
  const [count, setCount] = useState(initialValue); // 2. Uses hooks inside

  const increment = useCallback(() => setCount(c => c + 1), []);
  const decrement = useCallback(() => setCount(c => c - 1), []);
  const reset     = useCallback(() => setCount(initialValue), [initialValue]);

  return { count, increment, decrement, reset };  // 3. Returns anything useful
}

// Usage
function CounterUI() {
  const { count, increment, decrement, reset } = useCounter(10);
  return (
    <div>
      <p>{count}</p>
      <button onClick={increment}>+</button>
      <button onClick={decrement}>−</button>
      <button onClick={reset}>Reset</button>
    </div>
  );
}


// ============================================================
// 3. useFetch — Data fetching hook (most common interview example)
// ============================================================
//
//  Encapsulates: loading state, error state, data, abort cleanup.

function useFetch(url) {
  const [data,    setData]    = useState(null);
  const [loading, setLoading] = useState(true);
  const [error,   setError]   = useState(null);

  useEffect(() => {
    // Guard: if url is falsy, do nothing
    if (!url) return;

    let isCancelled = false; // prevents state update on unmounted component

    setLoading(true);
    setError(null);

    const controller = new AbortController(); // native cancellation

    async function fetchData() {
      try {
        const res = await fetch(url, { signal: controller.signal });

        if (!res.ok) throw new Error(`HTTP error: ${res.status}`);

        const json = await res.json();

        if (!isCancelled) {  // only update if component is still mounted
          setData(json);
          setLoading(false);
        }
      } catch (err) {
        if (err.name === "AbortError") return; // intentional cancellation
        if (!isCancelled) {
          setError(err.message);
          setLoading(false);
        }
      }
    }

    fetchData();

    // Cleanup: cancel fetch if component unmounts or url changes
    return () => {
      isCancelled = true;
      controller.abort();
    };
  }, [url]); // re-fetches when url changes

  return { data, loading, error };
}

// Usage
function UserProfile({ userId }) {
  const { data: user, loading, error } = useFetch(
    userId ? `/api/users/${userId}` : null
  );

  if (loading) return <p>Loading...</p>;
  if (error)   return <p>Error: {error}</p>;
  return <h1>{user?.name}</h1>;
}


// ============================================================
// 4. useLocalStorage — Persistent state hook
// ============================================================
//
//  Behaves exactly like useState but syncs with localStorage.
//  Handles JSON serialization and parse errors gracefully.

function useLocalStorage(key, initialValue) {
  // Lazy initializer — reads from storage once on mount
  const [storedValue, setStoredValue] = useState(() => {
    try {
      const item = localStorage.getItem(key);
      return item ? JSON.parse(item) : initialValue;
    } catch {
      return initialValue; // parse error → fall back to initial
    }
  });

  const setValue = useCallback((value) => {
    try {
      // Accept a function updater just like useState
      const valueToStore =
        value instanceof Function ? value(storedValue) : value;
      setStoredValue(valueToStore);
      localStorage.setItem(key, JSON.stringify(valueToStore));
    } catch (err) {
      console.error("useLocalStorage write error:", err);
    }
  }, [key, storedValue]);

  return [storedValue, setValue];
}

// Usage — drop-in replacement for useState
function ThemeSwitcher() {
  const [theme, setTheme] = useLocalStorage("theme", "light");
  return (
    <button onClick={() => setTheme(t => t === "light" ? "dark" : "light")}>
      Current theme: {theme}
    </button>
  );
}


// ============================================================
// 5. useDebounce — Delay state updates (search inputs, etc.)
// ============================================================
//
//  Returns a debounced version of a value that only updates
//  after the user stops changing it for `delay` milliseconds.

function useDebounce(value, delay = 500) {
  const [debouncedValue, setDebouncedValue] = useState(value);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    // Cleanup cancels the timer if value changes before delay
    return () => clearTimeout(timer);
  }, [value, delay]);

  return debouncedValue;
}

// Usage — prevents API call on every keystroke
function SearchInput() {
  const [query, setQuery] = useState("");
  const debouncedQuery = useDebounce(query, 400);

  // This effect only fires 400ms after the user stops typing
  useEffect(() => {
    if (debouncedQuery) {
      console.log("Searching for:", debouncedQuery);
      // fetch(`/api/search?q=${debouncedQuery}`)
    }
  }, [debouncedQuery]);

  return (
    <input
      value={query}
      onChange={(e) => setQuery(e.target.value)}
      placeholder="Type to search..."
    />
  );
}


// ============================================================
// 6. usePrevious — Access the previous value of state/prop
// ============================================================
//
//  Classic hook that uses useRef to remember the last value.
//  useRef persists across renders WITHOUT triggering re-renders.

function usePrevious(value) {
  const ref = useRef(undefined);

  useEffect(() => {
    // This runs AFTER render, so ref.current still holds
    // the previous value during the render itself.
    ref.current = value;
  });
  // Returns previous value (undefined on first render)
  return ref.current;
}

// Usage
function PriceDisplay({ price }) {
  const prevPrice = usePrevious(price);
  const direction = price > prevPrice ? "↑" : price < prevPrice ? "↓" : "–";
  return <p>{direction} ${price} (was ${prevPrice})</p>;
}


// ============================================================
// 7. useToggle — Boolean state with helpers
// ============================================================

function useToggle(initialValue = false) {
  const [value, setValue] = useState(initialValue);

  const toggle  = useCallback(() => setValue(v => !v), []);
  const setTrue  = useCallback(() => setValue(true),  []);
  const setFalse = useCallback(() => setValue(false), []);

  return [value, toggle, setTrue, setFalse];
}

// Usage
function Accordion({ title, children }) {
  const [isOpen, toggle] = useToggle(false);
  return (
    <div>
      <button onClick={toggle}>{isOpen ? "▲" : "▼"} {title}</button>
      {isOpen && <div>{children}</div>}
    </div>
  );
}


// ============================================================
// 8. useOnClickOutside — Detect clicks outside a DOM element
// ============================================================
//
//  Common use case: close a dropdown or modal when clicking outside.

function useOnClickOutside(ref, handler) {
  useEffect(() => {
    function listener(event) {
      // Do nothing if click was inside the referenced element
      if (!ref.current || ref.current.contains(event.target)) return;
      handler(event);
    }

    document.addEventListener("mousedown",  listener);
    document.addEventListener("touchstart", listener);

    return () => {
      document.removeEventListener("mousedown",  listener);
      document.removeEventListener("touchstart", listener);
    };
  }, [ref, handler]); // handler should be stable (wrap in useCallback in parent)
}

// Usage
function Dropdown() {
  const [open, setOpen] = useState(false);
  const ref = useRef(null);

  useOnClickOutside(ref, () => setOpen(false));

  return (
    <div ref={ref}>
      <button onClick={() => setOpen(o => !o)}>Menu</button>
      {open && <ul><li>Item 1</li><li>Item 2</li></ul>}
    </div>
  );
}


// ============================================================
// 9. useReducer-based custom hook — Complex state logic
// ============================================================
//
//  When state has multiple sub-values or complex transitions,
//  useReducer inside a custom hook keeps things organised.

const formReducer = (state, action) => {
  switch (action.type) {
    case "SET_FIELD":
      return {
        ...state,
        values: { ...state.values, [action.field]: action.value },
        errors: { ...state.errors, [action.field]: "" }, // clear error on change
      };
    case "SET_ERROR":
      return {
        ...state,
        errors: { ...state.errors, [action.field]: action.message },
      };
    case "SET_SUBMITTING":
      return { ...state, isSubmitting: action.value };
    case "RESET":
      return action.initialState;
    default:
      return state;
  }
};

function useForm(initialValues, validate) {
  const initialState = {
    values: initialValues,
    errors: {},
    isSubmitting: false,
  };

  const [state, dispatch] = useReducer(formReducer, initialState);

  const handleChange = useCallback((e) => {
    const { name, value } = e.target;
    dispatch({ type: "SET_FIELD", field: name, value });
  }, []);

  const handleSubmit = useCallback((onSubmit) => async (e) => {
    e.preventDefault();
    const errors = validate ? validate(state.values) : {};
    const hasErrors = Object.keys(errors).length > 0;

    if (hasErrors) {
      Object.entries(errors).forEach(([field, message]) =>
        dispatch({ type: "SET_ERROR", field, message })
      );
      return;
    }

    dispatch({ type: "SET_SUBMITTING", value: true });
    await onSubmit(state.values);
    dispatch({ type: "SET_SUBMITTING", value: false });
  }, [state.values, validate]);

  const reset = useCallback(() => {
    dispatch({ type: "RESET", initialState });
  }, []);

  return { ...state, handleChange, handleSubmit, reset };
}

// Usage
function LoginForm() {
  const validate = (values) => {
    const errors = {};
    if (!values.email)    errors.email    = "Email is required";
    if (!values.password) errors.password = "Password is required";
    return errors;
  };

  const { values, errors, isSubmitting, handleChange, handleSubmit } = useForm(
    { email: "", password: "" },
    validate
  );

  const onSubmit = handleSubmit(async (data) => {
    await fakeLoginAPI(data);
  });

  return (
    <form onSubmit={onSubmit}>
      <input  name="email"    value={values.email}    onChange={handleChange} />
      {errors.email    && <span>{errors.email}</span>}

      <input  name="password" value={values.password} onChange={handleChange} type="password" />
      {errors.password && <span>{errors.password}</span>}

      <button type="submit" disabled={isSubmitting}>
        {isSubmitting ? "Logging in..." : "Login"}
      </button>
    </form>
  );
}


// ============================================================
// 10. useInterval — Declarative setInterval
// ============================================================
//
//  A famous hook by Dan Abramov. Illustrates how custom hooks
//  can wrap imperative browser APIs into a declarative interface.

function useInterval(callback, delay) {
  const savedCallback = useRef(callback);

  // Keep the ref current without re-running the interval
  useEffect(() => {
    savedCallback.current = callback;
  }, [callback]);

  useEffect(() => {
    if (delay === null) return; // null delay = paused

    const id = setInterval(() => savedCallback.current(), delay);
    return () => clearInterval(id);
  }, [delay]);
}

// Usage
function Stopwatch() {
  const [seconds, setSeconds] = useState(0);
  const [running, setRunning] = useState(false);

  useInterval(() => setSeconds(s => s + 1), running ? 1000 : null);

  return (
    <div>
      <p>{seconds}s</p>
      <button onClick={() => setRunning(r => !r)}>
        {running ? "Pause" : "Start"}
      </button>
      <button onClick={() => { setRunning(false); setSeconds(0); }}>
        Reset
      </button>
    </div>
  );
}


// ============================================================
// 11. Composing custom hooks (hooks calling hooks)
// ============================================================
//
//  Custom hooks can call other custom hooks — this is how you
//  build layered abstractions.

// Layer 1 — raw fetch
function useFetchRaw(url) {
  const [state, setState] = useState({ data: null, loading: true, error: null });

  useEffect(() => {
    if (!url) return;
    setState({ data: null, loading: true, error: null });
    fetch(url)
      .then(r => r.json())
      .then(data => setState({ data, loading: false, error: null }))
      .catch(err => setState({ data: null, loading: false, error: err.message }));
  }, [url]);

  return state;
}

// Layer 2 — uses useFetchRaw + useDebounce
function useSearch(query) {
  const debouncedQuery = useDebounce(query, 400); // custom hook!
  const url = debouncedQuery ? `/api/search?q=${debouncedQuery}` : null;
  return useFetchRaw(url); // custom hook!
}

// Layer 3 — component just calls useSearch
function SearchResults() {
  const [query, setQuery] = useState("");
  const { data, loading, error } = useSearch(query); // clean, simple

  return (
    <div>
      <input value={query} onChange={e => setQuery(e.target.value)} />
      {loading && <p>Searching...</p>}
      {error   && <p>Error: {error}</p>}
      {data?.results?.map(r => <p key={r.id}>{r.title}</p>)}
    </div>
  );
}


// ============================================================
// 12. INTERVIEW QUESTIONS & MODEL ANSWERS
// ============================================================

/*
  Q1: What is a custom hook?
  A: A JavaScript function starting with "use" that can call other
     React hooks. It extracts stateful logic into a reusable unit
     without introducing extra components.

  Q2: Does every component that calls a custom hook share state?
  A: No. Each component call creates completely isolated state.
     Custom hooks share LOGIC, not STATE.

  Q3: Why must custom hooks start with "use"?
  A: It's a convention that enables React's linting rules (eslint-
     plugin-react-hooks) to enforce the Rules of Hooks automatically.
     Without it, React can't verify you're not calling hooks inside
     conditions or loops.

  Q4: What's the difference between a custom hook and a utility function?
  A: A utility function is a plain JS function with no hooks.
     A custom hook CAN call hooks (useState, useEffect, etc.) inside it.
     If your function doesn't need hooks, don't name it "use".

  Q5: Can a custom hook return JSX?
  A: Technically yes, but you'd be making a component, not a hook.
     Hooks return data/functions. Components return JSX.
     Mixing them is an anti-pattern.

  Q6: How do you test a custom hook?
  A: Use the `renderHook` utility from @testing-library/react.
     Example:
       const { result } = renderHook(() => useCounter(5));
       act(() => result.current.increment());
       expect(result.current.count).toBe(6);

  Q7: When should you NOT create a custom hook?
  A: - Logic used in only one place (no duplication yet)
     - Logic that doesn't involve hooks at all (use a plain function)
     - When it makes the abstraction harder to understand than
       the original inline code (over-abstraction)

  Q8: How are custom hooks different from HOCs or render props?
  A: HOCs and render props solve code reuse at the COMPONENT level,
     adding nesting and wrapper components. Custom hooks solve it
     at the LOGIC level with no extra components or nesting.
*/


// ============================================================
// 13. QUICK REFERENCE — Hooks covered
// ============================================================
//
//  useWindowWidth      → subscribes to browser resize events
//  useCounter          → increment / decrement / reset
//  useFetch            → data, loading, error + abort cleanup
//  useLocalStorage     → useState that persists to localStorage
//  useDebounce         → delays state update (search inputs)
//  usePrevious         → remembers last render's value
//  useToggle           → boolean state with helpers
//  useOnClickOutside   → fires callback on outside click
//  useForm             → form values, validation, submit flow
//  useInterval         → declarative setInterval with pause
//  useSearch           → composed: useDebounce + useFetch
// ============================================================

export {
  useWindowWidth, useCounter, useFetch, useLocalStorage,
  useDebounce, usePrevious, useToggle, useOnClickOutside,
  useForm, useInterval, useSearch,
};