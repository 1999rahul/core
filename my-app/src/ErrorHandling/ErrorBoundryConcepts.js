// =============================================================================
// REACT ERROR BOUNDARIES — EXPLAINED
// =============================================================================
// This file walks through:
//   1. A complete ErrorBoundary implementation
//   2. Review notes (real issues + minor nits) as comments
//   3. Real-world examples of errors that happen during the RENDER phase
//   4. Why getDerivedStateFromError / componentDidCatch are called
//      AUTOMATICALLY by React — you never call them yourself
// =============================================================================

// -----------------------------------------------------------------------------
// 1. THE IMPLEMENTATION
// -----------------------------------------------------------------------------
//
// React calls TWO lifecycle hooks automatically when a descendant throws
// during rendering. You never invoke either of these yourself — React's
// reconciler calls them the same way it automatically calls render() or
// componentDidMount().
//
//   PHASE 1 — getDerivedStateFromError(error)
//     - Render phase. Called FIRST, synchronously, before the DOM updates.
//     - Static method, so it has no `this`.
//     - Must be a PURE function: only return new state, no side effects
//       (no logging, no API calls, no this.setState calls here).
//
//   PHASE 2 — componentDidCatch(error, info)
//     - Commit phase. Called AFTER the DOM has been updated with the
//       fallback UI from phase 1.
//     - Instance method, so `this` works normally.
//     - Side effects are safe here: logging, analytics, calling callbacks.

class ErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorMessage: '',
    };
  }

  // PHASE 1 — Render phase
  // React calls this automatically the moment a child throws during render.
  static getDerivedStateFromError(error) {
    return {
      hasError: true,
      error,
      errorMessage: error.message,
    };
  }

  // PHASE 2 — Commit phase
  // React calls this automatically right after committing the fallback UI.
  componentDidCatch(error, info) {
    // NOTE (review): wrap risky side effects in try/catch. If Sentry isn't
    // initialized (e.g. in tests, or local dev without a DSN), this throws
    // INSIDE componentDidCatch itself — and an error thrown inside the
    // handler is NOT caught by this same boundary. It would propagate to
    // the next boundary up, or crash the app if there isn't one.
    try {
      console.group('ErrorBoundary caught an error');
      console.error('Error:', error.message);
      console.error('Type:', error.name);
      console.error('Component stack:', info.componentStack);
      console.groupEnd();

      // Log to monitoring
      Sentry.captureException(error, {
        extra: { componentStack: info.componentStack },
      });
    } catch (loggingError) {
      console.error('Failed to log error to monitoring:', loggingError);
    }

    // NOTE (review): this.props.onError can also throw. It's called from
    // componentDidCatch (commit phase, not render), so an uncaught throw
    // here surfaces as an unhandled error rather than being caught by a
    // boundary. Wrapping it defensively, same as above, is cheap insurance.
    try {
      this.props.onError?.(error, info);
    } catch (callbackError) {
      console.error('onError callback threw:', callbackError);
    }
  }

  render() {
    if (this.state.hasError) {
      const { fallback: Fallback } = this.props;

      if (React.isValidElement(Fallback)) return Fallback;

      if (typeof Fallback === 'function') {
        return (
          <Fallback
            error={this.state.error}
            errorMessage={this.state.errorMessage}
            // NOTE (review): this resets the boundary's OWN state, but does
            // NOT fix whatever caused the child to throw in the first place.
            // If the cause was bad props/data, re-rendering the same children
            // with the same props will likely throw again immediately.
            // A more robust fix accepts a `resetKeys` prop and changes a
            // `key` on `this.props.children` so the subtree actually remounts
            // (see the "BETTER resetError" pattern at the bottom of this file).
            resetError={() => this.setState({
              hasError: false,
              error: null,
              errorMessage: '',
            })}
          />
        );
      }

      return <p>Something went wrong: {this.state.errorMessage}</p>;
    }

    return this.props.children;
  }
}


// -----------------------------------------------------------------------------
// USAGE EXAMPLES
// -----------------------------------------------------------------------------

// Basic usage — wrap any component
//
// <ErrorBoundary>
//   <MyComponent />
// </ErrorBoundary>

// With a static fallback element
//
// <ErrorBoundary fallback={<div>Oops! Something broke here.</div>}>
//   <PaymentForm />
// </ErrorBoundary>

// With a fallback component (gets error info + a reset handler as props)
function MyFallback({ error, errorMessage, resetError }) {
  return (
    <div>
      <h2>Something went wrong</h2>
      <p>{errorMessage}</p>
      <button onClick={resetError}>Try again</button>
    </div>
  );
}

