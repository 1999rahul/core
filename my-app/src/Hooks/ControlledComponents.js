/**
 * ============================================================
 *   Controlled vs Uncontrolled Components — Complete Guide
 *   For React interviews and production usage
 * ============================================================
 *
 * TABLE OF CONTENTS
 * ----------------------------------------------------------
 *  1.  Core Concept — who owns the value?
 *  2.  Controlled Component — basic example
 *  3.  Uncontrolled Component — basic example
 *  4.  defaultValue vs value — the most confused distinction
 *  5.  The read-only trap — value without onChange
 *  6.  Switching between controlled/uncontrolled — common mistake
 *  7.  Complete controlled form — all input types
 *  8.  Complete uncontrolled form — all input types
 *  9.  Real-world example 1 — live validation (controlled only)
 *  10. Real-world example 2 — dependent fields (controlled only)
 *  11. Real-world example 3 — file input (always uncontrolled)
 *  12. Real-world example 4 — third-party library integration
 *  13. Real-world example 5 — instant field reset
 *  14. Real-world example 6 — dynamic form fields
 *  15. Side-by-side comparison table
 *  16. When to use which — decision guide
 *  17. Key rules & interview cheat sheet
 * ============================================================
 */


import { useState, useRef, useEffect } from 'react';


// ============================================================
// 1. CORE CONCEPT — who owns the value?
// ============================================================

/**
 * Every HTML form element (input, textarea, select, checkbox) has
 * internal state — the value the user has typed or selected.
 *
 * The key question in React is: WHO OWNS THAT STATE?
 *
 * CONTROLLED COMPONENT
 *   → React owns the value via state.
 *   → Value is passed via the "value" prop.
 *   → Every change goes through onChange → setState → re-render.
 *   → The DOM is just a REFLECTION of React state.
 *   → React always knows the current value.
 *
 * UNCONTROLLED COMPONENT
 *   → The DOM owns the value internally.
 *   → No "value" prop is provided.
 *   → React does not track keystrokes.
 *   → You read the value via a ref when you need it (e.g., on submit).
 *   → React has no idea what the user is typing between reads.
 *
 * DATA FLOW:
 *
 *   Controlled:
 *   User types → onChange fires → setState → re-render → input shows new state
 *   (React is always in the loop)
 *
 *   Uncontrolled:
 *   User types → DOM updates itself → (React is unaware)
 *   On submit → ref.current.value → React reads the DOM
 *   (React is only involved at read time)
 */


// ============================================================
// 2. CONTROLLED COMPONENT — basic example
// ============================================================

/**
 * The "value" prop hands ownership of the input's value to React.
 * React state becomes the single source of truth.
 * Every keystroke fires onChange, which updates state, which
 * causes a re-render, which updates the displayed value.
 *
 * This is a strict one-way data flow cycle:
 *   state → input display → user types → onChange → setState → state
 */

function ControlledInput() {
  const [name, setName] = useState(''); // React owns the value

  return (
    <div>
      <input
        type="text"
        value={name}                        // display what React state says
        onChange={(e) => setName(e.target.value)} // update state on every keystroke
        placeholder="Type your name"
      />
      {/* React always knows the current value — we can display it live */}
      <p>Live value: "{name}"</p>
      <p>Length: {name.length} characters</p>
    </div>
  );
}


// ============================================================
// 3. UNCONTROLLED COMPONENT — basic example
// ============================================================

/**
 * No "value" prop, no onChange, no state.
 * The DOM manages the input's value entirely on its own.
 * React is not involved until you explicitly read the value via ref.
 *
 * The input is silent during typing — no re-renders, no state updates.
 * You ask for the value only when you need it.
 */

function UncontrolledInput() {
  const nameRef = useRef(null); // pointer to the DOM node

  const handleSubmit = (e) => {
    e.preventDefault();
    // Read directly from the DOM node — only at this moment does React know the value
    const value = nameRef.current.value;
    console.log('Submitted value:', value);
  };

  return (
    <form onSubmit={handleSubmit}>
      {/* No value prop, no onChange — DOM owns this input's state */}
      <input
        type="text"
        ref={nameRef}
        placeholder="Type your name"
      />
      <button type="submit">Submit</button>
      {/* We CANNOT show live value here — React doesn't know what it is */}
    </form>
  );
}


// ============================================================
// 4. defaultValue vs value — THE MOST CONFUSED DISTINCTION
// ============================================================

/**
 * "value"        → CONTROLLED. React owns the value.
 *                  Input is always locked to what "value" says.
 *                  Must be paired with onChange to be editable.
 *
 * "defaultValue" → UNCONTROLLED. Sets the initial value ONCE on mount.
 *                  After that, the DOM takes over completely.
 *                  React never updates it again, even if the prop changes.
 *
 * Same distinction applies to other elements:
 *   <textarea value vs defaultValue>
 *   <select value vs defaultValue>
 *   <input type="checkbox" checked vs defaultChecked>
 *   <input type="radio" checked vs defaultChecked>
 */

