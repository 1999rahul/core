/**
 * ============================================================
 *   REACT ROUTER v6 — HOOKS REFERENCE
 * ============================================================
 *
 *  Hooks covered:
 *   1. useNavigate      — programmatic navigation
 *   2. useParams        — reading dynamic URL segments
 *   3. useLocation      — accessing pathname, search, state
 *   4. useSearchParams  — reading & updating query strings
 *
 *  Install: npm install react-router-dom
 *  Wrap your app in <BrowserRouter> before using any hook.
 * ============================================================
 */

import React, { useState, useEffect } from "react";
import {
  BrowserRouter,
  Routes,
  Route,
  Navigate,
  useNavigate,
  useParams,
  useLocation,
  useSearchParams,
} from "react-router-dom";


/* ============================================================
   1. useNavigate
   ============================================================
   Returns a navigate() function for programmatic navigation.
   Use it when you need to redirect AFTER something happens —
   a form submit, API call, timer, or any event handler.

   This is the imperative twin of <Navigate> component.

   SIGNATURE:
     const navigate = useNavigate();
     navigate(to, options?)

   OPTIONS:
     replace  — true/false  (default: false)
     state    — any data    (readable via useLocation)
     relative — "route" | "path"

   navigate() also accepts a NUMBER to move through history:
     navigate(-1)  → go back  one step  (browser Back)
     navigate( 1)  → go forward one step (browser Forward)
     navigate(-2)  → go back two steps
   ============================================================ */

// ── Example 1: Basic redirect after form submit ──────────────

