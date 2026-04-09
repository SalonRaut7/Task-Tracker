import { useCallback, useMemo, useState } from "react";
import CustomStore from "devextreme/data/custom_store";
import type { TaskDto, CreateTaskDto, UpdateTaskDto } from "../types/task";
import * as taskService from "../services/taskService";
import type { TaskGridLoadOptions } from "../services/taskService";
import { getErrorMessage } from "../components/tasks/taskHelpers";

export function useTasks() {
  const [storeVersion, setStoreVersion] = useState(0);
  const [errorMessage, setErrorMessage] = useState("");

  const clearError = useCallback(() => {
    setErrorMessage("");
  }, []);

  const fetchTasks = useCallback(async () => {
    setStoreVersion((currentVersion) => currentVersion + 1);
  }, []);

  const tasks = useMemo(
    () =>
      new CustomStore<TaskDto, number>({
        key: "id",
        loadMode: "processed",
        cacheRawData: false,
        load: async (loadOptions: TaskGridLoadOptions) => {
          try {
            return await taskService.loadTasks(loadOptions);
          } catch (err) {
            console.error("Failed to load tasks:", err);
            setErrorMessage(getErrorMessage(err, "Failed to fetch tasks."));
            throw err;
          }
        },
      }),
    [storeVersion]
  );

  const createTask = useCallback(async (dto: CreateTaskDto) => {
    await taskService.createTask(dto);
  }, []);

  const updateTask = useCallback(async (id: number, dto: UpdateTaskDto) => {
    await taskService.updateTask(id, dto);
  }, []);

  const deleteTask = useCallback(async (id: number) => {
    await taskService.deleteTask(id);
  }, []);

  return {
    tasks,
    errorMessage,
    setErrorMessage,
    clearError,
    fetchTasks,
    createTask,
    updateTask,
    deleteTask,
  };
}