function DefaultValueDemo() {
  const [reactValue, setReactValue] = useState('React owns this');
  const inputRef = useRef(null);

  return (
    <div>
      {/* CONTROLLED — React owns it, updates on every render */}
      <label>Controlled (value prop):</label>
      <input
        value={reactValue}
        onChange={(e) => setReactValue(e.target.value)}
      />

      {/* UNCONTROLLED — DOM owns it after first render */}
      {/* Even if you passed a new defaultValue prop later, it would be ignored */}
      <label>Uncontrolled (defaultValue prop):</label>
      <input
        defaultValue="DOM owns this after mount"
        ref={inputRef}
      />

      {/* CHECKBOX — controlled uses "checked", uncontrolled uses "defaultChecked" */}
      <label>Controlled checkbox:</label>
      <input type="checkbox" checked={true} onChange={() => {}} />

      <label>Uncontrolled checkbox:</label>
      <input type="checkbox" defaultChecked={true} ref={useRef(null)} />

      {/* SELECT — same pattern */}
      <label>Controlled select:</label>
      <select value="editor" onChange={() => {}}>
        <option value="viewer">Viewer</option>
        <option value="editor">Editor</option>
        <option value="admin">Admin</option>
      </select>

      <label>Uncontrolled select:</label>
      <select defaultValue="editor" ref={useRef(null)}>
        <option value="viewer">Viewer</option>
        <option value="editor">Editor</option>
        <option value="admin">Admin</option>
      </select>
    </div>
  );
}


// ============================================================
// 5. THE READ-ONLY TRAP — value without onChange
// ============================================================

/**
 * Providing "value" without "onChange" makes the input read-only.
 * React locks the displayed value to the state/prop value.
 * The user cannot type anything — every keystroke is immediately
 * overwritten by React re-rendering with the same value.
 *
 * React will warn in the console:
 * "You provided a `value` prop to a form field without an `onChange` handler.
 *  This will render a read-only field."
 *
 * This is intentional for display-only fields, but a common
 * beginner bug when you forget the onChange handler.
 */

function ReadOnlyTrap() {
  const [name] = useState('Alice'); // no setter — intentionally read-only

  return (
    <div>
      {/* ❌ ACCIDENTAL read-only — forgot onChange, user can't type */}
      {/* React warns about this */}
      <input value={name} />

      {/* ✅ INTENTIONAL read-only — use readOnly attribute instead */}
      {/* No React warning, clear intent */}
      <input value={name} onChange={() => {}} readOnly />

      {/* ✅ CORRECT controlled input — always pair value with onChange */}
      {/* This is what you almost always want */}
      {/* <input value={name} onChange={(e) => setName(e.target.value)} /> */}
    </div>
  );
}


// ============================================================
// 6. SWITCHING BETWEEN CONTROLLED/UNCONTROLLED — common mistake
// ============================================================

/**
 * React warns if a component switches between controlled and
 * uncontrolled during its lifetime. This happens when:
 *   - value starts as undefined (uncontrolled) and later becomes a string (controlled)
 *   - value starts as a string (controlled) and later becomes undefined (uncontrolled)
 *
 * The fix: always initialize state to an empty string, never undefined or null.
 *
 * React warning:
 * "A component is changing an uncontrolled input to be controlled."
 */

// ❌ WRONG — undefined initially makes it uncontrolled on first render
function BadControlled() {
  const [name, setName] = useState(); // undefined = uncontrolled on first render!

  return (
    <input
      value={name}              // undefined on first render → uncontrolled
      onChange={(e) => setName(e.target.value)} // then becomes controlled → React warns
    />
  );
}

// ❌ WRONG — null has the same problem as undefined
function AlsoBadControlled() {
  const [name, setName] = useState(null); // null = uncontrolled on first render!

  return (
    <input
      value={name}             // null on first render → uncontrolled
      onChange={(e) => setName(e.target.value)}
    />
  );
}

// ✅ CORRECT — empty string is a valid controlled value from the start
function GoodControlled() {
  const [name, setName] = useState(''); // '' = controlled from the very first render

  return (
    <input
      value={name}
      onChange={(e) => setName(e.target.value)}
    />
  );
}

// ✅ CORRECT — same for numbers, use 0 not undefined
function GoodNumberInput() {
  const [age, setAge] = useState(0); // 0 = controlled from start

  return (
    <input
      type="number"
      value={age}
      onChange={(e) => setAge(Number(e.target.value))}
    />
  );
}


// ============================================================
// 7. COMPLETE CONTROLLED FORM — all input types
// ============================================================