function LoginForm() {
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  async function handleSubmit(e) {
    e.preventDefault();

    try {
      await loginUser(email, password); // wait for API call
      navigate("/dashboard");           // then redirect — push by default
    } catch (err) {
      console.error("Login failed", err);
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      <input value={email}    onChange={e => setEmail(e.target.value)}    type="email"    />
      <input value={password} onChange={e => setPassword(e.target.value)} type="password" />
      <button type="submit">Log in</button>
    </form>
  );
}

// ── Example 2: push vs replace — the history stack ──────────
//
//  push (default) — adds a new entry to history
//    History before: [/home, /cart]
//    navigate('/checkout')
//    History after:  [/home, /cart, /checkout]
//    Back button → goes to /cart  ✓
//
//  replace — overwrites the current entry
//    History before: [/home, /protected-page]
//    navigate('/login', { replace: true })
//    History after:  [/home, /login]
//    Back button → goes to /home  ✓ (protected-page is gone)
//
//  RULE: Always use replace: true when redirecting from a page
//  the user should NEVER come back to (auth guards, post-payment,
//  post-form-submission). Without it, Back button returns to the
//  guarded page and triggers another redirect — an infinite loop.

function ProtectedRoute({ children }) {
  const location = useLocation();
  const isLoggedIn = useAuth(); // your auth check

  if (!isLoggedIn) {
    return (
      <Navigate
        to="/login"
        replace                             // remove /protected from history
        state={{ from: location.pathname }} // remember where user wanted to go
      />
    );
  }
  return children;
}

function LoginPageWithRedirectBack() {
  const navigate  = useNavigate();
  const location  = useLocation();

  // Read where the user came from (set by ProtectedRoute above)
  const from = location.state?.from ?? "/dashboard";

  async function handleLogin() {
    await loginUser();
    navigate(from, { replace: true }); // send them back, replace login in history
  }

  return <button onClick={handleLogin}>Log in</button>;
}

// ── Example 3: navigate(-1) — programmatic back button ───────

function DetailPage() {
  const navigate = useNavigate();

  return (
    <div>
      <button onClick={() => navigate(-1)}>← Back</button>
      <button onClick={() => navigate( 1)}>Forward →</button>
      <h1>Product Detail</h1>
    </div>
  );
}

// ── Example 4: navigate with state (pass invisible data) ─────

function CheckoutButton({ cartId, total }) {
  const navigate = useNavigate();

  function handleCheckout() {
    navigate("/order-confirm", {
      state: { cartId, total }, // not in URL — invisible to user
    });
  }

  return <button onClick={handleCheckout}>Confirm Order</button>;
}

function OrderConfirmPage() {
  const { state } = useLocation();
  // state → { cartId: "abc123", total: 1499 }
  // NOTE: state is gone if user refreshes — don't rely on it for critical data

  return (
    <div>
      <h1>Order Confirmed!</h1>
      <p>Cart: {state?.cartId} — ₹{state?.total}</p>
    </div>
  );
}

// ── Example 5: navigate after async operation ────────────────

function DeleteAccountButton() {
  const navigate = useNavigate();

  async function handleDelete() {
    const confirmed = window.confirm("Delete your account?");
    if (!confirmed) return;

    await deleteAccount();         // API call
    localStorage.clear();          // cleanup
    navigate("/goodbye", { replace: true }); // redirect, no going back
  }

  return <button onClick={handleDelete}>Delete Account</button>;
}

/*
  SUMMARY — useNavigate:
  ┌──────────────────────────────┬───────────────────────────────────┐
  │ Scenario                     │ Code                              │
  ├──────────────────────────────┼───────────────────────────────────┤
  │ Redirect after login         │ navigate('/dashboard')            │
  │ Auth redirect (no back loop) │ navigate('/login', {replace:true})│
  │ Go back like browser Back    │ navigate(-1)                      │
  │ Pass hidden data             │ navigate('/path', {state:{...}})  │
  │ Redirect from protected page │ <Navigate to="/login" replace />  │
  └──────────────────────────────┴───────────────────────────────────┘
*/


/* ============================================================
   2. useParams
   ============================================================
   Returns an object of key-value pairs from the dynamic
   segments of the currently matched route.

   Every :name in your <Route path> becomes a key.
   Every value is ALWAYS a STRING — parse manually if needed.

   SIGNATURE:
     const params = useParams();
     // params.id, params.slug, params.category, etc.

   IMPORTANT: useParams only works inside components rendered
   by a <Route> — it reads from the nearest matched route above
   it in the tree.
   ============================================================ */

// ── Example 1: Single param ──────────────────────────────────

// Route:     <Route path="/users/:userId" element={<UserProfile />} />
// URL visit: /users/42

function UserProfile() {
  const { userId } = useParams();
  // userId → "42"  (string, not number!)

  const numericId = Number(userId); // parse to number yourself

  return <h1>User #{numericId}</h1>;
}

// ── Example 2: Multiple params ───────────────────────────────

// Route:     <Route path="/shop/:category/:productId" element={<ProductPage />} />
// URL visit: /shop/electronics/iphone-15

function ProductPage() {
  const { category, productId } = useParams();
  // category  → "electronics"
  // productId → "iphone-15"

  return (
    <div>
      <h2>{productId}</h2>
      <p>Category: {category}</p>
    </div>
  );
}

// ── Example 3: Fetch data using a param ──────────────────────
//
//  CRITICAL: Put the param in useEffect's dependency array.
//  When navigating from /users/1 to /users/2, the UserCard
//  component stays mounted — only userId changes.
//  Without userId in deps, the fetch won't re-run → stale data.

function UserCard() {
  const { userId } = useParams();
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    fetch(`/api/users/${userId}`)       // re-fetches on every userId change
      .then(r => r.json())
      .then(data => {
        setUser(data);
        setLoading(false);
      });
  }, [userId]); // ← userId in deps — ALWAYS do this

  if (loading) return <p>Loading...</p>;
  if (!user)   return <p>User not found</p>;

  return <h1>{user.name}</h1>;
}

// ── Example 4: Nested dynamic segments ──────────────────────

// Route:     <Route path="/blog/:year/:month/:slug" element={<BlogPost />} />
// URL visit: /blog/2024/06/react-hooks-explained

function BlogPost() {
  const { year, month, slug } = useParams();
  // year  → "2024"
  // month → "06"
  // slug  → "react-hooks-explained"

  return (
    <article>
      <small>Published: {month}/{year}</small>
      <h1>{slug.replace(/-/g, " ")}</h1>
    </article>
  );
}

