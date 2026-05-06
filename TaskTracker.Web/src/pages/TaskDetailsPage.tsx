import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import ReactDOM from "react-dom";
import { Link, useLocation, useParams } from "react-router-dom";
import { Button } from "devextreme-react/button";
import SelectBox from "devextreme-react/select-box";
import TextArea, { type TextAreaRef } from "devextreme-react/text-area";
import TextBox from "devextreme-react/text-box";
import { useApp } from "../context/AppContext";
import {
  createComment,
  deleteComment,
  getComments,
  updateComment,
} from "../services/commentService";
import { getEpics } from "../services/epicService";
import { getMembersByScope } from "../services/memberService";
import { getSprints } from "../services/sprintService";
import { getTaskById } from "../services/taskService";
import { getErrorMessage } from "../utils/getErrorMessage";
import { AppPermissions, AppRoles } from "../security/permissions";
import type { BackendComment, BackendEpic, BackendSprint } from "../types/app";
import type { MentionableUser, ScopeMember } from "../types/invitation";
import type { TaskDto, TaskUserIdentity, UpdateTaskDto } from "../types/task";
import { priorityLabel, statusLabel } from "../utils/taskPresentation";
import { buildConnection } from "../services/signalRService";

function toDisplayDate(value: string | null | undefined): string {
  if (!value) {
    return "-";
  }

  return new Date(`${value}T00:00:00`).toLocaleDateString();
}

function formatArchivedAssigneeOption(
  identity: TaskUserIdentity | null | undefined,
  fallbackUserId: string
): string {
  if (!identity) {
    return `${fallbackUserId} (not currently assignable)`;
  }

  const name = identity.fullName?.trim() || fallbackUserId;
  const roleSuffix = identity.role ? ` (${identity.role})` : "";
  const status = identity.isArchived ? "Archived" : identity.isActive ? "Active" : "Inactive";
  return `${name}${roleSuffix} (${status})`;
}