/**
 * A production-style controlled form handling all common input types:
 * text, email, password, number, select, checkbox, radio, textarea.
 *
 * One generic onChange handler covers all of them using the "name"
 * attribute as the key and checking "type" for checkboxes.
 *
 * KEY BENEFITS DEMONSTRATED:
 * - Real-time validation on every keystroke
 * - Conditional submit button (disabled until form is valid)
 * - Instant reset with setState
 * - Full visibility into form state at every moment
 */

const INITIAL_STATE = {
  username: '',
  email: '',
  password: '',
  age: '',
  role: 'viewer',
  newsletter: false,
  gender: '',
  bio: '',
};

function ControlledForm() {
  const [formData, setFormData] = useState(INITIAL_STATE);
  const [errors, setErrors] = useState({});
  const [submitted, setSubmitted] = useState(false);

  // One handler for ALL input types
  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      // Checkboxes use "checked", everything else uses "value"
      [name]: type === 'checkbox' ? checked : value,
    }));

    // Clear the error for this field as user types
    if (errors[name]) {
      setErrors((prev) => ({ ...prev, [name]: '' }));
    }
  };

  const validate = () => {
    const newErrors = {};
    if (!formData.username.trim()) newErrors.username = 'Username is required';
    if (formData.username.length < 3) newErrors.username = 'Minimum 3 characters';
    if (!formData.email.includes('@')) newErrors.email = 'Invalid email address';
    if (formData.password.length < 8) newErrors.password = 'Minimum 8 characters';
    if (!formData.age || Number(formData.age) < 18) newErrors.age = 'Must be 18 or older';
    if (!formData.gender) newErrors.gender = 'Please select a gender';
    return newErrors;
  };

  // isValid computed from state — only possible because React owns the data
  const isValid = Object.keys(validate()).length === 0;

  const handleSubmit = (e) => {
    e.preventDefault();
    const validationErrors = validate();

    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    console.log('Form submitted:', formData);
    setSubmitted(true);
  };

  // Instant reset — just restore initial state. Only possible with controlled.
  const handleReset = () => {
    setFormData(INITIAL_STATE);
    setErrors({});
    setSubmitted(false);
  };

  if (submitted) {
    return (
      <div>
        <p>Submitted successfully!</p>
        <pre>{JSON.stringify(formData, null, 2)}</pre>
        <button onClick={handleReset}>Reset form</button>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit}>

      {/* Text input */}
      <div>
        <input
          name="username"
          type="text"
          value={formData.username}
          onChange={handleChange}
          placeholder="Username"
        />
        {errors.username && <span className="error">{errors.username}</span>}
      </div>

      {/* Email input */}
      <div>
        <input
          name="email"
          type="email"
          value={formData.email}
          onChange={handleChange}
          placeholder="Email"
        />
        {errors.email && <span className="error">{errors.email}</span>}
      </div>

      {/* Password input */}
      <div>
        <input
          name="password"
          type="password"
          value={formData.password}
          onChange={handleChange}
          placeholder="Password (min 8 chars)"
        />
        {errors.password && <span className="error">{errors.password}</span>}
      </div>

      {/* Number input */}
      <div>
        <input
          name="age"
          type="number"
          value={formData.age}
          onChange={handleChange}
          placeholder="Age"
          min="0"
          max="120"
        />
        {errors.age && <span className="error">{errors.age}</span>}
      </div>

      {/* Select dropdown */}
      <div>
        <select name="role" value={formData.role} onChange={handleChange}>
          <option value="viewer">Viewer</option>
          <option value="editor">Editor</option>
          <option value="admin">Admin</option>
        </select>
      </div>

      {/* Radio buttons — each has same "name", different "value" */}
      <div>
        <label>
          <input
            type="radio"
            name="gender"
            value="male"
            checked={formData.gender === 'male'}
            onChange={handleChange}
          />
          Male
        </label>
        <label>
          <input
            type="radio"
            name="gender"
            value="female"
            checked={formData.gender === 'female'}
            onChange={handleChange}
          />
          Female
        </label>
        <label>
          <input
            type="radio"
            name="gender"
            value="other"
            checked={formData.gender === 'other'}
            onChange={handleChange}
          />
          Other
        </label>
        {errors.gender && <span className="error">{errors.gender}</span>}
      </div>

      {/* Checkbox — uses "checked" not "value" */}
      <div>
        <label>
          <input
            type="checkbox"
            name="newsletter"
            checked={formData.newsletter}
            onChange={handleChange}
          />
          Subscribe to newsletter
        </label>
      </div>

      {/* Textarea */}
      <div>
        <textarea
          name="bio"
          value={formData.bio}
          onChange={handleChange}
          placeholder="Tell us about yourself (optional)"
          rows={4}
        />
        {/* Live character count — only possible because React owns the value */}
        <small>{formData.bio.length} / 500 characters</small>
      </div>

      {/* Submit button disabled until form is valid — only possible with controlled */}
      <button type="submit" disabled={!isValid}>
        Register
      </button>
      <button type="button" onClick={handleReset}>
        Reset
      </button>

    </form>
  );
}


