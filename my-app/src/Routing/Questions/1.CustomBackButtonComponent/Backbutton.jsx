import { useNavigate, useLocation } from "react-router-dom";

export default function BackButton({ fallback = "/" }) {
  const navigate = useNavigate();
  const location = useLocation();
  const hasHistory = location.key !== "default";

  return (
    <button
      onClick={() => hasHistory ? navigate(-1) : navigate(fallback, { replace: true })}
      disabled={!hasHistory}
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 6,
        padding: "5px 12px",
        fontSize: 13,
        cursor: hasHistory ? "pointer" : "not-allowed",
        opacity: hasHistory ? 1 : 0.4,
        background: "transparent",
        border: "1px solid currentColor",
        borderRadius: 6,
        color: "inherit",
      }}
    >
      ← Back
    </button>
  );
}