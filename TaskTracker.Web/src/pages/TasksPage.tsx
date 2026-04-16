import { useMemo, useState } from "react";
import { Button } from "devextreme-react/button";
import DataGrid, { Column, Paging } from "devextreme-react/data-grid";
import DateBox from "devextreme-react/date-box";
import Popup from "devextreme-react/popup";
import SelectBox from "devextreme-react/select-box";
import TextArea from "devextreme-react/text-area";
import TextBox from "devextreme-react/text-box";
import { useApp } from "../context/AppContext";
import { ApiError } from "../services/apiClient";
import { AppPermissions } from "../security/permissions";
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
  taskKey,
} from "../utils/taskPresentation";

type ViewMode = "list" | "kanban";

interface TaskFilters {
  status: Status | "all";
  priority: TaskPriority | "all";
}

interface TaskForm {
  title: string;
  description: string;
  status: Status;
  priority: TaskPriority;
  startDate: string;
  endDate: string;
}

const boardColumns: Array<{ id: Status; label: string }> = [
  { id: Status.NotStarted, label: "Not Started" },
  { id: Status.InProgress, label: "In Progress" },
  { id: Status.Completed, label: "Completed" },
  { id: Status.OnHold, label: "On Hold" },
  { id: Status.Cancelled, label: "Cancelled" },
];

function toDateOnly(value: unknown): string {
  if (!value) {
    return "";
  }

  const date = new Date(value as string | number | Date);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  return date.toISOString().split("T")[0];
}

function validateTaskForm(form: TaskForm): string | null {
  if (!form.title.trim()) {
    return "Title is required.";
  }

  if (form.title.trim().length > 100) {
    return "Title must be 100 characters or less.";
  }

  if (form.description.trim().length > 500) {
    return "Description must be 500 characters or less.";
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
    title: task.title ?? "",
    description: task.description ?? "",
    status: task.status,
    priority: task.priority,
    startDate: task.startDate ?? "",
    endDate: task.endDate ?? "",
  };
}

