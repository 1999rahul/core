//  Routing in React means navigating between different components/pages without a full browser page reload — this is what makes React a Single Page Application (SPA). 

// =================== How Routing Works in a Browser ===================
/**
 * Before understanding the difference, you need to understand one core concept — when you type a URL in the browser and hit enter, the browser sends a request to the server. 
 * In a traditional website, the server returns a different HTML page for each URL.
 * But in a React SPA, there is only one HTML file (index.html). So React Router has to intercept the URL and decide what to render — without the server being involved. 
 * Both BrowserRouter and HashRouter solve this problem, but in different ways.
 */

// ====================================================================== 


/**
 * ============================================================
 *   REACT ROUTER v6 — CORE CONCEPTS REFERENCE
 * ============================================================
 *
 *  Topics covered:
 *   1. Routes & Route       — URL matching & rendering
 *   2. Link vs NavLink      — navigation & active states
 *   3. Outlet               — nested routing & shared layouts
 *   4. Navigate component   — declarative redirects
 *   5. Index Routes         — default child route concept
 *
 *  NOTE: This file contains annotated JSX/React code.
 *  Run it in a React project (CRA / Vite) with:
 *    npm install react-router-dom
 * ============================================================
 */

import React, { createContext, useContext, useState } from "react";
import {
  BrowserRouter,
  Routes,
  Route,
  Link,
  NavLink,
  Outlet,
  Navigate,
  useNavigate,
  useLocation,
  useOutletContext,
  useParams,
} from "react-router-dom";


/* ============================================================
   1. ROUTES & ROUTE
   ============================================================
   - <Routes>  : Container. Looks at all children, renders
                 ONLY the first Route whose path best matches
                 the current URL. Replaces <Switch> from v5.
   - <Route>   : Declaration. Maps a URL pattern → element.
   
   KEY DIFFERENCES FROM v5:
   ┌─────────────────┬───────────────────┬────────────────────┐
   │ Feature         │ v5                │ v6                 │
   ├─────────────────┼───────────────────┼────────────────────┤
   │ Container tag   │ <Switch>          │ <Routes>           │
   │ Component prop  │ component={Home}  │ element={<Home/>}  │
   │ Exact matching  │ exact required    │ exact by default   │
   │ Route ranking   │ top-to-bottom     │ specificity-based  │
   │ 404 wildcard    │ <Route path="*">  │ same, ranked last  │
   └─────────────────┴───────────────────┴────────────────────┘
   ============================================================ */

function RoutesAndRouteExample() {
  return (
    <BrowserRouter>
      <Routes>
        {/* "/" matches only the root — exact by default in v6 */}
        <Route path="/" element={<HomePage />} />

        {/* Static paths */}
        <Route path="/about" element={<AboutPage />} />
        <Route path="/contact" element={<ContactPage />} />

        {/* Dynamic segment — :id is a URL param, read with useParams() */}
        <Route path="/users/:id" element={<UserProfile />} />

        {/* Nested routes — DashboardLayout has an <Outlet /> */}
        <Route path="/dashboard" element={<DashboardLayout />}>
          <Route index element={<DashboardHome />} />
          <Route path="stats" element={<DashboardStats />} />
          <Route path="reports" element={<DashboardReports />} />
        </Route>

        {/* Wildcard — renders for any unmatched URL.
            v6 always ranks this last regardless of position. */}
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </BrowserRouter>
  );
}

// Reading a dynamic segment from the URL
function UserProfile() {
  const { id } = useParams(); // reads ":id" from the URL
  return <h1>User #{id}</h1>;
}

/* ── Interview Q ──────────────────────────────────────────────
   Q: Why was <Switch> replaced with <Routes> in v6?
   A: <Routes> uses a specificity-based ranking algorithm
      (similar to CSS selector specificity) to pick the best
      match, rather than the first match. It also mandates the
      `element` prop (JSX), removing the confusion between
      `component`, `render`, and `children` props in v5.
      Additionally, `exact` is no longer needed — all paths
      match exactly by default.
   ──────────────────────────────────────────────────────────── */


