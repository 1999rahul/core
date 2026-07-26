import ToDos from "./ToDos";
import AddToDo from "./AddToDo";
import Container from "@mui/material/Container";

function ToDoApp() {
    return <Container maxWidth="md">
        <AddToDo></AddToDo>
        <ToDos />
    </Container>

}

export default ToDoApp;