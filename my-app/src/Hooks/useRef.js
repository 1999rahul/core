/**
 * ============================================================
 *   useRef & forwardRef — Complete Real-World Guide
 *   For React interviews and production usage
 * ============================================================
 *
 * TABLE OF CONTENTS
 * -----------------
 * 1.  useRef — Core Concept
 * 2.  forwardRef — Core Concept
 * 3.  Example 1  — Auto-focus modal input
 * 4.  Example 2  — Debounced search input
 * 5.  Example 3  — Infinite scroll (IntersectionObserver)
 * 6.  Example 4  — Reusable Input (design system / forwardRef)
 * 7.  Example 5  — Video player controls
 * 8.  Example 6  — Measuring DOM dimensions (tooltip)
 * 9.  Example 7  — useImperativeHandle with DataGrid
 * 10. Example 8  — Tracking previous props for animations
 * 11. Key Rules & Common Gotchas
 * ============================================================
 */

// ============================================================
// 1. useRef — CORE CONCEPT
// ============================================================

/**
 * useRef(initialValue) returns a plain object: { current: initialValue }
 *
 * - The object persists for the full lifetime of the component.
 * - Mutating .current does NOT trigger a re-render.
 * - React does not track or "watch" .current at all.
 *
 * Two primary use cases:
 *   A) Direct DOM access  →  attach via ref={myRef} on a JSX element
 *   B) Mutable storage    →  hold timers, previous values, flags, etc.
 *
 * Signature:
 *   const ref = useRef(initialValue);
 *   ref.current  →  the stored value / DOM node
 */

import { useRef, useState, useEffect, forwardRef, useImperativeHandle } from 'react';


// ============================================================
// 2. forwardRef — CORE CONCEPT
// ============================================================

/**
 * React reserves the "ref" prop — it is never forwarded inside props.
 * If you write <MyInput ref={ref} />, the ref is silently dropped
 * unless MyInput is wrapped with forwardRef.
 *
 * forwardRef(renderFn) wraps a component so it:
 *   - Receives the parent's ref as its SECOND argument (after props)
 *   - Can forward it to any inner DOM node or expose a custom API
 *
 * Signature:
 *   const MyComponent = forwardRef(function MyComponent(props, ref) {
 *     return <input ref={ref} {...props} />;
 *   });
 *
 * Always use a NAMED function (not arrow) inside forwardRef so the
 * component shows its real name in React DevTools instead of "ForwardRef".
 */


// ============================================================
// EXAMPLE 1 — Auto-focus a modal input when it opens
// ============================================================

/**
 * REAL-WORLD CONTEXT:
 * When a dialog/modal opens, UX best practice is to focus the first
 * interactive element automatically. useRef gives direct DOM access
 * to call .focus() imperatively inside a useEffect.
 *
 * WHY REF AND NOT STATE?
 * We don't need React to re-render when we focus the input.
 * We just need a pointer to the DOM node. That's exactly what ref is for.
 */

function Modal({ isOpen, onClose }) {
  const inputRef = useRef(null); // null until the component mounts

  useEffect(() => {
    // isOpen changed → if modal is now visible, focus the input
    if (isOpen) {
      // Optional chaining (?.) guards against ref.current being null
      // (e.g., if the component isn't mounted yet)
      inputRef.current?.focus();
    }
  }, [isOpen]); // runs whenever isOpen changes

  if (!isOpen) return null; // don't mount when closed

  return (
    <div className="modal-overlay">
      <div className="modal">
        {/* React sets inputRef.current = this DOM node after mount */}
        <input ref={inputRef} placeholder="Search..." />
        <button onClick={onClose}>Close</button>
      </div>
    </div>
  );
}


// ============================================================
// EXAMPLE 2 — Debounced search input
// ============================================================

/**
 * REAL-WORLD CONTEXT:
 * Firing an API call on every keystroke is expensive. We want to
 * wait until the user stops typing (400ms of silence) before searching.
 *
 * WHY REF AND NOT STATE?
 * The timer ID is purely internal implementation detail — the UI
 * doesn't need to display it or react to it changing. Storing it
 * in state would cause unnecessary re-renders on every keystroke.
 * A ref holds it silently.
 *
 * PATTERN:
 * Every keystroke → clear the previous timer → set a new one.
 * Only the last timer fires.
 */

