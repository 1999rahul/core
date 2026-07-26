import React, { useContext } from "react";
import { ToDoStateContext } from "./State";
import TableContainer from '@mui/material/TableContainer';
import Paper from '@mui/material/Paper';
import TableHead from "@mui/material/TableHead";
import Button from "@mui/material/Button";
import Table from "@mui/material/Table";
import TableRow from "@mui/material/TableRow";
import TableCell from "@mui/material/TableCell";
import TableBody from "@mui/material/TableBody";

function ToDos() {
    const { todos } = useContext(ToDoStateContext);
    console.log("ToDos: ", todos);
    return <TableContainer component={Paper}>
        <Table sx={{ minWidth: 650 }} aria-label="simple table">
            <TableHead>
                <TableRow>
                    <TableCell>Title</TableCell>
                    <TableCell align="right">Status</TableCell>
                    <TableCell align="right">Toggle</TableCell>
                </TableRow>
            </TableHead>
            <TableBody>
                {todos.map((row) => (
                    <TableRow
                        key={row.id}
                        sx={{ '&:last-child td, &:last-child th': { border: 0 } }}
                    >
                        <TableCell component="th" scope="row">{row.title}</TableCell>
                        <TableCell align="right">{row.completed ? "Done" : "In Progress"}</TableCell>
                        <TableCell align="right">
                            <Button>
                                Toggle
                            </Button>
                        </TableCell>
                    </TableRow>
                ))}
            </TableBody>
        </Table>
    </TableContainer>
}

export default ToDos;