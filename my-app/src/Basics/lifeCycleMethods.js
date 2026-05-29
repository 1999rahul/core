// The three lifecycle phases

/**
 * Every React component goes through three phases regardless of whether it is a class or function component.
 * In function components, all three phases are handled through the useEffect hook and its variants.
 * Mount — the component appears in the DOM for the first time. React calls your function, gets JSX back, builds the real DOM nodes, inserts them into the page, then runs your effects
 * Update — something changed. Either a state variable changed, a prop from the parent changed, or a context value changed. React calls your function again with the new values, diffs the output against the previous output, patches only the changed DOM nodes, then runs effects whose dependencies changed
 * Unmount — the component is removed from the DOM. React runs all effect cleanup functions and removes the DOM nodes.
 */

// =================================================================================================================================

// ============ useEffect — the core hook ===============

/**
 * useEffect lets you synchronize a component with an external system — an API, a timer, a WebSocket, a browser API, a third-party library.
 * It runs after React has committed changes to the DOM, so the browser has already painted when your effect runs.
 */


useEffect(() => {
    // setup — runs after render
    return () => {
        // cleanup — runs before next effect OR on unmount
    };
}, [dependencies]);

useEffect(() => { });          // no array — runs after EVERY render
useEffect(() => { }, []);      // empty array — runs once on MOUNT only
useEffect(() => { }, [id]);    // runs on mount AND whenever id changes
useEffect(() => { }, [a, b]);  // runs when a OR b changes

// Phase 1 — Mount (componentDidMount equivalent)
// Pass an empty dependency array. React runs this effect once after the first render — the component is in the DOM and the user can see it.

import { useState, useEffect } from "react";

function UserProfile({ userId }) {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        // Runs once after first render
        // Safe to access the DOM here — it exists
        console.log("Component mounted");

        async function fetchUser() {
            const res = await fetch(`/api/users/${userId}`);
            const data = await res.json();
            setUser(data);
            setLoading(false);
        }

        fetchUser();
    }, []); // empty array = mount only

    if (loading) return <p>Loading...</p>;
    return <h2>{user.name}</h2>;
}

// Phase 2 — Update (componentDidUpdate equivalent)
// List the values you want to watch in the dependency array. The effect re-runs whenever any of those values change. It also runs on the initial mount

function SearchResults({ query }) {
    const [results, setResults] = useState([]);

    useEffect(() => {
        if (!query) return;

        setResults([]); // clear previous results and re render the component
        const controller = new AbortController();

        async function search() {
            try {
                const res = await fetch(`/api/search?q=${query}`, {
                    signal: controller.signal,
                });
                const data = await res.json();
                setResults(data); // Re render the component
            } catch (err) {
                if (err.name !== "AbortError") throw err;
            }
        }

        search();
        // Why calling search() with no await keyword even if its an async function?
        // This is because the callback in the useEffect() must return a cleanup function or undefined, if we call search() with await, this will return an Promise which react does not expects.

        return () => controller.abort(); // cancel previous request on cleanup, React stores it and executes just before next effect logic executes
    }, [query]); // re-runs the component function when query changes

    return <ul>{results.map(r => <li key={r.id}>{r.name}</li>)}</ul>;

    // Phase 3 — Unmount (componentWillUnmount equivalent)

    /**
     * The function you return from useEffect is the cleanup.
     * It runs when the component unmounts.
     * It also runs before the next effect fires on re-runs. 
     * The setup and cleanup live in the same block — this is intentional, making it harder to forget cleanup.
     */

    function Timer() {
        const [seconds, setSeconds] = useState(0);

        useEffect(() => {
            // SETUP — starts the timer
            const intervalId = setInterval(() => {
                setSeconds(s => s + 1);
            }, 1000);

            // CLEANUP — runs on unmount (and before next effect if deps change)
            return () => {
                clearInterval(intervalId);
                console.log("Timer cleaned up");
            };
        }, []); // mount only — timer starts once, stops on unmount

        return <p>{seconds}s elapsed</p>;
    }
}

// ======================== Multiple useEffect calls — separating concerns =============================

/**
 * A function component can have as many useEffect calls as needed.Each handles one concern.
 * This is better than one large effect that does everything, because each concern has its own dependency array and its own cleanup.
 */

function Dashboard({ userId, theme }) {
  const [user, setUser] = useState(null);
  const [notifications, setNotifications] = useState([]);

  // Concern 1 — fetch user when userId changes
  useEffect(() => {
    fetchUser(userId).then(setUser);
  }, [userId]);

  // Concern 2 — WebSocket subscription, independent of userId
  useEffect(() => {
    const socket = new WebSocket("wss://api.example.com/live");

    socket.onmessage = (event) => {
      setNotifications(prev => [...prev, JSON.parse(event.data)]);
    };

    return () => socket.close();
  }, []); // only mounts/unmounts with the component

  // Concern 3 — update document title when user or notifications change
  useEffect(() => {
    if (user) {
      document.title = `${notifications.length} alerts — ${user.name}`;
    }
    return () => { document.title = "MyApp"; };
  }, [user, notifications]);

  return <div>...</div>;
}

// Each effect is independently readable. If you need to change the WebSocket logic, you know exactly which effect to touch


// =========================== The dependency array — rules and common mistakes =========================================

/**
 * Every value from the component scope that is read inside the effect must be in the array.
 * This includes state variables, props, context values, functions defined in the component, and refs (the ref object, not ref.current).
 */

// Missing dependency — stale closure bug
function BadExample({ userId }) {
  useEffect(() => {
    fetchUser(userId); // userId is used but not in deps
    // If userId changes, this effect does NOT re-run
    // It keeps fetching the original userId forever
  }, []); // ← userId missing
}
  // Correct — all used values declared
function GoodExample({ userId }) {
  useEffect(() => {
    fetchUser(userId);
  }, [userId]); // re-runs when userId changes
}

// Functions declared inside the component body change reference on every render. If you use a function in an effect and put it in the deps array, you will cause an infinite loop:

// Infinite loop — fetchData is recreated every render
function Bad({ id }) {
  function fetchData() { /* ... */ }

  useEffect(() => {
    fetchData();
  }, [fetchData]); // fetchData is new every render → effect runs every render
}


// Fix 1 — move the function inside the effect
function Good({ id }) {
  useEffect(() => {
    function fetchData() { /* uses id from closure */ }
    fetchData();
  }, [id]);
}

// Fix 2 — stabilize with useCallback
function Good2({ id }) {
  const fetchData = useCallback(() => {
    /* ... */
  }, [id]); // only changes when id changes

  useEffect(() => {
    fetchData();
  }, [fetchData]); // safe — fetchData is stable
}
// ======================================================================================================================

// ============ The full execution order — what runs when ==============

// On mount:
/**
 * 1. React calls your component function — state initializes, JSX is returned
 * 2. React builds the DOM and inserts it into the page
 * 3. The browser paints — user sees the UI
 * 4. useLayoutEffect setups run synchronously
 * 5. useEffect setups run asynchronously (after paint)
 */

// On update (state or prop change):

/**
 * 1. React calls your component function again with new values
 * 2. React diffs output against previous output and patches the DOM
 * 3. The browser paints
 * 4. Cleanup of previous useLayoutEffect runs, then new useLayoutEffect setup runs
 * 5. Cleanup of previous useEffect runs, then new useEffect setup runs (only for effects whose deps changed)
 */

// On unmount:
/**
 * 1. React removes the DOM nodes
 * 2. All useLayoutEffect cleanup functions run
 * 3. All useEffect cleanup functions run
 */