// ============================================================
// 8. COMPLETE UNCONTROLLED FORM — all input types
// ============================================================

/**
 * The same form built with uncontrolled inputs.
 * Notice: no state, no onChange, no re-renders during typing.
 * All values are read from the DOM only on submit.
 *
 * LIMITATIONS VISIBLE HERE:
 * - No live validation during typing
 * - No character count on textarea
 * - No conditional submit button (we don't know values until submit)
 * - Reset requires manually setting ref.current.value for each field
 */

function UncontrolledForm() {
  const usernameRef = useRef(null);
  const emailRef    = useRef(null);
  const passwordRef = useRef(null);
  const ageRef      = useRef(null);
  const roleRef     = useRef(null);
  const newsletterRef = useRef(null);
  const bioRef      = useRef(null);
  // Radio buttons — need to query DOM since multiple share a name
  const formRef     = useRef(null);

  const handleSubmit = (e) => {
    e.preventDefault();

    // Read all values at once from the DOM
    const selectedGender = formRef.current.querySelector(
      'input[name="gender"]:checked'
    );

    const data = {
      username:   usernameRef.current.value,
      email:      emailRef.current.value,
      password:   passwordRef.current.value,
      age:        ageRef.current.value,
      role:       roleRef.current.value,
      newsletter: newsletterRef.current.checked, // checkboxes use .checked
      gender:     selectedGender ? selectedGender.value : '',
      bio:        bioRef.current.value,
    };

    // Validation only at submit time — no live feedback possible
    if (!data.username) {
      alert('Username is required');
      usernameRef.current.focus();
      return;
    }

    console.log('Form submitted:', data);
  };

  // Reset requires manually clearing each ref — verbose and error-prone
  const handleReset = () => {
    usernameRef.current.value = '';
    emailRef.current.value    = '';
    passwordRef.current.value = '';
    ageRef.current.value      = '';
    roleRef.current.value     = 'viewer';
    newsletterRef.current.checked = false;
    bioRef.current.value      = '';
    // Radio buttons need to be unchecked individually
    formRef.current.querySelectorAll('input[name="gender"]').forEach((r) => {
      r.checked = false;
    });
  };

  return (
    <form ref={formRef} onSubmit={handleSubmit}>

      <input ref={usernameRef} type="text"     defaultValue="" placeholder="Username" />
      <input ref={emailRef}    type="email"    defaultValue="" placeholder="Email" />
      <input ref={passwordRef} type="password" defaultValue="" placeholder="Password" />
      <input ref={ageRef}      type="number"   defaultValue="" placeholder="Age" min="0" />

      <select ref={roleRef} defaultValue="viewer">
        <option value="viewer">Viewer</option>
        <option value="editor">Editor</option>
        <option value="admin">Admin</option>
      </select>

      <label>
        <input type="radio" name="gender" value="male" defaultChecked={false} />
        Male
      </label>
      <label>
        <input type="radio" name="gender" value="female" />
        Female
      </label>
      <label>
        <input type="radio" name="gender" value="other" />
        Other
      </label>

      <label>
        <input ref={newsletterRef} type="checkbox" defaultChecked={false} />
        Subscribe to newsletter
      </label>

      <textarea ref={bioRef} defaultValue="" placeholder="Bio" rows={4} />
      {/* Cannot show live character count here — we don't know the value */}

      {/* Cannot disable this button based on validity — we don't know the values */}
      <button type="submit">Register</button>
      <button type="button" onClick={handleReset}>Reset</button>

    </form>
  );
}


// ============================================================
// 9. REAL-WORLD EXAMPLE 1 — live validation (controlled only)
// ============================================================

/**
 * CONTEXT: Password field that shows strength as the user types.
 * This is ONLY possible with controlled components because React
 * knows the current value on every single keystroke.
 *
 * With uncontrolled, you would have no value between renders —
 * you could only validate at submit time.
 */