/* ============================================================
   2. LINK vs NAVLINK
   ============================================================
   Both render an <a> tag and navigate WITHOUT a full page
   reload (client-side navigation).

   <Link>    — plain navigation. No awareness of active state.
   <NavLink> — same, but knows if its `to` path is currently
               active. Passes { isActive, isPending } to
               className / style callbacks.

   WHEN TO USE:
   - Link    → buttons, breadcrumbs, back links, cards
   - NavLink → sidebars, top navs, tab bars, menus
   ============================================================ */

// ── Link examples ────────────────────────────────────────────

function LinkExamples() {
  return (
    <nav>
      {/* Basic navigation */}
      <Link to="/about">About</Link>

      {/* Relative navigation (goes up one segment, then to settings) */}
      <Link to="../settings">Settings</Link>

      {/* Pass state — readable via useLocation() in the target component */}
      <Link to="/checkout" state={{ cartId: "abc123" }}>
        Checkout
      </Link>

      {/* Replace current history entry (back button skips this page) */}
      <Link to="/home" replace>
        Go Home
      </Link>
    </nav>
  );
}

// Reading state passed via Link
function CheckoutPage() {
  const location = useLocation();
  const { cartId } = location.state ?? {}; // safely read passed state
  return <p>Cart ID: {cartId}</p>;
}

// ── NavLink examples ─────────────────────────────────────────

function NavLinkExamples() {
  return (
    <nav>
      {/* className callback — isActive is true when URL matches */}
      <NavLink
        to="/dashboard"
        className={({ isActive }) =>
          isActive ? "nav-link nav-link--active" : "nav-link"
        }
      >
        Dashboard
      </NavLink>

      {/* Inline style callback */}
      <NavLink
        to="/profile"
        style={({ isActive }) => ({
          fontWeight: isActive ? "600" : "400",
          color: isActive ? "#2563eb" : "#6b7280",
          textDecoration: "none",
        })}
      >
        Profile
      </NavLink>

      {/* `end` prop — only active on EXACT "/" match.
          Without `end`, the "/" NavLink stays active on every route
          because every path starts with "/".               */}
      <NavLink to="/" end className={({ isActive }) =>
        isActive ? "active" : ""
      }>
        Home
      </NavLink>
    </nav>
  );
}

// ── Building a full nav bar with NavLink ─────────────────────

const NAV_ITEMS = [
  { to: "/",         label: "Home",      end: true  },
  { to: "/products", label: "Products",  end: false },
  { to: "/blog",     label: "Blog",      end: false },
  { to: "/contact",  label: "Contact",   end: false },
];

function MainNavbar() {
  return (
    <header style={{ display: "flex", gap: "1rem", padding: "1rem" }}>
      {NAV_ITEMS.map(({ to, label, end }) => (
        <NavLink
          key={to}
          to={to}
          end={end}
          className={({ isActive }) =>
            ["nav-item", isActive && "nav-item--active"]
              .filter(Boolean)
              .join(" ")
          }
        >
          {label}
        </NavLink>
      ))}
    </header>
  );
}

/* ── Interview Q ──────────────────────────────────────────────
   Q: What is the difference between Link and NavLink?
   A: Link renders a client-side <a> tag with no extra logic.
      NavLink does the same but additionally knows whether its
      target URL is the currently active route. It exposes
      { isActive, isPending } to the className and style props
      as callbacks, allowing conditional styling. Use NavLink
      for navigation menus; use Link everywhere else.

   Q: What does the `end` prop do on NavLink?
   A: Without `end`, a NavLink to "/" is active on EVERY route
      because all paths begin with "/". The `end` prop tells
      NavLink to only be active when the URL matches the `to`
      path exactly (end of URL), not as a prefix.
   ──────────────────────────────────────────────────────────── */


