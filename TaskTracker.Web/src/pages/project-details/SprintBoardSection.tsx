import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Button } from "devextreme-react/button";
import DateBox from "devextreme-react/date-box";
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
  deleteAttachment,
  downloadAttachment,
  getAttachmentDownloadUrl,
  getAttachments,
  uploadAttachment,
} from "../../services/attachmentService";
import {
  ALLOWED_EXTENSIONS_ACCEPT,
  MAX_ATTACHMENTS_PER_TASK,
  formatFileSize,
  validateFiles,
} from "../../constants/attachments";
import type { BackendEpic, BackendSprint } from "../../types/app";
import type { ScopeMember } from "../../types/invitation";
import {
  Status,
  TaskPriority,
  type TaskAttachmentDto,
  type TaskDto,
  type UpdateTaskDto,
} from "../../types/task";
import {
  isTaskExpired,
  priorityLabel,
  priorityOptions,
  statusOptions,
} from "../../utils/taskPresentation";
import { toDateOnly } from "../../utils/toDateOnly";
import { getErrorMessage } from "../../utils/getErrorMessage";


type DetailPopupMode = "view" | "edit" | null;

interface TaskForm {
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
  if (!form.title.trim()) return "Title is required.";
  if (form.title.trim().length > 100) return "Title must be 100 characters or less.";
  if (form.description.trim().length > 500) return "Description must be 500 characters or less.";
  if (!form.startDate) return "Start date is required.";
  if (!form.endDate) return "End date is required.";
  if (form.startDate && form.endDate) {
    if (
      new Date(`${form.startDate}T00:00:00`).getTime() >
      new Date(`${form.endDate}T00:00:00`).getTime()
    )
      return "Start date cannot be after end date.";
  }
  return null;
}

