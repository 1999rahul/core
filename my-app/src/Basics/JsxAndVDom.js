// JSX — JavaScript XML
/**
 * JSX is not HTML.
 * It is a syntax extension for JavaScript that looks like HTML but compiles down to plain JavaScript function calls. 
 * Babel (the transpiler) converts every JSX element into a React.createElement(type, props, ...children) call before the browser ever sees it.
 */

// Why does JSX exist? Before JSX, writing UI in React looked like this:

React.createElement("div", { className: "card" },
    React.createElement("h1", null, "Hello"),
    React.createElement("p", null, "World")
)

// That's painful to read and write. JSX lets you write:

function Jsx() {
    return <div className="card">
        <h1>Hello</h1>
        <p>World</p>
    </div>
}
// Both produce the same result — a plain JavaScript object describing the UI
// What does the compiled output actually look like?

function CompiledOutput() {
    // You write:
    const button = <button onClick={handleClick} className="btn">Submit</button>;

    // Babel compiles it to:
    const button = React.createElement(
        "button",
        { onClick: handleClick, className: "btn" },
        "Submit"
    );

    // That function returns a plain JS object (a React element):
    const reactElement = {
        type: "button",
        props: { onClick: handleClick, className: "btn", children: "Submit" },
        key: null,
        ref: null
    } // This object is NOT a DOM node. It is just a description of what the DOM should look like.

    /** SOME IMPORTANT POINTS
     * class becomes className because class is a reserved keyword in JavaScript. Similarly, for in labels becomes htmlFor.
     * Every JSX expression must return a single root element. If you have siblings with no parent, wrap them in a Fragment: <>...</>. A Fragment renders no extra DOM node.
     * You cannot put statements like if, for, or while directly inside JSX
     * Event handlers are camelCase: onClick, onChange, onSubmit, onKeyDown. They receive native synthetic events, not raw browser events — React wraps them for cross-browser consistency
     * All tags must be closed. <input> in HTML is fine, but in JSX it must be <input />
     * Custom components must start with an uppercase letter. <div> tells React to render a DOM element. <Button> tells React to call the Button function/class
     */
}

// Virtual DOM — The Core Idea
// The real DOM (Document Object Model) is what the browser renders. It is a tree of objects representing every HTML element. 
// The problem is that DOM operations are slow — reading or writing to the DOM triggers browser processes like style recalculation, layout (reflow), and painting (repaint). 
// If you have a list of 500 items and one item's text changes, naively updating the entire DOM is wasteful

// React's solution is the Virtual DOM — a lightweight copy of the real DOM that lives entirely in JavaScript memory as plain objects. 
// When your component re-renders, React builds a new Virtual DOM tree and compares it to the previous one. This comparison (called diffing or reconciliation) happens entirely in JavaScript — no browser involvement. 
// Then React calculates the minimum set of real DOM changes needed and applies only those.

// A Virtual DOM node is just a plain JavaScript object:

const virtualDomNode = {
    type: "ul",
    props: { className: "list" },
    children: [
        { type: "li", props: { key: "1" }, children: ["Mango"] },
        { type: "li", props: { key: "2" }, children: ["Banana"] }
    ]
}