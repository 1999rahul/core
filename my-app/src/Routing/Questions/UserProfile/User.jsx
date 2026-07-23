import { BrowserRouter, Routes, useParams, Route } from "react-router-dom";
import { useState, useEffect } from "react";

function User() {
    const [user, setUser] = useState(null);
    const { id } = useParams();

    useEffect(() => {
        // Simulate fetching user data based on the ID from the URL
        async function fetchUser(id){
            setTimeout(() => {
                setUser({
                    id,
                    name: `User ${id}`,
                    email: `user${id}@example.com`,
                    role: `User ${id} Role`
                })
            }, 1000)
        }

        fetchUser(id)
    }, [id]);

    if (!user) {
        return <div>Loading...</div>;
    }

    return <div>
        <h1>Name: {user.name.data.data}</h1>
        <p>Email: {user.email}</p>
        <p>Role: {user.role}</p>
    </div>
}

export default function UserDemo() {
    return <BrowserRouter>
        <Routes>
            <Route path="/users/:id" element={<User />} />
            <Route path="*" element={<div>Please navigate to /users/:id (e.g., /users/1)</div>} />
        </Routes>
    </BrowserRouter>
}