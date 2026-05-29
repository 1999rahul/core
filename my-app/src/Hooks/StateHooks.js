// ==================== useState and useReducer in React – Interview Deep Dive =======================
/**
 * useState is the most fundamental React hook for managing local component state.
 * It returns an array with two elements: the current state value and a function to update it.
 */

// Basic Syntax

const [state, setState] = useState(initialValue);

// The initial value can be anything — a number, string, boolean, array, object, or even null/undefined. React only uses this initial value on the first render.

// Simple Example

import { useState } from "react";

function Counter() {
  const [count, setCount] = useState(0);

  return (
    <div>
      <p>Count: {count}</p>
      <button onClick={() => setCount(count + 1)}>Increment</button>
      <button onClick={() => setCount(count - 1)}>Decrement</button>
      <button onClick={() => setCount(0)}>Reset</button>
    </div>
  );
}

// Functional Update Form

/**
 * When the new state depends on the previous state, always use the functional update form.
 * This avoids stale closure bugs, especially inside event handlers or async code.
 */

// Wrong way – can cause bugs in batched updates
setCount(count + 1);
setCount(count + 1); // Both reads the same stale count

// Correct way – always gets the latest state
setCount(prev => prev + 1);
setCount(prev => prev + 1); // Works correctly, increments twice

// Managing Object State

/**
 * useState does NOT merge state automatically like this.setState did in class components. You must spread the old state manually.
 */

function UserForm() {
  const [user, setUser] = useState({ name: "", age: "", email: "" });

  const handleChange = (field, value) => {
    setUser(prev => ({ ...prev, [field]: value })); // spread old state first to get new reference because react works by checking if the variable is changes or not, 
                                                    // modifying existing objectwill not trigger the re render
  };

  return (
    <div>
      <input value={user.name} onChange={e => handleChange("name", e.target.value)} />
      <input value={user.age} onChange={e => handleChange("age", e.target.value)} />
    </div>
  );
}

// Lazy Initialization

/**
 * If the initial state requires a heavy computation, pass a function instead of a value. This function runs only once on the first render.
 */

// This runs on EVERY render — bad
const [data, setData] = useState(expensiveComputation());

// This runs only ONCE — good
const [data, setData] = useState(() => expensiveComputation());


// ========================= useReducer =================

/**
 * useReducer is a more powerful alternative to useState for managing complex state logic.
 * It is inspired by the Redux pattern.
 *  Instead of calling a setter directly, you dispatch an action, and a reducer function decides how the state should change based on that action
 */

// Syntax
const [state, dispatch] = useReducer(reducer, initialState);

// The reducer is a pure function with the signature:
function reducer(state, action) {
  // returns new state based on action
}

// Simple Counter with useReducer

import { useReducer } from "react";

const initialState = { count: 0 };

function reducer(state, action) {
  switch (action.type) {
    case "INCREMENT":
      return { count: state.count + 1 };
    case "DECREMENT":
      return { count: state.count - 1 };
    case "RESET":
      return { count: 0 };
    case "INCREMENT_BY":
      return { count: state.count + action.payload };
    default:
      return state; // always return state in default case
  }
}

function Counter() {
  const [state, dispatch] = useReducer(reducer, initialState);

  return (
    <div>
      <p>Count: {state.count}</p>
      <button onClick={() => dispatch({ type: "INCREMENT" })}>+</button>
      <button onClick={() => dispatch({ type: "DECREMENT" })}>-</button>
      <button onClick={() => dispatch({ type: "RESET" })}>Reset</button>
      <button onClick={() => dispatch({ type: "INCREMENT_BY", payload: 5 })}>+5</button>
    </div>
  );
}

// Complex Example – Shopping Cart
// This is where useReducer really shines over useState. Managing a cart with add, remove, and update quantity operations becomes clean and readable.

const initialState = {
  items: [],
  total: 0,
};

function cartReducer(state, action) {
  switch (action.type) {
    case "ADD_ITEM": {
      const exists = state.items.find(item => item.id === action.payload.id);
      if (exists) {
        const updatedItems = state.items.map(item =>
          item.id === action.payload.id
            ? { ...item, quantity: item.quantity + 1 }
            : item
        );
        return { ...state, items: updatedItems, total: state.total + action.payload.price };
      }
      return {
        ...state,
        items: [...state.items, { ...action.payload, quantity: 1 }],
        total: state.total + action.payload.price,
      };
    }
    case "REMOVE_ITEM": {
      const item = state.items.find(i => i.id === action.payload.id);
      return {
        ...state,
        items: state.items.filter(i => i.id !== action.payload.id),
        total: state.total - item.price * item.quantity,
      };
    }
    case "CLEAR_CART":
      return initialState;
    default:
      return state;
  }
}

function Cart() {
  const [cart, dispatch] = useReducer(cartReducer, initialState);

  return (
    <div>
      <button onClick={() => dispatch({ type: "ADD_ITEM", payload: { id: 1, name: "Laptop", price: 999 } })}>
        Add Laptop
      </button>
      <button onClick={() => dispatch({ type: "REMOVE_ITEM", payload: { id: 1 } })}>
        Remove Laptop
      </button>
      <p>Total: ${cart.total}</p>
    </div>
  );
}


// Key Differences – useState vs useReducer

