// ============================================================
//   CONTEXT + useReducer — Complete Notes & Examples
//   Topic: Global State Management in React (No Redux)
// ============================================================

// ============================================================
// SECTION 1 — THE CORE IDEA
// ============================================================

// In one line:
// useReducer  → manages complex state with structured transitions
// Context     → distributes that state globally across the component tree
// Together    → a lightweight Redux-like system with zero extra dependencies

// useReducer gives you:
//   - Predictable state transitions via actions
//   - A pure reducer function (easy to unit test)
//   - A stable dispatch function (never changes between renders)

// Context gives you:
//   - Global access to state and dispatch
//   - No prop drilling (no passing props through intermediate components)
//   - Subscription model — only consumers re-render, not all children


// ============================================================
// SECTION 2 — WHY TWO CONTEXTS (STATE + DISPATCH)?
// ============================================================

// KEY RULE:
// When a Context.Provider's value changes, every component that
// called useContext() for that context re-renders.

// If you combine state + dispatch in ONE context:
//   → Every state change recreates the value object { state, dispatch }
//   → New object reference = context value "changed"
//   → ALL consumers re-render, even those that only dispatch actions

// If you SPLIT into TWO contexts:
//   → StateContext    : changes on every state update  → state consumers re-render
//   → DispatchContext : dispatch is STABLE (React guarantees this)
//                       → dispatch consumers NEVER re-render due to state changes

// dispatch is stable because React internally guarantees the dispatch
// function returned by useReducer has the same reference across renders,
// exactly like the setState function from useState.

// The REDUCER FUNCTION itself (e.g. todoReducer) is NEVER stored in context.
// It is a plain JS function defined outside the component.
// useReducer uses it internally to compute new state when dispatch is called.


// ============================================================
// SECTION 3 — RE-RENDER RULES (Very Important for Interviews)
// ============================================================

// RULE: Being a CHILD of a Provider does NOT cause re-renders.
//       Only components that SUBSCRIBE via useContext() re-render.

// Context works like a SUBSCRIPTION (newspaper analogy):
//   - Provider  = newspaper company
//   - Children  = people in the same city (not automatically subscribed)
//   - useContext = explicitly subscribing to the newspaper
//   - State change = new edition of newspaper
//   - Only subscribers (useContext callers) get the new edition (re-render)

// Context SKIPS intermediate components entirely.
// A component 10 levels deep that calls useContext re-renders.
// All 9 intermediate components above it are completely unaffected.


// ============================================================
// SECTION 4 — COMPLETE IMPLEMENTATION (Todo App)
// ============================================================

import { createContext, useContext, useReducer, useState, useEffect } from "react";

// ----------------------------------------------------------
// 4a. INITIAL STATE
// ----------------------------------------------------------

const initialState = {
  todos: [],
  filter: "all",    // "all" | "active" | "completed"
  loading: false,
  error: null,
};

// ----------------------------------------------------------
// 4b. REDUCER FUNCTION
// ----------------------------------------------------------
// Pure function — no side effects, no async, no API calls
// Takes current state + action → returns NEW state object
// Always returns state in default case (never return undefined)

function todoReducer(state, action) {
  switch (action.type) {

    case "ADD_TODO":
      return {
        ...state,
        todos: [
          ...state.todos,
          { id: Date.now(), text: action.payload, completed: false },
        ],
      };

    case "TOGGLE_TODO":
      return {
        ...state,
        todos: state.todos.map((todo) =>
          todo.id === action.payload
            ? { ...todo, completed: !todo.completed }
            : todo
        ),
      };

    case "DELETE_TODO":
      return {
        ...state,
        todos: state.todos.filter((todo) => todo.id !== action.payload),
      };

    case "SET_FILTER":
      return {
        ...state,
        filter: action.payload,
      };

    case "SET_LOADING":
      return {
        ...state,
        loading: action.payload,
      };

    case "SET_ERROR":
      return {
        ...state,
        error: action.payload,
        loading: false,
      };

    case "LOAD_TODOS":
      return {
        ...state,
        todos: action.payload,
        loading: false,
      };

    default:
      return state; // ALWAYS return state in default — never return undefined
  }
}


