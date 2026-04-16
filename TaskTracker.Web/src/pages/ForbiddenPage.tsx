import { Link, useLocation } from "react-router-dom";

type ForbiddenState = {
  from?: string;
};

export function ForbiddenPage() {
  const location = useLocation();
  const state = location.state as ForbiddenState | null;

  return (
    <div className="page-stack">
      <section className="card state-card forbidden-state">
        <h1>Access denied</h1>
        <p className="page-subtitle">
          You do not have permission to access this area.
        </p>
        {state?.from ? (
          <p>
            Attempted route: <strong>{state.from}</strong>
          </p>
        ) : null}
        <div className="forbidden-actions">
          <Link to="/dashboard">Go to dashboard</Link>
          <Link to="/tasks">Go to tasks</Link>
        </div>
      </section>
    </div>
  );
}