export function TasksPage() {
  const { tasks, loadingData, addTask, updateTask, deleteTask, hasPermission } = useApp();

  const canCreate = hasPermission(AppPermissions.TasksCreate);
  const canUpdate = hasPermission(AppPermissions.TasksUpdate);
  const canDelete = hasPermission(AppPermissions.TasksDelete);

  const [viewMode, setViewMode] = useState<ViewMode>("list");
  const [query, setQuery] = useState("");
  const [selectedTask, setSelectedTask] = useState<TaskDto | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [requestError, setRequestError] = useState("");
  const [requestLoading, setRequestLoading] = useState(false);
  const [filters, setFilters] = useState<TaskFilters>({
    status: "all",
    priority: "all",
  });

  const [createForm, setCreateForm] = useState<TaskForm>({
    title: "",
    description: "",
    status: Status.NotStarted,
    priority: TaskPriority.Medium,
    startDate: "",
    endDate: "",
  });

  const [editForm, setEditForm] = useState<TaskForm | null>(null);

  const filteredTasks = useMemo(() => {
    return tasks
      .filter((task) => {
        const term = query.trim().toLowerCase();
        const matchesText =
          !term ||
          (task.title ?? "").toLowerCase().includes(term) ||
          taskKey(task).toLowerCase().includes(term);
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

  const setApiError = (error: unknown, fallback: string) => {
    if (error instanceof ApiError) {
      setRequestError(error.message);
      return;
    }

    if (error instanceof Error) {
      setRequestError(error.message);
      return;
    }

    setRequestError(fallback);
  };

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
        title: "",
        description: "",
        status: Status.NotStarted,
        priority: TaskPriority.Medium,
        startDate: "",
        endDate: "",
      });
      setShowCreate(false);
    } catch (error) {
      setApiError(error, "Failed to create task.");
    } finally {
      setRequestLoading(false);
    }
  };

  const openTaskDetails = (task: TaskDto) => {
    setSelectedTask(task);
    setEditForm(toTaskForm(task));
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
      const updated = await updateTask(selectedTask.id, toUpdateDto(editForm));
      setSelectedTask(updated);
      setEditForm(toTaskForm(updated));
    } catch (error) {
      setApiError(error, "Failed to update task.");
    } finally {
      setRequestLoading(false);
    }
  };

  const handleDeleteTask = async () => {
    if (!selectedTask) {
      return;
    }

    if (!canDelete) {
      setRequestError("You do not have permission to delete tasks.");
      return;
    }

    setRequestLoading(true);
    setRequestError("");

    try {
      await deleteTask(selectedTask.id);
      setSelectedTask(null);
      setEditForm(null);
    } catch (error) {
      setApiError(error, "Failed to delete task.");
    } finally {
      setRequestLoading(false);
    }
  };

  return (
    <div className="page-stack">
      <section className="page-title-row">
        <div>
          <h1>Tasks</h1>
          <p className="page-subtitle">Manage and track all tasks from the backend</p>
        </div>
        <Button
          text="New Task"
          icon="plus"
          type="default"
          disabled={!canCreate}
          onClick={() => setShowCreate(true)}
        />
      </section>

      {!canCreate || !canUpdate || !canDelete ? (
        <div className="page-inline-info">
          You are in restricted mode. Some task actions are disabled based on your permissions.
        </div>
      ) : null}

      {loadingData && <div className="page-inline-info">Refreshing tasks...</div>}
      {requestError && <div className="form-error">{requestError}</div>}

      <section className="toolbar-row">
        <TextBox
          mode="search"
          placeholder="Search tasks..."
          value={query}
          onValueChanged={(event) => setQuery(String(event.value ?? ""))}
          width={280}
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
            onRowClick={(event) => openTaskDetails(event.data as TaskDto)}
          >
            <Column
              caption="Key"
              width={110}
              cellRender={({ data }: { data: TaskDto }) => taskKey(data)}
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
                      onClick={() => openTaskDetails(task)}
                    >
                      <small>{taskKey(task)}</small>
                      <strong>{task.title || "Untitled task"}</strong>
                      <span>{priorityLabel(task.priority)}</span>
                    </button>
                  ))}
              </div>
            </article>
          ))}
        </section>
      )}

      <Popup
        visible={selectedTask !== null}
        onHiding={() => {
          setSelectedTask(null);
          setEditForm(null);
          setRequestError("");
        }}
        title={selectedTask ? `${taskKey(selectedTask)} - ${selectedTask.title ?? "Untitled"}` : "Task details"}
        width={760}
        height="auto"
        showCloseButton
      >
        {selectedTask && editForm && (
          <div className="task-popup-grid">
            <div className="task-popup-main">
              <label>
                Title
                <TextBox
                  value={editForm.title}
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
                Status
                <SelectBox
                  dataSource={statusOptions}
                  displayExpr="label"
                  valueExpr="id"
                  value={editForm.status}
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
                  onValueChanged={(event) =>
                    setEditForm((prev) =>
                      prev ? { ...prev, endDate: toDateOnly(event.value) } : prev
                    )
                  }
                />
              </label>

              <div className="popup-actions task-popup-actions">
                <Button
                  text={requestLoading ? "Saving..." : "Save"}
                  type="default"
                  onClick={handleUpdateTask}
                  disabled={requestLoading || !canUpdate}
                />
                <Button
                  text={requestLoading ? "Deleting..." : "Delete"}
                  stylingMode="outlined"
                  type="danger"
                  onClick={handleDeleteTask}
                  disabled={requestLoading || !canDelete}
                />
              </div>
            </div>
          </div>
        )}
      </Popup>

      <Popup
        visible={showCreate}
        onHiding={() => {
          setShowCreate(false);
          setRequestError("");
        }}
        title="Create New Task"
        width={640}
        height="auto"
        showCloseButton
      >
        <form className="popup-form" onSubmit={handleCreateTask}>
          <label>
            Title
            <TextBox
              value={createForm.title}
              onValueChanged={(event) =>
                setCreateForm((prev) => ({ ...prev, title: String(event.value ?? "") }))
              }
            />
          </label>

          <label>
            Description
            <TextArea
              value={createForm.description}
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