// <ErrorBoundary fallback={MyFallback}>
//   <UserDashboard />
// </ErrorBoundary>


// =============================================================================
// 2. REAL-WORLD EXAMPLES — ERRORS THAT HAPPEN DURING THE RENDER PHASE
// =============================================================================
//
// A "render-phase error" is anything thrown while React is calling your
// component function (or render() for class components) to produce JSX —
// BEFORE anything touches the DOM. This is exactly the category that
// getDerivedStateFromError / componentDidCatch catch. Event handlers and
// async code are NOT in this category (examples at the end show why).

// --- (1) Accessing properties on undefined / null ---------------------------
function UserCard({ user }) {
  return <h2>{user.profile.displayName}</h2>;
  // Throws if `user.profile` is undefined — e.g. API response shape changed,
  // an optional field was never checked, or `user` itself is still null
  // while data is loading.
}

// --- (2) Calling array/object methods on data that isn't what you expect ---
function OrderList({ orders }) {
  return orders.map(o => <OrderRow key={o.id} order={o} />);
  // Throws if `orders` is null on first render — very common when a parent
  // passes `data` from `useState(null)` before a fetch resolves, and the
  // child doesn't guard against it.
}

// --- (3) Invalid element type passed as a prop ------------------------------
function Icon({ component }) {
  return <component />;
  // Throws if `component` is undefined (e.g. a dynamic import that hasn't
  // resolved yet) or a typo'd key in an icon lookup map.
}

// --- (4) Hooks rules violations caught at runtime ---------------------------
function Form({ showExtra }) {
  if (showExtra) {
    const [extra, setExtra] = useState(''); // conditional hook call — BAD
  }
  // React detects the mismatch in hook call order between renders and throws.
}

// --- (5) Errors in computed/derived values during render -------------------
function PriceTag({ price, taxRate }) {
  const total = price.toFixed(2) * (1 + taxRate);
  return <span>${total}</span>;
  // Throws if `price` is a string like "free" instead of a number.
  // Any arithmetic, parsing, or formatting done inline during render
  // (toFixed, JSON.parse, new Date(invalidString).toISOString()) throws
  // synchronously into the render call.
}

// --- (6) Context value misuse -----------------------------------------------
function Avatar() {
  const { user } = useContext(AuthContext);
  return <img src={user.avatarUrl} />;
  // Throws if this component renders outside its required <AuthContext.Provider>,
  // so context falls back to a default value (often null) the component
  // never anticipated.
}

// --- (7) Third-party components throwing during their own render -----------
// Example: a charting library throwing on certain scale configs with
// negative-only datasets, or a markdown renderer throwing on a malformed AST.
// (No inline snippet — the throw happens inside the library's own render code,
// not yours, but it's still a render-phase error from React's perspective.)

// --- (8) Recursive / self-referencing data causing a stack overflow --------
function TreeNode({ node }) {
  return (
    <div>
      {node.label}
      {node.children.map(c => <TreeNode key={c.id} node={c} />)}
    </div>
  );
  // If `node.children` accidentally references an ancestor (common with
  // normalized state mismanagement), this throws:
  //   RangeError: Maximum call stack size exceeded
  // — which is still a render-phase error.
}


// --- WHAT DOES NOT COUNT (contrast cases — none of these are caught) -------

// onClick={() => user.profile.name}
//   — event handler, NOT caught by error boundaries.

// fetch().then(res => res.json().nonExistent.thing)
//   — asynchronous (inside a .then callback), NOT caught.

// useEffect(() => { throw new Error(); })
//   — runs after commit, NOT caught.

// setTimeout(() => { throw new Error(); }, 0)
//   — NOT caught.

// The unifying thread for what IS caught: the throw happens synchronously,
// inside the call stack of the component function itself, while React is
// building the tree.


