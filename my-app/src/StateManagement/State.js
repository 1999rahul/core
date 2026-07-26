import React from "react";

// Anything which affects the UI should be stored in the state.
const todoState = {
    todos: [{
        "id": 1,
        "title": "quis ut nam facilis et officia qui",
        "completed": false
    },
    {
        "id": 2,
        "title": "fugiat veniam minus",
        "completed": true
    }],
    filter: 'all', // ToDo, InProgress, , Done, all
}

// Should we add Add Todo to the state?
// No - Because although it affects the UI and will be shown in the UI, other components do not care about this
// We need to re render the add todo component on every keystroke => global state update => all the component subscribed to the state re renders for no reason
// The action of adding a todo will be handledby the reducer function

function ToDoReducer(state, action) {
    // Actions => AddToDo, RemoveToDo, Toggle, SetFilter, default

    switch (action.type) {
        case "ADD_TO_DO":
            return {
                ...state,
                todos: [...state.todos,
                {
                    "id": Date.now(),
                    "title": action.payload,
                    "completed": false
                }]
            }
        case "REMOVE_TO_DO":
            return {
                ...state,
                todos: state.todos.filter(todo => todo.id != action.payload)
            }
        case "TOGGLE_TO_DO":
            return {
                ...state,
                todos: state.todos.map((todo) => todo.id === action.payload ? { ...todo, completed: !todo.completed } : todo)
            }
        // Only update the filter state, updating the todos will loose the data, you set the todos to active only => looses the done todos in the state
        // Creating an filtered todo's is not an good option => two sources to truth which needs to be updated  in ADD_TO_DO, REMOVE_TO_DO and so on ....
        case "SET_FILTER":
            return {
                ...state,
                filter: action.payload
            }
        default:
            return state;
    }
}

const ToDoStateContext = React.createContext(todoState);
const ToDoDispatchContext = React.createContext(ToDoReducer);

export { todoState, ToDoStateContext, ToDoReducer, ToDoDispatchContext };