// ----------------------------------------------------------
// 4c. CREATE TWO SEPARATE CONTEXTS
// ----------------------------------------------------------
// null as default value → custom hooks will guard against missing Provider

const TodoStateContext    = createContext(null); // holds state object
const TodoDispatchContext = createContext(null); // holds dispatch function


// ----------------------------------------------------------
// 4d. PROVIDER COMPONENT
// ----------------------------------------------------------
// This is the component that:
//   1. Creates the reducer and holds state (useReducer)
//   2. Distributes state via TodoStateContext.Provider
//   3. Distributes dispatch via TodoDispatchContext.Provider

function TodoProvider({ children }) {
  const [state, dispatch] = useReducer(todoReducer, initialState);

  // state    → goes into StateContext    (changes on every action)
  // dispatch → goes into DispatchContext (NEVER changes — stable reference)

  return (
    <TodoStateContext.Provider value={state}>
      <TodoDispatchContext.Provider value={dispatch}>
        {children}
      </TodoDispatchContext.Provider>
    </TodoStateContext.Provider>
  );
}


// ----------------------------------------------------------
// 4e. CUSTOM HOOKS (Best Practice — Always Do This)
// ----------------------------------------------------------
// Never call useContext directly in components.
// Wrap in a custom hook for:
//   - Cleaner API
//   - Error guard (catches missing Provider early)
//   - Single place to change if context structure changes

function useTodoState() {
  const context = useContext(TodoStateContext);
  if (context === null) {
    throw new Error("useTodoState must be used within a TodoProvider");
  }
  return context;
}

function useTodoDispatch() {
  const context = useContext(TodoDispatchContext);
  if (context === null) {
    throw new Error("useTodoDispatch must be used within a TodoProvider");
  }
  return context;
}


// ----------------------------------------------------------
// 4f. ACTION CREATORS (Optional but Clean)
// ----------------------------------------------------------
// Pure functions that return action objects.
// Avoids hardcoding { type: "...", payload: ... } everywhere.
// Makes refactoring easy — change the action shape in one place.

const TodoActions = {
  addTodo:    (text)   => ({ type: "ADD_TODO",    payload: text   }),
  toggleTodo: (id)     => ({ type: "TOGGLE_TODO", payload: id     }),
  deleteTodo: (id)     => ({ type: "DELETE_TODO", payload: id     }),
  setFilter:  (filter) => ({ type: "SET_FILTER",  payload: filter }),
  setLoading: (bool)   => ({ type: "SET_LOADING", payload: bool   }),
  setError:   (msg)    => ({ type: "SET_ERROR",   payload: msg    }),
  loadTodos:  (todos)  => ({ type: "LOAD_TODOS",  payload: todos  }),
};


// ============================================================
// SECTION 5 — CONSUMING COMPONENTS
// ============================================================

// ----------------------------------------------------------
// 5a. Component that READS STATE
//     Re-renders when todos or filter changes
// ----------------------------------------------------------

function TodoList() {
  const { todos, filter } = useTodoState();   // subscribed to StateContext
  const dispatch = useTodoDispatch();          // also needs dispatch

  const filteredTodos = todos.filter((todo) => {
    if (filter === "active")    return !todo.completed;
    if (filter === "completed") return todo.completed;
    return true;
  });

  return (
    <ul>
      {filteredTodos.map((todo) => (
        <li key={todo.id}>
          <span
            style={{ textDecoration: todo.completed ? "line-through" : "none" }}
            onClick={() => dispatch(TodoActions.toggleTodo(todo.id))}
          >
            {todo.text}
          </span>
          <button onClick={() => dispatch(TodoActions.deleteTodo(todo.id))}>
            Delete
          </button>
        </li>
      ))}
    </ul>
  );
}


// ----------------------------------------------------------
// 5b. Component that ONLY DISPATCHES
//     Does NOT re-render when todos change (no state subscription)
// ----------------------------------------------------------

