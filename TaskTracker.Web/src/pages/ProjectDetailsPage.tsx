import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import TextArea from "devextreme-react/text-area";
import TextBox from "devextreme-react/text-box";
import { useApp } from "../context/AppContext";
import { ApiError } from "../services/apiClient";
import { getProjectById } from "../services/projectService";
import { AppPermissions } from "../security/permissions";
import type { BackendProject } from "../types/app";
import { EpicsSection } from "./project-details/EpicsSection";
import { SprintsSection } from "./project-details/SprintsSection";

export function ProjectDetailsPage() {
  const { id } = useParams();
  const { hasPermission } = useApp();
  const canViewProjectMembers =
    !!id && hasPermission(AppPermissions.MembersView, "Project", id);

  const canCreateEpic = hasPermission(AppPermissions.EpicsCreate);
  const canUpdateEpic = hasPermission(AppPermissions.EpicsUpdate);
  const canDeleteEpic = hasPermission(AppPermissions.EpicsDelete);

  const canCreateSprint = hasPermission(AppPermissions.SprintsCreate);
  const canUpdateSprint = hasPermission(AppPermissions.SprintsManage);
  const canDeleteSprint = hasPermission(AppPermissions.SprintsManage);

  const [project, setProject] = useState<BackendProject | null>(null);
  const [loading, setLoading] = useState(false);
  const [requestError, setRequestError] = useState("");

  const loadProject = async (projectId: string) => {
    setLoading(true);
    setRequestError("");

    try {
      const projectResult = await getProjectById(projectId);

      if (!projectResult.available || !projectResult.item) {
        setProject(null);
        setRequestError(projectResult.message ?? "Project details are unavailable.");
        return;
      }

      setProject(projectResult.item);
    } catch (error) {
      if (error instanceof ApiError) setRequestError(error.message);
      else if (error instanceof Error) setRequestError(error.message);
      else setRequestError("Failed to load project details.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!id) return;
    void loadProject(id);
  }, [id]);

  if (!id) {
    return (
      <div className="page-stack">
        <h1>Project not found</h1>
        <p className="page-subtitle">Missing project id in route.</p>
        <Link to="/projects">Back to projects</Link>
      </div>
    );
  }

  if (!project) {
    return (
      <div className="page-stack">
        <h1>Project Details</h1>
        {loading ? <p className="page-inline-info">Loading project...</p> : null}
        {requestError ? <div className="form-error">{requestError}</div> : null}
        <Link to="/projects">Back to projects</Link>
      </div>
    );
  }

  return (
    <div className="page-stack">
      <section>
        <Link to="/projects">Back to Projects</Link>
        <h1>{project.name}</h1>
        <p className="page-subtitle">Project operations are authorization-scoped to the project and organization.</p>
        {canViewProjectMembers && (
          <p>
            <Link to={`/projects/${project.id}/members`}>
              Manage members and invitations
            </Link>
          </p>
        )}
      </section>

      {requestError && <div className="form-error">{requestError}</div>}
      {loading && <div className="page-inline-info">Refreshing project details...</div>}

      <section className="card">
        <h2>Project Overview</h2>
        <div className="popup-form">
          <label>
            Name
            <TextBox
              value={project.name}
              readOnly
            />
          </label>

          <label>
            Key
            <TextBox
              value={project.key ?? "-"}
              readOnly
            />
          </label>

          <label>
            Description
            <TextArea
              value={project.description ?? "No description provided."}
              minHeight={80}
              readOnly
            />
          </label>
        </div>
      </section>

      <EpicsSection
        projectId={project.id}
        canCreateEpic={canCreateEpic}
        canUpdateEpic={canUpdateEpic}
        canDeleteEpic={canDeleteEpic}
      />

      <SprintsSection
        projectId={project.id}
        canCreateSprint={canCreateSprint}
        canUpdateSprint={canUpdateSprint}
        canDeleteSprint={canDeleteSprint}
      />
    </div>
  );
}