// =============================================================================
// 3. WHY THESE METHODS FIRE AUTOMATICALLY (no manual calls, ever)
// =============================================================================
//
// React's reconciler walks UP the tree from wherever a throw happened,
// looking for the nearest ancestor CLASS component that defines
// `static getDerivedStateFromError` and/or `componentDidCatch`.
// That ancestor becomes the error boundary for that specific throw.
// This is pure convention-over-configuration — React just checks
// "does this class define one of these two methods?"
//
// THE AUTOMATIC SEQUENCE:
//   1. Something throws inside a descendant during render.
//   2. React catches it internally — there's no visible try/catch in your code.
//   3. React calls getDerivedStateFromError(error) synchronously, IF DEFINED,
//      to compute new state. Must stay pure (render phase).
//   4. React re-renders the boundary with that new state -> fallback UI.
//   5. After commit, React calls componentDidCatch(error, info), IF DEFINED.
//      Side effects (logging, Sentry, onError) are safe here.
//
// Minimal illustration of the automatic calls:
//
//   class MinimalBoundary extends React.Component {
//     // React calls this automatically — render phase
//     static getDerivedStateFromError(error) {
//       return { hasError: true };
//     }
//
//     // React calls this automatically — commit phase
//     componentDidCatch(error, info) {
//       logErrorToService(error, info);
//     }
//
//     render() {
//       return this.state.hasError ? <Fallback /> : this.props.children;
//     }
//   }
//
// IMPORTANT NUANCES:
//
//   - NO BOUNDARY, NO CATCH: if no ancestor defines either method, the error
//     propagates all the way up and (React 16+) unmounts the ENTIRE tree by
//     default. There's no automatic fallback unless you've written a boundary.
//
//   - FUNCTION COMPONENTS CAN'T BE BOUNDARIES: there's no hook equivalent of
//     these two lifecycle methods. This is one of the few remaining cases
//     that effectively requires a class component (or a library like
//     react-error-boundary that wraps this for you).
//
//   - ONLY THE NEAREST BOUNDARY FIRES, ONCE: React doesn't bubble the same
//     error to multiple boundaries up the chain — it stops at the first one
//     found going upward.
//
//   - ERRORS INSIDE THE HANDLERS THEMSELVES ESCAPE THE SAME BOUNDARY: if
//     getDerivedStateFromError or componentDidCatch itself throws, that
//     throw is NOT caught by the same boundary — it propagates to the next
//     boundary up (or crashes the app if there is none). This is exactly
//     why the try/catch wrapping around Sentry/onError above (Phase 2,
//     componentDidCatch) is genuinely useful, not excessive caution.


// =============================================================================
// 4. BONUS — A MORE ROBUST resetError (addresses the review note in Section 1)
// =============================================================================
//
// Problem: clicking "Try again" with the original resetError just flips
// hasError back to false and re-renders the SAME children with the SAME
// props — which will likely throw again immediately if the cause was bad
// data/props rather than a transient glitch.
//
// Fix: accept a `resetKeys` prop. When any value in resetKeys changes,
// remount the children by changing a `key`. This forces React to throw
// away the old (broken) subtree and build a fresh one.

class RobustErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, error: null, errorMessage: '' };
  }

  static getDerivedStateFromError(error) {
    return { hasError: true, error, errorMessage: error.message };
  }

  componentDidCatch(error, info) {
    try {
      Sentry.captureException(error, { extra: { componentStack: info.componentStack } });
    } catch (loggingError) {
      console.error('Failed to log error to monitoring:', loggingError);
    }
    try {
      this.props.onError?.(error, info);
    } catch (callbackError) {
      console.error('onError callback threw:', callbackError);
    }
  }

  componentDidUpdate(prevProps) {
    // If the parent changes resetKeys (e.g. a route param, or a retry counter)
    // while we're in an error state, clear the error automatically.
    if (
      this.state.hasError &&
      prevProps.resetKeys &&
      this.props.resetKeys &&
      prevProps.resetKeys.some((val, i) => val !== this.props.resetKeys[i])
    ) {
      this.resetErrorState();
    }
  }

  resetErrorState = () => {
    this.setState({ hasError: false, error: null, errorMessage: '' });
  };

  render() {
    if (this.state.hasError) {
      const { fallback: Fallback } = this.props;
      if (React.isValidElement(Fallback)) return Fallback;
      if (typeof Fallback === 'function') {
        return (
          <Fallback
            error={this.state.error}
            errorMessage={this.state.errorMessage}
            resetError={this.resetErrorState}
          />
        );
      }
      return <p>Something went wrong: {this.state.errorMessage}</p>;
    }
    return this.props.children;
  }
}

// Usage — pass a value that changes when you want a forced remount on retry:
//
// <RobustErrorBoundary resetKeys={[userId]} fallback={MyFallback}>
//   <UserDashboard userId={userId} />
// </RobustErrorBoundary>
//
// Or, simplest of all: give the children a `key` tied to a retry counter,
// and bump the counter inside resetError instead of (or alongside) clearing
// hasError. Changing `key` always forces React to unmount + remount fresh.


module.exports = { ErrorBoundary, RobustErrorBoundary, MyFallback };