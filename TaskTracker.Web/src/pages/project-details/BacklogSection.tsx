import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Button } from "devextreme-react/button";
import DataGrid, { Column, Paging } from "devextreme-react/data-grid";
import DateBox from "devextreme-react/date-box";
import Popup from "devextreme-react/popup";
import SelectBox from "devextreme-react/select-box";
import TextArea from "devextreme-react/text-area";
import TextBox from "devextreme-react/text-box";
import { Modal } from "../../components/Modal";
import { useApp } from "../../context/AppContext";
import { getEpics } from "../../services/epicService";
import { getMembersByScope } from "../../services/memberService";
import { getSprints } from "../../services/sprintService";
import { loadTasks } from "../../services/taskService";
import {
  uploadAttachment,
  deleteAttachment,
  getAttachments,
  downloadAttachment,
  getAttachmentDownloadUrl,
} from "../../services/attachmentService";
import {
  ALLOWED_EXTENSIONS_ACCEPT,
  MAX_ATTACHMENTS_PER_TASK,
  validateFiles,
  formatFileSize,
} from "../../constants/attachments";
import type { BackendEpic, BackendSprint } from "../../types/app";
import type { ScopeMember } from "../../types/invitation";
import {
  Status,
  TaskPriority,
  type CreateTaskDto,
  type TaskAttachmentDto,
  type TaskDto,
  type UpdateTaskDto,
} from "../../types/task";
import {
  isTaskExpired,
  priorityLabel,
  priorityOptions,
  statusLabel,
  statusOptions,
} from "../../utils/taskPresentation";
import { toDateOnly } from "../../utils/toDateOnly";
import { getErrorMessage } from "../../utils/getErrorMessage";

type DetailPopupMode = "view" | "edit" | null;