function AddTodo() {
  const dispatch = useTodoDispatch(); // only subscribed to DispatchContext
  const [text, setText] = useState("");

  const handleAdd = () => {
    if (text.trim()) {
      dispatch(TodoActions.addTodo(text));
      setText("");
    }
  };

  // This component re-renders ONLY when its local `text` state changes.
  // Adding/deleting/toggling todos does NOT cause this to re-render.

  return (
    <div>
      <input
        value={text}
        onChange={(e) => setText(e.target.value)}
        placeholder="Add a todo..."
      />
      <button onClick={handleAdd}>Add</button>
    </div>
  );
}


// ----------------------------------------------------------
// 5c. Component that ONLY DISPATCHES (filter actions)
//     Also does NOT re-render when todos change
// ----------------------------------------------------------

function FilterBar() {
  const dispatch = useTodoDispatch(); // only subscribed to DispatchContext

  return (
    <div>
      <button onClick={() => dispatch(TodoActions.setFilter("all"))}>All</button>
      <button onClick={() => dispatch(TodoActions.setFilter("active"))}>Active</button>
      <button onClick={() => dispatch(TodoActions.setFilter("completed"))}>Completed</button>
    </div>
  );
}


// ----------------------------------------------------------
// 5d. Component that is a CHILD of Provider but uses NO context
//     NEVER re-renders due to state changes
// ----------------------------------------------------------

function Navbar() {
  // No useContext call here
  // Even though this is inside TodoProvider, it NEVER re-renders
  // when todos change. Being a child ≠ being a subscriber.
  return <nav>My Todo App</nav>;
}


// ============================================================
// SECTION 6 — ASYNC ACTIONS (API Calls)
// ============================================================

// The reducer MUST be a pure synchronous function.
// Handle async logic OUTSIDE the reducer — in custom hooks or components.
// Dispatch actions before and after the async operation to update state.

function useFetchTodos() {
  const dispatch = useTodoDispatch();

  const fetchTodos = async () => {
    dispatch(TodoActions.setLoading(true)); // tell state: loading started

    try {
      const response = await fetch("https://jsonplaceholder.typicode.com/todos?_limit=5");
      const data = await response.json();
      dispatch(TodoActions.loadTodos(data)); // tell state: here is the data
    } catch (error) {
      dispatch(TodoActions.setError(error.message)); // tell state: something failed
    }
    // loading is set to false inside SET_ERROR and LOAD_TODOS reducers
  };

  return fetchTodos;
}

// Usage in a component
function TodoApp() {
  const { loading, error } = useTodoState();
  const fetchTodos = useFetchTodos();

  useEffect(() => {
    fetchTodos(); // fetch on mount
  }, []);

  if (loading) return <p>Loading...</p>;
  if (error)   return <p>Error: {error}</p>;

  return (
    <div>
      <AddTodo />
      <FilterBar />
      <TodoList />
    </div>
  );
}


// ============================================================
// SECTION 7 — APP ROOT (Wiring It All Together)
// ============================================================

// Wrap the app (or the relevant subtree) with the Provider.
// Every component inside TodoProvider can now access state and dispatch
// directly without any prop drilling.

function App() {
  return (
    <TodoProvider>
      <Navbar />    {/* child, no useContext → never re-renders from todo state */}
      <TodoApp />   {/* uses useTodoState → re-renders when state changes */}
    </TodoProvider>
  );
}

export default App;


// ============================================================
// SECTION 8 — RE-RENDER SUMMARY (Visual)
// ============================================================

// State changes (e.g. ADD_TODO dispatched):
//
// TodoProvider          → re-renders (owns the useReducer state)
// ├── Navbar            → ❌ NO re-render (no useContext)
// └── TodoApp
//     ├── AddTodo       → ❌ NO re-render (only useTodoDispatch)
//     ├── FilterBar     → ❌ NO re-render (only useTodoDispatch)
//     └── TodoList      → ✅ RE-RENDERS   (uses useTodoState)


// ============================================================
// SECTION 9 — UNIT TESTING THE REDUCER (No React Needed)
// ============================================================

// Because the reducer is a pure JS function, you can test it
// completely independently of React, components, or DOM.

