import { useMemo, useState } from "react";
import { Button } from "devextreme-react/button";
import DataGrid, { Column, Paging } from "devextreme-react/data-grid";
import SelectBox from "devextreme-react/select-box";
import TextArea from "devextreme-react/text-area";
import TextBox from "devextreme-react/text-box";
import { Modal } from "../components/Modal";
import { useNavigate } from "react-router-dom";
import { useApp } from "../context/AppContext";
import { ApiError } from "../services/apiClient";
import { getOrganizations } from "../services/organizationService";
import { createProject, deleteProject, updateProject } from "../services/projectService";
import { AppPermissions } from "../security/permissions";
import type { BackendOrganization, BackendProject } from "../types/app";

type ProjectCreateForm = {
  organizationId: string;
  name: string;
  key: string;
  description: string;
};

type ProjectPopupMode = "view" | "edit" | null;

const emptyCreateForm: ProjectCreateForm = {
  organizationId: "",
  name: "",
  key: "",
  description: "",
};

export function ProjectsPage() {
  const navigate = useNavigate();
  const {
    projects,
    loadingData,
    projectsApiAvailable,
    projectsApiMessage,
    hasPermission,
    refreshWorkspaceData,
  } = useApp();

  const [query, setQuery] = useState("");
  const [showCreatePopup, setShowCreatePopup] = useState(false);

  const [organizations, setOrganizations] = useState<BackendOrganization[]>([]);
  const [loadingOrganizations, setLoadingOrganizations] = useState(false);

  const [pageError, setPageError] = useState("");
  const [createError, setCreateError] = useState("");
  const [editError, setEditError] = useState("");
  const [savingProject, setSavingProject] = useState(false);

  const [createForm, setCreateForm] = useState<ProjectCreateForm>(emptyCreateForm);

  const [selectedProject, setSelectedProject] = useState<BackendProject | null>(null);
  const [projectPopupMode, setProjectPopupMode] = useState<ProjectPopupMode>(null);
  const [editForm, setEditForm] = useState<ProjectCreateForm>(emptyCreateForm);

  const [updatingProject, setUpdatingProject] = useState(false);
  const [deletingProject, setDeletingProject] = useState(false); // used for GRID delete only

  const canCreateProject = hasPermission(AppPermissions.ProjectsCreate);
  const canUpdateProject = hasPermission(AppPermissions.ProjectsUpdate);
  const canDeleteProject = hasPermission(AppPermissions.ProjectsDelete);

  const organizationNameById = useMemo(() => {
    const map = new Map<string, string>();
    organizations.forEach((item) => {
      map.set(item.id, item.name);
    });
    return map;
  }, [organizations]);

  const filteredProjects = useMemo(() => {
    const term = query.trim().toLowerCase();

    return projects.filter((project) => {
      if (!term) return true;

      return (
        project.name.toLowerCase().includes(term) ||
        (project.key ?? "").toLowerCase().includes(term)
      );
    });
  }, [projects, query]);

  const selectedOrganizationLabel = useMemo(() => {
    if (!editForm.organizationId) {
      return "";
    }

    const name = organizationNameById.get(editForm.organizationId);
    if (name) {
      return name;
    }

    if (loadingOrganizations) {
      return "Loading organization...";
    }

    return "Unknown organization";
  }, [editForm.organizationId, organizationNameById, loadingOrganizations]);

  const emptyState =
    projectsApiAvailable && !loadingData && filteredProjects.length === 0;

  const loadOrganizations = async () => {
    setLoadingOrganizations(true);

    if (showCreatePopup) {
      setCreateError("");
    }

    if (selectedProject) {
      setEditError("");
    }

    try {
      const result = await getOrganizations();
      setOrganizations(result);

      setCreateForm((prev) => {
        if (prev.organizationId || result.length === 0) return prev;
        return { ...prev, organizationId: result[0].id };
      });
    } catch (error) {
      const message =
        error instanceof ApiError
          ? error.message
          : error instanceof Error
          ? error.message
          : "Failed to load organizations.";

      if (showCreatePopup) {
        setCreateError(message);
      } else if (selectedProject) {
        setEditError(message);
      } else {
        setPageError(message);
      }
    } finally {
      setLoadingOrganizations(false);
    }
  };

  const openCreatePopup = () => {
    setCreateError("");
    setShowCreatePopup(true);

    if (organizations.length === 0) {
      void loadOrganizations();
    }
  };

  const closeCreatePopup = () => {
    setShowCreatePopup(false);
    setCreateError("");
    setCreateForm((prev) => ({
      ...emptyCreateForm,
      organizationId: prev.organizationId,
    }));
  };

  const handleCreateProject = async (event: React.FormEvent) => {
    event.preventDefault();
    setCreateError("");

    if (!canCreateProject) {
      setCreateError("You do not have permission to create projects.");
      return;
    }

    if (!createForm.organizationId.trim()) {
      setCreateError("Organization is required.");
      return;
    }

    if (!createForm.name.trim()) {
      setCreateError("Project name is required.");
      return;
    }

    if (!createForm.key.trim()) {
      setCreateError("Project key is required.");
      return;
    }

    if (createForm.key.trim().length > 10) {
      setCreateError("Project key must be 10 characters or less.");
      return;
    }

    setSavingProject(true);

    try {
      await createProject({
        organizationId: createForm.organizationId.trim(),
        name: createForm.name.trim(),
        key: createForm.key.trim(),
        description: createForm.description.trim() || undefined,
      });

      closeCreatePopup();
      await refreshWorkspaceData({ includeTasks: false });
    } catch (error) {
      if (error instanceof ApiError) setCreateError(error.message);
      else if (error instanceof Error) setCreateError(error.message);
      else setCreateError("Failed to create project.");
    } finally {
      setSavingProject(false);
    }
  };

  const openProjectPopup = (project: BackendProject, mode: ProjectPopupMode) => {
    setSelectedProject(project);
    setProjectPopupMode(mode);
    setEditForm({
      organizationId: project.organizationId ?? "",
      name: project.name,
      key: project.key ?? "",
      description: project.description ?? "",
    });
    setEditError("");

    if (organizations.length === 0 && !loadingOrganizations) {
      void loadOrganizations();
    }
  };

  const closeEditPopup = () => {
    setSelectedProject(null);
    setProjectPopupMode(null);
    setEditError("");
    setEditForm(emptyCreateForm);
  };

  const handleUpdateProject = async () => {
    if (!selectedProject) return;

    if (!canUpdateProject) {
      setEditError("You do not have permission to update projects.");
      return;
    }

    if (!editForm.name.trim()) {
      setEditError("Project name is required.");
      return;
    }

    if (!editForm.key.trim()) {
      setEditError("Project key is required.");
      return;
    }

    if (editForm.key.trim().length > 10) {
      setEditError("Project key must be 10 characters or less.");
      return;
    }

    setEditError("");
    setUpdatingProject(true);

    try {
      await updateProject(selectedProject.id, {
        name: editForm.name.trim(),
        key: editForm.key.trim(),
        description: editForm.description.trim() || undefined,
      });

      await refreshWorkspaceData({ includeTasks: false });
      closeEditPopup();
    } catch (error) {
      if (error instanceof ApiError) setEditError(error.message);
      else if (error instanceof Error) setEditError(error.message);
      else setEditError("Failed to update project.");
    } finally {
      setUpdatingProject(false);
    }
  };

  const handleDeleteProjectFromGrid = async (project: BackendProject) => {
    if (!canDeleteProject) {
      setPageError("You do not have permission to delete projects.");
      return;
    }

    const confirmed = window.confirm(`Delete project "${project.name}"?`);
    if (!confirmed) return;

    setPageError("");
    setDeletingProject(true);

    try {
      await deleteProject(project.id);
      if (selectedProject?.id === project.id) closeEditPopup();
      await refreshWorkspaceData({ includeTasks: false });
    } catch (error) {
      if (error instanceof ApiError) setPageError(error.message);
      else if (error instanceof Error) setPageError(error.message);
      else setPageError("Failed to delete project.");
    } finally {
      setDeletingProject(false);
    }
  };

  return (
    <div className="page-stack">
      <section className="page-title-row">
        <div>
          <h1>Projects</h1>
          <p className="page-subtitle">Manage and track all projects</p>
        </div>

        <Button
          text="New Project"
          icon="plus"
          type="default"
          disabled={!canCreateProject}
          onClick={openCreatePopup}
        />
      </section>

      {pageError && <div className="form-error">{pageError}</div>}

      {!projectsApiAvailable && (
        <div className="card state-card warning-state">
          <h3>Projects endpoint unavailable</h3>
          <p>{projectsApiMessage || "No /api/Projects endpoint is currently exposed by the backend."}</p>
        </div>
      )}

      {loadingData && <div className="page-inline-info">Refreshing projects...</div>}

      <section className="toolbar-row">
        <TextBox
          mode="search"
          placeholder="Search projects..."
          value={query}
          onValueChanged={(event) => setQuery(String(event.value ?? ""))}
          width="min(100%, 340px)"
          showClearButton
        />
      </section>

      <section className="card">
        {emptyState ? (
          <div className="empty-state">
            <h3>No projects found</h3>
            <p>Create a project to get started.</p>
            <Button
              text="Create Project"
              type="default"
              icon="plus"
              disabled={!canCreateProject}
              onClick={openCreatePopup}
            />
          </div>
        ) : (
          <DataGrid
            dataSource={filteredProjects}
            keyExpr="id"
            rowAlternationEnabled
            hoverStateEnabled
            showBorders={false}
            columnAutoWidth
            columnHidingEnabled
            wordWrapEnabled
            columnMinWidth={120}
            noDataText="No projects found."
            onRowClick={(event) => {
              if (event.rowType !== "data" || !event.data) return;

              const target = event.event?.target as HTMLElement | null;
              if (target?.closest(".inline-actions")) return;

              openProjectPopup(event.data as BackendProject, "view");
            }}
          >
            <Column dataField="name" caption="Project" />
            <Column dataField="key" caption="Key" width={120} />
            <Column
              dataField="updatedAt"
              caption="Updated"
              width={140}
              cellRender={({ data }: { data: BackendProject }) =>
                data.updatedAt ? new Date(data.updatedAt).toLocaleDateString() : "-"
              }
            />

            <Column
              caption="Actions"
              width={250}
              allowSorting={false}
              allowFiltering={false}
              cellRender={({ data }: { data: BackendProject }) => (
                <div
                  className="inline-actions"
                  onClick={(event) => event.stopPropagation()}
                  onMouseDown={(event) => event.stopPropagation()}
                  onPointerDown={(event) => event.stopPropagation()}
                >
                  <Button
                    text="Details"
                    stylingMode="text"
                    onClick={(event) => {
                      event?.event?.preventDefault?.();
                      event?.event?.stopPropagation?.();
                      navigate(`/projects/${data.id}`);
                    }}
                  />
                  <Button
                    text="Edit"
                    stylingMode="text"
                    disabled={!canUpdateProject}
                    onClick={(event) => {
                      event?.event?.preventDefault?.();
                      event?.event?.stopPropagation?.();
                      openProjectPopup(data, "edit");
                    }}
                  />
                  <Button
                    text="Delete"
                    type="danger"
                    stylingMode="text"
                    disabled={!canDeleteProject || deletingProject}
                    onClick={(event) => {
                      event?.event?.preventDefault?.();
                      event?.event?.stopPropagation?.();
                      void handleDeleteProjectFromGrid(data);
                    }}
                  />
                </div>
              )}
            />
            <Paging enabled pageSize={10} />
          </DataGrid>
        )}
      </section>

      {/* Create modal */}
      <Modal
        visible={showCreatePopup}
        onClose={closeCreatePopup}
        title="Create Project"
        width={620}
      >
        <form className="popup-form" onSubmit={handleCreateProject}>
          {createError && <div className="form-error">{createError}</div>}

          {organizations.length > 0 ? (
            <label>
              Organization
              <SelectBox
                dataSource={organizations}
                displayExpr="name"
                valueExpr="id"
                value={createForm.organizationId || null}
                disabled={loadingOrganizations}
                onValueChanged={(event) =>
                  setCreateForm((prev) => ({
                    ...prev,
                    organizationId: String(event.value ?? ""),
                  }))
                }
              />
            </label>
          ) : (
            <label>
              Organization Id
              <TextBox
                value={createForm.organizationId}
                disabled={loadingOrganizations}
                placeholder="Paste organization id"
                onValueChanged={(event) =>
                  setCreateForm((prev) => ({
                    ...prev,
                    organizationId: String(event.value ?? ""),
                  }))
                }
              />
            </label>
          )}

          <label>
            Project Name
            <TextBox
              value={createForm.name}
              onValueChanged={(event) =>
                setCreateForm((prev) => ({
                  ...prev,
                  name: String(event.value ?? ""),
                }))
              }
            />
          </label>

          <label>
            Project Key
            <TextBox
              value={createForm.key}
              maxLength={10}
              onValueChanged={(event) =>
                setCreateForm((prev) => ({
                  ...prev,
                  key: String(event.value ?? "")
                    .toUpperCase()
                    .replace(/\s+/g, ""),
                }))
              }
            />
          </label>

          <label>
            Description
            <TextArea
              value={createForm.description}
              minHeight={90}
              onValueChanged={(event) =>
                setCreateForm((prev) => ({
                  ...prev,
                  description: String(event.value ?? ""),
                }))
              }
            />
          </label>

          <div className="popup-actions">
            <Button text="Cancel" stylingMode="outlined" onClick={closeCreatePopup} />
            <Button
              text={savingProject ? "Creating..." : "Create Project"}
              type="default"
              useSubmitBehavior
              disabled={savingProject || loadingOrganizations || !canCreateProject}
            />
          </div>
        </form>
      </Modal>

      <Modal
        visible={selectedProject !== null}
        onClose={closeEditPopup}
        title={
          selectedProject
            ? projectPopupMode === "edit"
              ? `Edit Project: ${selectedProject.name}`
              : `Project: ${selectedProject.name}`
            : "Project"
        }
        width={680}
      >
        {selectedProject && (
          <div className="popup-form">
            {projectPopupMode === "edit" && editError && (
              <div className="form-error">{editError}</div>
            )}

            <label>
              Organization
              <TextBox
                value={selectedOrganizationLabel}
                readOnly
              />
            </label>

            <label>
              Project Name
              <TextBox
                value={editForm.name}
                readOnly={projectPopupMode === "view"}
                onValueChanged={(event) =>
                  setEditForm((prev) => ({
                    ...prev,
                    name: String(event.value ?? ""),
                  }))
                }
              />
            </label>

            <label>
              Project Key
              <TextBox
                value={editForm.key}
                maxLength={10}
                readOnly={projectPopupMode === "view"}
                onValueChanged={(event) =>
                  setEditForm((prev) => ({
                    ...prev,
                    key: String(event.value ?? "")
                      .toUpperCase()
                      .replace(/\s+/g, ""),
                  }))
                }
              />
            </label>

            <label>
              Description
              <TextArea
                value={editForm.description}
                minHeight={90}
                readOnly={projectPopupMode === "view"}
                onValueChanged={(event) =>
                  setEditForm((prev) => ({
                    ...prev,
                    description: String(event.value ?? ""),
                  }))
                }
              />
            </label>

            <div className="popup-actions">
              {projectPopupMode === "edit" ? (
                <>
                  <Button
                    text="Cancel"
                    stylingMode="outlined"
                    onClick={closeEditPopup}
                    disabled={updatingProject}
                  />
                  <Button
                    text={updatingProject ? "Saving..." : "Save"}
                    type="default"
                    disabled={updatingProject || !canUpdateProject}
                    onClick={() => void handleUpdateProject()}
                  />
                </>
              ) : (
                <Button text="Close" stylingMode="outlined" onClick={closeEditPopup} />
              )}
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}