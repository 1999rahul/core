// What are Props
/**
 * Props (short for properties) are the mechanism React uses to pass data from a parent component down to a child component. 
 * They are the primary way components communicate with each other.
 * Props flow in one direction only — from parent to child — and this is called unidirectional data flow. 
 * A child component cannot modify the props it receives. From the child's perspective, props are read-only.
 * Props are passed like HTML attributes and received inside the component as a plain JavaScript object. Every attribute you write on a JSX element becomes a key on the props object.
 */

// Parent passes props
function App() {
    return <UserCard name="Rahul" age={28} isAdmin={true} />;
}

// Child receives them as one object
function UserCard(props) {
    return (
        <div>
            <h2>{props.name}</h2>
            <p>Age: {props.age}</p>
            {props.isAdmin && <span>Admin</span>}
        </div>
    );
}

// You can destructure the props object immediately in the function signature, which is the standard pattern you will see in every modern codebase:
function UserCard({ name, age, isAdmin }) {
    return (
        <div>
            <h2>{name}</h2>
            <p>Age: {age}</p>
            {isAdmin && <span>Admin</span>}
        </div>
    );
}

// What can be passed as props?

/**
 * Anything that is a valid JavaScript value can be a prop — strings, numbers, booleans, objects, arrays, functions, and even other React elements (JSX).
 * This is important for interviews because it means components are composable in very flexible ways.
 */

// // String
// <Button label="Submit" />

// // Number — must use curly braces
// <Avatar size={48} />

// // Boolean — shorthand: just writing the name means true
// <Input disabled />          // same as disabled={true}
// <Input disabled={false} />  // explicitly false

// // Object
// <Profile user={{ name: "Rahul", city: "Patna" }} />

// // Array
// <TagList tags={["react", "hooks", "jsx"]} />

// // Function — used for callbacks (child-to-parent communication)
// <Button onClick={() => console.log("clicked")} />

// // JSX / React elements — used for composition
// <Modal header={<h2>Title</h2>} body={<p>Content</p>} />

// // Children — a special prop that represents nested JSX
// <Card>
//   <p>This becomes props.children</p>
// </Card>

// The children prop
/**
 * props.children is a special built-in prop that React automatically populates with whatever JSX you nest between a component's opening and closing tags. 
 * It is how you build layout components that do not need to know what they will contain.
 */

function Card({ children, title }) {
    return (
        <div className="card">
            <h3>{title}</h3>
            <div className="card-body">{children}</div>
        </div>
    );
}

// Usage — anything between tags becomes children
<Card title="User Info">
    <Avatar src="/photo.jpg" />
    <p>Rahul, 28</p>
    <button>Follow</button>
</Card>
// The Card component has no knowledge of what goes inside it. This is the composition pattern — much more flexible than passing specific props for every possible child element.


// =========== Props vs State — the interview distinction ============

/**
 * This is one of the most commonly asked interview questions. The cleanest answer: props are configuration passed in from outside — the component does not own them and cannot change them. 
 * State is data owned and managed internally by the component — it can change, and when it does, the component re-renders.
 */

// Props — passed in, read-only from the child's perspective
function Temperature({ value, unit }) {
  return <p>{value}°{unit}</p>;
}

// State — owned internally, mutable
function Thermometer() {
  const [temp, setTemp] = useState(36.6);
  return (
    <div>
      <Temperature value={temp} unit="C" />
      <button onClick={() => setTemp(t => t + 0.1)}>+</button>
    </div>
  );
}

// A useful mental model: if a component is like a function, props are the arguments and state is the local variables. 
// Arguments come from the caller. Local variables belong to the function.

// ====================== Callback props — child-to-parent communication ==========================

/**
 * Since data only flows downward, how does a child tell the parent something happened? 
 * The parent passes a function down as a prop. The child calls it when needed. 
 * The parent's function then updates the parent's state, which flows back down as updated props.
 */

function Parent() {
  const [selected, setSelected] = useState(null);

  function handleSelect(item) {
    setSelected(item);  // parent owns the state
  }

  return (
    <div>
      <p>Selected: {selected ?? "none"}</p>
      <ItemList onSelect={handleSelect} />
    </div>
  );
}

function ItemList({ onSelect }) {
  const items = ["Mango", "Banana", "Cherry"];
  return (
    <ul>
      {items.map(item => (
        <li key={item}>
          <button onClick={() => onSelect(item)}>{item}</button>
        </li>
      ))}
    </ul>
  );
}

// This pattern is called lifting state up — the state lives in the closest common ancestor of all components that need it, and callbacks let children communicate changes up to it.


// ==================== Prop Drilling — the problem =======================