interface TaskForm {
  epicId: string;
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

const emptyTaskForm = (): TaskForm => ({
  epicId: "",
  assigneeId: "",
  title: "",
  description: "",
  status: Status.NotStarted,
  priority: TaskPriority.Medium,
  startDate: "",
  endDate: "",
});

function validateTaskForm(form: TaskForm): string | null {
  if (!form.title.trim()) return "Title is required.";
  if (form.title.trim().length > 100) return "Title must be 100 characters or less.";
  if (form.description.trim().length > 500) return "Description must be 500 characters or less.";
  if (!form.startDate) return "Start date is required.";
  if (!form.endDate) return "End date is required.";
  if (form.startDate && form.endDate) {
    const s = new Date(`${form.startDate}T00:00:00`).getTime();
    const e = new Date(`${form.endDate}T00:00:00`).getTime();
    if (s > e) return "Start date cannot be after end date.";
  }
  return null;
}

function toCreateDto(projectId: string, form: TaskForm): CreateTaskDto {
  return {
    projectId,
    epicId: form.epicId.trim() || null,
    sprintId: null,
    assigneeId: form.assigneeId.trim() || null,
    title: form.title.trim(),
    description: form.description.trim() || undefined,
    status: form.status,
    priority: form.priority,
    startDate: form.startDate || null,
    endDate: form.endDate || null,
  };
}

function toUpdateDto(form: TaskForm, sprintId?: string | null): UpdateTaskDto {
  return {
    epicId: form.epicId.trim() || null,
    sprintId: sprintId !== undefined ? sprintId : null,
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
    epicId: task.epicId ?? "",
    assigneeId: task.assigneeId ?? "",
    title: task.title ?? "",
    description: task.description ?? "",
    status: task.status,
    priority: task.priority,
    startDate: task.startDate ?? "",
    endDate: task.endDate ?? "",
  };
}

type BacklogSectionProps = {
  projectId: string;
  canCreateTask: boolean;
  canUpdateTask: boolean;
  canDeleteTask: boolean;
  canAssignTask: boolean;
};

export function BacklogSection({
  projectId,
  canCreateTask,
  canUpdateTask,
  canDeleteTask,
  canAssignTask,
}: BacklogSectionProps) {
  const { addTask, updateTask, deleteTask, user } = useApp();

  const [backlogTasks, setBacklogTasks] = useState<TaskDto[]>([]);
  const [sprints, setSprints] = useState<BackendSprint[]>([]);
  const [epics, setEpics] = useState<BackendEpic[]>([]);
  const [members, setMembers] = useState<ScopeMember[]>([]);
  const [loading, setLoading] = useState(false);
  const [pageError, setPageError] = useState("");

  const [showCreate, setShowCreate] = useState(false);
  const [createForm, setCreateForm] = useState<TaskForm>(emptyTaskForm());
  const [createError, setCreateError] = useState("");
  const [createLoading, setCreateLoading] = useState(false);
  const [attachmentWarning, setAttachmentWarning] = useState("");
  const [createFiles, setCreateFiles] = useState<File[]>([]);
  const [createFileErrors, setCreateFileErrors] = useState<string[]>([]);
  const createFileInputRef = useRef<HTMLInputElement>(null);

  const [selectedTask, setSelectedTask] = useState<TaskDto | null>(null);
  const [popupMode, setPopupMode] = useState<DetailPopupMode>(null);
  const [editForm, setEditForm] = useState<TaskForm>(emptyTaskForm());
  const [editError, setEditError] = useState("");
  const [editLoading, setEditLoading] = useState(false);
  const [modalAttachments, setModalAttachments] = useState<TaskAttachmentDto[]>([]);
  const [modalAttachmentsLoading, setModalAttachmentsLoading] = useState(false);
  const [modalAttachmentUploadLoading, setModalAttachmentUploadLoading] = useState(false);
  const modalFileInputRef = useRef<HTMLInputElement>(null);

  const [moveTask, setMoveTask] = useState<TaskDto | null>(null);
  const [selectedSprintId, setSelectedSprintId] = useState<string>("");
  const [moveError, setMoveError] = useState("");
  const [moveLoading, setMoveLoading] = useState(false);

  const suppressNextRowClickRef = useRef(false);

  const dropDownOptions = useMemo(
    () => ({ wrapperAttr: { class: "modal-selectbox-overlay" } }),
    []
  );

  const loadBacklog = useCallback(async () => {
    setLoading(true);
    setPageError("");
    try {
      const [taskResult, sprintResult, epicResult, memberResult] = await Promise.all([
        loadTasks({ skip: 0, take: 500 }, projectId),
        getSprints(projectId),
        getEpics(projectId),
        getMembersByScope(1, projectId),
      ]);
      // Client-side filter: tasks with no sprint assigned = backlog
      setBacklogTasks(taskResult.data.filter((t) => !t.sprintId));
      setSprints(sprintResult);
      setEpics(epicResult);
      setMembers(memberResult.members);
    } catch (error) {
      setPageError(getErrorMessage(error, "Failed to load backlog."));
    } finally {
      setLoading(false);
    }
  }, [projectId]);

  useEffect(() => {
    void loadBacklog();
  }, [loadBacklog]);

  const epicNameById = useMemo(() => {
    const map = new Map<string, string>();
    epics.forEach((e) => map.set(e.id, e.title));
    return map;
  }, [epics]);

  const assigneeOptions = useMemo<AssigneeOption[]>(() => {
    let options = members.map((m) => ({
      id: m.userId,
      label: `${m.firstName} ${m.lastName}`.trim() + ` (${m.role})`,
    }));
    if (user?.id && !options.some((o) => o.id === user.id)) {
      options.push({ id: user.id, label: `${user.fullName} (Inherited)` });
    }
    if (!canAssignTask && user?.id) {
      options = options.filter((o) => o.id === user.id);
    }
    return options;
  }, [members, canAssignTask, user]);

  const assigneeNameById = useMemo(() => {
    const map = new Map<string, string>();
    assigneeOptions.forEach((o) => map.set(o.id, o.label));
    return map;
  }, [assigneeOptions]);

  const handleCreateFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const fileList = e.target.files;
    if (!fileList || fileList.length === 0) return;
    const next = [...createFiles, ...Array.from(fileList)];
    setCreateFiles(next);
    setCreateFileErrors(validateFiles(next, 0).errors);
    e.target.value = "";
  };

