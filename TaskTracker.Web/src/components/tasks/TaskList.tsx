import { useCallback, useState } from "react";
import notify from "devextreme/ui/notify";
import { confirm } from "devextreme/ui/dialog";
import type { ExportingEvent } from "devextreme/ui/data_grid";
import type { TaskDto } from "../../types/task";
import { useTasks } from "../../hooks/useTasks";
import { buildCreateDto, buildUpdateDto, getErrorMessage, type TaskDraft } from "./taskHelpers";
import { exportTasks } from "./taskExport";
import { isTaskValid, validateTask } from "./taskValidation";
import TaskGrid from "./TaskGrid";
import TaskEditPopup from "./TaskEditPopup";
import "./TaskList.css";

export default function TaskList() {
  const {
    tasks,
    loading,
    errorMessage,
    setErrorMessage,
    clearError,
    fetchTasks,
    createTask,
    updateTask,
    deleteTask,
  } = useTasks();

  const [popupVisible, setPopupVisible] = useState(false);
  const [popupMode, setPopupMode] = useState<"add" | "edit">("add");
  const [selectedTask, setSelectedTask] = useState<TaskDto | null>(null);
  const [saving, setSaving] = useState(false);

  const showSuccess = useCallback((message: string) => {
    notify(message, "success", 2500);
  }, []);

  const showError = useCallback((message: string) => {
    setErrorMessage(message);
  }, [setErrorMessage]);

  const handleAdd = useCallback(() => {
    clearError();
    setSelectedTask(null);
    setPopupMode("add");
    setPopupVisible(true);
  }, [clearError]);

  const handleEdit = useCallback((task: TaskDto) => {
    clearError();
    setSelectedTask(task);
    setPopupMode("edit");
    setPopupVisible(true);
  }, [clearError]);

  const handleDelete = useCallback(
    async (task: TaskDto) => {
      const result = await confirm(
        `Are you sure you want to delete "${task.title}"?`,
        "Confirm Delete"
      );

      if (!result) return;

      clearError();

      try {
        if (typeof task.id !== "number" || task.id <= 0) {
          showError("Invalid task id.");
          return;
        }

        await deleteTask(task.id);
        showSuccess("Task deleted successfully.");
        await fetchTasks();
      } catch (err) {
        console.error("Delete failed:", err);
        showError(getErrorMessage(err, "Failed to delete task."));
      }
    },
    [clearError, deleteTask, fetchTasks, showError, showSuccess]
  );

  const handleSave = useCallback(
    async (data: TaskDraft) => {
      clearError();

      const dto = popupMode === "add" ? buildCreateDto(data) : buildUpdateDto(data);
      const errors = validateTask(dto);

      if (!isTaskValid(dto)) {
        const firstError = Object.values(errors)[0] ?? "Please correct the form errors.";
        showError(firstError);
        return;
      }

      setSaving(true);

      try {
        if (popupMode === "add") {
          await createTask(dto);
          showSuccess("Task created successfully.");
        } else {
          if (!selectedTask?.id || selectedTask.id <= 0) {
            showError("Invalid task id.");
            return;
          }

          await updateTask(selectedTask.id, dto);
          showSuccess("Task updated successfully.");
        }

        setPopupVisible(false);
        setSelectedTask(null);
        await fetchTasks();
      } catch (err) {
        console.error("Save failed:", err);
        showError(getErrorMessage(err, "Operation failed."));
      } finally {
        setSaving(false);
      }
    },
    [
      clearError,
      createTask,
      fetchTasks,
      popupMode,
      selectedTask,
      showError,
      showSuccess,
      updateTask,
    ]
  );

  const onExporting = useCallback(
    async (e: ExportingEvent<TaskDto, number>) => {
      await exportTasks(e);
    },
    []
  );

  return (
    <div className="tasklist-page">
      <div className="tasklist-header">
        <h2>Task List</h2>
        <p>
          Manage tasks with validation, controlled editing, exporting, column
          chooser, filtering, and reordering.
        </p>
      </div>

      {errorMessage && (
        <div className="tasklist-error-banner" role="alert">
          <span>{errorMessage}</span>
          <button
            type="button"
            className="tasklist-error-close"
            onClick={clearError}
            aria-label="Dismiss error"
          >
            ×
          </button>
        </div>
      )}

      <div className="tasklist-grid-card">
        <TaskGrid
          tasks={tasks}
          loading={loading}
          onAdd={handleAdd}
          onEdit={handleEdit}
          onDelete={handleDelete}
          onExporting={onExporting}
        />
      </div>

      <TaskEditPopup
        visible={popupVisible}
        mode={popupMode}
        task={selectedTask}
        saving={saving}
        onClose={() => {
          if (!saving) {
            setPopupVisible(false);
            setSelectedTask(null);
            clearError();
          }
        }}
        onSave={handleSave}
      />
    </div>
  );
}