/* ============================================================
   3. OUTLET
   ============================================================
   <Outlet /> is a placeholder inside a parent route's element.
   It renders whichever CHILD route currently matches the URL.

   This enables SHARED LAYOUTS — the parent component (header,
   sidebar, nav) mounts ONCE and stays mounted. Only the content
   at the <Outlet /> position swaps when navigating.

   FLOW:
     URL: /dashboard          → DashboardLayout + DashboardHome
     URL: /dashboard/stats    → DashboardLayout + DashboardStats
     URL: /dashboard/reports  → DashboardLayout + DashboardReports

   The sidebar in DashboardLayout never unmounts.
   ============================================================ */

// ── Route config (defined in App or router setup) ────────────

function AppWithNestedRoutes() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Parent route — element must contain <Outlet /> */}
        <Route path="/dashboard" element={<DashboardLayout />}>
          <Route index element={<DashboardHome />} />          {/* /dashboard       */}
          <Route path="stats" element={<DashboardStats />} />  {/* /dashboard/stats  */}
          <Route path="reports" element={<DashboardReports />} />{/* /dashboard/reports */}
          <Route path="users/:id" element={<UserProfile />} /> {/* /dashboard/users/5 */}
        </Route>

        {/* Another layout — e.g. auth pages share a centered card layout */}
        <Route path="/auth" element={<AuthLayout />}>
          <Route index element={<LoginPage />} />
          <Route path="register" element={<RegisterPage />} />
          <Route path="forgot-password" element={<ForgotPasswordPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

// ── Parent layout — contains <Outlet /> ──────────────────────

function DashboardLayout() {
  return (
    <div style={{ display: "flex", height: "100vh" }}>
      {/* Sidebar — persists across all child routes */}
      <aside style={{ width: 240, borderRight: "1px solid #e5e7eb" }}>
        <nav style={{ padding: "1rem" }}>
          {/* Use NavLink here so the active child gets highlighted */}
          <NavLink to="/dashboard" end>Overview</NavLink>
          <NavLink to="/dashboard/stats">Stats</NavLink>
          <NavLink to="/dashboard/reports">Reports</NavLink>
        </nav>
      </aside>

      {/* Main content area — child component renders here */}
      <main style={{ flex: 1, padding: "2rem" }}>
        <Outlet />  {/* ← THIS is where child routes appear */}
      </main>
    </div>
  );
}

// ── Passing data to child routes via Outlet context ──────────
//
//    Outlet accepts a `context` prop — any value you want
//    available to ALL child routes without prop drilling.
//    Children read it with useOutletContext().

function DashboardLayoutWithContext() {
  const [user] = useState({ name: "Alice", role: "admin" });
  const [theme, setTheme] = useState("light");

  return (
    <div>
      <aside>
        <button onClick={() => setTheme(t => t === "light" ? "dark" : "light")}>
          Toggle theme
        </button>
        <Outlet context={{ user, theme, setTheme }} />
      </aside>
    </div>
  );
}

// Child reads context
function DashboardStats() {
  const { user, theme } = useOutletContext();
  return (
    <div>
      <p>Logged in as: {user.name} (theme: {theme})</p>
      <h2>Stats</h2>
    </div>
  );
}

/* ── Interview Q ──────────────────────────────────────────────
   Q: How does <Outlet> enable shared layouts in React Router v6?
   A: When you nest Route elements inside another Route, the
      parent's element component mounts once. As the user
      navigates between child routes, only the content at the
      <Outlet /> position is swapped — the rest of the parent
      (navbar, sidebar, header) stays mounted and doesn't
      re-render. This avoids duplicating layout code across
      every page component.

   Q: How do you pass data from a parent layout to nested routes?
   A: Use the `context` prop on <Outlet context={value} /> and
      read it with the useOutletContext() hook in the child.
   ──────────────────────────────────────────────────────────── */


/* ============================================================
   4. NAVIGATE COMPONENT
   ============================================================
   <Navigate /> triggers a navigation as a SIDE EFFECT of
   rendering. When React renders this component, the user
   is immediately redirected to the `to` path.

   Replaces <Redirect> from v5.

   DECLARATIVE  (render-time)  →  use <Navigate>
   IMPERATIVE   (event-time)   →  use useNavigate() hook

   KEY PROP: `replace`
   - Default (push):  adds a new history entry. Back button
                      returns to the page that triggered redirect.
   - replace:         replaces current history entry. Back button
                      goes to the page BEFORE the redirect.
   → Always use `replace` on protected route redirects to avoid
     back-button loops.
   ============================================================ */

// ── Most common use case: Protected Route ────────────────────

// Simulated auth context
const AuthContext = createContext(null);

function AuthProvider({ children }) {
  const [user, setUser] = useState(null); // null = not logged in
  const login  = (u) => setUser(u);
  const logout = ()  => setUser(null);
  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

function useAuth() {
  return useContext(AuthContext);
}

// Protected route wrapper — redirects to /login if not authenticated
function ProtectedRoute({ children }) {
  const { user } = useAuth();
  const location = useLocation();

  if (!user) {
    return (
      <Navigate
        to="/login"
        replace                            // don't pollute history
        state={{ from: location.pathname }} // remember where user came from
      />
    );
  }

  return children; // render protected content
}

// Usage in route config
function AppWithProtectedRoutes() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login"   element={<LoginPage />} />
          <Route path="/signup"  element={<RegisterPage />} />

          {/* Wrap protected pages */}
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <DashboardLayout />
              </ProtectedRoute>
            }
          >
            <Route index element={<DashboardHome />} />
            <Route path="settings" element={<SettingsPage />} />
          </Route>

          {/* Redirect root to dashboard */}
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

// ── Login page — redirect back after successful login ────────

function LoginPage() {
  const { login } = useAuth();
  const navigate  = useNavigate();
  const location  = useLocation();

  // Where to go after login (set by ProtectedRoute above)
  const from = location.state?.from ?? "/dashboard";

  function handleSubmit(e) {
    e.preventDefault();
    const formData = new FormData(e.target);
    const email    = formData.get("email");
    // ... verify credentials ...
    login({ email, name: "Alice" });   // update auth state
    navigate(from, { replace: true }); // imperative: redirect after action
  }

  return (
    <form onSubmit={handleSubmit}>
      <input name="email" type="email" placeholder="Email" />
      <input name="password" type="password" placeholder="Password" />
      <button type="submit">Log in</button>
    </form>
  );
}

// ── Navigate with conditional logic ──────────────────────────

function RoleBasedRedirect() {
  const { user } = useAuth();

  if (!user)              return <Navigate to="/login"   replace />;
  if (user.role === "admin") return <Navigate to="/admin"   replace />;
  if (user.role === "manager") return <Navigate to="/manager" replace />;

  return <Navigate to="/home" replace />;
}

// ── Navigate vs useNavigate — comparison ─────────────────────
/*
  <Navigate>         — component, fires on RENDER, use in JSX conditions
  useNavigate()      — hook, returns a function, use inside event handlers

  | Scenario                             | Use              |
  |--------------------------------------|------------------|
  | Redirect unauthenticated user        | <Navigate>       |
  | Redirect after form submit           | useNavigate()    |
  | Redirect after API call succeeds     | useNavigate()    |
  | Default route redirect (path="/")    | <Navigate>       |
  | Role-based routing on render         | <Navigate>       |
  | Back navigation (navigate(-1))       | useNavigate()    |
*/

function FormWithImperativeNav() {
  const navigate = useNavigate();

  async function handleSubmit(e) {
    e.preventDefault();
    await saveData();                  // wait for async action
    navigate("/success", {
      replace: true,
      state: { message: "Saved!" },
    });
  }

  return <form onSubmit={handleSubmit}>...</form>;
}

/* ── Interview Q ──────────────────────────────────────────────
   Q: What is the difference between <Navigate> and useNavigate?
   A: <Navigate> is a component that redirects when rendered —
      it's used for declarative, render-time redirects in JSX,
      like guarding a protected route. useNavigate() is a hook
      that returns an imperative navigate() function, used when
      navigation should happen as a result of user action or
      async logic (e.g., after form submission or API response).

   Q: Why should you use `replace` on protected route redirects?
   A: Without `replace`, the browser adds an entry to history.
      If the user hits the back button, they return to the
      protected page, which triggers another redirect — an
      infinite loop. With `replace`, the redirect replaces the
      protected page in history, so back goes to the page before
      they ever tried to access the protected route.
   ──────────────────────────────────────────────────────────── */


/* ============================================================
   5. INDEX ROUTES
   ============================================================
   An index route is a child <Route> with the `index` prop
   (no `path`). It renders at the PARENT's EXACT URL, filling
   the <Outlet /> when no other child path matches.

   Think of it as the "index.html" of a folder: visiting the
   parent's URL shows the index route's component.

   RULES:
   ✓ Use `index` prop — never a `path` prop on the same route
   ✓ Renders at parent's exact URL
   ✓ Cannot have child routes
   ✓ One per parent (at most)
   ✓ Does not affect URL — no new URL segment added
   ============================================================ */

// ── Without index route — the "empty Outlet" problem ─────────

function AppWithoutIndex() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/settings" element={<SettingsLayout />}>
          {/* Visiting "/settings" shows SettingsLayout with an
              EMPTY Outlet — nothing renders in the content area! */}
          <Route path="account"  element={<AccountSettings />} />
          <Route path="billing"  element={<BillingSettings />} />
          <Route path="security" element={<SecuritySettings />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

// ── With index route — solved ─────────────────────────────────

function AppWithIndex() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/settings" element={<SettingsLayout />}>
          {/* Now visiting "/settings" shows GeneralSettings */}
          <Route index element={<GeneralSettings />} />         {/* /settings          */}
          <Route path="account"  element={<AccountSettings />} />{/* /settings/account  */}
          <Route path="billing"  element={<BillingSettings />} />{/* /settings/billing  */}
          <Route path="security" element={<SecuritySettings />} />{/* /settings/security */}
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

// ── Full real-world example: Dashboard with index ────────────

function FullDashboardRouter() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Auth layout — centered card for auth pages */}
        <Route path="/auth" element={<AuthLayout />}>
          <Route index element={<LoginPage />} />            {/* /auth                */}
          <Route path="register" element={<RegisterPage />} />{/* /auth/register       */}
          <Route path="reset"    element={<ForgotPasswordPage />} />{/* /auth/reset   */}
        </Route>

        {/* Main app — protected, with sidebar */}
        <Route
          path="/"
          element={
            <ProtectedRoute>
              <DashboardLayout />
            </ProtectedRoute>
          }
        >
          {/* "/" → shows Overview by default */}
          <Route index element={<OverviewPage />} />

          {/* Nested settings with its OWN index */}
          <Route path="settings" element={<SettingsLayout />}>
            <Route index element={<GeneralSettings />} />     {/* /settings           */}
            <Route path="account"  element={<AccountSettings />} />
            <Route path="billing"  element={<BillingSettings />} />
          </Route>

          {/* Team section */}
          <Route path="team" element={<TeamLayout />}>
            <Route index element={<TeamOverview />} />       {/* /team                */}
            <Route path=":memberId" element={<MemberDetail />} />{/* /team/42         */}
          </Route>

          <Route path="analytics" element={<AnalyticsPage />} />
        </Route>

        {/* Root redirect */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

/* ── Interview Q ──────────────────────────────────────────────
   Q: What is an index route and when would you use it?
   A: An index route is a child Route declared with the `index`
      prop (no `path`). It renders at the parent route's exact
      URL, filling the Outlet with default content when no
      other child route matches. Use it whenever a layout route
      needs to show meaningful content at its own URL — like
      a dashboard overview on "/dashboard", or general settings
      on "/settings". Without an index route, the Outlet renders
      nothing when the user is at the parent's URL.

   Q: Can an index route have its own children?
   A: No. Index routes cannot have child routes. They are always
      leaf nodes in the route tree.

   Q: What is the difference between path="/" and index?
   A: <Route path="/"> is a top-level route matching the root
      URL. <Route index> is a CHILD route that renders at the
      parent's URL — it is relative to the parent, not absolute.
      Inside a parent at "/dashboard", an index route matches
      exactly "/dashboard", not "/".
   ──────────────────────────────────────────────────────────── */


/* ============================================================
   QUICK REFERENCE — ALL FIVE CONCEPTS
   ============================================================

   CONCEPT          COMPONENT/HOOK        KEY PROP / USAGE
   ─────────────────────────────────────────────────────────
   Route container  <Routes>              wraps all <Route>s
   Route mapping    <Route>               path, element
   Basic nav        <Link>                to, replace, state
   Active nav       <NavLink>             to, end, className({isActive})
   Layout slot      <Outlet>              context (optional)
   Read context     useOutletContext()    —
   Declarative redir <Navigate>           to, replace, state
   Imperative nav   useNavigate()         navigate(path, options)
   Default child    <Route index>         no path needed
   URL params       useParams()           reads :param from URL
   Location info    useLocation()         pathname, state, search
   ─────────────────────────────────────────────────────────

   NESTED ROUTE ANATOMY:
   ┌─────────────────────────────────────────────────────┐
   │  <Route path="/dashboard" element={<Layout />}>     │
   │    ├── <Route index element={<Home />} />           │
   │    ├── <Route path="stats"   element={<Stats />} /> │
   │    └── <Route path="profile" element={<Profile />}/>│
   │  </Route>                                           │
   │                                                     │
   │  Layout.jsx                                         │
   │  ┌──────────────────────────────────┐               │
   │  │  <Sidebar /> (always mounted)    │               │
   │  │  <main>                          │               │
   │  │    <Outlet /> ← child goes here  │               │
   │  │  </main>                         │               │
   │  └──────────────────────────────────┘               │
   └─────────────────────────────────────────────────────┘

   URL             SIDEBAR    OUTLET CONTENT
   /dashboard    → visible  → <Home />
   /dashboard/stats → visible → <Stats />
   /dashboard/profile → visible → <Profile />
   ============================================================ */


// ── Placeholder components used above (stubs for completeness) ──

function HomePage()            { return <h1>Home</h1>; }
function AboutPage()           { return <h1>About</h1>; }
function ContactPage()         { return <h1>Contact</h1>; }
function NotFoundPage()        { return <h1>404 — Not Found</h1>; }
function DashboardHome()       { return <h2>Dashboard Overview</h2>; }
function DashboardReports()    { return <h2>Reports</h2>; }
function OverviewPage()        { return <h2>Overview</h2>; }
function AnalyticsPage()       { return <h2>Analytics</h2>; }
function SettingsPage()        { return <h2>Settings</h2>; }
function SettingsLayout()      { return <div><h2>Settings</h2><Outlet /></div>; }
function GeneralSettings()     { return <h3>General</h3>; }
function AccountSettings()     { return <h3>Account</h3>; }
function BillingSettings()     { return <h3>Billing</h3>; }
function SecuritySettings()    { return <h3>Security</h3>; }
function AuthLayout()          { return <div className="auth-card"><Outlet /></div>; }
function RegisterPage()        { return <h2>Register</h2>; }
function ForgotPasswordPage()  { return <h2>Forgot Password</h2>; }
function TeamLayout()          { return <div><h2>Team</h2><Outlet /></div>; }
function TeamOverview()        { return <h3>Team Overview</h3>; }
function MemberDetail()        { const { memberId } = useParams(); return <h3>Member #{memberId}</h3>; }

async function saveData() { /* stub */ }

export default FullDashboardRouter;