  const removeCreateFile = (index: number) => {
    setCreateFiles((prev) => {
      const next = prev.filter((_, i) => i !== index);
      setCreateFileErrors(validateFiles(next, 0).errors);
      return next;
    });
  };

  const loadModalAttachments = useCallback(async (taskId: number) => {
    setModalAttachmentsLoading(true);
    try {
      setModalAttachments(await getAttachments(taskId));
    } catch {
      setModalAttachments([]);
    } finally {
      setModalAttachmentsLoading(false);
    }
  }, []);

  const handleModalAttachmentUpload = async (taskId: number, files: FileList | null) => {
    if (!files || files.length === 0) return;
    const fileArr = Array.from(files);
    const { valid, errors } = validateFiles(fileArr, modalAttachments.length);
    if (errors.length > 0) { setEditError(errors.join(" ")); return; }
    setModalAttachmentUploadLoading(true);
    setEditError("");
    try {
      const results = await Promise.allSettled(valid.map((f) => uploadAttachment(taskId, f)));
      const failed = results.filter((r) => r.status === "rejected");
      if (failed.length > 0) setEditError(`${failed.length} file(s) failed to upload.`);
      await loadModalAttachments(taskId);
    } catch (error) {
      setEditError(getErrorMessage(error, "Failed to upload attachments."));
    } finally {
      setModalAttachmentUploadLoading(false);
      if (modalFileInputRef.current) modalFileInputRef.current.value = "";
    }
  };

  const handleModalAttachmentDelete = async (attachmentId: string, taskId: number) => {
    if (!window.confirm("Delete this attachment?")) return;
    setEditError("");
    try {
      await deleteAttachment(attachmentId, taskId);
      setModalAttachments((prev) => prev.filter((a) => a.id !== attachmentId));
    } catch (error) {
      setEditError(getErrorMessage(error, "Failed to delete attachment."));
    }
  };

  const handleCreateTask = async () => {
    setCreateError("");
    setAttachmentWarning("");

    const validation = validateTaskForm(createForm);
    if (validation) { setCreateError(validation); return; }

    const attachErrors = validateFiles(createFiles, 0).errors;
    if (attachErrors.length > 0) { setCreateError(attachErrors.join(" ")); return; }

    setCreateLoading(true);
    try {
      const created = await addTask(toCreateDto(projectId, createForm));
      if (createFiles.length > 0 && created?.id) {
        const results = await Promise.allSettled(
          createFiles.map((f) => uploadAttachment(created.id, f))
        );
        const failed = results.filter((r) => r.status === "rejected");
        if (failed.length > 0) {
          setAttachmentWarning(`Task created, but ${failed.length} file(s) failed to upload.`);
        }
      }
      // Refresh backlog list
      setBacklogTasks((prev) => [created, ...prev]);
      setCreateForm(emptyTaskForm());
      setCreateFiles([]);
      setCreateFileErrors([]);
      setShowCreate(false);
    } catch (error) {
      setCreateError(getErrorMessage(error, "Failed to create task."));
    } finally {
      setCreateLoading(false);
    }
  };

  const openTaskModal = (task: TaskDto, mode: DetailPopupMode) => {
    setSelectedTask(task);
    setPopupMode(mode);
    setEditForm(toTaskForm(task));
    setEditError("");
    setModalAttachments([]);
    void loadModalAttachments(task.id);
  };

  const closeTaskModal = () => {
    setSelectedTask(null);
    setPopupMode(null);
    setEditForm(emptyTaskForm());
    setEditError("");
    setModalAttachments([]);
  };

