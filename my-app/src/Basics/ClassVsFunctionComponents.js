// What are Class Components?
// Class components were the original way to write stateful React components. 
// They extend React.Component and must implement a render() method that returns JSX. 
// Before React 16.8, if you needed state or lifecycle methods, you had no choice but to use a class component. 
// Function components were called "stateless functional components" and could only accept props and return JSX — nothing more

import React, { Component } from "react";

class Counter extends Component {
  constructor(props) {
    super(props);           // must call super(props) always
    this.state = {
      count: 0,
    };
    this.increment = this.increment.bind(this); // must bind manually
  }

  increment() {
    this.setState({ count: this.state.count + 1 });
  }

  render() {
    return (
      <div>
        <p>Count: {this.state.count}</p>
        <button onClick={this.increment}>Increment</button>
      </div>
    );
  }
}

// What are Functional Components?

/**
 * A functional component is a plain JavaScript function that accepts props and returns JSX.
 * Before hooks, they had no state and no lifecycle. After React 16.8 introduced hooks, function components became fully capable — they can have state, side effects, context, refs, memoization — everything a class component can do, and more clearly
 */

import { useState } from "react";

function Counter() {
  const [count, setCount] = useState(0);

  return (
    <div>
      <p>Count: {count}</p>
      <button onClick={() => setCount(c => c + 1)}>Increment</button>
    </div>
  );
}

// Lifecycle methods vs useEffect

// Class components have explicit lifecycle methods for different phases of a component's life. 
// Function components consolidate all of these into useEffect with different dependency arrays.