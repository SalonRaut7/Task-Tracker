import { Link, useParams } from "react-router-dom";
import { useApp } from "../context/AppContext";

export function ProjectDetailsPage() {
  const { id } = useParams();
  const { projects, projectsApiAvailable, projectsApiMessage } = useApp();

  const project = projects.find((item) => item.id === id);

  if (!projectsApiAvailable) {
    return (
      <div className="page-stack">
        <h1>Project Details</h1>
        <div className="card state-card warning-state">
          <h3>Project details are unavailable</h3>
          <p>
            {projectsApiMessage ||
              "The backend does not currently expose project endpoints."}
          </p>
        </div>
        <Link to="/projects">Back to projects</Link>
      </div>
    );
  }

  if (!project) {
    return (
      <div className="page-stack">
        <h1>Project not found</h1>
        <p className="page-subtitle">The project you requested does not exist.</p>
        <Link to="/projects">Back to projects</Link>
      </div>
    );
  }

  return (
    <div className="page-stack">
      <section>
        <Link to="/projects">Back to Projects</Link>
        <h1>{project.name}</h1>
        <p className="page-subtitle">{project.description || "No description provided."}</p>
      </section>

      <section className="dashboard-grid">
        <article className="card">
          <h2>Project Info</h2>
          <ul className="metric-list">
            <li>ID: {project.id}</li>
            <li>Key: {project.key ?? "-"}</li>
            <li>Status: {project.status ?? "-"}</li>
            <li>
              Created: {project.createdAt ? new Date(project.createdAt).toLocaleString() : "-"}
            </li>
            <li>
              Updated: {project.updatedAt ? new Date(project.updatedAt).toLocaleString() : "-"}
            </li>
          </ul>
        </article>

        <article className="card state-card">
          <h2>Task Mapping</h2>
          <p>
            The current tasks API does not include a project reference, so per-project
            task analytics cannot be calculated yet.
          </p>
        </article>
      </section>
    </div>
  );
}