// When to use useState:
/**
 * Simple, independent state values (a boolean toggle, a string, a number)
 * When state transitions are straightforward — just set a new value
 * Small components with 1-3 state variables that don't relate to each other
 */

// When to use useReducer:
/**
 * Multiple related state values that update together
 * Complex state transitions involving multiple sub-values
 * When next state depends on the previous state in non-trivial ways
 * When you want to separate state logic from the component (the reducer is a plain function, easily unit tested)
 * When you'd otherwise need to pass multiple setter functions through props
 */

// ============================================================================================================================
// =================== useContext ========================

// Before understanding useContext, you need to understand the problem it solves — prop drilling.

// The Three Parts of Context

// To use context in React, you always need three things working together.

// 1. createContext — Creates the context object with an optional default value.
// 2. Context.Provider — A component that wraps part of your tree and provides a value to all descendants.
// 3. useContext — The hook that any descendant component uses to read the current context value.


import { createContext, useContext, useState } from "react";

// Step 1: Create the context
// The argument to createContext is the default value
// This default is only used when a component has NO Provider above it
const ThemeContext = createContext("light");

// Step 2: Create a Provider component (good practice to wrap it)
function ThemeProvider({ children }) {
  const [theme, setTheme] = useState("light");

  const toggleTheme = () => {
    setTheme(prev => (prev === "light" ? "dark" : "light"));
  };

  return (
    <ThemeContext.Provider value={{ theme, toggleTheme }}>
      {children}
    </ThemeContext.Provider>
  );
}

// Step 3: Wrap your app with the Provider
function App() {
  return (
    <ThemeProvider>
      <Dashboard />
    </ThemeProvider>
  );
}

// Any deeply nested component can now consume the context directly
function Dashboard() {
  return <Navbar />;
}

function Navbar() {
  return <ThemeToggleButton />;
}

function ThemeToggleButton() {
  // Step 4: Consume context with useContext — no props needed
  const { theme, toggleTheme } = useContext(ThemeContext);

  return (
    <button
      onClick={toggleTheme}
      style={{ background: theme === "light" ? "#fff" : "#333", color: theme === "light" ? "#000" : "#fff" }}
    >
      Current Theme: {theme}
    </button>
  );
}


// Real World Example – Auth Context
// This is one of the most common patterns you'll see in production apps and a very common interview topic.

import { createContext, useContext, useState } from "react";

// Create the context
const AuthContext = createContext(null);

// Provider component
function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [isLoggedIn, setIsLoggedIn] = useState(false);

  const login = (userData) => {
    setUser(userData);
    setIsLoggedIn(true);
  };

  const logout = () => {
    setUser(null);
    setIsLoggedIn(false);
  };

  // Always pass a stable object or memoize (discussed below)
  const value = { user, isLoggedIn, login, logout };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
}

// Custom hook to consume auth context (best practice)
function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}

// Usage anywhere in the tree
function Navbar() {
  const { user, isLoggedIn, logout } = useAuth();

  return (
    <nav>
      {isLoggedIn ? (
        <>
          <span>Hello, {user.name}</span>
          <button onClick={logout}>Logout</button>
        </>
      ) : (
        <span>Please log in</span>
      )}
    </nav>
  );
}

function LoginPage() {
  const { login } = useAuth();

  const handleLogin = () => {
    login({ name: "Rahul", role: "admin" });
  };

  return <button onClick={handleLogin}>Login</button>;
}

function App() {
  return (
    <AuthProvider>
      <Navbar />
      <LoginPage />
    </AuthProvider>
  );
}

// Custom Hook Pattern – Why It Matters in Interviews
// Always create a custom hook to consume your context instead of calling useContext directly everywhere. Here is why:

// Without custom hook — repeated everywhere, error-prone
function SomeComponent() {
  const context = useContext(AuthContext);
  // If someone forgets the Provider, context is null and crashes silently
}

// With custom hook — clean, safe, reusable
function useAuth() {
  const context = useContext(AuthContext);
  if (context === null || context === undefined) {
    throw new Error("useAuth must be used within AuthProvider. Check your component tree.");
  }
  return context;
}

function SomeComponent() {
  const { user } = useAuth(); // Clean, safe, descriptive error if misused
}

// Multiple Contexts – Composing Providers
// In real apps you'll have multiple contexts. The pattern is to compose them at the root level.

const ThemeContext = createContext(null);
const AuthContext = createContext(null);
const CartContext = createContext(null);

// Option 1: Nest them manually
function App() {
  return (
    <AuthProvider>
      <ThemeProvider>
        <CartProvider>
          <MainApp />
        </CartProvider>
      </ThemeProvider>
    </AuthProvider>
  );
}

// Option 2: Compose them into one component (cleaner)
function AppProviders({ children }) {
  return (
    <AuthProvider>
      <ThemeProvider>
        <CartProvider>
          {children}
        </CartProvider>
      </ThemeProvider>
    </AuthProvider>
  );
}

function App() {
  return (
    <AppProviders>
      <MainApp />
    </AppProviders>
  );
}

// ========================================================================
// ============== Context + useReducer – The Most Powerful Pattern ========

// Context + useReducer – The Most Powerful Pattern
// This is the most asked interview combination. Pairing useContext with useReducer gives you a Redux-like global state management solution without any external library


// ========================================================================