// ── Example 5: Optional — checking if a param exists ─────────

function TeamOrPersonPage() {
  const { teamId, memberId } = useParams();
  // memberId may or may not be in the URL depending on which route matched

  if (memberId) {
    return <h1>Member #{memberId} of Team #{teamId}</h1>;
  }
  return <h1>Team #{teamId}</h1>;
}

/*
  SUMMARY — useParams:
  ┌─────────────────────────────────────────────────────────┐
  │ Route path          │ URL            │ useParams()      │
  ├─────────────────────┼────────────────┼──────────────────┤
  │ /users/:id          │ /users/42      │ { id: "42" }     │
  │ /shop/:cat/:id      │ /shop/tech/7   │ { cat:"tech",    │
  │                     │                │   id: "7" }      │
  │ /blog/:year/:slug   │ /blog/2024/hi  │ { year:"2024",   │
  │                     │                │   slug: "hi" }   │
  └─────────────────────┴────────────────┴──────────────────┘

  GOTCHAS:
  ✗ Values are ALWAYS strings — parse with Number() if needed
  ✗ Put params in useEffect deps — or data goes stale on nav
  ✗ useParams() returns {} if no params match — not undefined
*/


/* ============================================================
   3. useLocation
   ============================================================
   Returns the current location object — a snapshot of the
   browser's current URL plus any hidden navigation state.

   Re-renders your component every time the URL changes.

   SIGNATURE:
     const location = useLocation();

   SHAPE:
     location.pathname  → "/products/42"         (the path)
     location.search    → "?sort=price&page=2"   (query string, raw)
     location.hash      → "#reviews"             (fragment)
     location.state     → { from: "/cart" }      (invisible state)
     location.key       → "a1b2c3"               (unique per nav)

   NOTE: Use useSearchParams to PARSE location.search —
   don't parse it manually from useLocation.
   ============================================================ */

// ── Example 1: Reading the current pathname ──────────────────

function Header() {
  const location = useLocation();

  // Hide the header on auth pages
  const isAuthPage = location.pathname.startsWith("/auth");
  if (isAuthPage) return null;

  return <header>My App — {location.pathname}</header>;
}

// ── Example 2: Track page views (analytics) ──────────────────

function Analytics() {
  const location = useLocation();

  useEffect(() => {
    // Fires once on mount AND every time pathname changes
    console.log("Page view:", location.pathname);
    // analytics.track('pageView', { path: location.pathname });
  }, [location.pathname]); // ← re-fires on every navigation

  return null; // this component renders nothing visible
}

// ── Example 3: Scroll to top on navigation ──────────────────

function ScrollToTop() {
  const location = useLocation();

  useEffect(() => {
    window.scrollTo(0, 0); // reset scroll on every page change
  }, [location.pathname]);

  return null;
}

// Usage: Place <ScrollToTop /> once inside <BrowserRouter>,
// above <Routes>, and it applies globally.

// ── Example 4: Active tab without NavLink ───────────────────

function Sidebar() {
  const location = useLocation();

  const tabs = [
    { path: "/dashboard",  label: "Dashboard"  },
    { path: "/analytics",  label: "Analytics"  },
    { path: "/settings",   label: "Settings"   },
  ];

  return (
    <nav>
      {tabs.map(tab => (
        <a
          key={tab.path}
          href={tab.path}
          style={{
            fontWeight: location.pathname.startsWith(tab.path) ? "600" : "400",
            color: location.pathname.startsWith(tab.path) ? "blue" : "gray",
          }}
        >
          {tab.label}
        </a>
      ))}
    </nav>
  );
}

// ── Example 5: The login redirect pattern (full flow) ────────
//
//  This is the most common useLocation pattern in interviews.
//  Step 1: Protected route saves where the user was trying to go
//  Step 2: After login, send them back there

// Step 1 — AuthGuard saves current location in state
function AuthGuard({ children }) {
  const location = useLocation();
  const isLoggedIn = checkAuth();

  if (!isLoggedIn) {
    return (
      <Navigate
        to="/login"
        replace
        state={{ from: location.pathname }} // e.g. "/dashboard/settings"
      />
    );
  }
  return children;
}

