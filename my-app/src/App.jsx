import UserDemo from "./Routing/Questions/UserProfile/User";
import ErrorHandlerComponent from "./ErrorHandling/ErrorHandlerComponent";
import React from "react";
import ToDoApp from "./StateManagement/ToDoApp";
import { todoState, ToDoReducer, ToDoStateContext, ToDoDispatchContext } from "./StateManagement/State";
import { useReducer } from "react";

function App() {
  const [state, dispatch] = useReducer(ToDoReducer, todoState);

  return <ToDoStateContext.Provider value={state}>
    <ToDoDispatchContext.Provider value={dispatch}>
      <ToDoApp />
    </ToDoDispatchContext.Provider>
  </ToDoStateContext.Provider>;
}

export default App;