  const handleUpdateTask = async () => {
    if (!selectedTask) return;
    setEditError("");
    const validation = validateTaskForm(editForm);
    if (validation) { setEditError(validation); return; }

    setEditLoading(true);
    try {
      // Preserve the current sprintId (null for backlog, or whatever the task had)
      const updated = await updateTask(
        selectedTask.id,
        selectedTask.projectId,
        toUpdateDto(editForm, selectedTask.sprintId ?? null)
      );
      // If task still has no sprint, keep it in backlog; otherwise remove it
      setBacklogTasks((prev) =>
        updated.sprintId
          ? prev.filter((t) => t.id !== updated.id)
          : prev.map((t) => (t.id === updated.id ? updated : t))
      );
      closeTaskModal();
    } catch (error) {
      setEditError(getErrorMessage(error, "Failed to update task."));
    } finally {
      setEditLoading(false);
    }
  };

  const handleDeleteTask = async (task: TaskDto) => {
    if (!window.confirm(`Delete task "${task.title || `#${task.id}`}"?`)) return;
    setPageError("");
    try {
      await deleteTask(task.id, task.projectId);
      setBacklogTasks((prev) => prev.filter((t) => t.id !== task.id));
      if (selectedTask?.id === task.id) closeTaskModal();
    } catch (error) {
      setPageError(getErrorMessage(error, "Failed to delete task."));
    }
  };

  const openMoveToSprint = (task: TaskDto) => {
    setMoveTask(task);
    setSelectedSprintId("");
    setMoveError("");
  };

  const handleMoveToSprint = async () => {
    if (!moveTask) return;
    if (!selectedSprintId) { setMoveError("Please select a sprint."); return; }
    setMoveError("");
    setMoveLoading(true);
    try {
      await updateTask(
        moveTask.id,
        moveTask.projectId,
        toUpdateDto(toTaskForm(moveTask), selectedSprintId)
      );
      setBacklogTasks((prev) => prev.filter((t) => t.id !== moveTask.id));
      setMoveTask(null);
    } catch (error) {
      setMoveError(getErrorMessage(error, "Failed to move task to sprint."));
    } finally {
      setMoveLoading(false);
    }
  };

  return (
    <section className="card">
      <div className="page-title-row">
        <div>
          <h2>Task Backlog</h2>
          <p className="page-subtitle" style={{ margin: 0 }}>
            Tasks not yet assigned to a sprint
          </p>
        </div>
        <Button
          text="New Task"
          icon="plus"
          type="default"
          disabled={!canCreateTask}
          onClick={() => {
            setCreateError("");
            setAttachmentWarning("");
            setCreateFiles([]);
            setCreateFileErrors([]);
            setCreateForm(emptyTaskForm());
            setShowCreate(true);
          }}
        />
      </div>

      {pageError && <div className="form-error">{pageError}</div>}
      {loading && <div className="page-inline-info">Loading backlog...</div>}

      <DataGrid
        dataSource={backlogTasks}
        keyExpr="id"
        showBorders={false}
        rowAlternationEnabled
        hoverStateEnabled
        columnAutoWidth={false}
        noDataText="No backlog tasks. Click 'New Task' to create one."
        onRowClick={(event) => {
          if (suppressNextRowClickRef.current) {
            suppressNextRowClickRef.current = false;
            return;
          }
          if (event.rowType !== "data" || !event.data) return;
          const target = event.event?.target as HTMLElement | null;
          if (target?.closest(".inline-actions")) return;
          openTaskModal(event.data as TaskDto, "view");
        }}
      >
        <Column dataField="taskCode" caption="Code" width={110} />
        <Column
          dataField="title"
          caption="Title"
          cellRender={({ data }: { data: TaskDto }) => data.title || "Untitled task"}
        />
        <Column
          caption="Status"
          width={130}
          cellRender={({ data }: { data: TaskDto }) => statusLabel(data.status)}
        />
        <Column
          caption="Priority"
          width={110}
          cellRender={({ data }: { data: TaskDto }) => priorityLabel(data.priority)}
        />
        <Column
          caption="Assignee"
          width={160}
          cellRender={({ data }: { data: TaskDto }) =>
            data.assigneeId ? (assigneeNameById.get(data.assigneeId) ?? data.assigneeId) : "—"
          }
        />
        <Column
          caption="Start"
          width={110}
          cellRender={({ data }: { data: TaskDto }) =>
            data.startDate ? new Date(`${data.startDate}T00:00:00`).toLocaleDateString() : "—"
          }
        />
        <Column
          caption="End"
          width={110}
          cellRender={({ data }: { data: TaskDto }) =>
            data.endDate ? new Date(`${data.endDate}T00:00:00`).toLocaleDateString() : "—"
          }
        />
        <Column
          caption="Expired"
          width={90}
          cellRender={({ data }: { data: TaskDto }) =>
            isTaskExpired(data) ? (
              <span className="badge badge-expired">Expired</span>
            ) : (
              <span className="badge badge-active">Active</span>
            )
          }
        />
        <Column
          caption="Actions"
          width={280}
          allowSorting={false}
          allowFiltering={false}
          cellRender={({ data }: { data: TaskDto }) => (
            <div
              className="inline-actions"
              onClick={(e) => { suppressNextRowClickRef.current = true; e.stopPropagation(); }}
              onMouseDown={(e) => { suppressNextRowClickRef.current = true; e.stopPropagation(); }}
              onPointerDown={(e) => { suppressNextRowClickRef.current = true; e.stopPropagation(); }}
            >
              <Button
                text="View"
                stylingMode="text"
                onClick={(e) => {
                  suppressNextRowClickRef.current = true;
                  e?.event?.preventDefault?.();
                  e?.event?.stopPropagation?.();
                  openTaskModal(data, "view");
                }}
              />
              <Button
                text="Edit"
                stylingMode="text"
                disabled={!canUpdateTask}
                onClick={(e) => {
                  suppressNextRowClickRef.current = true;
                  e?.event?.preventDefault?.();
                  e?.event?.stopPropagation?.();
                  openTaskModal(data, "edit");
                }}
              />
              <Button
                text="Delete"
                type="danger"
                stylingMode="text"
                disabled={!canDeleteTask}
                onClick={(e) => {
                  suppressNextRowClickRef.current = true;
                  e?.event?.preventDefault?.();
                  e?.event?.stopPropagation?.();
                  void handleDeleteTask(data);
                }}
              />
              <Button
                text="→ Sprint"
                stylingMode="outlined"
                disabled={!canUpdateTask || sprints.length === 0}
                hint={sprints.length === 0 ? "No sprints in this project yet" : "Move to a sprint"}
                onClick={(e) => {
                  suppressNextRowClickRef.current = true;
                  e?.event?.preventDefault?.();
                  e?.event?.stopPropagation?.();
                  openMoveToSprint(data);
                }}
              />
            </div>
          )}
        />
        <Paging enabled pageSize={10} />
      </DataGrid>

      <Popup
        visible={showCreate}
        onHiding={() => {
          setShowCreate(false);
          setCreateError("");
          setCreateFiles([]);
          setCreateFileErrors([]);
          setAttachmentWarning("");
        }}
        title="New Backlog Task"
        width={620}
        maxHeight="92vh"
        height="auto"
        dragEnabled={false}
        hideOnOutsideClick={false}
        showCloseButton
      >
        <div className="popup-form">
          {createError && <div className="form-error">{createError}</div>}

          <label>
            Epic
            <SelectBox
              dataSource={epics}
              displayExpr="title"
              valueExpr="id"
              value={createForm.epicId || null}
              showClearButton
              placeholder="Select epic (optional)"
              dropDownOptions={dropDownOptions}
              onValueChanged={(e) =>
                setCreateForm((prev) => ({ ...prev, epicId: String(e.value ?? "") }))
              }
            />
          </label>

          <label>
            Assignee
            <SelectBox
              dataSource={assigneeOptions}
              displayExpr="label"
              valueExpr="id"
              value={createForm.assigneeId || null}
              showClearButton
              placeholder={canAssignTask ? "Select assignee (optional)" : "Select yourself (optional)"}
              dropDownOptions={dropDownOptions}
              onValueChanged={(e) =>
                setCreateForm((prev) => ({ ...prev, assigneeId: String(e.value ?? "") }))
              }
            />
          </label>

          <label>
            Title <span style={{ color: "var(--danger)" }}>*</span>
            <TextBox
              value={createForm.title}
              maxLength={100}
              onValueChanged={(e) =>
                setCreateForm((prev) => ({ ...prev, title: String(e.value ?? "") }))
              }
            />
          </label>

          <label>
            Description
            <TextArea
              value={createForm.description}
              maxLength={500}
              minHeight={80}
              onValueChanged={(e) =>
                setCreateForm((prev) => ({ ...prev, description: String(e.value ?? "") }))
              }
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
                dropDownOptions={dropDownOptions}
                onValueChanged={(e) =>
                  setCreateForm((prev) => ({
                    ...prev,
                    status: (e.value as Status) ?? Status.NotStarted,
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
                dropDownOptions={dropDownOptions}
                onValueChanged={(e) =>
                  setCreateForm((prev) => ({
                    ...prev,
                    priority: (e.value as TaskPriority) ?? TaskPriority.Medium,
                  }))
                }
              />
            </label>
          </div>

          <div className="form-grid-two">
            <label>
              Start Date <span style={{ color: "var(--danger)" }}>*</span>
              <DateBox
                type="date"
                value={createForm.startDate || null}
                dropDownOptions={dropDownOptions}
                onValueChanged={(e) =>
                  setCreateForm((prev) => ({ ...prev, startDate: toDateOnly(e.value) }))
                }
              />
            </label>
            <label>
              End Date <span style={{ color: "var(--danger)" }}>*</span>
              <DateBox
                type="date"
                value={createForm.endDate || null}
                dropDownOptions={dropDownOptions}
                onValueChanged={(e) =>
                  setCreateForm((prev) => ({ ...prev, endDate: toDateOnly(e.value) }))
                }
              />
            </label>
          </div>

          <div className="task-attachments-section">
            <label>
              Attachments{" "}
              <span className="attachment-upload-note">
                (optional, max {MAX_ATTACHMENTS_PER_TASK} files, 10 MB each)
              </span>
            </label>
            <input
              ref={createFileInputRef}
              type="file"
              multiple
              accept={ALLOWED_EXTENSIONS_ACCEPT}
              onChange={handleCreateFileSelect}
            />
            {createFileErrors.length > 0 && (
              <div className="form-error">
                {createFileErrors.map((err, i) => (
                  <div key={i}>{err}</div>
                ))}
              </div>
            )}
            {createFiles.length > 0 && (
              <div className="attachment-list">
                {createFiles.map((file, i) => (
                  <div key={i} className="attachment-item attachment-item--pending">
                    <span className="attachment-file">
                      <span className="attachment-file-name">{file.name}</span>
                      <span className="attachment-meta">{formatFileSize(file.size)}</span>
                    </span>
                    <Button icon="close" stylingMode="text" hint="Remove" onClick={() => removeCreateFile(i)} />
                  </div>
                ))}
              </div>
            )}
          </div>

          {attachmentWarning && <div className="page-inline-info">{attachmentWarning}</div>}

          <div className="popup-actions">
            <Button
              text="Cancel"
              stylingMode="outlined"
              disabled={createLoading}
              onClick={() => {
                setShowCreate(false);
                setCreateFiles([]);
                setCreateFileErrors([]);
                setAttachmentWarning("");
              }}
            />
            <Button
              text={createLoading ? "Creating..." : "Create Task"}
              type="default"
              disabled={createLoading || !canCreateTask}
              onClick={handleCreateTask}
            />
          </div>
        </div>
      </Popup>

      <Modal
        visible={selectedTask !== null}
        onClose={closeTaskModal}
        title={
          selectedTask
            ? popupMode === "edit"
              ? `Edit: ${selectedTask.title ?? "Untitled"}`
              : `Task: ${selectedTask.title ?? "Untitled"}`
            : "Task"
        }
        width={700}
      >
        {selectedTask && (
          <div className="task-popup-grid">
            {/* Main column */}
            <div className="task-popup-main">
              {editError && <div className="form-error">{editError}</div>}

              <label>
                Assignee
                {popupMode === "edit" ? (
                  <SelectBox
                    dataSource={assigneeOptions}
                    displayExpr="label"
                    valueExpr="id"
                    value={editForm.assigneeId || null}
                    readOnly={!canAssignTask}
                    showClearButton={canAssignTask}
                    placeholder={canAssignTask ? "Select assignee (optional)" : "No assign permission"}
                    dropDownOptions={dropDownOptions}
                    onValueChanged={(e) =>
                      setEditForm((prev) => ({ ...prev, assigneeId: String(e.value ?? "") }))
                    }
                  />
                ) : (
                  <TextBox
                    value={
                      editForm.assigneeId
                        ? assigneeNameById.get(editForm.assigneeId) ?? editForm.assigneeId
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
                  readOnly={popupMode === "view"}
                  onValueChanged={(e) =>
                    setEditForm((prev) => ({ ...prev, title: String(e.value ?? "") }))
                  }
                />
              </label>

              <label>
                Description
                <TextArea
                  value={editForm.description}
                  maxLength={500}
                  minHeight={110}
                  readOnly={popupMode === "view"}
                  onValueChanged={(e) =>
                    setEditForm((prev) => ({ ...prev, description: String(e.value ?? "") }))
                  }
                />
              </label>
            </div>

            <div className="task-popup-side">
              <label>
                Epic
                {popupMode === "edit" ? (
                  <SelectBox
                    dataSource={epics}
                    displayExpr="title"
                    valueExpr="id"
                    value={editForm.epicId || null}
                    showClearButton
                    placeholder="Select epic (optional)"
                    dropDownOptions={dropDownOptions}
                    onValueChanged={(e) =>
                      setEditForm((prev) => ({ ...prev, epicId: String(e.value ?? "") }))
                    }
                  />
                ) : (
                  <TextBox
                    value={editForm.epicId ? epicNameById.get(editForm.epicId) ?? "—" : "—"}
                    readOnly
                  />
                )}
              </label>

              <label>
                Status
                <SelectBox
                  dataSource={statusOptions}
                  displayExpr="label"
                  valueExpr="id"
                  value={editForm.status}
                  readOnly={popupMode === "view"}
                  dropDownOptions={dropDownOptions}
                  onValueChanged={(e) =>
                    setEditForm((prev) => ({
                      ...prev,
                      status: (e.value as Status) ?? prev.status,
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
                  value={editForm.priority}
                  readOnly={popupMode === "view"}
                  dropDownOptions={dropDownOptions}
                  onValueChanged={(e) =>
                    setEditForm((prev) => ({
                      ...prev,
                      priority: (e.value as TaskPriority) ?? prev.priority,
                    }))
                  }
                />
              </label>

              <label>
                Start Date
                <DateBox
                  type="date"
                  value={editForm.startDate || null}
                  readOnly={popupMode === "view"}
                  dropDownOptions={dropDownOptions}
                  onValueChanged={(e) =>
                    setEditForm((prev) => ({ ...prev, startDate: toDateOnly(e.value) }))
                  }
                />
              </label>

              <label>
                End Date
                <DateBox
                  type="date"
                  value={editForm.endDate || null}
                  readOnly={popupMode === "view"}
                  dropDownOptions={dropDownOptions}
                  onValueChanged={(e) =>
                    setEditForm((prev) => ({ ...prev, endDate: toDateOnly(e.value) }))
                  }
                />
              </label>

              <div className="task-attachments-section task-attachments-section--full-width">
                <h4 className="task-attachments-title">
                  Attachments ({modalAttachments.length}/{MAX_ATTACHMENTS_PER_TASK})
                </h4>
                {modalAttachmentsLoading && (
                  <div className="page-inline-info">Loading attachments...</div>
                )}
                {modalAttachments.length > 0 && (
                  <div className="attachment-list">
                    {modalAttachments.map((att) => (
                      <div key={att.id} className="attachment-item">
                        <span className="attachment-file">
                          <a
                            href={getAttachmentDownloadUrl(att.taskId, att.id)}
                            className="attachment-file-link"
                            onClick={(e) => {
                              e.preventDefault();
                              void downloadAttachment(att.taskId, att.id, att.fileName);
                            }}
                          >
                            {att.fileName}
                          </a>
                        </span>
                        <span className="attachment-meta">{formatFileSize(att.fileSizeBytes)}</span>
                        {popupMode === "edit" && canUpdateTask && (
                          <Button
                            icon="trash"
                            stylingMode="text"
                            hint="Delete attachment"
                            onClick={() => void handleModalAttachmentDelete(att.id, selectedTask!.id)}
                          />
                        )}
                      </div>
                    ))}
                  </div>
                )}
                {!modalAttachmentsLoading && modalAttachments.length === 0 && (
                  <div className="page-inline-info">No attachments.</div>
                )}
                {popupMode === "edit" && canUpdateTask && modalAttachments.length < MAX_ATTACHMENTS_PER_TASK && (
                  <div className="attachment-upload-toolbar">
                    <input
                      ref={modalFileInputRef}
                      type="file"
                      multiple
                      accept={ALLOWED_EXTENSIONS_ACCEPT}
                      onChange={(e) => void handleModalAttachmentUpload(selectedTask!.id, e.target.files)}
                      disabled={modalAttachmentUploadLoading}
                    />
                    {modalAttachmentUploadLoading && (
                      <span className="attachment-upload-note">Uploading...</span>
                    )}
                  </div>
                )}
              </div>

              <div className="popup-actions task-popup-actions">
                {popupMode === "edit" ? (
                  <>
                    <Button
                      text="Cancel"
                      stylingMode="outlined"
                      disabled={editLoading}
                      onClick={closeTaskModal}
                    />
                    {canDeleteTask && (
                      <Button
                        text="Delete"
                        type="danger"
                        stylingMode="outlined"
                        disabled={editLoading}
                        onClick={() => void handleDeleteTask(selectedTask!)}
                      />
                    )}
                    <Button
                      text={editLoading ? "Saving..." : "Save"}
                      type="default"
                      disabled={editLoading || !canUpdateTask}
                      onClick={handleUpdateTask}
                    />
                  </>
                ) : (
                  <>
                    <Button text="Close" stylingMode="outlined" onClick={closeTaskModal} />
                    {canUpdateTask && (
                      <Button
                        text="Edit"
                        type="default"
                        onClick={() => setPopupMode("edit")}
                      />
                    )}
                  </>
                )}
              </div>
            </div>
          </div>
        )}
      </Modal>

      <Popup
        visible={moveTask !== null}
        onHiding={() => {
          setMoveTask(null);
          setMoveError("");
          setSelectedSprintId("");
        }}
        title={`Move "${moveTask?.title ?? "task"}" to Sprint`}
        width={400}
        height="auto"
        dragEnabled={false}
        hideOnOutsideClick={false}
        showCloseButton
      >
        <div className="move-to-sprint-form">
          {moveError && <div className="form-error">{moveError}</div>}

          <label>
            Select Sprint
            <SelectBox
              dataSource={sprints}
              displayExpr="name"
              valueExpr="id"
              value={selectedSprintId || null}
              placeholder="Choose a sprint..."
              dropDownOptions={dropDownOptions}
              onValueChanged={(e) => setSelectedSprintId(String(e.value ?? ""))}
            />
          </label>

          {sprints.length === 0 && (
            <div className="page-inline-info">
              No sprints exist for this project yet. Create a sprint first.
            </div>
          )}

          <div className="popup-actions">
            <Button
              text="Cancel"
              stylingMode="outlined"
              disabled={moveLoading}
              onClick={() => { setMoveTask(null); setMoveError(""); setSelectedSprintId(""); }}
            />
            <Button
              text={moveLoading ? "Moving..." : "Move to Sprint"}
              type="default"
              disabled={moveLoading || !selectedSprintId || sprints.length === 0}
              onClick={handleMoveToSprint}
            />
          </div>
        </div>
      </Popup>
    </section>
  );
}