// Step 2 — Login reads that state and redirects after success
function LoginWithReturnTo() {
  const navigate = useNavigate();
  const location = useLocation();

  // "from" is the path the user was trying to reach before being redirected
  const from = location.state?.from ?? "/";

  async function handleLogin(credentials) {
    await loginUser(credentials);
    navigate(from, { replace: true }); // send them where they wanted to go
  }

  return (
    <div>
      <p>You need to log in to view: {from}</p>
      <button onClick={() => handleLogin({ email: "x", password: "y" })}>
        Log in
      </button>
    </div>
  );
}

// ── Example 6: Reading all location properties ───────────────
//
//  URL: https://example.com/products?sort=price&page=2#reviews
//  (with state passed from a previous navigate() call)

function LocationDebugger() {
  const location = useLocation();

  return (
    <pre>
      {JSON.stringify(
        {
          pathname: location.pathname, // "/products"
          search:   location.search,   // "?sort=price&page=2"
          hash:     location.hash,     // "#reviews"
          state:    location.state,    // { from: "/cart" }  or null
          key:      location.key,      // "abc123" — unique per navigation
        },
        null, 2
      )}
    </pre>
  );
}

/*
  SUMMARY — useLocation:
  ┌────────────────┬─────────────────────────────────────────────┐
  │ Property       │ Use for                                     │
  ├────────────────┼─────────────────────────────────────────────┤
  │ pathname       │ Active states, analytics, conditional UI    │
  │ search         │ Raw query string — parse with useSearchParams│
  │ hash           │ Scroll-to-anchor, section tracking          │
  │ state          │ Redirect origin, temp context (not in URL)  │
  │ key            │ Animation triggers, per-navigation effects  │
  └────────────────┴─────────────────────────────────────────────┘

  LOCATION STATE vs URL PARAMS vs QUERY STRINGS:
  - URL params (/users/:id)   → visible in URL, survives refresh
  - Query strings (?sort=asc) → visible in URL, survives refresh
  - location.state            → NOT in URL, gone on hard refresh
                                Use for temporary one-trip context only
*/


/* ============================================================
   4. useSearchParams
   ============================================================
   Works like useState — but the state lives in the URL's
   query string (?key=value).

   Benefits:
   ✓ State survives page refresh
   ✓ Users can bookmark/share filtered views
   ✓ Back/Forward browser buttons work naturally

   SIGNATURE:
     const [searchParams, setSearchParams] = useSearchParams();

   searchParams   → URLSearchParams object  (for reading)
   setSearchParams → function               (for writing)

   URLSearchParams methods:
     .get(key)        → value string or null
     .getAll(key)     → array (for repeated params like ?tag=a&tag=b)
     .set(key, value) → mutates in place
     .delete(key)     → removes a param
     .has(key)        → boolean
     .toString()      → "key=val&key2=val2"
   ============================================================ */

// ── Example 1: Basic read and write ──────────────────────────

// URL: /products?sort=price&page=2

function ProductListBasic() {
  const [searchParams, setSearchParams] = useSearchParams();

  // Reading — always returns strings or null
  const sort = searchParams.get("sort") ?? "newest"; // "price" or default
  const page = Number(searchParams.get("page") ?? "1"); // parse to number

  function handleSortChange(newSort) {
    // Object form — REPLACES entire query string
    setSearchParams({ sort: newSort, page: "1" });
    // URL becomes: ?sort=newSort&page=1  (other params would be lost)
  }

  return (
    <div>
      <p>Sorted by: {sort} | Page: {page}</p>
      <button onClick={() => handleSortChange("price")}>Sort by Price</button>
      <button onClick={() => handleSortChange("name")}>Sort by Name</button>
    </div>
  );
}

// ── Example 2: Preserve existing params when updating one ────
//
//  MOST COMMON MISTAKE IN INTERVIEWS:
//
//  setSearchParams({ page: "2" })
//  → This WIPES all other params — sort, category, etc. are gone!
//
//  CORRECT APPROACH — use the callback form:

