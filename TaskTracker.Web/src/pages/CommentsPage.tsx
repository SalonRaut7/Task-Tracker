import { useEffect, useMemo, useState } from "react";
import { Button } from "devextreme-react/button";
import DataGrid, { Column, Paging } from "devextreme-react/data-grid";
import NumberBox from "devextreme-react/number-box";
import Popup from "devextreme-react/popup";
import TextArea from "devextreme-react/text-area";
import TextBox from "devextreme-react/text-box";
import { useApp } from "../context/AppContext";
import { getErrorMessage } from "../utils/getErrorMessage";
import {
  createComment,
  deleteComment,
  getComments,
  updateComment,
} from "../services/commentService";
import { AppPermissions } from "../security/permissions";
import type { BackendComment } from "../types/app";

export function CommentsPage() {
  const { hasPermission } = useApp();
  const canCreate = hasPermission(AppPermissions.CommentsAdd);
  const canUpdate = hasPermission(AppPermissions.CommentsUpdate);
  const canDelete = hasPermission(AppPermissions.CommentsDelete);

  const [taskIdFilter, setTaskIdFilter] = useState<number | null>(null);
  const [comments, setComments] = useState<BackendComment[]>([]);
  const [requestError, setRequestError] = useState("");
  const [loading, setLoading] = useState(false);
  const [showCreate, setShowCreate] = useState(false);
  const [newCommentTaskId, setNewCommentTaskId] = useState<number | null>(null);
  const [newCommentContent, setNewCommentContent] = useState("");
  const [selectedComment, setSelectedComment] = useState<BackendComment | null>(null);
  const [editContent, setEditContent] = useState("");

  const sortedComments = useMemo(
    () => [...comments].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()),
    [comments]
  );

  const load = async () => {
    setLoading(true);
    setRequestError("");

    try {
      const result = await getComments(taskIdFilter ?? undefined);
      setComments(result);
    } catch (error) {
      setRequestError(getErrorMessage(error, "Failed to load comments."));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const handleCreate = async () => {
    if (!canCreate) {
      setRequestError("You do not have permission to add comments.");
      return;
    }

    if (!newCommentTaskId || !newCommentContent.trim()) {
      setRequestError("Task ID and comment content are required.");
      return;
    }

    setRequestError("");

    try {
      const created = await createComment({
        taskId: newCommentTaskId,
        content: newCommentContent.trim(),
      });
      setComments((prev) => [created, ...prev]);
      setShowCreate(false);
      setNewCommentTaskId(null);
      setNewCommentContent("");
    } catch (error) {
      setRequestError(getErrorMessage(error, "Failed to create comment."));
    }
  };

  const handleUpdate = async () => {
    if (!selectedComment) {
      return;
    }

    if (!canUpdate) {
      setRequestError("You do not have permission to update comments.");
      return;
    }

    if (!editContent.trim()) {
      setRequestError("Comment content cannot be empty.");
      return;
    }

    setRequestError("");

    try {
      const updated = await updateComment(selectedComment.id, { content: editContent.trim() });
      setComments((prev) => prev.map((item) => (item.id === updated.id ? updated : item)));
      setSelectedComment(updated);
    } catch (error) {
      setRequestError(getErrorMessage(error, "Failed to update comment."));
    }
  };

  const handleDelete = async () => {
    if (!selectedComment) {
      return;
    }

    if (!canDelete) {
      setRequestError("You do not have permission to delete comments.");
      return;
    }

    setRequestError("");

    try {
      await deleteComment(selectedComment.id);
      setComments((prev) => prev.filter((item) => item.id !== selectedComment.id));
      setSelectedComment(null);
    } catch (error) {
      setRequestError(getErrorMessage(error, "Failed to delete comment."));
    }
  };

  return (
    <div className="page-stack">
      <section className="page-title-row">
        <div>
          <h1>Comments</h1>
          <p className="page-subtitle">Moderate task comments with scoped authorization</p>
        </div>
        <Button text="New Comment" icon="plus" type="default" disabled={!canCreate} onClick={() => setShowCreate(true)} />
      </section>

      {requestError && <div className="form-error">{requestError}</div>}
      {loading && <div className="page-inline-info">Refreshing comments...</div>}

      <section className="toolbar-row">
        <NumberBox
          placeholder="Filter by task id"
          min={1}
          value={taskIdFilter ?? undefined}
          showSpinButtons
          width={200}
          onValueChanged={(event) => setTaskIdFilter(typeof event.value === "number" ? event.value : null)}
        />
        <Button text="Apply Filter" stylingMode="outlined" onClick={() => void load()} />
        <Button
          text="Clear"
          stylingMode="text"
          onClick={() => {
            setTaskIdFilter(null);
            void load();
          }}
        />
      </section>

      <section className="card">
        <DataGrid
          dataSource={sortedComments}
          keyExpr="id"
          showBorders={false}
          rowAlternationEnabled
          hoverStateEnabled
          onRowClick={(event) => {
            if (event.rowType !== "data" || !event.data) {
              return;
            }

            const comment = event.data as BackendComment;
            setSelectedComment(comment);
            setEditContent(comment.content);
          }}
          noDataText="No comments found."
        >
          <Column dataField="taskId" caption="Task ID" width={120} />
          <Column dataField="authorName" caption="Author" width={190} />
          <Column dataField="content" caption="Comment" />
          <Column
            dataField="createdAt"
            caption="Created"
            width={180}
            cellRender={({ data }: { data: BackendComment }) =>
              data.createdAt ? new Date(data.createdAt).toLocaleString() : "-"
            }
          />
          <Paging enabled pageSize={12} />
        </DataGrid>
      </section>

      <Popup
        visible={showCreate}
        onHiding={() => setShowCreate(false)}
        title="Create Comment"
        width={560}
        height="auto"
        showCloseButton
      >
        <div className="popup-form">
          <label>
            Task ID
            <NumberBox
              min={1}
              value={newCommentTaskId ?? undefined}
              showSpinButtons
              onValueChanged={(event) => setNewCommentTaskId(typeof event.value === "number" ? event.value : null)}
            />
          </label>
          <label>
            Content
            <TextArea
              value={newCommentContent}
              maxLength={5000}
              minHeight={100}
              onValueChanged={(event) => setNewCommentContent(String(event.value ?? ""))}
            />
          </label>
          <div className="popup-actions">
            <Button text="Create" type="default" onClick={handleCreate} />
            <Button text="Cancel" stylingMode="outlined" onClick={() => setShowCreate(false)} />
          </div>
        </div>
      </Popup>

      <Popup
        visible={selectedComment !== null}
        onHiding={() => setSelectedComment(null)}
        title={selectedComment ? `Comment by ${selectedComment.authorName}` : "Comment"}
        width={620}
        height="auto"
        showCloseButton
      >
        {selectedComment && (
          <div className="popup-form">
            <label>
              Task ID
              <TextBox value={String(selectedComment.taskId)} readOnly />
            </label>
            <label>
              Content
              <TextArea
                value={editContent}
                maxLength={5000}
                minHeight={120}
                onValueChanged={(event) => setEditContent(String(event.value ?? ""))}
              />
            </label>
            <div className="popup-actions">
              <Button text="Save" type="default" disabled={!canUpdate} onClick={handleUpdate} />
              <Button text="Delete" type="danger" stylingMode="outlined" disabled={!canDelete} onClick={handleDelete} />
            </div>
          </div>
        )}
      </Popup>
    </div>
  );
}
