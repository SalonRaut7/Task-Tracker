import { useEffect, useMemo, useState } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import { Button } from "devextreme-react/button";
import TextArea from "devextreme-react/text-area";
import TextBox from "devextreme-react/text-box";
import { useApp } from "../context/AppContext";
import {
  createComment,
  deleteComment,
  getComments,
  updateComment,
} from "../services/commentService";
import { getEpics } from "../services/epicService";
import { getSprints } from "../services/sprintService";
import { getTaskById } from "../services/taskService";
import { ApiError } from "../services/apiClient";
import { AppPermissions } from "../security/permissions";
import type { BackendComment, BackendEpic, BackendSprint } from "../types/app";
import type { TaskDto } from "../types/task";
import { priorityLabel, statusLabel } from "../utils/taskPresentation";

function toDisplayDate(value: string | null | undefined): string {
  if (!value) {
    return "-";
  }

  return new Date(`${value}T00:00:00`).toLocaleDateString();
}

export function TaskDetailsPage() {
  const location = useLocation();
  const { projectId, taskId } = useParams();
  const { tasks, projects, hasPermission, user } = useApp();

  const canViewComments = hasPermission(AppPermissions.CommentsView);
  const canAddComment = hasPermission(AppPermissions.CommentsAdd);
  const canUpdateComment = hasPermission(AppPermissions.CommentsUpdate);
  const canDeleteComment = hasPermission(AppPermissions.CommentsDelete);

  const [task, setTask] = useState<TaskDto | null>(null);
  const [loadingTask, setLoadingTask] = useState(false);
  const [taskError, setTaskError] = useState("");

  const [epics, setEpics] = useState<BackendEpic[]>([]);
  const [sprints, setSprints] = useState<BackendSprint[]>([]);

  const [comments, setComments] = useState<BackendComment[]>([]);
  const [commentsLoading, setCommentsLoading] = useState(false);
  const [commentsError, setCommentsError] = useState("");
  const [commentDraft, setCommentDraft] = useState("");
  const [commentActionLoading, setCommentActionLoading] = useState(false);
  const [editingCommentId, setEditingCommentId] = useState<string | null>(null);
  const [editingCommentContent, setEditingCommentContent] = useState("");

  const parsedTaskId = useMemo(() => {
    const numeric = Number(taskId);
    return Number.isFinite(numeric) && numeric > 0 ? numeric : null;
  }, [taskId]);

  const resolvedProjectId = useMemo(() => {
    if (projectId) {
      return projectId;
    }

    const queryProjectId = new URLSearchParams(location.search).get("projectId");
    return queryProjectId?.trim() ? queryProjectId : null;
  }, [projectId, location.search]);

  const projectNameById = useMemo(() => {
    const map = new Map<string, string>();
    projects.forEach((project) => {
      map.set(project.id, project.name);
    });
    return map;
  }, [projects]);

  const epicNameById = useMemo(() => {
    const map = new Map<string, string>();
    epics.forEach((epic) => {
      map.set(epic.id, epic.title);
    });
    return map;
  }, [epics]);

  const sprintNameById = useMemo(() => {
    const map = new Map<string, string>();
    sprints.forEach((sprint) => {
      map.set(sprint.id, sprint.name);
    });
    return map;
  }, [sprints]);

  const orderedComments = useMemo(() => {
    return [...comments].sort(
      (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
    );
  }, [comments]);

  const reporterDisplay = useMemo(() => {
    if (!task?.reporterId) {
      return "-";
    }

    if (task.reporterId === user?.id) {
      return `${user.fullName} (You)`;
    }

    return `User ID: ${task.reporterId}`;
  }, [task?.reporterId, user?.id, user?.fullName]);

  const assigneeDisplay = useMemo(() => {
    if (!task?.assigneeId) {
      return "Unassigned";
    }

    if (task.assigneeId === user?.id) {
      return `${user.fullName} (You)`;
    }

    return `User ID: ${task.assigneeId}`;
  }, [task?.assigneeId, user?.id, user?.fullName]);

  useEffect(() => {
    if (!resolvedProjectId || !parsedTaskId) {
      setTask(null);
      return;
    }

    const initial = tasks.find(
      (item) => item.id === parsedTaskId && item.projectId === resolvedProjectId
    );
    if (initial) {
      setTask(initial);
    }

    let cancelled = false;

    const loadTask = async () => {
      setLoadingTask(true);
      setTaskError("");

      try {
        const result = await getTaskById(parsedTaskId, resolvedProjectId);
        if (!cancelled) {
          if (!result) {
            setTask(null);
            setTaskError("Task details are unavailable.");
            return;
          }

          setTask(result);
        }
      } catch (error) {
        if (!cancelled) {
          if (error instanceof ApiError) {
            setTaskError(error.message);
          } else if (error instanceof Error) {
            setTaskError(error.message);
          } else {
            setTaskError("Failed to load task details.");
          }
        }
      } finally {
        if (!cancelled) {
          setLoadingTask(false);
        }
      }
    };

    void loadTask();

    return () => {
      cancelled = true;
    };
  }, [resolvedProjectId, parsedTaskId, tasks]);

  useEffect(() => {
    if (!resolvedProjectId) {
      setEpics([]);
      setSprints([]);
      return;
    }

    let cancelled = false;

    const loadLinks = async () => {
      try {
        const [epicsResult, sprintsResult] = await Promise.all([
          getEpics(resolvedProjectId),
          getSprints(resolvedProjectId),
        ]);

        if (!cancelled) {
          setEpics(epicsResult);
          setSprints(sprintsResult);
        }
      } catch {
        if (!cancelled) {
          setEpics([]);
          setSprints([]);
        }
      }
    };

    void loadLinks();

    return () => {
      cancelled = true;
    };
  }, [resolvedProjectId]);

  useEffect(() => {
    if (!canViewComments || !parsedTaskId) {
      setComments([]);
      setCommentsError("");
      setCommentsLoading(false);
      return;
    }

    let cancelled = false;

    const loadComments = async () => {
      setCommentsLoading(true);
      setCommentsError("");

      try {
        const result = await getComments(parsedTaskId);
        if (!cancelled) {
          setComments(result);
        }
      } catch (error) {
        if (!cancelled) {
          if (error instanceof ApiError) {
            setCommentsError(error.message);
          } else if (error instanceof Error) {
            setCommentsError(error.message);
          } else {
            setCommentsError("Failed to load comments.");
          }
        }
      } finally {
        if (!cancelled) {
          setCommentsLoading(false);
        }
      }
    };

    void loadComments();

    return () => {
      cancelled = true;
    };
  }, [canViewComments, parsedTaskId]);

  const handleAddComment = async () => {
    if (!parsedTaskId) {
      return;
    }

    if (!canAddComment) {
      setCommentsError("You do not have permission to add comments.");
      return;
    }

    if (!commentDraft.trim()) {
      setCommentsError("Comment content is required.");
      return;
    }

    setCommentsError("");
    setCommentActionLoading(true);

    try {
      const created = await createComment({
        taskId: parsedTaskId,
        content: commentDraft.trim(),
      });
      setComments((prev) => [...prev, created]);
      setCommentDraft("");
    } catch (error) {
      if (error instanceof ApiError) {
        setCommentsError(error.message);
      } else if (error instanceof Error) {
        setCommentsError(error.message);
      } else {
        setCommentsError("Failed to create comment.");
      }
    } finally {
      setCommentActionLoading(false);
    }
  };

  const startEditingComment = (comment: BackendComment) => {
    setCommentsError("");
    setEditingCommentId(comment.id);
    setEditingCommentContent(comment.content);
  };

  const cancelEditingComment = () => {
    setEditingCommentId(null);
    setEditingCommentContent("");
    setCommentsError("");
  };

  const handleUpdateComment = async (commentId: string) => {
    if (!canUpdateComment) {
      setCommentsError("You do not have permission to update comments.");
      return;
    }

    if (!editingCommentContent.trim()) {
      setCommentsError("Comment content cannot be empty.");
      return;
    }

    setCommentsError("");
    setCommentActionLoading(true);

    try {
      const updated = await updateComment(commentId, {
        content: editingCommentContent.trim(),
      });

      setComments((prev) =>
        prev.map((item) => (item.id === updated.id ? updated : item))
      );
      setEditingCommentId(null);
      setEditingCommentContent("");
    } catch (error) {
      if (error instanceof ApiError) {
        setCommentsError(error.message);
      } else if (error instanceof Error) {
        setCommentsError(error.message);
      } else {
        setCommentsError("Failed to update comment.");
      }
    } finally {
      setCommentActionLoading(false);
    }
  };

  const handleDeleteComment = async (comment: BackendComment) => {
    if (!canDeleteComment) {
      setCommentsError("You do not have permission to delete comments.");
      return;
    }

    const confirmed = window.confirm("Delete this comment?");
    if (!confirmed) {
      return;
    }

    setCommentsError("");
    setCommentActionLoading(true);

    try {
      await deleteComment(comment.id);
      setComments((prev) => prev.filter((item) => item.id !== comment.id));

      if (editingCommentId === comment.id) {
        cancelEditingComment();
      }
    } catch (error) {
      if (error instanceof ApiError) {
        setCommentsError(error.message);
      } else if (error instanceof Error) {
        setCommentsError(error.message);
      } else {
        setCommentsError("Failed to delete comment.");
      }
    } finally {
      setCommentActionLoading(false);
    }
  };

  if (!resolvedProjectId || !parsedTaskId) {
    return (
      <div className="page-stack">
        <h1>Task not found</h1>
        <p className="page-subtitle">Missing or invalid task route parameters.</p>
        <Link to="/tasks">Back to tasks</Link>
      </div>
    );
  }

  return (
    <div className="page-stack">
      <section>
        <Link to="/tasks">Back to Tasks</Link>
        <h1>{task?.title || `Task #${parsedTaskId}`}</h1>
        <p className="page-subtitle">Task details and collaboration thread</p>
      </section>

      {loadingTask && <div className="page-inline-info">Loading task details...</div>}
      {taskError && <div className="form-error">{taskError}</div>}

      {task ? (
        <section className="card">
          <div className="task-popup-grid">
            <div className="task-popup-main">
              <label>
                Title
                <TextBox value={task.title ?? "Untitled"} readOnly />
              </label>

              <label>
                Description
                <TextArea
                  value={task.description ?? "No description provided."}
                  minHeight={110}
                  readOnly
                />
              </label>
            </div>

            <div className="task-popup-side">
              <label>
                Reporter
                <TextBox value={reporterDisplay} readOnly />
              </label>

              <label>
                Assignee
                <TextBox value={assigneeDisplay} readOnly />
              </label>

              <label>
                Project
                <TextBox
                  value={projectNameById.get(task.projectId) ?? task.projectId}
                  readOnly
                />
              </label>

              <label>
                Epic
                <TextBox
                  value={task.epicId ? epicNameById.get(task.epicId) ?? "-" : "-"}
                  readOnly
                />
              </label>

              <label>
                Sprint
                <TextBox
                  value={task.sprintId ? sprintNameById.get(task.sprintId) ?? "-" : "-"}
                  readOnly
                />
              </label>

              <label>
                Status
                <TextBox value={statusLabel(task.status)} readOnly />
              </label>

              <label>
                Priority
                <TextBox value={priorityLabel(task.priority)} readOnly />
              </label>

              <label>
                Start Date
                <TextBox value={toDisplayDate(task.startDate)} readOnly />
              </label>

              <label>
                End Date
                <TextBox value={toDisplayDate(task.endDate)} readOnly />
              </label>

              <label>
                Created
                <TextBox
                  value={task.createdAt ? new Date(task.createdAt).toLocaleString() : "-"}
                  readOnly
                />
              </label>

              <label>
                Updated
                <TextBox
                  value={task.updatedAt ? new Date(task.updatedAt).toLocaleString() : "-"}
                  readOnly
                />
              </label>
            </div>
          </div>

          <section className="task-comments-section">
            <div className="task-comments-header">
              <h3>Comments</h3>
              <span>{orderedComments.length}</span>
            </div>

            {!canViewComments ? (
              <div className="page-inline-info">You do not have permission to view comments.</div>
            ) : (
              <>
                {commentsError && <div className="form-error">{commentsError}</div>}
                {commentsLoading && <div className="page-inline-info">Loading comments...</div>}

                {!commentsLoading && orderedComments.length === 0 && (
                  <div className="page-inline-info">No comments yet.</div>
                )}

                <div className="task-comment-list">
                  {orderedComments.map((comment) => (
                    <article key={comment.id} className="task-comment-item">
                      <div className="task-comment-meta">
                        <strong>{comment.authorName}</strong>
                        <small>
                          {comment.updatedAt !== comment.createdAt ? "Edited " : "Posted "}
                          {new Date(comment.updatedAt || comment.createdAt).toLocaleString()}
                        </small>
                      </div>

                      {editingCommentId === comment.id ? (
                        <>
                          <TextArea
                            value={editingCommentContent}
                            minHeight={80}
                            onValueChanged={(event) =>
                              setEditingCommentContent(String(event.value ?? ""))
                            }
                          />
                          <div className="popup-actions task-comment-actions">
                            <Button
                              text="Cancel"
                              stylingMode="outlined"
                              onClick={cancelEditingComment}
                              disabled={commentActionLoading}
                            />
                            <Button
                              text={commentActionLoading ? "Saving..." : "Save"}
                              type="default"
                              onClick={() => void handleUpdateComment(comment.id)}
                              disabled={commentActionLoading}
                            />
                          </div>
                        </>
                      ) : (
                        <>
                          <p>{comment.content}</p>
                          <div className="inline-actions task-comment-actions">
                            {canUpdateComment && (
                              <Button
                                text="Edit"
                                stylingMode="text"
                                onClick={() => startEditingComment(comment)}
                                disabled={commentActionLoading}
                              />
                            )}
                            {canDeleteComment && (
                              <Button
                                text="Delete"
                                type="danger"
                                stylingMode="text"
                                onClick={() => void handleDeleteComment(comment)}
                                disabled={commentActionLoading}
                              />
                            )}
                          </div>
                        </>
                      )}
                    </article>
                  ))}
                </div>

                {canAddComment && (
                  <div className="task-comment-compose">
                    <TextArea
                      value={commentDraft}
                      minHeight={80}
                      placeholder="Write a comment..."
                      onValueChanged={(event) =>
                        setCommentDraft(String(event.value ?? ""))
                      }
                    />
                    <div className="popup-actions task-comment-actions">
                      <Button
                        text={commentActionLoading ? "Adding..." : "Add Comment"}
                        type="default"
                        onClick={() => void handleAddComment()}
                        disabled={commentActionLoading}
                      />
                    </div>
                  </div>
                )}
              </>
            )}
          </section>
        </section>
      ) : null}
    </div>
  );
}