function PasswordStrengthInput() {
  const [password, setPassword] = useState('');

  const getStrength = (pwd) => {
    if (pwd.length === 0) return { label: '', color: 'transparent', score: 0 };
    if (pwd.length < 6)   return { label: 'Too short', color: 'red', score: 1 };

    let score = 0;
    if (pwd.length >= 8)         score++; // length
    if (/[A-Z]/.test(pwd))       score++; // uppercase
    if (/[0-9]/.test(pwd))       score++; // number
    if (/[^A-Za-z0-9]/.test(pwd)) score++; // special char

    if (score <= 1) return { label: 'Weak',   color: 'orange', score };
    if (score <= 2) return { label: 'Fair',   color: 'yellow', score };
    if (score <= 3) return { label: 'Good',   color: 'lightgreen', score };
    return           { label: 'Strong', color: 'green', score };
  };

  const strength = getStrength(password);

  return (
    <div>
      <input
        type="password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        placeholder="Enter password"
      />

      {/* Live strength indicator — updates on every keystroke */}
      {password.length > 0 && (
        <div>
          <div style={{
            width: `${(strength.score / 4) * 100}%`,
            height: 4,
            background: strength.color,
            transition: 'all 0.3s',
          }} />
          <small style={{ color: strength.color }}>{strength.label}</small>
        </div>
      )}

      {/* Live requirements checklist */}
      <ul style={{ fontSize: 12 }}>
        <li style={{ color: password.length >= 8 ? 'green' : 'red' }}>
          {password.length >= 8 ? '✓' : '✗'} At least 8 characters
        </li>
        <li style={{ color: /[A-Z]/.test(password) ? 'green' : 'red' }}>
          {/[A-Z]/.test(password) ? '✓' : '✗'} One uppercase letter
        </li>
        <li style={{ color: /[0-9]/.test(password) ? 'green' : 'red' }}>
          {/[0-9]/.test(password) ? '✓' : '✗'} One number
        </li>
        <li style={{ color: /[^A-Za-z0-9]/.test(password) ? 'green' : 'red' }}>
          {/[^A-Za-z0-9]/.test(password) ? '✓' : '✗'} One special character
        </li>
      </ul>
    </div>
  );
}


// ============================================================
// 10. REAL-WORLD EXAMPLE 2 — dependent fields (controlled only)
// ============================================================

/**
 * CONTEXT: Country → State → City cascading dropdowns.
 * When the user changes Country, the State options update.
 * When they change State, the City options update.
 *
 * This cascading dependency is ONLY possible with controlled
 * components because React knows the selected values at all times
 * and can derive the options for the next field from them.
 */

const LOCATION_DATA = {
  India: {
    Jharkhand: ['Ranchi', 'Jamshedpur', 'Dhanbad'],
    Maharashtra: ['Mumbai', 'Pune', 'Nagpur'],
  },
  USA: {
    California: ['Los Angeles', 'San Francisco', 'San Diego'],
    Texas: ['Houston', 'Austin', 'Dallas'],
  },
};

function CascadingDropdowns() {
  const [country, setCountry] = useState('');
  const [state, setState]     = useState('');
  const [city, setCity]       = useState('');

  // Derived options — computed from current selections
  const states = country ? Object.keys(LOCATION_DATA[country] || {}) : [];
  const cities = (country && state)
    ? LOCATION_DATA[country][state] || []
    : [];

  const handleCountryChange = (e) => {
    setCountry(e.target.value);
    setState(''); // reset dependent fields
    setCity('');
  };

  const handleStateChange = (e) => {
    setState(e.target.value);
    setCity(''); // reset dependent field
  };

  return (
    <div>
      {/* Country */}
      <select value={country} onChange={handleCountryChange}>
        <option value="">Select country</option>
        {Object.keys(LOCATION_DATA).map((c) => (
          <option key={c} value={c}>{c}</option>
        ))}
      </select>

      {/* State — disabled until country is selected */}
      <select value={state} onChange={handleStateChange} disabled={!country}>
        <option value="">Select state</option>
        {states.map((s) => (
          <option key={s} value={s}>{s}</option>
        ))}
      </select>

      {/* City — disabled until state is selected */}
      <select
        value={city}
        onChange={(e) => setCity(e.target.value)}
        disabled={!state}
      >
        <option value="">Select city</option>
        {cities.map((c) => (
          <option key={c} value={c}>{c}</option>
        ))}
      </select>

      {/* Live summary — only possible with controlled */}
      {city && <p>Selected: {city}, {state}, {country}</p>}
    </div>
  );
}


// ============================================================
// 11. REAL-WORLD EXAMPLE 3 — file input (always uncontrolled)
// ============================================================

/**
 * CONTEXT: File upload component.
 *
 * File inputs are a SPECIAL CASE — they are ALWAYS uncontrolled.
 * Browsers do not allow JavaScript to programmatically set the value
 * of a file input for security reasons. You cannot do:
 *   fileInput.value = 'something'; // ❌ browser blocks this
 *
 * React cannot control file inputs. You must always use a ref
 * and read from ref.current.files (a FileList object, not .value).
 *
 * This is the one case where uncontrolled is NOT a choice — it is forced.
 */