function SearchBar({ onSearch }) {
  const timerRef = useRef(null);

  const handleChange = (e) => {
    // Capture value NOW — e.target becomes stale inside setTimeout
    const value = e.target.value;

    // Cancel any pending timer from a previous keystroke
    clearTimeout(timerRef.current);

    // Start a fresh timer; only fires if user stops typing for 400ms
    timerRef.current = setTimeout(() => {
      onSearch(value); // API call happens here
    }, 400);
  };

  // Cleanup on unmount — avoids calling onSearch after component is gone
  useEffect(() => {
    return () => clearTimeout(timerRef.current);
  }, []);

  return <input onChange={handleChange} placeholder="Search products..." />;
}


// ============================================================
// EXAMPLE 3 — Infinite scroll with IntersectionObserver
// ============================================================

/**
 * REAL-WORLD CONTEXT:
 * Instead of listening to scroll events (which fire hundreds of times
 * per second), attach an IntersectionObserver to a tiny invisible
 * "sentinel" div at the bottom of the list. When it enters the viewport,
 * load the next page of data.
 *
 * WHY REF?
 * We need a direct DOM reference to the sentinel element so we can
 * pass it to observer.observe(). This is DOM manipulation — ref territory.
 *
 * CLEANUP:
 * Always disconnect the observer when the component unmounts to prevent
 * memory leaks and phantom callbacks.
 */

function ProductList({ products, onLoadMore }) {
  const sentinelRef = useRef(null);

  useEffect(() => {
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          // Sentinel is visible in the viewport → load next page
          onLoadMore();
        }
      },
      { threshold: 0.1 } // fire when 10% of sentinel is visible
    );

    const sentinel = sentinelRef.current;
    if (sentinel) observer.observe(sentinel);

    // Cleanup: stop observing when component unmounts or onLoadMore changes
    return () => {
      if (sentinel) observer.unobserve(sentinel);
    };
  }, [onLoadMore]);

  return (
    <div>
      {products.map((p) => (
        <div key={p.id} className="product-card">
          {p.name}
        </div>
      ))}

      {/* Invisible element at the bottom — triggers loading when visible */}
      <div ref={sentinelRef} style={{ height: 1 }} aria-hidden="true" />
    </div>
  );
}


// ============================================================
// EXAMPLE 4 — Reusable Input component (forwardRef in design systems)
// ============================================================

/**
 * REAL-WORLD CONTEXT:
 * Every design system (shadcn/ui, MUI, Radix, Chakra) does this.
 * You want a styled <Input> component with a label and error state,
 * but the parent form still needs to programmatically focus or
 * interact with the underlying <input> DOM element.
 *
 * WITHOUT forwardRef: the ref passed by the parent is silently lost.
 * WITH forwardRef:    the ref tunnels through to the real <input>.
 *
 * NOTE: The second argument "ref" only exists because of forwardRef.
 * It would be undefined in a normal function component.
 */

const Input = forwardRef(function Input(
  { label, error, className, ...props }, // standard props
  ref                                     // ref from parent — forwarded by React
) {
  return (
    <div className="input-wrapper">
      {label && (
        <label className="input-label">
          {label}
        </label>
      )}

      {/* ref is attached here — parent gets a pointer to this DOM node */}
      <input
        ref={ref}
        className={`input ${error ? 'input--error' : ''} ${className || ''}`}
        {...props}
      />

      {error && (
        <span className="input-error-msg" role="alert">
          {error}
        </span>
      )}
    </div>
  );
});

// Usage — parent controls focus without knowing Input's internals
function SignupForm() {
  const emailRef = useRef(null);
  const [error, setError] = useState('');

  const handleSubmit = (e) => {
    e.preventDefault();
    const email = emailRef.current.value;

    if (!email.includes('@')) {
      setError('Please enter a valid email');
      emailRef.current.focus(); // jump back to the invalid field
      return;
    }

    setError('');
    // ... submit form
  };

  return (
    <form onSubmit={handleSubmit}>
      <Input
        ref={emailRef}
        label="Email address"
        type="email"
        error={error}
        placeholder="you@example.com"
      />
      <button type="submit">Sign up</button>
    </form>
  );
}


