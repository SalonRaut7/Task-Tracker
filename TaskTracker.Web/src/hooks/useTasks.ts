import { useCallback, useEffect, useState } from "react";
import type { TaskDto, CreateTaskDto, UpdateTaskDto } from "../types/task";
import * as taskService from "../services/taskService";
import { getErrorMessage } from "../components/tasks/taskHelpers";

export function useTasks() {
  const [tasks, setTasks] = useState<TaskDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  const clearError = useCallback(() => {
    setErrorMessage("");
  }, []);

  const fetchTasks = useCallback(async () => {
    setLoading(true);
    clearError();

    try {
      const data = await taskService.getTasks();
      setTasks(data);
    } catch (err) {
      console.error("Failed to fetch tasks:", err);
      setErrorMessage(getErrorMessage(err, "Failed to fetch tasks."));
    } finally {
      setLoading(false);
    }
  }, [clearError]);

  const createTask = useCallback(async (dto: CreateTaskDto) => {
    await taskService.createTask(dto);
  }, []);

  const updateTask = useCallback(async (id: number, dto: UpdateTaskDto) => {
    await taskService.updateTask(id, dto);
  }, []);

  const deleteTask = useCallback(async (id: number) => {
    await taskService.deleteTask(id);
  }, []);

  useEffect(() => {
    void fetchTasks();
  }, [fetchTasks]);

  return {
    tasks,
    loading,
    errorMessage,
    setErrorMessage,
    clearError,
    fetchTasks,
    createTask,
    updateTask,
    deleteTask,
  };
}