function SafeParamUpdate() {
  const [searchParams, setSearchParams] = useSearchParams();

  function goToPage(pageNum) {
    setSearchParams(prev => {
      // prev is a mutable copy of current URLSearchParams
      prev.set("page", String(pageNum)); // update only page
      return prev;                       // sort, category etc. stay intact
    });
  }

  function removeFilter(key) {
    setSearchParams(prev => {
      prev.delete(key); // remove a single param
      return prev;
    });
  }

  function addTag(tag) {
    setSearchParams(prev => {
      prev.append("tag", tag); // append without replacing existing tags
      return prev;
    });
  }

  return (
    <div>
      <button onClick={() => goToPage(2)}>Page 2</button>
      <button onClick={() => removeFilter("category")}>Clear Category</button>
      <button onClick={() => addTag("react")}>Add React tag</button>
    </div>
  );
}

// ── Example 3: Full filter + sort + pagination ───────────────
//
//  URL shape: /shop?category=electronics&sort=price&page=2

function ShopPage() {
  const [searchParams, setSearchParams] = useSearchParams();

  // Read all params with fallback defaults
  const category = searchParams.get("category") ?? "all";
  const sort      = searchParams.get("sort")     ?? "newest";
  const page      = Number(searchParams.get("page") ?? "1");

  // Generic setter — updates one param, always resets page
  // (except when changing page itself)
  function updateFilter(key, value) {
    setSearchParams(prev => {
      prev.set(key, value);
      if (key !== "page") prev.set("page", "1"); // reset page on filter change
      return prev;
    });
  }

  return (
    <div>
      {/* Category filter */}
      <select
        value={category}
        onChange={e => updateFilter("category", e.target.value)}
      >
        <option value="all">All</option>
        <option value="electronics">Electronics</option>
        <option value="books">Books</option>
        <option value="clothing">Clothing</option>
      </select>

      {/* Sort */}
      <select
        value={sort}
        onChange={e => updateFilter("sort", e.target.value)}
      >
        <option value="newest">Newest</option>
        <option value="price">Price</option>
        <option value="rating">Rating</option>
      </select>

      {/* Pagination */}
      <button
        onClick={() => updateFilter("page", String(page - 1))}
        disabled={page <= 1}
      >
        ← Prev
      </button>
      <span>Page {page}</span>
      <button onClick={() => updateFilter("page", String(page + 1))}>
        Next →
      </button>

      <p>
        Showing {category} sorted by {sort}, page {page}
      </p>
    </div>
  );
}

// ── Example 4: Multi-value params (repeated keys) ────────────
//
//  URL: /results?tag=react&tag=typescript&tag=hooks

function TagFilter() {
  const [searchParams, setSearchParams] = useSearchParams();

  // getAll returns an array for repeated keys
  const activeTags = searchParams.getAll("tag"); // ["react", "typescript", "hooks"]

  function toggleTag(tag) {
    setSearchParams(prev => {
      const current = prev.getAll("tag");
      // Remove all existing tags, then re-add without the toggled one
      prev.delete("tag");
      const next = current.includes(tag)
        ? current.filter(t => t !== tag) // remove it
        : [...current, tag];             // add it
      next.forEach(t => prev.append("tag", t));
      return prev;
    });
  }

  const allTags = ["react", "typescript", "hooks", "router"];

  return (
    <div>
      {allTags.map(tag => (
        <button
          key={tag}
          onClick={() => toggleTag(tag)}
          style={{ fontWeight: activeTags.includes(tag) ? "bold" : "normal" }}
        >
          {tag}
        </button>
      ))}
      <p>Active: {activeTags.join(", ") || "none"}</p>
    </div>
  );
}

// ── Example 5: Search input synced to URL ────────────────────
//
//  URL: /search?q=react+hooks

function SearchPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const query = searchParams.get("q") ?? "";

  function handleSearch(e) {
    const value = e.target.value;
    setSearchParams(prev => {
      if (value) {
        prev.set("q", value);
      } else {
        prev.delete("q"); // clean URL when input is empty
      }
      return prev;
    });
  }

  return (
    <div>
      <input
        type="search"
        value={query}
        onChange={handleSearch}
        placeholder="Search..."
      />
      {query && <p>Results for: "{query}"</p>}
    </div>
  );
}

// ── Example 6: useSearchParams vs useState — comparison ──────
//
//  BOTH hold state and trigger re-renders when changed.
//  The key difference is WHERE the state lives.
//
//  useState:
//    - State lives in component memory
//    - Gone on refresh / navigation
//    - Not shareable via URL
//    - Use for: UI state (open/closed, hover, form inputs mid-type)
//
//  useSearchParams:
//    - State lives in the URL query string
//    - Survives refresh, shareable, bookmarkable
//    - Back/Forward browser buttons work
//    - Use for: filters, sort, pagination, search query, tabs

/*
  SUMMARY — useSearchParams:
  ┌──────────────────────────────────────┬──────────────────────────────┐
  │ Task                                 │ Code                         │
  ├──────────────────────────────────────┼──────────────────────────────┤
  │ Read one param                       │ searchParams.get('key')      │
  │ Read repeated params                 │ searchParams.getAll('tag')   │
  │ Check if param exists                │ searchParams.has('key')      │
  │ Replace ALL params                   │ setSearchParams({k:v})       │
  │ Update ONE param safely              │ setSearchParams(prev => {    │
  │                                      │   prev.set('k','v');         │
  │                                      │   return prev;})             │
  │ Delete a param                       │ prev.delete('key')           │
  │ Add repeated param                   │ prev.append('tag', val)      │
  └──────────────────────────────────────┴──────────────────────────────┘

  ALL VALUES ARE STRINGS — always parse numbers manually:
    Number(searchParams.get('page'))
    parseInt(searchParams.get('limit'), 10)
*/


/* ============================================================
   QUICK REFERENCE — ALL FOUR HOOKS
   ============================================================

   HOOK              RETURNS              USE WHEN
   ─────────────────────────────────────────────────────────
   useNavigate()     navigate() fn        Redirect from events/async code
   useParams()       { key: "value" }     Read :id, :slug etc. from URL path
   useLocation()     location object      Read pathname, state, hash, search
   useSearchParams() [params, setter]     Read/write ?key=val query strings
   ─────────────────────────────────────────────────────────

   TYPE SAFETY — all URL-derived values are STRINGS:
     useParams()       → "42"   not 42
     searchParams.get  → "true" not true
     Always parse: Number(id), Boolean(flag === "true")

   COMMON INTERVIEW PATTERNS:
   ─────────────────────────────────────────────────────────
   1. LOGIN REDIRECT
      ProtectedRoute → <Navigate to="/login" state={{from: location.pathname}} replace />
      LoginPage      → navigate(location.state?.from ?? '/', { replace: true })

   2. FETCH ON PARAM CHANGE
      useEffect(() => { fetch('/api/' + id) }, [id])  ← id from useParams

   3. SHAREABLE FILTERS
      setSearchParams(prev => { prev.set('sort', val); return prev; })

   4. CUSTOM BACK BUTTON
      navigate(-1)

   5. ANALYTICS ON EVERY PAGE
      useEffect(() => { analytics.page(location.pathname) }, [location.pathname])
   ─────────────────────────────────────────────────────────
*/


// ── Stub helpers used above (not part of the tutorial) ───────

function checkAuth()         { return false; }
function useAuth()           { return false; }
async function loginUser()   { return { user: { name: "Alice" } }; }
async function deleteAccount() { return true; }

export {
  LoginForm,
  LoginPageWithRedirectBack,
  DetailPage,
  CheckoutButton,
  OrderConfirmPage,
  UserProfile,
  UserCard,
  ProductPage,
  BlogPost,
  Header,
  Analytics,
  ScrollToTop,
  LoginWithReturnTo,
  LocationDebugger,
  ShopPage,
  TagFilter,
  SearchPage,
};