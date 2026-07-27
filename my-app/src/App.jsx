import UserDemo from "./Routing/Questions/UserProfile/User";
import ErrorHandlerComponent from "./ErrorHandling/ErrorHandlerComponent";
import React from "react";
import ToDoApp from "./StateManagement/ToDoApp";
import { todoState, ToDoReducer, ToDoStateContext, ToDoDispatchContext } from "./StateManagement/State";
import { useReducer } from "react";
import useFetch  from "./Hooks/CustomHookQuestions/useFetch";

function App() {
  //const [state, dispatch] = useReducer(ToDoReducer, todoState);

  // return <ToDoStateContext.Provider value={state}>
  //   <ToDoDispatchContext.Provider value={dispatch}>
  //     <ToDoApp />
  //   </ToDoDispatchContext.Provider>
  // </ToDoStateContext.Provider>;

  const [loading, error, data] = useFetch("https://jsonplaceholder.typicode.com/todos?_limit=5");

  if (loading){
    return <div>Loading...</div>;
  }

  if (error){
    return <div>Something went wrong.. Error message: {error?.message}</div>;
  }

  return <div>
    {data && data.map(item => {
      return <div>{item.title}</div>
    })}
  </div>
}

export default App;