function FileUploader() {
  const fileRef    = useRef(null);
  const [preview, setPreview] = useState(null);
  const [uploading, setUploading] = useState(false);
  const [fileName, setFileName]   = useRef('');

  // Show a preview when the user selects an image
  const handleFileChange = (e) => {
    const file = e.target.files[0];
    if (!file) return;

    // We CAN use onChange to react to file selection
    // We just can't control the input's value
    setFileName(file.name);

    if (file.type.startsWith('image/')) {
      const reader = new FileReader();
      reader.onload = (ev) => setPreview(ev.target.result);
      reader.readAsDataURL(file);
    }
  };

  const handleUpload = async () => {
    // Read from ref.current.files — a FileList, not .value
    const file = fileRef.current.files[0];
    if (!file) {
      alert('Please select a file first');
      return;
    }

    setUploading(true);
    try {
      const formData = new FormData();
      formData.append('file', file);

      await fetch('/api/upload', { method: 'POST', body: formData });
      alert('Upload successful!');
    } catch (err) {
      alert('Upload failed');
    } finally {
      setUploading(false);
    }
  };

  const handleClear = () => {
    // The one way to reset a file input — set value to empty string
    fileRef.current.value = '';
    setPreview(null);
  };

  return (
    <div>
      {/* type="file" is always uncontrolled — never use value= here */}
      <input
        type="file"
        ref={fileRef}
        onChange={handleFileChange}
        accept=".jpg,.jpeg,.png,.pdf"
      />

      {preview && (
        <img
          src={preview}
          alt="Preview"
          style={{ width: 200, height: 200, objectFit: 'cover' }}
        />
      )}

      <button onClick={handleUpload} disabled={uploading}>
        {uploading ? 'Uploading...' : 'Upload'}
      </button>
      <button onClick={handleClear}>Clear</button>
    </div>
  );
}


// ============================================================
// 12. REAL-WORLD EXAMPLE 4 — third-party library integration
// ============================================================

/**
 * CONTEXT: Integrating a rich text editor (like Quill, TipTap, CodeMirror).
 * These libraries take over a DOM node and manage it entirely.
 *
 * WHY UNCONTROLLED HERE:
 * The library manages its own internal state and DOM mutations.
 * If you tried to control the editor's content with React state,
 * you would fight against the library — causing double updates,
 * cursor jumps, and performance issues.
 *
 * The correct pattern:
 *   - Give the library a DOM node via ref
 *   - Let the library own the DOM node
 *   - Listen to the library's onChange callback to extract the value
 *   - Only store the value in React state when you actually need it
 */

function RichTextEditor({ initialContent, onContentChange }) {
  const editorRef     = useRef(null);
  const editorInstance = useRef(null); // store the library instance, not in state

  useEffect(() => {
    // Third-party library takes over the DOM node completely
    // This is a hypothetical API — real editors like Quill work similarly
    editorInstance.current = initializeRichTextEditor(editorRef.current, {
      initialContent,
      onChange: (htmlContent) => {
        // Only now does React learn about content changes
        onContentChange(htmlContent);
      },
      toolbar: ['bold', 'italic', 'link', 'image'],
    });

    // Cleanup: destroy the library instance on unmount
    return () => {
      editorInstance.current?.destroy();
    };
  }, []); // runs once — we don't re-initialize on every render

  // Expose imperative methods to parent if needed (via forwardRef + useImperativeHandle)
  const clearEditor = () => {
    editorInstance.current?.setContent('');
  };

  return (
    <div>
      {/*
        React renders this div once on mount.
        After that, the third-party library owns everything inside it.
        We never touch ref.current's children ourselves.
      */}
      <div
        ref={editorRef}
        className="editor-container"
        style={{ minHeight: 200, border: '1px solid #ccc' }}
      />
      <button onClick={clearEditor}>Clear editor</button>
    </div>
  );
}

// Placeholder for the hypothetical third-party library initializer
function initializeRichTextEditor(domNode, options) {
  console.log('Editor initialized on', domNode);
  // Real implementation would be: new Quill(domNode, options) etc.
  return {
    setContent: (content) => { domNode.innerHTML = content; },
    getContent: () => domNode.innerHTML,
    destroy: () => { domNode.innerHTML = ''; },
  };
}


// ============================================================
// 13. REAL-WORLD EXAMPLE 5 — instant field reset
// ============================================================

/**
 * CONTEXT: A search bar with a clear button.
 *
 * Controlled: reset is one setState call. Instant, clean.
 * Uncontrolled: reset requires ref.current.value = '' AND
 *               manually re-focusing and potentially more DOM manipulation.
 *
 * This illustrates why controlled is preferred for interactive forms.
 */