/**
 * Prop drilling is what happens when you need to pass data through multiple layers of components that do not themselves use the data — they only exist to pass it down to a deeply nested component that does.
 * Imagine a user object that is fetched at the top-level App component and needs to be displayed in a UserAvatar that sits four levels deep:
 * App
  └── Dashboard
        └── Sidebar
              └── Navigation
                    └── UserAvatar ← actually needs the user prop
                    * Without any state management solution, you must pass user through every level:
 */


// Level 1 — has the data
function App() {
  const [user, setUser] = useState({ name: "Rahul", avatar: "/pic.jpg" });
  return <Dashboard user={user} />;
}

// Level 2 — does not use user, just passes it
function Dashboard({ user }) {
  return (
    <div>
      <Sidebar user={user} />
      <main>Content</main>
    </div>
  );
}

// Level 3 — does not use user, just passes it
function Sidebar({ user }) {
  return (
    <aside>
      <Navigation user={user} />
    </aside>
  );
}

// Level 4 — does not use user, just passes it
function Navigation({ user }) {
  return (
    <nav>
      <UserAvatar user={user} />
    </nav>
  );
}

// Level 5 — ACTUALLY uses the prop
function UserAvatar({ user }) {
  return <img src={user.avatar} alt={user.name} />;
}

// Dashboard, Sidebar, and Navigation are all just pipes. They have no interest in the user prop — they just carry it. This creates several problems:
/**
 * Refactoring pain. Moving UserAvatar to a different position in the tree means rewriting the prop chain across every component it passes through.
 * Reduced readability. Looking at Dashboard, you see a user prop and have to trace through the tree to understand why Dashboard even has it. The component's interface lies about what it actually does
 * Testing overhead. Every intermediate component now requires user to be provided in tests even though it does not use it.
 */

// ====================== Solutions to prop drilling ===================

// 1. Component composition — often the best first fix
/**
 * Before reaching for Context or a state manager, try restructuring with composition. 
 *  Instead of passing data through intermediaries, pass the already-rendered components themselves
 */

// Instead of passing user through Dashboard → Sidebar → Navigation
// render UserAvatar at the top level and pass the element down

function App() {
  const [user, setUser] = useState({ name: "Rahul", avatar: "/pic.jpg" });

  return <Dashboard sidebar={<Sidebar nav={<Navigation avatar={<UserAvatar user={user} />} />} />} />;
}

// Dashboard now receives a ready-made sidebar element
function Dashboard({ sidebar }) {
  return (
    <div>
      {sidebar}
      <main>Content</main>
    </div>
  );
}

// Sidebar receives a ready-made nav element
function Sidebar({ nav }) {
  return <aside>{nav}</aside>;
}

// Navigation receives the already-rendered avatar
function Navigation({ avatar }) {
  return <nav>{avatar}</nav>;
}

// UserAvatar — no change needed
function UserAvatar({ user }) {
  return <img src={user.avatar} alt={user.name} />;
}

// Now Dashboard and Sidebar have no idea about user at all. They just render whatever they receive
// This pattern scales to the children prop too — you can slot components into any position without drilling.

// ================== React Context — for genuinely global data ================

/**
 * Context lets you broadcast a value from a provider anywhere in the tree to any consumer below it, skipping all intermediate components. 
 * It is best suited for data that is truly global — authentication, theme, language, feature flags.
 */

// Step 1 — create the context
const UserContext = createContext(null);

// Step 2 — provide the value at the top
function App() {
  const [user, setUser] = useState({ name: "Rahul", avatar: "/pic.jpg" });
  return (
    <UserContext.Provider value={user}>
      <Dashboard />  {/* no user prop needed */}
    </UserContext.Provider>
  );
}

// Steps 3, 4, 5 — intermediate components are clean
function Dashboard() { return <Sidebar />; }
function Sidebar() { return <Navigation />; }
function Navigation() { return <UserAvatar />; }

// Step 6 — consumer reads directly, no prop chain
function UserAvatar() {
  const user = useContext(UserContext);
  return <img src={user.avatar} alt={user.name} />;
}

/**
 * The critical interview caveat about Context: every component that calls useContext(UserContext) re-renders whenever the context value changes. 
 * If you store a large object in context and it updates frequently, you will cause many unnecessary re-renders.
 * For high-frequency updates, split contexts or use state management libraries
 */

// ============ When to use which solution ================
// Prop drilling of 1–2 levels is completely fine and preferred — it keeps data flow explicit and components independent. 
// Refactor when you notice yourself passing a prop through three or more components that do not use it.
// Reach for composition first — it solves many drilling problems with zero new concepts.
// Reach for Context for data that is read by many components at many levels — theme, auth, locale. 
// Reach for a state manager when you need complex mutations, time-travel debugging, or state that is shared across unrelated subtrees.