function toUpdateDto(form: TaskForm): UpdateTaskDto {
  return {
    epicId: form.epicId.trim() || null,
    sprintId: form.sprintId.trim() || null,
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

type SprintBoardSectionProps = {
  projectId: string;
  canUpdateTask: boolean;
  canDeleteTask: boolean;
  canAssignTask: boolean;
};

export function SprintBoardSection({
  projectId,
  canUpdateTask,
  canDeleteTask,
  canAssignTask,
}: SprintBoardSectionProps) {
  const { updateTask, deleteTask, user } = useApp();

  const [sprints, setSprints] = useState<BackendSprint[]>([]);
  const [selectedSprintId, setSelectedSprintId] = useState<string>("");
  const [sprintsLoading, setSprintsLoading] = useState(false);
  const [boardTasks, setBoardTasks] = useState<TaskDto[]>([]);
  const [tasksLoading, setTasksLoading] = useState(false);
  const [pageError, setPageError] = useState("");

  const [epics, setEpics] = useState<BackendEpic[]>([]);
  const [members, setMembers] = useState<ScopeMember[]>([]);

  const [selectedTask, setSelectedTask] = useState<TaskDto | null>(null);
  const [popupMode, setPopupMode] = useState<DetailPopupMode>(null);
  const [editForm, setEditForm] = useState<TaskForm>(toTaskForm({} as TaskDto));
  const [editError, setEditError] = useState("");
  const [editLoading, setEditLoading] = useState(false);
  const [modalAttachments, setModalAttachments] = useState<TaskAttachmentDto[]>([]);
  const [modalAttachmentsLoading, setModalAttachmentsLoading] = useState(false);
  const [modalAttachmentUploadLoading, setModalAttachmentUploadLoading] = useState(false);
  const modalFileInputRef = useRef<HTMLInputElement>(null);

  const dropDownOptions = useMemo(
    () => ({ wrapperAttr: { class: "modal-selectbox-overlay" } }),
    []
  );

  useEffect(() => {
    const init = async () => {
      setSprintsLoading(true);
      try {
        const [sprintResult, epicResult, memberResult] = await Promise.all([
          getSprints(projectId),
          getEpics(projectId),
          getMembersByScope(1, projectId),
        ]);
        setSprints(sprintResult);
        setEpics(epicResult);
        setMembers(memberResult.members);
      } catch (error) {
        setPageError(getErrorMessage(error, "Failed to load sprint board data."));
      } finally {
        setSprintsLoading(false);
      }
    };
    void init();
  }, [projectId]);

  useEffect(() => {
    if (!selectedSprintId) {
      setBoardTasks([]);
      return;
    }
    const fetch = async () => {
      setTasksLoading(true);
      setPageError("");
      try {
        const result = await loadTasks({ skip: 0, take: 500 }, projectId, undefined, selectedSprintId);
        setBoardTasks(result.data);
      } catch (error) {
        setPageError(getErrorMessage(error, "Failed to load sprint tasks."));
        setBoardTasks([]);
      } finally {
        setTasksLoading(false);
      }
    };
    void fetch();
  }, [projectId, selectedSprintId]);

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

  const memberNameById = useMemo(() => {
    const map = new Map<string, string>();
    members.forEach((m) => {
      map.set(m.userId, `${m.firstName} ${m.lastName}`.trim());
    });
    if (user?.id && !map.has(user.id)) {
      map.set(user.id, user.fullName);
    }
    return map;
  }, [members, user]);

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
    const { valid, errors } = validateFiles(Array.from(files), modalAttachments.length);
    if (errors.length > 0) { setEditError(errors.join(" ")); return; }
    setModalAttachmentUploadLoading(true);
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
    try {
      await deleteAttachment(attachmentId, taskId);
      setModalAttachments((prev) => prev.filter((a) => a.id !== attachmentId));
    } catch (error) {
      setEditError(getErrorMessage(error, "Failed to delete attachment."));
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
      const updated = await updateTask(selectedTask.id, selectedTask.projectId, toUpdateDto(editForm));
      // Keep on board if still in the same sprint, remove if sprint changed
      setBoardTasks((prev) =>
        updated.sprintId === selectedSprintId
          ? prev.map((t) => (t.id === updated.id ? updated : t))
          : prev.filter((t) => t.id !== updated.id)
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
      setBoardTasks((prev) => prev.filter((t) => t.id !== task.id));
      if (selectedTask?.id === task.id) closeTaskModal();
    } catch (error) {
      setPageError(getErrorMessage(error, "Failed to delete task."));
    }
  };

  return (
    <section className="card">
      <div className="page-title-row sprint-board-title-row">
        <div>
          <h2>Sprint Board</h2>
          <p className="page-subtitle sprint-board-subtitle">
            Visual task board by status for a sprint
          </p>
        </div>
      </div>

      <div className="sprint-board-header">
        <span className="sprint-board-select-label">
          Select Sprint:
        </span>
        <SelectBox
          dataSource={sprints}
          displayExpr="name"
          valueExpr="id"
          value={selectedSprintId || null}
          placeholder={sprintsLoading ? "Loading sprints..." : "Choose a sprint to view its board…"}
          showClearButton
          width={320}
          onValueChanged={(e) => setSelectedSprintId(String(e.value ?? ""))}
        />
        {tasksLoading && (
          <span className="page-inline-info sprint-board-loading-text">Loading tasks...</span>
        )}
      </div>

      {pageError && <div className="form-error">{pageError}</div>}

      {!selectedSprintId && !sprintsLoading && (
        <div className="page-inline-info sprint-board-empty-info">
          {sprints.length === 0
            ? "No sprints in this project yet. Create a sprint in the Sprints section below."
            : "Select a sprint above to view its Sprint Board."}
        </div>
      )}

      {selectedSprintId && !tasksLoading && (
        <>
          {boardTasks.length === 0 ? (
            <div className="page-inline-info sprint-board-empty-info">
              No tasks in this sprint yet. Move tasks from the Backlog using the "→ Sprint" button.
            </div>
          ) : (
            <section className="kanban-grid kanban-grid-wide">
              {boardColumns.map((col) => {
                const colTasks = boardTasks.filter((t) => t.status === col.id);
                return (
                  <article className="kanban-column" key={col.id}>
                    <h3>
                      {col.label}
                      <span className="sprint-board-col-count">
                        ({colTasks.length})
                      </span>
                    </h3>
                    <div className="kanban-list">
                      {colTasks.length === 0 && (
                        <p className="sprint-board-empty-col">
                          No tasks
                        </p>
                      )}
                      {colTasks.map((task) => (
                        <div
                          key={task.id}
                          className="kanban-card"
                          role="button"
                          tabIndex={0}
                          onClick={() => openTaskModal(task, "view")}
                          onKeyDown={(e) => {
                            if (e.key === "Enter" || e.key === " ") openTaskModal(task, "view");
                          }}
                        >
                          <span className="kanban-card-code">
                            {task.taskCode || `TASK-${task.id}`}
                          </span>
                          <strong className="sprint-board-card-title">
                            {task.title || "Untitled task"}
                          </strong>
                          <span className="sprint-board-card-priority">
                            {priorityLabel(task.priority)}
                          </span>
                          {task.assigneeId && memberNameById.get(task.assigneeId) && (
                            <span className="sprint-board-card-assignee">
                              <i className="dx-icon dx-icon-user" style={{ fontSize: '0.8rem', marginRight: '0.2rem' }}></i>
                              {memberNameById.get(task.assigneeId)}
                            </span>
                          )}
                          {isTaskExpired(task) && (
                            <span className="badge badge-expired sprint-board-card-expired">
                              Expired
                            </span>
                          )}
                          <div
                            className="sprint-board-card-actions"
                            onClick={(e) => e.stopPropagation()}
                            onKeyDown={(e) => e.stopPropagation()}
                          >
                            {canUpdateTask && (
                              <Button
                                text="Edit"
                                stylingMode="outlined"
                                onClick={(e) => {
                                  e?.event?.preventDefault?.();
                                  e?.event?.stopPropagation?.();
                                  openTaskModal(task, "edit");
                                }}
                              />
                            )}
                            {canDeleteTask && (
                              <Button
                                text="Delete"
                                type="danger"
                                stylingMode="text"
                                onClick={(e) => {
                                  e?.event?.preventDefault?.();
                                  e?.event?.stopPropagation?.();
                                  void handleDeleteTask(task);
                                }}
                              />
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  </article>
                );
              })}
            </section>
          )}
        </>
      )}

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
                        ? (memberNameById.get(editForm.assigneeId) ?? editForm.assigneeId)
                        : "Unassigned"
                    }
                    readOnly
                  />
                )}
              </label>

              <label>
                Reporter
                <TextBox
                  value={
                    selectedTask.reporterUser?.fullName ??
                    memberNameById.get(selectedTask.reporterId) ??
                    selectedTask.reporterId
                  }
                  readOnly
                />
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
                    value={editForm.epicId ? (epicNameById.get(editForm.epicId) ?? "—") : "—"}
                    readOnly
                  />
                )}
              </label>

              <label>
                Sprint
                {popupMode === "edit" ? (
                  <SelectBox
                    dataSource={sprints}
                    displayExpr="name"
                    valueExpr="id"
                    value={editForm.sprintId || null}
                    showClearButton
                    placeholder="Select sprint (optional)"
                    dropDownOptions={dropDownOptions}
                    onValueChanged={(e) =>
                      setEditForm((prev) => ({ ...prev, sprintId: String(e.value ?? "") }))
                    }
                  />
                ) : (
                  <TextBox
                    value={
                      editForm.sprintId
                        ? (sprints.find((s) => s.id === editForm.sprintId)?.name ?? "—")
                        : "—"
                    }
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
                            onClick={() =>
                              void handleModalAttachmentDelete(att.id, selectedTask!.id)
                            }
                          />
                        )}
                      </div>
                    ))}
                  </div>
                )}
                {!modalAttachmentsLoading && modalAttachments.length === 0 && (
                  <div className="page-inline-info">No attachments.</div>
                )}
                {popupMode === "edit" &&
                  canUpdateTask &&
                  modalAttachments.length < MAX_ATTACHMENTS_PER_TASK && (
                    <div className="attachment-upload-toolbar">
                      <input
                        ref={modalFileInputRef}
                        type="file"
                        multiple
                        accept={ALLOWED_EXTENSIONS_ACCEPT}
                        onChange={(e) =>
                          void handleModalAttachmentUpload(selectedTask!.id, e.target.files)
                        }
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
    </section>
  );
}
