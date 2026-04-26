import { useEffect, useMemo, useRef, useState } from "react";
import { Button } from "devextreme-react/button";
import DataGrid, { Column, Paging } from "devextreme-react/data-grid";
import DateBox from "devextreme-react/date-box";
import Popup from "devextreme-react/popup";
import SelectBox from "devextreme-react/select-box";
import TextArea from "devextreme-react/text-area";
import TextBox from "devextreme-react/text-box";
import { useNavigate } from "react-router-dom";
import { Modal } from "../components/Modal";
import { useApp } from "../context/AppContext";
import { getErrorMessage } from "../utils/getErrorMessage";
import { getEpics } from "../services/epicService";
import { getMembersByScope } from "../services/memberService";
import { getSprints } from "../services/sprintService";
import { AppPermissions } from "../security/permissions";
import type { BackendEpic, BackendSprint } from "../types/app";
import type { ScopeMember } from "../types/invitation";
import {
  Status,
  TaskPriority,
  type CreateTaskDto,
  type TaskDto,
  type UpdateTaskDto,
} from "../types/task";
import {
  priorityLabel,
  priorityOptions,
  statusLabel,
  statusOptions,
} from "../utils/taskPresentation";
import { toDateOnly } from "../utils/toDateOnly";

type ViewMode = "list" | "kanban";
type TaskPopupMode = "view" | "edit" | null;

interface TaskFilters {
  status: Status | "all";
  priority: TaskPriority | "all";
}

interface TaskForm {
  projectId: string;
  epicId: string;
  sprintId: string;
  assigneeId: string;
  title: string;
  description: string;
  status: Status;
  priority: TaskPriority;
  startDate: string;
  endDate: string;
}

interface AssigneeOption {
  id: string;
  label: string;
}

const boardColumns: Array<{ id: Status; label: string }> = [
  { id: Status.NotStarted, label: "Not Started" },
  { id: Status.InProgress, label: "In Progress" },
  { id: Status.Completed, label: "Completed" },
  { id: Status.OnHold, label: "On Hold" },
  { id: Status.Cancelled, label: "Cancelled" },
];



function validateTaskForm(form: TaskForm): string | null {
  if (!form.projectId.trim()) {
    return "Project is required.";
  }

  if (!form.epicId.trim()) {
    return "Epic is required.";
  }

  if (!form.sprintId.trim()) {
    return "Sprint is required.";
  }

  if (!form.title.trim()) {
    return "Title is required.";
  }

  if (form.title.trim().length > 100) {
    return "Title must be 100 characters or less.";
  }

  if (form.description.trim().length > 500) {
    return "Description must be 500 characters or less.";
  }

  if (form.assigneeId && !form.assigneeId.trim()) {
    return "Assignee id cannot be whitespace.";
  }

  if (!form.startDate) {
    return "Start date is required.";
  }

  if (!form.endDate) {
    return "End date is required.";
  }

  if (form.startDate && form.endDate) {
    const startDate = new Date(`${form.startDate}T00:00:00`).getTime();
    const endDate = new Date(`${form.endDate}T00:00:00`).getTime();

    if (startDate > endDate) {
      return "Start date cannot be after end date.";
    }
  }

  return null;
}

function toCreateDto(form: TaskForm): CreateTaskDto {
  return {
    projectId: form.projectId,
    epicId: form.epicId.trim(),
    sprintId: form.sprintId.trim(),
    assigneeId: form.assigneeId.trim() || null,
    title: form.title.trim(),
    description: form.description.trim() || undefined,
    status: form.status,
    priority: form.priority,
    startDate: form.startDate || null,
    endDate: form.endDate || null,
  };
}

function toUpdateDto(form: TaskForm): UpdateTaskDto {
  return {
    epicId: form.epicId.trim(),
    sprintId: form.sprintId.trim(),
    assigneeId: form.assigneeId.trim() || null,
    title: form.title.trim(),
    description: form.description.trim() || undefined,
    status: form.status,
    priority: form.priority,
    startDate: form.startDate || null,
    endDate: form.endDate || null,
  };
}

function toTaskForm(task: TaskDto): TaskForm {
  return {
    projectId: task.projectId,
    epicId: task.epicId ?? "",
    sprintId: task.sprintId ?? "",
    assigneeId: task.assigneeId ?? "",
    title: task.title ?? "",
    description: task.description ?? "",
    status: task.status,
    priority: task.priority,
    startDate: task.startDate ?? "",
    endDate: task.endDate ?? "",
  };
}