export function TaskDetailsPage() {
  const location = useLocation();
  const { projectId, taskId } = useParams();
  const { tasks, projects, hasPermission, user, userPermissions, updateTask } = useApp();

  const canViewComments = hasPermission(AppPermissions.CommentsView);
  const canAddComment = hasPermission(AppPermissions.CommentsAdd);
  const canUpdateComment = hasPermission(AppPermissions.CommentsUpdate);
  const canDeleteComment = hasPermission(AppPermissions.CommentsDelete);

  const [task, setTask] = useState<TaskDto | null>(null);
  const [loadingTask, setLoadingTask] = useState(false);
  const [taskError, setTaskError] = useState("");

  const [epics, setEpics] = useState<BackendEpic[]>([]);
  const [sprints, setSprints] = useState<BackendSprint[]>([]);
  const [projectMembers, setProjectMembers] = useState<ScopeMember[]>([]);
  const [scopeMentionableUsers, setScopeMentionableUsers] = useState<MentionableUser[]>([]);
  const [mentionQuery, setMentionQuery] = useState<string | null>(null);
  const commentTextAreaRef = useRef<TextAreaRef>(null);
  const mentionStartRef = useRef<number | null>(null);
  const [dropdownRect, setDropdownRect] = useState<DOMRect | null>(null);

  const [comments, setComments] = useState<BackendComment[]>([]);
  const [commentsLoading, setCommentsLoading] = useState(false);
  const [commentsError, setCommentsError] = useState("");
  const [commentDraft, setCommentDraft] = useState("");
  const [commentMentions, setCommentMentions] = useState<{ id: string; label: string }[]>([]);
  const [commentActionLoading, setCommentActionLoading] = useState(false);
  const [editingCommentId, setEditingCommentId] = useState<string | null>(null);
  const [editingCommentContent, setEditingCommentContent] = useState("");
  const [selectedAssigneeId, setSelectedAssigneeId] = useState("");
  const [reassignLoading, setReassignLoading] = useState(false);
  const [reassignError, setReassignError] = useState("");
  const [reassignSuccess, setReassignSuccess] = useState("");

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

  const memberInfoById = useMemo(() => {
    const map = new Map<string, { name: string; role: string }>();
    projectMembers.forEach((member) => {
      map.set(member.userId, {
        name: `${member.firstName} ${member.lastName}`.trim(),
        role: member.role,
      });
    });
    return map;
  }, [projectMembers]);

  const currentProjectRole = useMemo(() => {
    if (!user?.id) {
      return null;
    }

    return memberInfoById.get(user.id)?.role ?? null;
  }, [memberInfoById, user?.id]);

  const orderedComments = useMemo(() => {
    return [...comments].sort(
      (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
    );
  }, [comments]);

  const reloadLinks = useCallback(async () => {
    if (!resolvedProjectId) {
      setEpics([]);
      setSprints([]);
      setProjectMembers([]);
      setScopeMentionableUsers([]);
      return;
    }

    try {
      const [epicsResult, sprintsResult, membersResult] = await Promise.all([
        getEpics(resolvedProjectId),
        getSprints(resolvedProjectId),
        getMembersByScope(1, resolvedProjectId),
      ]);

      setEpics(epicsResult);
      setSprints(sprintsResult);
      setProjectMembers(membersResult.members);
      setScopeMentionableUsers(membersResult.mentionableUsers ?? []);
    } catch {
      setEpics([]);
      setSprints([]);
      setProjectMembers([]);
      setScopeMentionableUsers([]);
    }
  }, [resolvedProjectId]);

  const reloadComments = useCallback(async () => {
    if (!canViewComments || !parsedTaskId) {
      setComments([]);
      setCommentsError("");
      setCommentsLoading(false);
      return;
    }

    setCommentsLoading(true);
    setCommentsError("");

    try {
      const result = await getComments(parsedTaskId);
      setComments(result);
    } catch (error) {
      setCommentsError(getErrorMessage(error, "Failed to load comments."));
    } finally {
      setCommentsLoading(false);
    }
  }, [canViewComments, parsedTaskId]);

  const buildPersonDisplay = (userId: string, options?: { fallbackUnassigned?: boolean }) => {
    if (!userId) {
      return options?.fallbackUnassigned ? "Unassigned" : "-";
    }

    const identity =
      (task?.reporterId === userId ? task.reporterUser : null) ??
      (task?.assigneeId === userId ? task.assigneeUser : null);

    if (identity) {
      const displayName = identity.fullName?.trim() || userId;
      const roleSuffix = identity.role ? ` (${identity.role})` : "";
      const statusLabel = identity.isArchived
        ? "Archived"
        : identity.isActive
        ? "Active"
        : "Inactive";
      const statusSuffix = ` (${statusLabel})`;
      const youSuffix = userId === user?.id ? " (You)" : "";
      return `${displayName}${roleSuffix}${statusSuffix}${youSuffix}`;
    }

    const member = memberInfoById.get(userId);
    if (member) {
      const youSuffix = userId === user?.id ? " (You)" : "";
      return `${member.name} (${member.role})${youSuffix}`;
    }

    if (userId === user?.id) {
      return `${user.fullName} (You)`;
    }

    return `User ID: ${userId}`;
  };

  const reporterDisplay = useMemo(() => {
    if (!task?.reporterId) {
      return "-";
    }

    return buildPersonDisplay(task.reporterId);
  }, [task?.reporterId, task?.reporterUser, task?.assigneeId, task?.assigneeUser, memberInfoById, user?.id, user?.fullName]);

  const assigneeDisplay = useMemo(() => {
    if (!task?.assigneeId) {
      return "Unassigned";
    }

    return buildPersonDisplay(task.assigneeId, { fallbackUnassigned: true });
  }, [task?.assigneeId, task?.assigneeUser, task?.reporterId, task?.reporterUser, memberInfoById, user?.id, user?.fullName]);

  const canReassignTask = useMemo(() => {
    if (!task || !resolvedProjectId) {
      return false;
    }

    const canUpdateTask = hasPermission(AppPermissions.TasksUpdate, "Project", resolvedProjectId);
    const canAssignTask = hasPermission(AppPermissions.TasksAssign, "Project", resolvedProjectId);
    return canUpdateTask && (canAssignTask || Boolean(userPermissions?.isSuperAdmin));
  }, [hasPermission, resolvedProjectId, task, userPermissions?.isSuperAdmin]);

  const toMemberLabel = (member: ScopeMember): string =>
    `${member.firstName} ${member.lastName}`.trim() + ` (${member.role})`;

  const assignableOptions = useMemo(() => {
    const options = projectMembers.map((member) => ({
      id: member.userId,
      label: toMemberLabel(member),
    }));

    const currentAssigneeId = task?.assigneeId?.trim();
    if (
      currentAssigneeId &&
      !options.some((option) => option.id === currentAssigneeId)
    ) {
      const archivedAssigneeLabel = formatArchivedAssigneeOption(task?.assigneeUser, currentAssigneeId);
      options.unshift({
        id: currentAssigneeId,
        label: archivedAssigneeLabel,
      });
    }

    return options;
  }, [projectMembers, task?.assigneeId, task?.assigneeUser]);

  type MentionOption = { id: string; label: string; role: string };

  const mentionableUsers = useMemo(() => {
    const list: MentionOption[] = [];
    const addedIds = new Set<string>();
    const sourceUsers =
      scopeMentionableUsers.length > 0
        ? scopeMentionableUsers
        : projectMembers.map((member) => ({
            userId: member.userId,
            firstName: member.firstName,
            lastName: member.lastName,
            role: member.role,
          }));

    sourceUsers.forEach((mentionableUser) => {
      const fullName = `${mentionableUser.firstName} ${mentionableUser.lastName}`.trim();
      if (
        !mentionableUser.userId ||
        mentionableUser.userId === user?.id ||
        !fullName ||
        addedIds.has(mentionableUser.userId)
      ) {
        return;
      }

      list.push({
        id: mentionableUser.userId,
        label:
          mentionableUser.role === AppRoles.SuperAdmin
            ? `${fullName} (Super Admin)`
            : fullName,
        role: mentionableUser.role,
      });
      addedIds.add(mentionableUser.userId);
    });

    return list;
  }, [projectMembers, scopeMentionableUsers, user?.id]);

  const filteredMentionableUsers = useMemo(() => {
    if (mentionQuery === null) return [];
    const q = mentionQuery.toLowerCase();
    return q === ""
      ? mentionableUsers
      : mentionableUsers.filter((u) => u.label.toLowerCase().includes(q));
  }, [mentionQuery, mentionableUsers]);

  const closeMentionDropdown = () => {
    setMentionQuery(null);
    setDropdownRect(null);
    mentionStartRef.current = null;
  };

  const cleanMentionLabel = (label: string) => label.replace(/\s+\(Super Admin\)$/, "").trim();

  const getMentionMeta = (mention: MentionOption) => {
    if (mention.role === AppRoles.SuperAdmin) {
      return "Global Super Admin";
    }

    return mention.role || "Project member";
  };

  const getMentionInitials = (label: string) => {
    const cleanLabel = cleanMentionLabel(label);
    const [first = "", second = ""] = cleanLabel.split(/\s+/);
    return `${first.charAt(0)}${second.charAt(0)}`.toUpperCase() || "@";
  };

  const getCommentTextarea = useCallback(() => {
    const root = commentTextAreaRef.current?.instance().element() as HTMLElement | undefined;
    return root?.querySelector("textarea") ?? null;
  }, []);

  const findMentionTrigger = (value: string, caretIndex: number) => {
    const beforeCaret = value.slice(0, caretIndex);
    const atIndex = beforeCaret.lastIndexOf("@");

    if (atIndex === -1) {
      return null;
    }

    const charBeforeAt = atIndex > 0 ? beforeCaret[atIndex - 1] : "";
    if (charBeforeAt && !/\s/.test(charBeforeAt)) {
      return null;
    }

    const query = beforeCaret.slice(atIndex + 1);
    if (/[\s@]/.test(query)) {
      return null;
    }

    return { startIndex: atIndex, query };
  };

  const updateMentionDropdown = (value: string) => {
    const textarea = getCommentTextarea();
    const caretIndex = textarea?.selectionStart ?? value.length;
    const trigger = findMentionTrigger(value, caretIndex);

    if (!trigger) {
      closeMentionDropdown();
      return;
    }

    mentionStartRef.current = trigger.startIndex;
    setMentionQuery(trigger.query);

    const root = commentTextAreaRef.current?.instance().element() as HTMLElement | undefined;
    setDropdownRect((textarea ?? root)?.getBoundingClientRect() ?? null);
  };

  const handleCommentDraftChange = (value: string) => {
    setCommentDraft(value);
    setCommentMentions((prev) =>
      prev.filter((mention) => value.includes(`@${cleanMentionLabel(mention.label)}`))
    );
    updateMentionDropdown(value);
  };

  const handleCommentCaretChange = () => {
    updateMentionDropdown(commentDraft);
  };

  const insertMention = (targetUser: MentionOption) => {
    const textarea = getCommentTextarea();
    const caretIndex = textarea?.selectionStart ?? commentDraft.length;
    const trigger = findMentionTrigger(commentDraft, caretIndex);
    const startIndex = mentionStartRef.current ?? trigger?.startIndex ?? commentDraft.lastIndexOf("@");

    if (startIndex < 0) {
      closeMentionDropdown();
      return;
    }

    const cleanLabel = cleanMentionLabel(targetUser.label);
    const after = commentDraft.slice(caretIndex);
    const insertedText = `@${cleanLabel}${after.startsWith(" ") || after.startsWith("\n") ? "" : " "}`;
    const nextDraft = `${commentDraft.slice(0, startIndex)}${insertedText}${after}`;
    const nextCaretPosition = startIndex + insertedText.length;

    setCommentDraft(nextDraft);
    setCommentMentions((prev) => {
      if (prev.some((mention) => mention.id === targetUser.id)) {
        return prev;
      }

      return [...prev, { id: targetUser.id, label: cleanLabel }];
    });
    closeMentionDropdown();

    window.setTimeout(() => {
      const nextTextarea = getCommentTextarea();
      commentTextAreaRef.current?.instance().focus();
      nextTextarea?.setSelectionRange(nextCaretPosition, nextCaretPosition);
    }, 0);
  };

  const extractMentionIdsFromDraft = (value: string) => {
    const ids = new Set<string>();
    const candidates = mentionableUsers
      .map((mention) => ({
        id: mention.id,
        label: cleanMentionLabel(mention.label),
      }))
      .filter((mention) => mention.label)
      .sort((a, b) => b.label.length - a.label.length);

    candidates.forEach((candidate) => {
      const escapedLabel = candidate.label.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
      const regex = new RegExp(`(^|\\s)@${escapedLabel}(?=$|\\s|[.,!?;:)])`, "i");
      if (regex.test(value)) {
        ids.add(candidate.id);
      }
    });

    return Array.from(ids);
  };

  const renderPlainMentionText = (text: string, keyPrefix: string) => {
    const candidates = mentionableUsers
      .map((mention) => ({
        id: mention.id,
        label: cleanMentionLabel(mention.label),
      }))
      .filter((mention) => mention.label)
      .sort((a, b) => b.label.length - a.label.length);

    if (candidates.length === 0) {
      return text;
    }

    const escapedLabels = candidates.map((mention) =>
      mention.label.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")
    );
    const regex = new RegExp(`@(${escapedLabels.join("|")})(?=$|\\s|[.,!?;:)])`, "g");
    const parts: ReactNode[] = [];
    let lastIndex = 0;
    let match: RegExpExecArray | null;

    while ((match = regex.exec(text)) !== null) {
      if (match.index > lastIndex) {
        parts.push(
          <span key={`${keyPrefix}-t-${lastIndex}`}>
            {text.substring(lastIndex, match.index)}
          </span>
        );
      }

      const matchedLabel = match[1];
      const matchedUser = candidates.find((candidate) => candidate.label === matchedLabel);
      const MentionTag = matchedUser?.id === user?.id ? "mark" : "strong";

      parts.push(
        <MentionTag
          key={`${keyPrefix}-m-${match.index}`}
          className={`mention-highlight${matchedUser?.id === user?.id ? " self" : ""}`}
        >
          @{matchedLabel}
        </MentionTag>
      );

      lastIndex = regex.lastIndex;
    }

    if (lastIndex < text.length) {
      parts.push(<span key={`${keyPrefix}-t-${lastIndex}`}>{text.substring(lastIndex)}</span>);
    }

    return parts.length > 0 ? parts : text;
  };

  const renderCommentContent = (content: string) => {
    const regex = /@\[([^\]]+)\]\(([^)]+)\)/g;
    const parts: ReactNode[] = [];
    let lastIndex = 0;
    let match: RegExpExecArray | null;

    while ((match = regex.exec(content)) !== null) {
      if (match.index > lastIndex) {
        parts.push(renderPlainMentionText(content.substring(lastIndex, match.index), `t-${lastIndex}`));
      }

      const label = match[1];
      const id = match[2];

      if (id === user?.id) {
         parts.push(<mark key={`m-${match.index}`} className="mention-highlight self">@{label}</mark>);
      } else {
         parts.push(<strong key={`m-${match.index}`} className="mention-highlight">@{label}</strong>);
      }

      lastIndex = regex.lastIndex;
    }

    if (lastIndex < content.length) {
      parts.push(renderPlainMentionText(content.substring(lastIndex), `t-${lastIndex}`));
    }

    return parts.length > 0 ? parts : renderPlainMentionText(content, "plain");
  };

  useEffect(() => {
    setSelectedAssigneeId(task?.assigneeId ?? "");
    setReassignError("");
    setReassignSuccess("");
  }, [task?.id, task?.assigneeId]);

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
          if (error instanceof Error) {
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
    void reloadLinks();
  }, [reloadLinks]);

  useEffect(() => {
    void reloadComments();
  }, [reloadComments]);

  useEffect(() => {
    if (!resolvedProjectId || !parsedTaskId) {
      return;
    }

    const conn = buildConnection();

    const handleTaskCommentsChanged = (payload: { projectId: string; taskId: number }) => {
      if (payload.projectId === resolvedProjectId && payload.taskId === parsedTaskId) {
        void reloadComments();
      }
    };

    const handleScopeMembersChanged = (payload: { scopeType: string; scopeId: string }) => {
      if (payload.scopeType === "Project" && payload.scopeId === resolvedProjectId) {
        void reloadLinks();
      }
    };

    conn.on("TaskCommentsChanged", handleTaskCommentsChanged);
    conn.on("ScopeMembersChanged", handleScopeMembersChanged);

    return () => {
      conn.off("TaskCommentsChanged", handleTaskCommentsChanged);
      conn.off("ScopeMembersChanged", handleScopeMembersChanged);
    };
  }, [parsedTaskId, reloadComments, reloadLinks, resolvedProjectId]);

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
      const mentionedUserIdsRaw = commentMentions
        .filter((mention) => commentDraft.includes(`@${cleanMentionLabel(mention.label)}`))
        .map((mention) => mention.id)
        .concat(extractMentionIdsFromDraft(commentDraft))
        .filter(id => id !== user?.id);

      const created = await createComment({
        taskId: parsedTaskId,
        content: commentDraft.trim(),
        mentionedUserIds: Array.from(new Set(mentionedUserIdsRaw))
      });
      setComments((prev) => [...prev, created]);
      setCommentDraft("");
      setCommentMentions([]);
      closeMentionDropdown();
    } catch (error) {
      setCommentsError(getErrorMessage(error, "Failed to create comment."));
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
    const targetComment = comments.find((item) => item.id === commentId);

    if (!canUpdateComment) {
      setCommentsError("You do not have permission to update comments.");
      return;
    }

    if (targetComment && !canModerateComment(targetComment)) {
      setCommentsError("You do not have permission to edit this comment.");
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
      setCommentsError(getErrorMessage(error, "Failed to update comment."));
    } finally {
      setCommentActionLoading(false);
    }
  };

  const handleDeleteComment = async (comment: BackendComment) => {
    if (!canDeleteComment) {
      setCommentsError("You do not have permission to delete comments.");
      return;
    }

    if (!canModerateComment(comment)) {
      setCommentsError("You do not have permission to delete this comment.");
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
      setCommentsError(getErrorMessage(error, "Failed to delete comment."));
    } finally {
      setCommentActionLoading(false);
    }
  };

  const handleReassignTask = async () => {
    if (!task || !resolvedProjectId) {
      return;
    }

    if (!canReassignTask) {
      setReassignError("You do not have permission to reassign this task.");
      return;
    }

    const normalizedCurrentAssignee = task.assigneeId ?? "";
    const normalizedSelectedAssignee = selectedAssigneeId.trim();
    if (normalizedCurrentAssignee === normalizedSelectedAssignee) {
      setReassignSuccess("Task assignee is already up to date.");
      setReassignError("");
      return;
    }

    const updatePayload: UpdateTaskDto = {
      epicId: task.epicId ?? null,
      sprintId: task.sprintId ?? null,
      assigneeId: normalizedSelectedAssignee || null,
      title: task.title ?? "",
      description: task.description ?? undefined,
      status: task.status,
      priority: task.priority,
      startDate: task.startDate ?? null,
      endDate: task.endDate ?? null,
    };

    setReassignLoading(true);
    setReassignError("");
    setReassignSuccess("");

    try {
      const updatedTask = await updateTask(task.id, task.projectId, updatePayload);
      const refreshedTask = await getTaskById(task.id, task.projectId);
      const taskForView = refreshedTask ?? updatedTask;
      setTask(taskForView);
      setSelectedAssigneeId(taskForView.assigneeId ?? "");
      setReassignSuccess("Task reassigned successfully.");
    } catch (error) {
      setReassignError(getErrorMessage(error, "Failed to reassign task."));
    } finally {
      setReassignLoading(false);
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

  function canModerateComment(comment: BackendComment): boolean {
    if (!user?.id) {
      return false;
    }

    if (userPermissions?.isSuperAdmin) {
      return true;
    }

    if (comment.authorId === user.id) {
      return true;
    }

    const authorRole = memberInfoById.get(comment.authorId)?.role ?? null;

    if (!currentProjectRole) {
      return false;
    }

    if (currentProjectRole === AppRoles.OrgAdmin) {
      return true;
    }

    if (currentProjectRole === AppRoles.ProjectManager) {
      return (
        authorRole !== AppRoles.ProjectManager &&
        authorRole !== AppRoles.OrgAdmin
      );
    }

    return false;
  }

  const mentionPortal =
    mentionQuery !== null && filteredMentionableUsers.length > 0 && dropdownRect
      ? (() => {
          const dropdownHeight = Math.min(272, 12 + filteredMentionableUsers.length * 58);
          const opensAbove =
            dropdownRect.bottom + dropdownHeight + 12 > window.innerHeight &&
            dropdownRect.top - dropdownHeight > 8;
          const top = opensAbove
            ? dropdownRect.top - dropdownHeight - 6
            : dropdownRect.bottom + 6;

          return ReactDOM.createPortal(
            <div
              className="mention-dropdown mention-dropdown-fixed"
              style={{
                top,
                left: dropdownRect.left,
                width: Math.min(Math.max(dropdownRect.width * 0.45, 300), 380),
                maxHeight: dropdownHeight,
              }}
              role="listbox"
            >
              <ul style={{ margin: 0, padding: 0, listStyle: "none" }}>
                {filteredMentionableUsers.map((mention) => (
                  <li
                    key={mention.id}
                    className="mention-item"
                    onMouseDown={(event) => {
                      event.preventDefault();
                      insertMention(mention);
                    }}
                    role="option"
                    aria-selected="false"
                  >
                    <span className="mention-avatar">{getMentionInitials(mention.label)}</span>
                    <span className="mention-copy">
                      <span>{cleanMentionLabel(mention.label)}</span>
                      <small>{getMentionMeta(mention)}</small>
                    </span>
                  </li>
                ))}
              </ul>
            </div>,
            document.body
          );
        })()
      : null;

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
                {canReassignTask && (
                  <div className="page-stack" style={{ marginTop: "0.55rem", gap: "0.45rem" }}>
                    <SelectBox
                      dataSource={assignableOptions}
                      displayExpr="label"
                      valueExpr="id"
                      value={selectedAssigneeId || null}
                      showClearButton
                      placeholder="Reassign task (optional)"
                      onValueChanged={(event) => {
                        setSelectedAssigneeId(String(event.value ?? ""));
                        setReassignError("");
                        setReassignSuccess("");
                      }}
                    />
                    <div className="inline-actions" style={{ justifyContent: "flex-end" }}>
                      <Button
                        text={reassignLoading ? "Saving..." : "Save Assignee"}
                        type="default"
                        onClick={() => void handleReassignTask()}
                        disabled={reassignLoading}
                      />
                    </div>
                    {reassignError && <div className="form-error">{reassignError}</div>}
                    {reassignSuccess && <div className="page-inline-info">{reassignSuccess}</div>}
                  </div>
                )}
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
                            maxLength={5000}
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
                          <p>{renderCommentContent(comment.content)}</p>
                          <div className="inline-actions task-comment-actions">
                            {canUpdateComment && canModerateComment(comment) && (
                              <Button
                                text="Edit"
                                stylingMode="text"
                                onClick={() => startEditingComment(comment)}
                                disabled={commentActionLoading}
                              />
                            )}
                            {canDeleteComment && canModerateComment(comment) && (
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
                      ref={commentTextAreaRef}
                      value={commentDraft}
                      maxLength={5000}
                      minHeight={80}
                      placeholder="Write a comment... (Type @ to mention)"
                      valueChangeEvent="input"
                      onValueChanged={(event) =>
                        handleCommentDraftChange(String(event.value ?? ""))
                      }
                      onKeyUp={handleCommentCaretChange}
                      onFocusOut={() => {
                        setTimeout(closeMentionDropdown, 150);
                      }}
                    />

                    {mentionPortal}

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
