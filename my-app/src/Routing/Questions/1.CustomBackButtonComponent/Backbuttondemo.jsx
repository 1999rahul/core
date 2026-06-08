import { MemoryRouter, Routes, Route, Link, useLocation } from "react-router-dom";
import BackButton from "./BackButton";

function Home() {
  return (
    <div>
      <BackButton />
      <h2>Home</h2>
      <Link to="/about">Go to About</Link>
    </div>
  );
}

function About() {
  return (
    <div>
      <BackButton />
      <h2>About</h2>
      <Link to="/contact">Go to Contact</Link>
    </div>
  );
}

function Contact() {
  return (
    <div>
      <BackButton/>
      <h2>Contact</h2>
    </div>
  );
}

export default function BackButtonDemo() {
  return (
    <MemoryRouter>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/about" element={<About />} />
        <Route path="/contact" element={<Contact />} />
      </Routes>
    </MemoryRouter>
  );
}