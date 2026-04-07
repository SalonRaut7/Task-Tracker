import { useEffect, useState } from "react";
import DataGrid, {
  Column,
  Editing,
  LoadPanel,
  Pager,
  Paging,
  SearchPanel
} from "devextreme-react/data-grid";
import type { TaskDto, CreateTaskDto, UpdateTaskDto } from "../types/task";
import { Status } from "../types/task";
import * as taskService from "../services/taskService";
import "./TaskList.css";

const statusOptions = [
  { id: Status.NotStarted, name: "NotStarted" },
  { id: Status.InProgress, name: "InProgress" },
  { id: Status.Completed, name: "Completed" },
  { id: Status.Cancelled, name: "Cancelled" },
  { id: Status.OnHold, name: "OnHold" },
];

export default function TaskList() {
  const [tasks, setTasks] = useState<TaskDto[]>([]);
  const [loading, setLoading] = useState(false);

  const fetchTasks = async () => {
    setLoading(true);
    try {
      const data = await taskService.getTasks();
      setTasks(data);
    } catch (err) {
      console.error("Failed to fetch tasks:", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTasks();
  }, []);

  const onRowInserted = async (e: any) => {
    const newTask: CreateTaskDto = {
      title: e.data.title,
      description: e.data.description,
    };

    try {
      await taskService.createTask(newTask);
      await fetchTasks();
    } catch (err) {
      console.error("Failed to create task:", err);
      await fetchTasks();
    }
  };

  const onRowUpdated = async (e: any) => {
    const updatedTask: UpdateTaskDto = {
      title: e.data.title ?? e.oldData.title,
      description: e.data.description ?? e.oldData.description,
      status: e.data.status ?? e.oldData.status,
    };

    try {
      await taskService.updateTask(e.key, updatedTask);
      await fetchTasks();
    } catch (err) {
      console.error("Failed to update task:", err);
      await fetchTasks();
    }
  };

  const onRowRemoving = async (e: any) => {
    try {
      await taskService.deleteTask(e.key);
    } catch (err) {
      console.error("Failed to delete task:", err);
      e.cancel = true;
    }
  };

  const onRowRemoved = async () => {
    await fetchTasks();
  };

  const onInitNewRow = (e: any) => {
    // Display default status in the new row
    e.data.status = Status.NotStarted;
  };

  const onEditorPreparing = (e: any) => {
    // Prevent editing status for new rows
    if (
      e.parentType === "dataRow" &&
      e.dataField === "status" &&
      e.row?.isNewRow
    ) {
      e.editorOptions.disabled = true;
    }
  };

  return (
    <div className="tasklist-container">
      <h2>Task List</h2>

      <DataGrid
        dataSource={tasks}
        keyExpr="id"
        showBorders={true}
        columnAutoWidth={true}
        rowAlternationEnabled={true}
        wordWrapEnabled={true}
        height={500}
        onRowInserted={onRowInserted}
        onRowUpdated={onRowUpdated}
        onRowRemoving={onRowRemoving}
        onRowRemoved={onRowRemoved}
        onInitNewRow={onInitNewRow}
        onEditorPreparing={onEditorPreparing}
      >
        <LoadPanel enabled={loading} />
        <SearchPanel visible={true} placeholder="Search tasks..." />
        <Paging defaultPageSize={10} />
        <Pager
          showPageSizeSelector={true}
          allowedPageSizes={[5, 10, 20]}
          showInfo={true}
        />
        <Editing
          allowUpdating={true}
          allowAdding={true}
          allowDeleting={true}
          mode="row"
        />

        <Column dataField="title" caption="Title" />
        <Column dataField="description" caption="Description" />
        <Column
          dataField="status"
          caption="Status"
          lookup={{
            dataSource: statusOptions,
            valueExpr: "id",
            displayExpr: "name",
          }}
          cellRender={(cellData) => (
            <span>{Status[cellData.value] ?? "Unknown"}</span>
          )}
        />
      </DataGrid>
    </div>
  );
}