// ============================================================
// EXAMPLE 5 — Video player controls
// ============================================================

/**
 * REAL-WORLD CONTEXT:
 * HTML media elements (video, audio) expose an imperative API:
 * .play(), .pause(), .currentTime, .volume, etc.
 * This is inherently imperative — it cannot be modelled with props alone.
 * Refs are the correct tool here.
 *
 * This pattern is used in custom video players, podcasts, voice recorders,
 * screen recorders, and anything built on top of native media elements.
 */

function VideoPlayer({ src, poster }) {
  const videoRef = useRef(null);
  const [isPlaying, setIsPlaying] = useState(false);

  const play = async () => {
    await videoRef.current.play(); // returns a Promise
    setIsPlaying(true);
  };

  const pause = () => {
    videoRef.current.pause();
    setIsPlaying(false);
  };

  const skip = (seconds) => {
    // currentTime is a direct DOM property — only accessible via ref
    videoRef.current.currentTime += seconds;
  };

  const setVolume = (level) => {
    // level should be 0.0 to 1.0
    videoRef.current.volume = level;
  };

  return (
    <div className="video-player">
      <video
        ref={videoRef}
        src={src}
        poster={poster}
        onEnded={() => setIsPlaying(false)} // sync UI when video finishes
      />

      <div className="controls">
        <button onClick={isPlaying ? pause : play}>
          {isPlaying ? 'Pause' : 'Play'}
        </button>
        <button onClick={() => skip(-10)}>-10s</button>
        <button onClick={() => skip(10)}>+10s</button>
        <input
          type="range"
          min={0}
          max={1}
          step={0.1}
          defaultValue={1}
          onChange={(e) => setVolume(Number(e.target.value))}
        />
      </div>
    </div>
  );
}


// ============================================================
// EXAMPLE 6 — Measuring DOM dimensions (tooltip positioning)
// ============================================================

/**
 * REAL-WORLD CONTEXT:
 * Tooltips, popovers, dropdowns, and context menus need to know
 * the position and size of their trigger element to position themselves
 * correctly. getBoundingClientRect() gives the live measurements.
 *
 * WHY REF?
 * You need a direct DOM pointer to call .getBoundingClientRect().
 * There's no React API for this — it's a native DOM measurement.
 *
 * NOTE:
 * The position (pos) IS stored in state because changing it should
 * update the tooltip's CSS. The ref is just the measurement tool.
 */

function Tooltip({ text, children }) {
  const triggerRef = useRef(null);
  const [pos, setPos] = useState({ top: 0, left: 0 });
  const [visible, setVisible] = useState(false);

  const handleMouseEnter = () => {
    const rect = triggerRef.current.getBoundingClientRect();

    // Calculate absolute position accounting for scroll
    setPos({
      top: rect.bottom + window.scrollY + 8,  // 8px gap below trigger
      left: rect.left + window.scrollX,
    });

    setVisible(true);
  };

  const handleMouseLeave = () => setVisible(false);

  return (
    <>
      <span
        ref={triggerRef}
        onMouseEnter={handleMouseEnter}
        onMouseLeave={handleMouseLeave}
        className="tooltip-trigger"
      >
        {children}
      </span>

      {visible && (
        <div
          role="tooltip"
          className="tooltip"
          style={{
            position: 'absolute',
            top: pos.top,
            left: pos.left,
          }}
        >
          {text}
        </div>
      )}
    </>
  );
}

// Usage
function App() {
  return (
    <p>
      Hover over{' '}
      <Tooltip text="This is extra info about the term">
        <strong>this term</strong>
      </Tooltip>{' '}
      to see the tooltip.
    </p>
  );
}


// ============================================================
// EXAMPLE 7 — useImperativeHandle with a DataGrid component
// ============================================================