function runTests() {
  console.log("--- Running Reducer Unit Tests ---");

  // Test 1: ADD_TODO
  const state1 = todoReducer(initialState, TodoActions.addTodo("Buy milk"));
  console.assert(state1.todos.length === 1,        "Test 1a FAILED");
  console.assert(state1.todos[0].text === "Buy milk", "Test 1b FAILED");
  console.assert(state1.todos[0].completed === false, "Test 1c FAILED");
  console.log("Test 1 PASSED — ADD_TODO");

  // Test 2: TOGGLE_TODO
  const state2 = todoReducer(state1, TodoActions.toggleTodo(state1.todos[0].id));
  console.assert(state2.todos[0].completed === true, "Test 2 FAILED");
  console.log("Test 2 PASSED — TOGGLE_TODO");

  // Test 3: DELETE_TODO
  const state3 = todoReducer(state2, TodoActions.deleteTodo(state2.todos[0].id));
  console.assert(state3.todos.length === 0, "Test 3 FAILED");
  console.log("Test 3 PASSED — DELETE_TODO");

  // Test 4: SET_FILTER
  const state4 = todoReducer(initialState, TodoActions.setFilter("completed"));
  console.assert(state4.filter === "completed", "Test 4 FAILED");
  console.log("Test 4 PASSED — SET_FILTER");

  // Test 5: default case returns state unchanged
  const state5 = todoReducer(initialState, { type: "UNKNOWN_ACTION" });
  console.assert(state5 === initialState, "Test 5 FAILED");
  console.log("Test 5 PASSED — default case");

  console.log("--- All Tests Passed ---");
}

runTests();


// ============================================================
// SECTION 10 — CONTEXT + useReducer vs REDUX
// ============================================================

// Context + useReducer:
//   ✅ Built into React — zero extra dependencies
//   ✅ Great for small to medium apps
//   ✅ Good for low-to-medium frequency state updates
//   ✅ Simple to set up
//   ❌ No middleware (no built-in way to handle complex async like sagas)
//   ❌ No DevTools (no time-travel debugging)
//   ❌ No single global store — multiple contexts for multiple domains
//   ❌ Context re-render optimization requires manual splitting

// Redux:
//   ✅ Middleware support (redux-thunk, redux-saga for complex async)
//   ✅ Redux DevTools — time-travel debugging, action log
//   ✅ Single global store for entire app
//   ✅ Large ecosystem and community
//   ✅ Strict conventions — great for large teams
//   ❌ Extra dependency and boilerplate
//   ❌ Overkill for small/medium apps

// Rule of thumb:
//   Use Context + useReducer → small/medium apps, few global state concerns
//                              (auth, theme, cart, language/locale)
//   Use Redux               → large apps, complex async, big teams, need DevTools


// ============================================================
// SECTION 11 — KEY INTERVIEW POINTS (Quick Reference)
// ============================================================

// 1. The REDUCER FUNCTION is never stored in Context.
//    Only STATE and DISPATCH are stored in Context.

// 2. DISPATCH is STABLE — React guarantees the same function reference
//    across every render. This is why DispatchContext never triggers re-renders.

// 3. SPLIT contexts (StateContext + DispatchContext) to prevent
//    dispatch-only components from re-rendering on state changes.

// 4. Components that are CHILDREN of a Provider do NOT automatically re-render.
//    Only components that CALL useContext() are subscribed and re-render.

// 5. Context SKIPS intermediate components. A component 10 levels deep
//    that calls useContext re-renders. All 9 above it are unaffected.

// 6. The REDUCER must be PURE and SYNCHRONOUS.
//    Handle async (API calls) OUTSIDE the reducer. Dispatch actions
//    before and after the async operation to reflect loading/success/error.

// 7. Always create CUSTOM HOOKS (useTodoState, useTodoDispatch) to:
//    - Add error guards for missing Provider
//    - Provide a clean, descriptive API
//    - Centralize context consumption logic

// 8. ACTION CREATORS are optional but highly recommended for clean code.
//    They make refactoring easier and reduce typos in action type strings.

// 9. The DEFAULT CASE in reducer must always return the current state.
//    Never return undefined — that would wipe out your entire state.

// 10. This pattern is essentially what Redux does internally —
//     a reducer for state logic + a store (context) for global distribution.