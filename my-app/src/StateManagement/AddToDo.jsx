import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import { ToDoDispatchContext } from "./State";
import { useContext, useState } from "react";

function AddToDo() {
    const toDoDispatch = useContext(ToDoDispatchContext);
    const [toDoTitle, setToDoTitle] = useState("");

    function handleAddToDo() {
        if (toDoTitle.trim() === "" || toDoTitle === null || toDoTitle === undefined) {
            alert("Please enter a title for the to-do item.");
            return;
        }

        toDoDispatch({ type: "ADD_TO_DO", payload: toDoTitle });
        setToDoTitle(""); // Clear the input field after adding the to-do item
    }

    return <Stack direction="row" spacing={2}>
        <TextField id="outlined-basic" label="Outlined" variant="outlined" value={toDoTitle} onChange={(e) => { setToDoTitle(e.target.value) }} />
        <Button variant="contained" onClick={handleAddToDo}>Add To Do</Button>
    </Stack>
}

export default AddToDo;