/**
 * REAL-WORLD CONTEXT:
 * Complex components like data tables, rich text editors, maps, and
 * carousels often need to expose imperative actions to their parents
 * (clear selection, scroll to row, reset, export, etc.).
 *
 * PROBLEM with raw forwardRef:
 * Exposing the full DOM node gives the parent too much power —
 * they can accidentally break the component's internal state.
 *
 * SOLUTION — useImperativeHandle:
 * Define exactly what the parent is allowed to do. The parent gets
 * a clean, documented API. Everything else stays private.
 *
 * Think of it as a public interface for a component.
 */

const DataGrid = forwardRef(function DataGrid({ rows }, ref) {
  const [selectedIds, setSelectedIds] = useState([]);
  const containerRef = useRef(null); // internal ref — not exposed

  // useImperativeHandle(ref, factory, deps?)
  // ref      → the ref passed from the parent
  // factory  → returns the object the parent sees on ref.current
  // deps     → re-run factory when these change (like useEffect deps)
  useImperativeHandle(ref, () => ({

    // Parent can clear the selection without touching internal state directly
    clearSelection() {
      setSelectedIds([]);
    },

    // Parent can scroll to any row by index
    scrollToRow(index) {
      const rows = containerRef.current?.querySelectorAll('.grid-row');
      rows?.[index]?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    },

    // Parent can read selected IDs (read-only snapshot)
    getSelectedIds() {
      return [...selectedIds]; // return a copy so parent can't mutate state
    },

  }), [selectedIds]); // re-create API when selectedIds changes

  const toggleSelect = (id) => {
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]
    );
  };

  return (
    <div ref={containerRef} className="data-grid">
      {rows.map((row) => (
        <div
          key={row.id}
          className={`grid-row ${selectedIds.includes(row.id) ? 'selected' : ''}`}
          onClick={() => toggleSelect(row.id)}
        >
          {row.name}
        </div>
      ))}
    </div>
  );
});

// Parent uses the grid through a clean API
function Dashboard() {
  const gridRef = useRef(null);

  const data = [
    { id: 1, name: 'Alice' },
    { id: 2, name: 'Bob' },
    { id: 3, name: 'Charlie' },
  ];

  return (
    <div>
      <DataGrid ref={gridRef} rows={data} />

      <div className="toolbar">
        <button onClick={() => gridRef.current.clearSelection()}>
          Clear selection
        </button>
        <button onClick={() => gridRef.current.scrollToRow(0)}>
          Go to top
        </button>
        <button onClick={() => console.log(gridRef.current.getSelectedIds())}>
          Log selected
        </button>
      </div>
    </div>
  );
}


// ============================================================
// EXAMPLE 8 — Tracking previous props (animation direction)
// ============================================================

/**
 * REAL-WORLD CONTEXT:
 * Animation libraries (Framer Motion, react-spring) and custom
 * animated counters need to know the PREVIOUS value to decide
 * which direction to animate.
 *
 * HOW IT WORKS:
 * useEffect runs AFTER the render. So when the component re-renders
 * with a new value, useEffect updates ref.current AFTER the new render
 * is already painted. On the NEXT render, ref.current still holds
 * the old value — which is what we return as "previous".
 *
 * WHY REF AND NOT STATE?
 * Storing previous value in state would cause an extra re-render
 * every time the value changes. A ref stores it silently.
 *
 * This is a common custom hook pattern used across many codebases.
 */

function usePrevious(value) {
  const ref = useRef(); // no initial value — starts as undefined

  useEffect(() => {
    // This runs AFTER every render
    // At this point, the component has already painted with the new value
    // We save it now so it becomes "previous" on the NEXT render
    ref.current = value;
  }); // no dependency array → runs after every render

  // Return the value from the PREVIOUS render
  return ref.current;
}

// Usage — animated counter that slides up or down
function AnimatedCounter({ count }) {
  const prevCount = usePrevious(count);

  // On first render prevCount is undefined, so direction defaults to 'up'
  const direction = prevCount !== undefined && count < prevCount ? 'down' : 'up';

  return (
    <div
      className={`counter counter--slide-${direction}`}
      key={count} // new key triggers CSS animation on every change
    >
      {count}
    </div>
  );
}

