import { useCallback, useEffect, useMemo, useState } from "react";
import { Button } from "devextreme-react/button";
import DataGrid, { Column } from "devextreme-react/data-grid";
import SelectBox from "devextreme-react/select-box";
import TextArea from "devextreme-react/text-area";
import TextBox from "devextreme-react/text-box";
import { Modal } from "../components/Modal";
import { useNavigate } from "react-router-dom";
import { useApp } from "../context/AppContext";
import { PaginationControls } from "../components/PaginationControls";
import { useDebouncedValue } from "../hooks/useDebouncedValue";
import { usePagination } from "../hooks/usePagination";
import { getErrorMessage } from "../utils/getErrorMessage";
import { getOrganizations } from "../services/organizationService";
import { createProject, deleteProject, getProjects, updateProject } from "../services/projectService";
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
  const { loadingData, hasPermission, refreshWorkspaceData } = useApp();

  const canCreateProject = hasPermission(AppPermissions.ProjectsCreate);

  const [query, setQuery] = useState("");
  const debouncedQuery = useDebouncedValue(query, 250);

  const [totalCount, setTotalCount] = useState(0);
  const { page, pageSize, skip, setPage, setPageSize, resetPage } = usePagination({
    totalCount,
    initialPageSize: 10,
  });

  const [projects, setProjects] = useState<BackendProject[]>([]);
  const [projectsLoading, setProjectsLoading] = useState(false);
  const [projectsApiAvailable, setProjectsApiAvailable] = useState(true);
  const [projectsApiMessage, setProjectsApiMessage] = useState("");
  const [reloadTick, setReloadTick] = useState(0);

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
  const [deletingProject, setDeletingProject] = useState(false);

  useEffect(() => {
    resetPage();
  }, [debouncedQuery, resetPage]);

  useEffect(() => {
    let isCancelled = false;

    const loadProjects = async () => {
      setProjectsLoading(true);

      try {
        const response = await getProjects({
          search: debouncedQuery.trim() || undefined,
          skip,
          take: pageSize,
        });

        if (isCancelled) return;

        setProjects(response.data);
        setTotalCount(response.totalCount);
        setProjectsApiAvailable(response.available);
        setProjectsApiMessage(response.message ?? "");
        setPageError("");
      } catch (error) {
        if (isCancelled) return;

        setProjects([]);
        setTotalCount(0);
        setPageError(getErrorMessage(error, "Failed to load projects."));
      } finally {
        if (!isCancelled) {
          setProjectsLoading(false);
        }
      }
    };

    void loadProjects();

    return () => {
      isCancelled = true;
    };
  }, [debouncedQuery, pageSize, reloadTick, skip]);

  const organizationNameById = useMemo(() => {
    const map = new Map<string, string>();
    organizations.forEach((item) => {
      map.set(item.id, item.name);
    });
    return map;
  }, [organizations]);

  const selectedOrganizationLabel = useMemo(() => {
    if (!editForm.organizationId) return "";

    const name = organizationNameById.get(editForm.organizationId);
    if (name) return name;
    if (loadingOrganizations) return "Loading organization...";
    return "Unknown organization";
  }, [editForm.organizationId, organizationNameById, loadingOrganizations]);

  const canDeleteProjectInScope = (projectId: string): boolean =>
    hasPermission(AppPermissions.ProjectsDelete, "Project", projectId);

  const canUpdateProjectInScope = (projectId: string): boolean =>
    hasPermission(AppPermissions.ProjectsUpdate, "Project", projectId);

  const canViewMembersInScope = (projectId: string): boolean =>
    hasPermission(AppPermissions.MembersView, "Project", projectId);

  const emptyState =
    projectsApiAvailable && !projectsLoading && !loadingData && projects.length === 0;

  // ─── Fixed loadOrganizations with context param, no stale closures ───
  const loadOrganizations = useCallback(
    async (context: "create" | "edit" | "page") => {
      setOrganizations([]); // reset first so DevExtreme sees a fresh array
      setLoadingOrganizations(true);

      if (context === "create") setCreateError("");
      if (context === "edit") setEditError("");

      try {
        const result = await getOrganizations();

        setOrganizations(result);

        if (context === "create") {
          setCreateForm((prev) => {
            if (prev.organizationId || result.length === 0) return prev;
            return { ...prev, organizationId: String(result[0].id) };
          });
        }
      } catch (error) {
        const message = getErrorMessage(error, "Failed to load organizations.");

        if (context === "create") setCreateError(message);
        else if (context === "edit") setEditError(message);
        else setPageError(message);
      } finally {
        setLoadingOrganizations(false);
      }
    },
    [],
  );

  const openCreatePopup = () => {
    setCreateError("");
    setCreateForm(emptyCreateForm);
    setShowCreatePopup(true);
    void loadOrganizations("create");
  };

  const closeCreatePopup = () => {
    setShowCreatePopup(false);
    setCreateError("");
    setCreateForm(emptyCreateForm);
  };

  const reloadProjects = useCallback(async () => {
    await refreshWorkspaceData({ includeTasks: false });
    setReloadTick((previous) => previous + 1);
  }, [refreshWorkspaceData]);

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
      await reloadProjects();
    } catch (error) {
      setCreateError(getErrorMessage(error, "Failed to create project."));
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
      void loadOrganizations("edit");
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

    if (!canUpdateProjectInScope(selectedProject.id)) {
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

      await reloadProjects();
      closeEditPopup();
    } catch (error) {
      setEditError(getErrorMessage(error, "Failed to update project."));
    } finally {
      setUpdatingProject(false);
    }
  };

  const handleDeleteProjectFromGrid = async (project: BackendProject) => {
    if (!canDeleteProjectInScope(project.id)) {
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
      await reloadProjects();
    } catch (error) {
      setPageError(getErrorMessage(error, "Failed to delete project."));
    } finally {
      setDeletingProject(false);
    }
  };
  const selectBoxDropDownOptions = useMemo(
    () => ({
      wrapperAttr: { class: "modal-selectbox-overlay" },
    }),
    []
  );

  const organizationItems = useMemo(
    () => organizations.map((org) => ({ id: String(org.id), name: org.name })),
    [organizations]
  );

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
          <p>
            {projectsApiMessage ||
              "No /api/Projects endpoint is currently exposed by the backend."}
          </p>
        </div>
      )}

      {(loadingData || projectsLoading) && (
        <div className="page-inline-info">Refreshing projects...</div>
      )}

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
          <>
            <DataGrid
              dataSource={projects}
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
                width={320}
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
                      text="Team"
                      stylingMode="text"
                      disabled={!canViewMembersInScope(data.id)}
                      onClick={(event) => {
                        event?.event?.preventDefault?.();
                        event?.event?.stopPropagation?.();
                        navigate(`/projects/${data.id}/members`);
                      }}
                    />
                    <Button
                      text="Edit"
                      stylingMode="text"
                      disabled={!canUpdateProjectInScope(data.id)}
                      hint={
                        !canUpdateProjectInScope(data.id)
                          ? "You do not have permission to update this project."
                          : undefined
                      }
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
                      disabled={!canDeleteProjectInScope(data.id) || deletingProject}
                      hint={
                        !canDeleteProjectInScope(data.id)
                          ? "You do not have permission to delete this project."
                          : undefined
                      }
                      onClick={(event) => {
                        event?.event?.preventDefault?.();
                        event?.event?.stopPropagation?.();
                        void handleDeleteProjectFromGrid(data);
                      }}
                    />
                  </div>
                )}
              />
            </DataGrid>

            <PaginationControls
              page={page}
              pageSize={pageSize}
              totalCount={totalCount}
              loading={projectsLoading}
              onPageChange={setPage}
              onPageSizeChange={setPageSize}
            />
          </>
        )}
      </section>
      <Modal
        visible={showCreatePopup}
        onClose={closeCreatePopup}
        title="Create Project"
        width={620}
      >
        <form className="popup-form" onSubmit={handleCreateProject}>
          {createError && <div className="form-error">{createError}</div>}

          <label>
            Organization
            {loadingOrganizations ? (
              <TextBox value="Loading organizations..." readOnly />
            ) : (
              <SelectBox
                key={`org-select-${organizations.length}`}
                items={organizationItems}
                displayExpr="name"
                valueExpr="id"
                value={createForm.organizationId || null}
                disabled={loadingOrganizations || organizationItems.length === 0}
                placeholder="Select an organization..."
                showClearButton={false}
                dropDownOptions={selectBoxDropDownOptions}
                onValueChanged={(e) => {
                  if (e.value == null) return;
                  setCreateForm((prev) => ({ ...prev, organizationId: String(e.value) }));
                }}
              />
            )}
          </label>

          <label>
            Project Name
            <TextBox
              value={createForm.name}
              maxLength={200}
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
              maxLength={2000}
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
              <TextBox value={selectedOrganizationLabel} readOnly />
            </label>

            <label>
              Project Name
              <TextBox
                value={editForm.name}
                maxLength={200}
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
                maxLength={2000}
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
                    disabled={updatingProject || !canUpdateProjectInScope(selectedProject.id)}
                    onClick={() => void handleUpdateProject()}
                  />
                </>
              ) : (
                <>
                  {canViewMembersInScope(selectedProject.id) && (
                    <Button
                      text="Manage Team"
                      stylingMode="outlined"
                      onClick={() => {
                        closeEditPopup();
                        navigate(`/projects/${selectedProject.id}/members`);
                      }}
                    />
                  )}
                  <Button text="Close" stylingMode="outlined" onClick={closeEditPopup} />
                </>
              )}
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}