// ✅ Controlled — simple and clean
function SearchBarControlled() {
  const [query, setQuery] = useState('');

  const handleClear = () => {
    setQuery('');  // one line — instantly resets everything
    // If we had errors, filters, suggestions — one setState object resets all of them
  };

  return (
    <div style={{ display: 'flex' }}>
      <input
        type="search"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Search..."
      />
      {query && (
        <button onClick={handleClear}>✕</button>
      )}
      {/* We can conditionally render the button because we know the value */}
      <p>Searching for: "{query}"</p>
    </div>
  );
}

// Uncontrolled — more verbose, less elegant
function SearchBarUncontrolled() {
  const inputRef = useRef(null);
  const [hasValue, setHasValue] = useState(false); // need extra state just to show/hide button

  const handleChange = (e) => {
    // Need this extra handler just to track whether input has a value
    setHasValue(e.target.value.length > 0);
  };

  const handleClear = () => {
    inputRef.current.value = ''; // manually clear the DOM
    inputRef.current.focus();    // manually restore focus
    setHasValue(false);          // manually update the extra state
  };

  return (
    <div style={{ display: 'flex' }}>
      <input
        type="search"
        ref={inputRef}
        onChange={handleChange}
        placeholder="Search..."
      />
      {hasValue && (
        <button onClick={handleClear}>✕</button>
      )}
    </div>
  );
}


// ============================================================
// 14. REAL-WORLD EXAMPLE 6 — dynamic form fields
// ============================================================

/**
 * CONTEXT: A form where the user can add/remove fields dynamically.
 * e.g., adding multiple phone numbers, addresses, or team members.
 *
 * Controlled makes this straightforward — the list of fields IS the state.
 * Adding a field = adding an item to the array.
 * Removing a field = filtering it out.
 * All values are always available in React state.
 */

function DynamicPhoneFields() {
  const [phones, setPhones] = useState([
    { id: 1, value: '', label: 'Mobile' }
  ]);

  const addPhone = () => {
    setPhones((prev) => [
      ...prev,
      { id: Date.now(), value: '', label: 'Mobile' }
    ]);
  };

  const removePhone = (id) => {
    setPhones((prev) => prev.filter((p) => p.id !== id));
  };

  const updatePhone = (id, field, value) => {
    setPhones((prev) =>
      prev.map((p) => (p.id === id ? { ...p, [field]: value } : p))
    );
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    // All values immediately available — no DOM reading needed
    console.log('Phone numbers:', phones);
  };

  return (
    <form onSubmit={handleSubmit}>
      {phones.map((phone, index) => (
        <div key={phone.id} style={{ display: 'flex', gap: 8, marginBottom: 8 }}>
          <select
            value={phone.label}
            onChange={(e) => updatePhone(phone.id, 'label', e.target.value)}
          >
            <option value="Mobile">Mobile</option>
            <option value="Home">Home</option>
            <option value="Work">Work</option>
          </select>

          <input
            type="tel"
            value={phone.value}
            onChange={(e) => updatePhone(phone.id, 'value', e.target.value)}
            placeholder={`Phone ${index + 1}`}
          />

          {/* Don't allow removing the last field */}
          {phones.length > 1 && (
            <button type="button" onClick={() => removePhone(phone.id)}>
              Remove
            </button>
          )}
        </div>
      ))}

      <button type="button" onClick={addPhone}>+ Add phone</button>
      <button type="submit">Save</button>
    </form>
  );
}


// ============================================================
// 15. SIDE-BY-SIDE COMPARISON
// ============================================================

/**
 *
 * Feature                      Controlled              Uncontrolled
 * ─────────────────────────────────────────────────────────────────
 * Who owns the value?          React state             The DOM
 * How to read the value?       Directly from state     ref.current.value
 * Key prop used                value={}                defaultValue={}
 * Checkbox prop                checked={}              defaultChecked={}
 * Re-renders on each key?      Yes                     No
 * Live validation              ✅ Yes                  ❌ Submit only
 * Real-time character count    ✅ Yes                  ❌ No
 * Conditional submit button    ✅ Yes                  ❌ Difficult
 * Dependent/cascading fields   ✅ Easy                 ❌ Complex
 * Instant field reset          ✅ One setState call     ❌ Manual per ref
 * Dynamic add/remove fields    ✅ Easy (array in state) ❌ Complex
 * Works with file inputs       ❌ No (browser blocks)  ✅ Always
 * Third-party library integration ❌ Fights the library ✅ Easy
 * Code verbosity               More (value + onChange)  Less
 * React's recommendation       ✅ Preferred            ⚠ When necessary
 *
 *
 * CONTROLLED — USE WHEN:
 *   ✅ You need live validation or instant feedback
 *   ✅ Fields depend on each other (cascading dropdowns)
 *   ✅ You need to reset, transform, or format the value
 *   ✅ Submit button should be conditionally disabled
 *   ✅ Dynamic fields (add/remove rows)
 *   ✅ Almost all production forms
 *
 * UNCONTROLLED — USE WHEN:
 *   ✅ File inputs (no choice — always uncontrolled)
 *   ✅ Integrating third-party DOM libraries (rich text editors, maps)
 *   ✅ Very simple one-off forms where values only matter at submit
 *   ✅ Migrating legacy non-React code incrementally
 *   ✅ Performance-critical forms with hundreds of fields
 *      (avoiding re-renders on every keystroke)
 */


