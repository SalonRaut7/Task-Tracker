import { useCallback, useEffect, useState } from "react";
import DataGrid, {
  Column,
  Editing,
  LoadPanel,
  Pager,
  Paging,
  Popup as EditingPopup,
  Form as EditingForm,
  SearchPanel,
} from "devextreme-react/data-grid";
import notify from "devextreme/ui/notify";
import type {
  SavingEvent,
  InitNewRowEvent,
} from "devextreme/ui/data_grid";
import type { ValidationCallbackData } from "devextreme/common";
import type { TaskDto, CreateTaskDto, UpdateTaskDto } from "../types/task";
import { Status, TaskPriority } from "../types/task";
import * as taskService from "../services/taskService";
import "./TaskList.css";

const statusOptions = [
  { id: Status.NotStarted, name: "Not Started" },
  { id: Status.InProgress, name: "In Progress" },
  { id: Status.Completed, name: "Completed" },
  { id: Status.OnHold, name: "On Hold" },
  { id: Status.Cancelled, name: "Cancelled" },
];

const priorityOptions = [
  { id: TaskPriority.Lowest, name: "Lowest" },
  { id: TaskPriority.Low, name: "Low" },
  { id: TaskPriority.Medium, name: "Medium" },
  { id: TaskPriority.High, name: "High" },
  { id: TaskPriority.Highest, name: "Highest" },
];

type TaskDraft = Partial<TaskDto>;

const statusClassMap: Record<number, string> = {
  [Status.NotStarted]: "badge badge-gray",
  [Status.InProgress]: "badge badge-blue",
  [Status.Completed]: "badge badge-green",
  [Status.OnHold]: "badge badge-yellow",
  [Status.Cancelled]: "badge badge-red",
};

const priorityClassMap: Record<number, string> = {
  [TaskPriority.Lowest]: "badge badge-gray",
  [TaskPriority.Low]: "badge badge-blue",
  [TaskPriority.Medium]: "badge badge-yellow",
  [TaskPriority.High]: "badge badge-orange",
  [TaskPriority.Highest]: "badge badge-red",
};