export function TasksPage() {
  const navigate = useNavigate();
  const { tasks, projects, loadingData, addTask, updateTask, deleteTask, hasPermission, user } = useApp();

  const canCreate = hasPermission(AppPermissions.TasksCreate);
  const canUpdate = hasPermission(AppPermissions.TasksUpdate);
  const canDelete = hasPermission(AppPermissions.TasksDelete);
  const canAssign = hasPermission(AppPermissions.TasksAssign);

  const [viewMode, setViewMode] = useState<ViewMode>("list");
  const [query, setQuery] = useState("");
  const [selectedTask, setSelectedTask] = useState<TaskDto | null>(null);
  const [taskPopupMode, setTaskPopupMode] = useState<TaskPopupMode>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [requestError, setRequestError] = useState("");
  const [requestLoading, setRequestLoading] = useState(false);
  const [filters, setFilters] = useState<TaskFilters>({
    status: "all",
    priority: "all",
  });

  const [createForm, setCreateForm] = useState<TaskForm>({
    projectId: "",
    epicId: "",
    sprintId: "",
    assigneeId: "",
    title: "",
    description: "",
    status: Status.NotStarted,
    priority: TaskPriority.Medium,
    startDate: "",
    endDate: "",
  });

  const [editForm, setEditForm] = useState<TaskForm | null>(null);
  const [createEpics, setCreateEpics] = useState<BackendEpic[]>([]);
  const [createSprints, setCreateSprints] = useState<BackendSprint[]>([]);
  const [createAssignableMembers, setCreateAssignableMembers] = useState<ScopeMember[]>([]);
  const [editEpics, setEditEpics] = useState<BackendEpic[]>([]);
  const [editSprints, setEditSprints] = useState<BackendSprint[]>([]);
  const [editAssignableMembers, setEditAssignableMembers] = useState<ScopeMember[]>([]);
  const projectLinksCacheRef = useRef<
    Record<string, { epics: BackendEpic[]; sprints: BackendSprint[] }>
  >({});
  const projectLinksInflightRef = useRef<
    Record<string, Promise<{ epics: BackendEpic[]; sprints: BackendSprint[] }>>
  >({});
  const projectMembersCacheRef = useRef<Record<string, ScopeMember[]>>({});
  const projectMembersInflightRef = useRef<Record<string, Promise<ScopeMember[]>>>({});
  const suppressNextRowClickRef = useRef(false);
  const [viewport, setViewport] = useState(() => ({
    width: typeof window !== "undefined" ? window.innerWidth : 1366,
    height: typeof window !== "undefined" ? window.innerHeight : 768,
  }));

  useEffect(() => {
    const handleResize = () => {
      setViewport({ width: window.innerWidth, height: window.innerHeight });
    };

    window.addEventListener("resize", handleResize);
    return () => window.removeEventListener("resize", handleResize);
  }, []);

  const createPopupWidth =
    viewport.width <= 820 ? Math.max(320, viewport.width - 24) : viewport.width <= 1440 ? 560 : 640;

  const detailsPopupWidth =
    viewport.width <= 820 ? Math.max(340, viewport.width - 20) : viewport.width <= 1440 ? 680 : 760;

  const createLinksMissing =
    Boolean(createForm.projectId) && (createEpics.length === 0 || createSprints.length === 0);

  const editLinksMissing =
    Boolean(editForm?.projectId) && (editEpics.length === 0 || editSprints.length === 0);

  const editEpicNameById = useMemo(() => {
    const map = new Map<string, string>();
    editEpics.forEach((epic) => {
      map.set(epic.id, epic.title);
    });
    return map;
  }, [editEpics]);

  const editSprintNameById = useMemo(() => {
    const map = new Map<string, string>();
    editSprints.forEach((sprint) => {
      map.set(sprint.id, sprint.name);
    });
    return map;
  }, [editSprints]);

  const editEpicsForSelect = useMemo(() => {
    const currentEpicId = editForm?.epicId?.trim();
    if (!currentEpicId) {
      return editEpics;
    }

    if (editEpics.some((epic) => epic.id === currentEpicId)) {
      return editEpics;
    }

    return [
      {
        id: currentEpicId,
        projectId: editForm?.projectId ?? "",
        title: currentEpicId,
        status: 0,
      },
      ...editEpics,
    ];
  }, [editEpics, editForm?.epicId, editForm?.projectId]);

  const editSprintsForSelect = useMemo(() => {
    const currentSprintId = editForm?.sprintId?.trim();
    if (!currentSprintId) {
      return editSprints;
    }

    if (editSprints.some((sprint) => sprint.id === currentSprintId)) {
      return editSprints;
    }

    return [
      {
        id: currentSprintId,
        projectId: editForm?.projectId ?? "",
        name: currentSprintId,
        goal: undefined,
        startDate: "",
        endDate: "",
        status: 0,
      },
      ...editSprints,
    ];
  }, [editSprints, editForm?.sprintId, editForm?.projectId]);

  const selectBoxDropDownOptions = useMemo(
    () => ({
      wrapperAttr: { class: "modal-selectbox-overlay" },
    }),
    []
  );

  const toAssigneeLabel = (member: ScopeMember): string =>
    `${member.firstName} ${member.lastName}`.trim() + ` (${member.role})`;

  const createAssigneeOptions = useMemo<AssigneeOption[]>(() => {
    let options = createAssignableMembers.map((member) => ({
      id: member.userId,
      label: toAssigneeLabel(member),
    }));

    if (user?.id && !options.some((o) => o.id === user.id)) {
      options.push({ id: user.id, label: `${user.fullName} (Inherited)` });
    }

    if (!canAssign && user?.id) {
      options = options.filter((o) => o.id === user.id);
    }
    return options;
  }, [createAssignableMembers, canAssign, user]);

  const editAssigneeOptions = useMemo<AssigneeOption[]>(() => {
    let options = editAssignableMembers.map((member) => ({
      id: member.userId,
      label: toAssigneeLabel(member),
    }));

    if (user?.id && !options.some((o) => o.id === user.id)) {
      options.push({ id: user.id, label: `${user.fullName} (Inherited)` });
    }

    const currentAssigneeId = editForm?.assigneeId?.trim();
    if (!currentAssigneeId || options.some((option) => option.id === currentAssigneeId)) {
      return options;
    }

    return [
      { id: currentAssigneeId, label: `${currentAssigneeId} (not currently assignable)` },
      ...options,
    ];
  }, [editAssignableMembers, editForm?.assigneeId, user]);

  const editAssigneeLabelById = useMemo(() => {
    const map = new Map<string, string>();
    editAssigneeOptions.forEach((member) => {
      map.set(member.id, member.label);
    });
    return map;
  }, [editAssigneeOptions]);

  const loadProjectLinks = async (
    projectId: string,
    onEpics: (value: BackendEpic[]) => void,
    onSprints: (value: BackendSprint[]) => void
  ) => {
    if (!projectId) {
      onEpics([]);
      onSprints([]);
      return;
    }

    const cachedLinks = projectLinksCacheRef.current[projectId];
    if (cachedLinks) {
      onEpics(cachedLinks.epics);
      onSprints(cachedLinks.sprints);
      return;
    }

    let inflightRequest = projectLinksInflightRef.current[projectId];
    if (!inflightRequest) {
      inflightRequest = Promise.all([getEpics(projectId), getSprints(projectId)]).then(
        ([epicsResult, sprintsResult]) => ({
          epics: epicsResult,
          sprints: sprintsResult,
        })
      );
      projectLinksInflightRef.current[projectId] = inflightRequest;
    }

    try {
      const links = await inflightRequest;
      projectLinksCacheRef.current[projectId] = links;
      onEpics(links.epics);
      onSprints(links.sprints);
    } catch {
      onEpics([]);
      onSprints([]);
    } finally {
      delete projectLinksInflightRef.current[projectId];
    }
  };

  const loadProjectAssignableMembers = async (
    projectId: string,
    onMembers: (value: ScopeMember[]) => void
  ) => {
    if (!projectId) {
      onMembers([]);
      return;
    }

    const cachedMembers = projectMembersCacheRef.current[projectId];
    if (cachedMembers) {
      onMembers(cachedMembers);
      return;
    }

    let inflightRequest = projectMembersInflightRef.current[projectId];
    if (!inflightRequest) {
      inflightRequest = getMembersByScope(1, projectId).then((result) => result.members);
      projectMembersInflightRef.current[projectId] = inflightRequest;
    }

    try {
      const members = await inflightRequest;
      projectMembersCacheRef.current[projectId] = members;
      onMembers(members);
    } catch {
      onMembers([]);
    } finally {
      delete projectMembersInflightRef.current[projectId];
    }
  };

  useEffect(() => {
    if (projects.length === 0) {
      return;
    }

    setCreateForm((prev) => {
      if (prev.projectId) {
        return prev;
      }

      return {
        ...prev,
        projectId: projects[0].id,
      };
    });
  }, [projects]);

  useEffect(() => {
    if (!showCreate || !createForm.projectId) {
      setCreateAssignableMembers([]);
      return;
    }

    void loadProjectLinks(createForm.projectId, setCreateEpics, setCreateSprints);
    void loadProjectAssignableMembers(createForm.projectId, setCreateAssignableMembers);
  }, [showCreate, createForm.projectId]);

  useEffect(() => {
    if (taskPopupMode !== "edit" || !editForm?.projectId) {
      setEditEpics([]);
      setEditSprints([]);
      setEditAssignableMembers([]);
      return;
    }

    void loadProjectLinks(editForm.projectId, setEditEpics, setEditSprints);
    void loadProjectAssignableMembers(editForm.projectId, setEditAssignableMembers);
  }, [taskPopupMode, editForm?.projectId]);

  useEffect(() => {
    if (!selectedTask?.projectId || !editForm?.projectId) {
      return;
    }

    void loadProjectLinks(editForm.projectId, setEditEpics, setEditSprints);
    void loadProjectAssignableMembers(editForm.projectId, setEditAssignableMembers);
  }, [selectedTask?.id, selectedTask?.projectId, editForm?.projectId]);

  const filteredTasks = useMemo(() => {
    return tasks
      .filter((task) => {
        const term = query.trim().toLowerCase();
        const matchesText =
          !term ||
          (task.title ?? "").toLowerCase().includes(term) ||
          String(task.id).toLowerCase().includes(term);
        const matchesStatus =
          filters.status === "all" || task.status === filters.status;
        const matchesPriority =
          filters.priority === "all" || task.priority === filters.priority;

        return matchesText && matchesStatus && matchesPriority;
      })
      .sort(
        (a, b) =>
          new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
      );
  }, [tasks, query, filters]);

  const projectNameById = useMemo(() => {
    const map = new Map<string, string>();
    projects.forEach((project) => {
      map.set(project.id, project.name);
    });
    return map;
  }, [projects]);



  const handleCreateTask = async (event: React.FormEvent) => {
    event.preventDefault();
    setRequestError("");

    if (!canCreate) {
      setRequestError("You do not have permission to create tasks.");
      return;
    }

    const validation = validateTaskForm(createForm);
    if (validation) {
      setRequestError(validation);
      return;
    }

    setRequestLoading(true);
    try {
      await addTask(toCreateDto(createForm));
      setCreateForm({
        projectId: createForm.projectId,
        epicId: "",
        sprintId: "",
        assigneeId: "",
        title: "",
        description: "",
        status: Status.NotStarted,
        priority: TaskPriority.Medium,
        startDate: "",
        endDate: "",
      });
      setShowCreate(false);
    } catch (error) {
      setRequestError(getErrorMessage(error, "Failed to create task."));
    } finally {
      setRequestLoading(false);
    }
  };

  const openTaskDetails = (task: TaskDto, mode: TaskPopupMode) => {
    setSelectedTask(task);
    setTaskPopupMode(mode);
    setEditForm(toTaskForm(task));
    setRequestError("");
  };

  const closeTaskDetails = () => {
    setSelectedTask(null);
    setTaskPopupMode(null);
    setEditForm(null);
    setRequestError("");
  };

  const handleUpdateTask = async () => {
    if (!selectedTask || !editForm) {
      return;
    }

    setRequestError("");

    if (!canUpdate) {
      setRequestError("You do not have permission to update tasks.");
      return;
    }

    const validation = validateTaskForm(editForm);
    if (validation) {
      setRequestError(validation);
      return;
    }

    setRequestLoading(true);
    try {
      await updateTask(selectedTask.id, selectedTask.projectId, toUpdateDto(editForm));
      closeTaskDetails();
    } catch (error) {
      setRequestError(getErrorMessage(error, "Failed to update task."));
    } finally {
      setRequestLoading(false);
    }
  };

  const handleDeleteFromGrid = async (task: TaskDto) => {
    if (!canDelete) {
      setRequestError("You do not have permission to delete tasks.");
      return;
    }

    const confirmed = window.confirm(`Delete task \"${task.title || `#${task.id}`}\"?`);
    if (!confirmed) {
      return;
    }

    setRequestLoading(true);
    setRequestError("");

    try {
      await deleteTask(task.id, task.projectId);
      if (selectedTask?.id === task.id) {
        setSelectedTask(null);
        setEditForm(null);
      }
    } catch (error) {
      setRequestError(getErrorMessage(error, "Failed to delete task."));
    } finally {
      setRequestLoading(false);
    }
  };

  const goToTaskDetails = (task: TaskDto) => {
    const query = new URLSearchParams();

    if (task.projectId) {
      query.set("projectId", task.projectId);
    }

    const querySuffix = query.toString() ? `?${query.toString()}` : "";
    navigate(`/tasks/details/${task.id}${querySuffix}`);
  };

  return (
    <div className="page-stack">
      <section className="page-title-row">
        <div>
          <h1>Tasks</h1>
          <p className="page-subtitle">Manage and track all tasks</p>
        </div>
        <Button
          text="New Task"
          icon="plus"
          type="default"
          disabled={!canCreate}
          onClick={() => {
            setRequestError("");
            setShowCreate(true);
          }}
        />
      </section>

      {!canCreate || !canUpdate || !canDelete ? (
        <div className="page-inline-info">
          You are in restricted mode. Some task actions are disabled based on your permissions.
        </div>
      ) : null}

      {loadingData && <div className="page-inline-info">Refreshing tasks...</div>}
      {!showCreate && !selectedTask && requestError && <div className="form-error">{requestError}</div>}

      <section className="toolbar-row">
        <TextBox
          mode="search"
          placeholder="Search tasks..."
          value={query}
          onValueChanged={(event) => setQuery(String(event.value ?? ""))}
          width="min(100%, 320px)"
          showClearButton
        />

        <div className="toolbar-filters">
          <SelectBox
            dataSource={[{ id: "all", label: "All statuses" }, ...statusOptions]}
            displayExpr="label"
            valueExpr="id"
            value={filters.status}
            onValueChanged={(event) =>
              setFilters((prev) => ({
                ...prev,
                status: (event.value as Status | "all") ?? "all",
              }))
            }
            width={190}
          />

          <SelectBox
            dataSource={[{ id: "all", label: "All priorities" }, ...priorityOptions]}
            displayExpr="label"
            valueExpr="id"
            value={filters.priority}
            onValueChanged={(event) =>
              setFilters((prev) => ({
                ...prev,
                priority: (event.value as TaskPriority | "all") ?? "all",
              }))
            }
            width={190}
          />

          <div className="view-toggle">
            <Button
              text="List"
              stylingMode={viewMode === "list" ? "contained" : "text"}
              onClick={() => setViewMode("list")}
            />
            <Button
              text="Kanban"
              stylingMode={viewMode === "kanban" ? "contained" : "text"}
              onClick={() => setViewMode("kanban")}
            />
          </div>
        </div>
      </section>

      {viewMode === "list" && (
        <section className="card">
          <DataGrid
            dataSource={filteredTasks}
            keyExpr="id"
            showBorders={false}
            rowAlternationEnabled
            hoverStateEnabled
            columnAutoWidth={false}
            columnHidingEnabled={false}
            wordWrapEnabled={false}
            columnMinWidth={120}
            onRowClick={(event) => {
              if (suppressNextRowClickRef.current) {
                suppressNextRowClickRef.current = false;
                return;
              }

              if (event.rowType !== "data" || !event.data) {
                return;
              }

              const target = event.event?.target as HTMLElement | null;
              if (target?.closest(".inline-actions")) {
                return;
              }

              goToTaskDetails(event.data as TaskDto);
            }}
          >
            <Column
              caption="Project"
              width={180}
              cellRender={({ data }: { data: TaskDto }) =>
                projectNameById.get(data.projectId) ?? data.projectId
              }
            />
            <Column
              dataField="title"
              caption="Task"
              cellRender={({ data }: { data: TaskDto }) => data.title || "Untitled task"}
            />
            <Column
              caption="Status"
              width={140}
              cellRender={({ data }: { data: TaskDto }) => statusLabel(data.status)}
            />
            <Column
              caption="Priority"
              width={140}
              cellRender={({ data }: { data: TaskDto }) => priorityLabel(data.priority)}
            />
            <Column
              caption="Start"
              width={130}
              cellRender={({ data }: { data: TaskDto }) =>
                data.startDate ? new Date(`${data.startDate}T00:00:00`).toLocaleDateString() : "-"
              }
            />
            <Column
              caption="End"
              width={130}
              cellRender={({ data }: { data: TaskDto }) =>
                data.endDate ? new Date(`${data.endDate}T00:00:00`).toLocaleDateString() : "-"
              }
            />
            <Column
              caption="Actions"
              width={250}
              allowSorting={false}
              allowFiltering={false}
              cellRender={({ data }: { data: TaskDto }) => (
                <div
                  className="inline-actions"
                  onClick={(event) => {
                    suppressNextRowClickRef.current = true;
                    event.stopPropagation();
                  }}
                  onMouseDown={(event) => {
                    suppressNextRowClickRef.current = true;
                    event.stopPropagation();
                  }}
                  onPointerDown={(event) => {
                    suppressNextRowClickRef.current = true;
                    event.stopPropagation();
                  }}
                >
                  <Button
                    text="View"
                    stylingMode="text"
                    onClick={(event) => {
                      suppressNextRowClickRef.current = true;
                      event?.event?.preventDefault?.();
                      event?.event?.stopPropagation?.();
                      goToTaskDetails(data);
                    }}
                  />
                  <Button
                    text="Edit"
                    stylingMode="text"
                    disabled={!canUpdate}
                    onClick={(event) => {
                      suppressNextRowClickRef.current = true;
                      event?.event?.preventDefault?.();
                      event?.event?.stopPropagation?.();
                      openTaskDetails(data, "edit");
                    }}
                  />
                  <Button
                    text="Delete"
                    type="danger"
                    stylingMode="text"
                    disabled={!canDelete || requestLoading}
                    onClick={(event) => {
                      suppressNextRowClickRef.current = true;
                      event?.event?.preventDefault?.();
                      event?.event?.stopPropagation?.();
                      void handleDeleteFromGrid(data);
                    }}
                  />
                </div>
              )}
            />
            <Paging enabled pageSize={10} />
          </DataGrid>
        </section>
      )}

      {viewMode === "kanban" && (
        <section className="kanban-grid kanban-grid-wide">
          {boardColumns.map((column) => (
            <article className="kanban-column" key={column.id}>
              <h3>{column.label}</h3>
              <div className="kanban-list">
                {filteredTasks
                  .filter((task) => task.status === column.id)
                  .map((task) => (
                    <button
                      type="button"
                      key={task.id}
                      className="kanban-card"
                      onClick={() => goToTaskDetails(task)}
                    >
                      <strong>{task.title || "Untitled task"}</strong>
                      <span>{priorityLabel(task.priority)}</span>
                    </button>
                  ))}
              </div>
            </article>
          ))}
        </section>
      )}

      <Modal
        visible={selectedTask !== null}
        onClose={closeTaskDetails}
        title={
          selectedTask
            ? taskPopupMode === "edit"
              ? `Edit Task: ${selectedTask.title ?? "Untitled"}`
              : `Task: ${selectedTask.title ?? "Untitled"}`
            : "Task details"
        }
        width={detailsPopupWidth}
      >
        {selectedTask && editForm && (
          <div className="task-popup-grid">
            <div className="task-popup-main">
              {requestError && <div className="form-error">{requestError}</div>}

              <label>
                Assignee
                {taskPopupMode === "edit" ? (
                  <SelectBox
                    dataSource={editAssigneeOptions}
                    displayExpr="label"
                    valueExpr="id"
                    value={editForm.assigneeId || null}
                    readOnly={!canAssign}
                    showClearButton={canAssign}
                    placeholder={canAssign ? "Select assignee (optional)" : "No assign permission"}
                    dropDownOptions={selectBoxDropDownOptions}
                    onValueChanged={(event) =>
                      setEditForm((prev) =>
                        prev ? { ...prev, assigneeId: String(event.value ?? "") } : prev
                      )
                    }
                  />
                ) : (
                  <TextBox
                    value={
                      editForm.assigneeId
                        ? editAssigneeLabelById.get(editForm.assigneeId) ?? editForm.assigneeId
                        : "Unassigned"
                    }
                    readOnly
                  />
                )}
              </label>

              <label>
                Title
                <TextBox
                  value={editForm.title}
                  maxLength={100}
                  readOnly={taskPopupMode === "view"}
                  onValueChanged={(event) =>
                    setEditForm((prev) =>
                      prev ? { ...prev, title: String(event.value ?? "") } : prev
                    )
                  }
                />
              </label>

              <label>
                Description
                <TextArea
                  value={editForm.description}
                  maxLength={500}
                  readOnly={taskPopupMode === "view"}
                  onValueChanged={(event) =>
                    setEditForm((prev) =>
                      prev ? { ...prev, description: String(event.value ?? "") } : prev
                    )
                  }
                  minHeight={110}
                />
              </label>
            </div>

            <div className="task-popup-side">
              <label>
                Project
                {taskPopupMode === "edit" ? (
                  <SelectBox
                    dataSource={projects}
                    displayExpr="name"
                    valueExpr="id"
                    value={editForm.projectId}
                    readOnly={false}
                    dropDownOptions={selectBoxDropDownOptions}
                    onValueChanged={(event) =>
                      setEditForm((prev) =>
                        prev
                          ? {
                              ...prev,
                              projectId: String(event.value ?? ""),
                              epicId: "",
                              sprintId: "",
                              assigneeId: "",
                            }
                          : prev
                      )
                    }
                  />
                ) : (
                  <TextBox
                    value={projectNameById.get(editForm.projectId) ?? editForm.projectId}
                    readOnly
                  />
                )}
              </label>

              <label>
                Epic
                {taskPopupMode === "edit" ? (
                  <SelectBox
                    dataSource={editEpicsForSelect}
                    displayExpr="title"
                    valueExpr="id"
                    value={editForm.epicId || null}
                    readOnly={false}
                    dropDownOptions={selectBoxDropDownOptions}
                    onValueChanged={(event) =>
                      setEditForm((prev) =>
                        prev ? { ...prev, epicId: String(event.value ?? "") } : prev
                      )
                    }
                  />
                ) : (
                  <TextBox
                    value={
                      editForm.epicId
                        ? editEpicNameById.get(editForm.epicId) ?? "-"
                        : "-"
                    }
                    readOnly
                  />
                )}
              </label>

              <label>
                Sprint
                {taskPopupMode === "edit" ? (
                  <SelectBox
                    dataSource={editSprintsForSelect}
                    displayExpr="name"
                    valueExpr="id"
                    value={editForm.sprintId || null}
                    readOnly={false}
                    dropDownOptions={selectBoxDropDownOptions}
                    onValueChanged={(event) =>
                      setEditForm((prev) =>
                        prev ? { ...prev, sprintId: String(event.value ?? "") } : prev
                      )
                    }
                  />
                ) : (
                  <TextBox
                    value={
                      editForm.sprintId
                        ? editSprintNameById.get(editForm.sprintId) ?? "-"
                        : "-"
                    }
                    readOnly
                  />
                )}
              </label>

              {editLinksMissing && (
                <div className="form-error">
                  The selected project must have at least one epic and one sprint.
                </div>
              )}

              <label>
                Status
                <SelectBox
                  dataSource={statusOptions}
                  displayExpr="label"
                  valueExpr="id"
                  value={editForm.status}
                  readOnly={taskPopupMode === "view"}
                  dropDownOptions={selectBoxDropDownOptions}
                  onValueChanged={(event) =>
                    setEditForm((prev) =>
                      prev ? { ...prev, status: (event.value as Status) ?? prev.status } : prev
                    )
                  }
                />
              </label>

              <label>
                Priority
                <SelectBox
                  dataSource={priorityOptions}
                  displayExpr="label"
                  valueExpr="id"
                  value={editForm.priority}
                  readOnly={taskPopupMode === "view"}
                  dropDownOptions={selectBoxDropDownOptions}
                  onValueChanged={(event) =>
                    setEditForm((prev) =>
                      prev
                        ? {
                            ...prev,
                            priority: (event.value as TaskPriority) ?? prev.priority,
                          }
                        : prev
                    )
                  }
                />
              </label>

              <label>
                Start Date
                <DateBox
                  type="date"
                  value={editForm.startDate || null}
                  readOnly={taskPopupMode === "view"}
                  onValueChanged={(event) =>
                    setEditForm((prev) =>
                      prev ? { ...prev, startDate: toDateOnly(event.value) } : prev
                    )
                  }
                />
              </label>

              <label>
                End Date
                <DateBox
                  type="date"
                  value={editForm.endDate || null}
                  readOnly={taskPopupMode === "view"}
                  onValueChanged={(event) =>
                    setEditForm((prev) =>
                      prev ? { ...prev, endDate: toDateOnly(event.value) } : prev
                    )
                  }
                />
              </label>

              <div className="popup-actions task-popup-actions">
                {taskPopupMode === "edit" ? (
                  <>
                    <Button
                      text="Cancel"
                      stylingMode="outlined"
                      onClick={closeTaskDetails}
                      disabled={requestLoading}
                    />
                    <Button
                      text={requestLoading ? "Saving..." : "Save"}
                      type="default"
                      onClick={handleUpdateTask}
                      disabled={requestLoading || !canUpdate || editLinksMissing}
                    />
                  </>
                ) : (
                  <Button text="Close" stylingMode="outlined" onClick={closeTaskDetails} />
                )}
              </div>
            </div>
          </div>
        )}
      </Modal>

      <Popup
        visible={showCreate}
        onHiding={() => {
          setShowCreate(false);
          setRequestError("");
        }}
        title="Create New Task"
        width={createPopupWidth}
        maxHeight="92vh"
        height="auto"
        dragEnabled={false}
        hideOnOutsideClick={false}
        showCloseButton
      >
        <form className="popup-form" onSubmit={handleCreateTask}>
          {requestError && <div className="form-error">{requestError}</div>}

          <label>
            Project
            <SelectBox
              dataSource={projects}
              displayExpr="name"
              valueExpr="id"
              value={createForm.projectId || null}
              dropDownOptions={selectBoxDropDownOptions}
              onValueChanged={(event) =>
                setCreateForm((prev) => ({
                  ...prev,
                  projectId: String(event.value ?? ""),
                  epicId: "",
                  sprintId: "",
                  assigneeId: "",
                }))
              }
              placeholder="Select project"
            />
          </label>

          <div className="form-grid-two">
            <label>
              Epic
              <SelectBox
                dataSource={createEpics}
                displayExpr="title"
                valueExpr="id"
                value={createForm.epicId || null}
                dropDownOptions={selectBoxDropDownOptions}
                onValueChanged={(event) =>
                  setCreateForm((prev) => ({
                    ...prev,
                    epicId: String(event.value ?? ""),
                  }))
                }
              />
            </label>

            <label>
              Sprint
              <SelectBox
                dataSource={createSprints}
                displayExpr="name"
                valueExpr="id"
                value={createForm.sprintId || null}
                dropDownOptions={selectBoxDropDownOptions}
                onValueChanged={(event) =>
                  setCreateForm((prev) => ({
                    ...prev,
                    sprintId: String(event.value ?? ""),
                  }))
                }
              />
            </label>
          </div>

          {createLinksMissing && (
            <div className="form-error">
              The selected project must have at least one epic and one sprint.
            </div>
          )}

          <label>
            Assignee
            <SelectBox
              dataSource={createAssigneeOptions}
              displayExpr="label"
              valueExpr="id"
              value={createForm.assigneeId || null}
              showClearButton={true}
              placeholder={canAssign ? "Select assignee (optional)" : "Select yourself (optional)"}
              dropDownOptions={selectBoxDropDownOptions}
              onValueChanged={(event) =>
                setCreateForm((prev) => ({
                  ...prev,
                  assigneeId: String(event.value ?? ""),
                }))
              }
            />
          </label>

          <label>
            Title
            <TextBox
              value={createForm.title}
              maxLength={100}
              onValueChanged={(event) =>
                setCreateForm((prev) => ({ ...prev, title: String(event.value ?? "") }))
              }
            />
          </label>

          <label>
            Description
            <TextArea
              value={createForm.description}
              maxLength={500}
              onValueChanged={(event) =>
                setCreateForm((prev) => ({ ...prev, description: String(event.value ?? "") }))
              }
              minHeight={80}
            />
          </label>

          <div className="form-grid-two">
            <label>
              Status
              <SelectBox
                dataSource={statusOptions}
                displayExpr="label"
                valueExpr="id"
                value={createForm.status}
                dropDownOptions={selectBoxDropDownOptions}
                onValueChanged={(event) =>
                  setCreateForm((prev) => ({
                    ...prev,
                    status: (event.value as Status) ?? Status.NotStarted,
                  }))
                }
              />
            </label>

            <label>
              Priority
              <SelectBox
                dataSource={priorityOptions}
                displayExpr="label"
                valueExpr="id"
                value={createForm.priority}
                dropDownOptions={selectBoxDropDownOptions}
                onValueChanged={(event) =>
                  setCreateForm((prev) => ({
                    ...prev,
                    priority: (event.value as TaskPriority) ?? TaskPriority.Medium,
                  }))
                }
              />
            </label>
          </div>

          <div className="form-grid-two">
            <label>
              Start Date
              <DateBox
                type="date"
                value={createForm.startDate || null}
                onValueChanged={(event) =>
                  setCreateForm((prev) => ({
                    ...prev,
                    startDate: toDateOnly(event.value),
                  }))
                }
              />
            </label>

            <label>
              End Date
              <DateBox
                type="date"
                value={createForm.endDate || null}
                onValueChanged={(event) =>
                  setCreateForm((prev) => ({
                    ...prev,
                    endDate: toDateOnly(event.value),
                  }))
                }
              />
            </label>
          </div>

          <div className="popup-actions">
            <Button text="Cancel" stylingMode="outlined" onClick={() => setShowCreate(false)} />
            <Button
              text={requestLoading ? "Creating..." : "Create Task"}
              type="default"
              useSubmitBehavior
              disabled={requestLoading || !canCreate}
            />
          </div>
        </form>
      </Popup>
    </div>
  );
}