// Demonstrates the pattern in action
function ScoreBoard() {
  const [score, setScore] = useState(0);

  return (
    <div>
      <AnimatedCounter count={score} />
      <button onClick={() => setScore((s) => s + 10)}>+10</button>
      <button onClick={() => setScore((s) => s - 5)}>-5</button>
    </div>
  );
}


// ============================================================
// KEY RULES & COMMON GOTCHAS (interview gold)
// ============================================================

/**
 * RULE 1 — Never read or write ref.current during rendering
 * --------------------------------------------------------
 * React renders must be pure functions. Refs are mutable and
 * untracked, so reading them during render causes inconsistencies,
 * especially in React 18 concurrent mode where renders can be
 * paused and restarted.
 *
 *   ❌  function Bad() {
 *         const ref = useRef(0);
 *         ref.current++;         // mutation during render
 *         return <p>{ref.current}</p>;
 *       }
 *
 *   ✅  Access ref.current only inside:
 *       - useEffect / useLayoutEffect callbacks
 *       - Event handlers (onClick, onChange, etc.)
 *       - Imperative handles (useImperativeHandle factory)
 *
 *
 * RULE 2 — ref.current is null until after mount
 * -----------------------------------------------
 * React sets ref.current to the DOM node AFTER the component's
 * first render is committed to the DOM. Before mount (e.g., during
 * SSR, or in the render phase), it is null.
 * Always use optional chaining: ref.current?.focus()
 *
 *
 * RULE 3 — useRef vs useState decision guide
 * -------------------------------------------
 * Ask: "Does the UI need to re-render when this value changes?"
 *   YES → useState
 *   NO  → useRef
 *
 * Common ref candidates: timer IDs, previous values, scroll position,
 *   animation frame IDs, WebSocket instances, form dirty flags.
 *
 *
 * RULE 4 — forwardRef naming for DevTools
 * ----------------------------------------
 * Anonymous arrow functions inside forwardRef show as "ForwardRef"
 * in React DevTools, making debugging painful.
 *
 *   ❌  const Input = forwardRef((props, ref) => <input ref={ref} />);
 *
 *   ✅  const Input = forwardRef(function Input(props, ref) {
 *         return <input ref={ref} />;
 *       });
 *
 *   ✅  Or set displayName after definition:
 *       Input.displayName = 'Input';
 *
 *
 * RULE 5 — useImperativeHandle keeps components encapsulated
 * -----------------------------------------------------------
 * Exposing the raw DOM node via forwardRef gives the parent
 * unlimited access to manipulate the component's internals.
 * For design system or library components, always pair forwardRef
 * with useImperativeHandle to expose only what's intentional.
 *
 *
 * RULE 6 — Cleanup refs in useEffect
 * ------------------------------------
 * If you store subscriptions, observers, or listeners in refs,
 * always clean them up in the useEffect return function:
 *
 *   useEffect(() => {
 *     const observer = new IntersectionObserver(callback);
 *     observer.observe(ref.current);
 *     return () => observer.disconnect(); // ← cleanup
 *   }, []);
 *
 *
 * RULE 7 — forwardRef is not needed in React 19+
 * -----------------------------------------------
 * React 19 passes `ref` as a regular prop, so forwardRef will
 * eventually be deprecated. For now (React 16–18), it's still required.
 * React 19 allows: function Input({ ref, ...props }) { ... }
 */


// ============================================================
// EXPORTS (for use in tests or Storybook)
// ============================================================

export {
  Modal,           // Example 1 — auto-focus
  SearchBar,       // Example 2 — debounced search
  ProductList,     // Example 3 — infinite scroll
  Input,           // Example 4 — forwardRef design system input
  VideoPlayer,     // Example 5 — video controls
  Tooltip,         // Example 6 — DOM measurement
  DataGrid,        // Example 7 — useImperativeHandle
  usePrevious,     // Example 8 — custom hook (previous value)
  AnimatedCounter, // Example 8 — usage of usePrevious
  ScoreBoard,      // Example 8 — parent demo
};