export default function TaskList() {
  const [tasks, setTasks] = useState<TaskDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  const showSuccess = useCallback((message: string) => {
    notify(message, "success", 2500);
  }, []);

  const showError = useCallback((message: string) => {
    setErrorMessage(message);
  }, []);

  const clearError = useCallback(() => {
    setErrorMessage("");
  }, []);

  const getErrorMessage = useCallback((err: unknown, fallback: string) => {
    if (err instanceof Error && err.message.trim()) {
      return err.message;
    }
    return fallback;
  }, []);

  const isValidDateRange = useCallback(
    (startDate?: string | Date | null, endDate?: string | Date | null): boolean => {
      if (!startDate || !endDate) return true;

      const start = new Date(startDate);
      const end = new Date(endDate);

      return start <= end;
    },
    []
  );

  const validateTaskBeforeSave = useCallback(
    (task: Partial<CreateTaskDto | UpdateTaskDto>): string | null => {
      const title = task.title?.trim() ?? "";
      const description = task.description?.trim() ?? "";

      if (!title) return "Title is required.";
      if (title.length > 100) return "Title cannot exceed 100 characters.";
      if (description.length > 500) return "Description cannot exceed 500 characters.";

      if (task.status === undefined || task.status === null) {
        return "Status is required.";
      }

      if (task.priority === undefined || task.priority === null) {
        return "Priority is required.";
      }

      return null;
    },
    []
  );

  const fetchTasks = useCallback(async () => {
    setLoading(true);
    clearError();

    try {
      const data = await taskService.getTasks();
      setTasks(data);
    } catch (err) {
      console.error("Failed to fetch tasks:", err);
      showError(getErrorMessage(err, "Failed to fetch tasks."));
    } finally {
      setLoading(false);
    }
  }, [clearError, getErrorMessage, showError]);

  useEffect(() => {
    void fetchTasks();
  }, [fetchTasks]);

  const onInitNewRow = useCallback((e: InitNewRowEvent<TaskDto, number>) => {
    e.data.status = Status.NotStarted;
    e.data.priority = TaskPriority.Medium;
    e.data.startDate = null;
    e.data.endDate = null;
    e.data.description = "";
  }, []);

  const buildCreateDto = (data: TaskDraft): CreateTaskDto => ({
    title: data.title?.trim() ?? "",
    description: data.description?.trim() ?? "",
    status: data.status ?? Status.NotStarted,
    priority: data.priority ?? TaskPriority.Medium,
    startDate: data.startDate ?? null,
    endDate: data.endDate ?? null,
  });

  const buildUpdateDto = (oldData: TaskDraft, newData: TaskDraft): UpdateTaskDto => ({
    title: (newData.title ?? oldData.title ?? "").trim(),
    description: (newData.description ?? oldData.description ?? "").trim(),
    status: newData.status ?? oldData.status ?? Status.NotStarted,
    priority: newData.priority ?? oldData.priority ?? TaskPriority.Medium,
    startDate: newData.startDate ?? oldData.startDate ?? null,
    endDate: newData.endDate ?? oldData.endDate ?? null,
  });

  const onSaving = useCallback(
    async (e: SavingEvent<TaskDto, number>) => {
      const change = e.changes?.[0];
      if (!change) return;

      e.cancel = true;
      clearError();

      try {
        if (change.type === "insert") {
          const dto = buildCreateDto(change.data ?? {});
          const validationError = validateTaskBeforeSave(dto);

          if (validationError) {
            showError(validationError);
            return;
          }

          await taskService.createTask(dto);
          showSuccess("Task created successfully.");
        }

        if (change.type === "update") {
          const oldData = tasks.find((task) => task.id === change.key) ?? {};
          const dto = buildUpdateDto(oldData, change.data ?? {});
          const validationError = validateTaskBeforeSave(dto);

          if (validationError) {
            showError(validationError);
            return;
          }

          if (typeof change.key !== "number" || change.key <= 0) {
            showError("Invalid task id.");
            return;
          }

          await taskService.updateTask(change.key, dto);
          showSuccess("Task updated successfully.");
        }

        if (change.type === "remove") {
          if (typeof change.key !== "number" || change.key <= 0) {
            showError("Invalid task id.");
            return;
          }

          await taskService.deleteTask(change.key);
          showSuccess("Task deleted successfully.");
        }

        await fetchTasks();
        e.component.cancelEditData();
      } catch (err) {
        console.error("Save failed:", err);
        showError(getErrorMessage(err, "Operation failed."));
        await fetchTasks();
      }
    },
    [clearError, fetchTasks, getErrorMessage, showError, showSuccess, tasks, validateTaskBeforeSave]
  );

  const endDateValidation = useCallback(
    (options: ValidationCallbackData): boolean => {
      const startDate = (options.data as TaskDraft | undefined)?.startDate ?? null;
      const endDate =
        options.value instanceof Date ||
        typeof options.value === "string" ||
        options.value === null
          ? options.value
          : null;

      return isValidDateRange(startDate, endDate);
    },
    [isValidDateRange]
  );

  const renderStatusBadge = (cellData: { value: Status }) => {
    const className = statusClassMap[cellData.value] ?? "badge badge-gray";
    const text =
      statusOptions.find((x) => x.id === cellData.value)?.name ?? "Unknown";

    return <span className={className}>{text}</span>;
  };

  const renderPriorityBadge = (cellData: { value: TaskPriority }) => {
    const className = priorityClassMap[cellData.value] ?? "badge badge-gray";
    const text =
      priorityOptions.find((x) => x.id === cellData.value)?.name ?? "Unknown";

    return <span className={className}>{text}</span>;
  };

  return (
    <div className="tasklist-page">
      <div className="tasklist-header">
        <h2>Task List</h2>
        <p>Manage your tasks with clean editing, validation, and status tracking.</p>
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
        <DataGrid<TaskDto, number>
          dataSource={tasks}
          keyExpr="id"
          showBorders={false}
          columnAutoWidth={true}
          rowAlternationEnabled={true}
          wordWrapEnabled={true}
          hoverStateEnabled={true}
          repaintChangesOnly={true}
          height={650}
          noDataText="No tasks found."
          onInitNewRow={onInitNewRow}
          onSaving={onSaving}
        >
          <LoadPanel enabled={loading} />
          <SearchPanel visible={true} placeholder="Search tasks..." width={260} />
          <Paging defaultPageSize={10} />
          <Pager
            showPageSizeSelector={true}
            allowedPageSizes={[5, 10, 20]}
            showInfo={true}
            showNavigationButtons={true}
          />

          <Editing
            mode="popup"
            allowAdding={true}
            allowUpdating={true}
            allowDeleting={true}
            useIcons={true}
            confirmDelete={true}
          >
            <EditingPopup
              title="Task Details"
              showTitle={true}
              width={720}
              height="auto"
            />
            <EditingForm colCount={2} labelLocation="top" />
          </Editing>

          <Column
            dataField="title"
            caption="Title"
            validationRules={[
              { type: "required", message: "Title is required." },
              { type: "stringLength", max: 100, message: "Title cannot exceed 100 characters." },
            ]}
          />

          <Column
            dataField="description"
            caption="Description"
            editorOptions={{ type: "dxTextArea", height: 100 }}
            validationRules={[
              { type: "stringLength", max: 500, message: "Description cannot exceed 500 characters." },
            ]}
          />

          <Column
            dataField="status"
            caption="Status"
            lookup={{
              dataSource: statusOptions,
              valueExpr: "id",
              displayExpr: "name",
            }}
            cellRender={renderStatusBadge}
            validationRules={[
              { type: "required", message: "Status is required." },
            ]}
          />

          <Column
            dataField="priority"
            caption="Priority"
            lookup={{
              dataSource: priorityOptions,
              valueExpr: "id",
              displayExpr: "name",
            }}
            cellRender={renderPriorityBadge}
            validationRules={[
              { type: "required", message: "Priority is required." },
            ]}
          />

          <Column
            dataField="startDate"
            caption="Start Date"
            dataType="date"
          />

          <Column
            dataField="endDate"
            caption="End Date"
            dataType="date"
            validationRules={[
              {
                type: "custom",
                reevaluate: true,
                message: "Start date must be before or equal to end date.",
                validationCallback: endDateValidation,
              },
            ]}
          />

          <Column
            dataField="createdAt"
            caption="Created At"
            dataType="datetime"
            allowEditing={false}
          />
        </DataGrid>
      </div>
    </div>
  );
}