// ============================================================
// 16. WHEN TO USE WHICH — decision guide
// ============================================================

/**
 * START HERE: Do you need to know the value BEFORE the user submits?
 *
 *   YES → Use CONTROLLED
 *   (live validation, character count, conditional logic,
 *    dependent fields, instant reset, dynamic fields)
 *
 *   NO → Ask: Is it a file input or third-party library integration?
 *
 *     YES → Use UNCONTROLLED (no choice for files; correct for libraries)
 *     NO  → Either works. Controlled is still preferred for consistency.
 *
 *
 * QUICK DECISION TABLE:
 *
 *   Need live validation?           → Controlled
 *   Need to reset programmatically? → Controlled
 *   Fields depend on each other?    → Controlled
 *   Disable submit until valid?     → Controlled
 *   Dynamic add/remove fields?      → Controlled
 *   File input?                     → Uncontrolled (forced)
 *   Third-party editor/map/picker?  → Uncontrolled
 *   Simple form, values at submit?  → Either (controlled preferred)
 */


// ============================================================
// 17. KEY RULES & INTERVIEW CHEAT SHEET
// ============================================================

/**
 * THE FUNDAMENTAL DIFFERENCE:
 *   Controlled  → React is the single source of truth. DOM reflects state.
 *   Uncontrolled → DOM is the source of truth. React reads it when asked.
 *
 * MOST COMMON INTERVIEW FOLLOW-UP QUESTIONS:
 *
 *   Q: What happens if you pass "value" without "onChange"?
 *   A: The input becomes read-only. React locks it to the state value.
 *      User cannot type. React logs a warning in the console.
 *      Fix: add onChange, or use readOnly attribute intentionally.
 *
 *   Q: What is defaultValue?
 *   A: Sets the initial value for an uncontrolled input — applied once
 *      on mount. The DOM takes over after that. React never updates it
 *      again, even if the prop changes on re-render.
 *
 *   Q: Can you mix controlled and uncontrolled in the same form?
 *   A: Yes. Each input independently decides its type. A form can have
 *      some controlled inputs (text, select) and one uncontrolled input
 *      (file). But mixing is generally confusing and should be avoided.
 *
 *   Q: Why does React warn "changing uncontrolled to controlled"?
 *   A: When value starts as undefined (uncontrolled) and later becomes
 *      a string (controlled), React detects the switch and warns.
 *      Fix: always initialize state to '' (empty string), never undefined.
 *
 *   Q: Which does React recommend?
 *   A: Controlled components. They give predictable data flow, easier
 *      debugging, and enable features impossible with uncontrolled inputs.
 *      Uncontrolled is for specific cases: file inputs and third-party libraries.
 *
 *   Q: Can you validate an uncontrolled form?
 *   A: Only at submit time. You read values via refs and validate then.
 *      You cannot validate on each keystroke without adding onChange,
 *      which at that point is essentially a controlled component anyway.
 *
 *   Q: How do you reset an uncontrolled form?
 *   A: Manually set ref.current.value = '' for each field. For checkboxes,
 *      ref.current.checked = false. Much more verbose than controlled reset.
 *      Alternatively, change the component's key to force a remount.
 *
 *   Q: Are file inputs ever controlled?
 *   A: No. Browsers block programmatic setting of file input values for
 *      security. File inputs are always uncontrolled. Use ref.current.files
 *      (a FileList object) to access the selected files.
 */


// ============================================================
// EXPORTS
// ============================================================

export {
  ControlledInput,          // Section 2  — basic controlled input
  UncontrolledInput,        // Section 3  — basic uncontrolled input
  DefaultValueDemo,         // Section 4  — value vs defaultValue
  ReadOnlyTrap,             // Section 5  — value without onChange
  GoodControlled,           // Section 6  — correct initialization
  ControlledForm,           // Section 7  — complete controlled form
  UncontrolledForm,         // Section 8  — complete uncontrolled form
  PasswordStrengthInput,    // Section 9  — live validation
  CascadingDropdowns,       // Section 10 — dependent fields
  FileUploader,             // Section 11 — file input (always uncontrolled)
  RichTextEditor,           // Section 12 — third-party library
  SearchBarControlled,      // Section 13 — instant reset (controlled)
  SearchBarUncontrolled,    // Section 13 — instant reset (uncontrolled)
  DynamicPhoneFields,       // Section 14 — dynamic form fields
};