import type { TaskDraft, TaskItem, TaskStatus, TaskTypeStat } from "./types";

const taskApi = import.meta.env.VITE_TASK_API_URL ?? "http://localhost:5265";
const analyticsApi = import.meta.env.VITE_ANALYTICS_API_URL ?? "http://localhost:5196";

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    headers: { "Content-Type": "application/json", ...init?.headers },
    ...init,
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null) as { title?: string; message?: string } | null;
    throw new Error(body?.title ?? body?.message ?? "Не вдалося виконати запит. Спробуйте ще раз.");
  }

  return response.status === 204 ? undefined as T : response.json() as Promise<T>;
}

export const api = {
  getTasks: () => request<TaskItem[]>(`${taskApi}/api/tasks`),
  createTask: (draft: TaskDraft) => request<TaskItem>(`${taskApi}/api/tasks`, {
    method: "POST", body: JSON.stringify({ ...draft, assigneeId: draft.assigneeId || null }),
  }),
  updateTask: (id: string, draft: TaskDraft) => request<TaskItem>(`${taskApi}/api/tasks/${id}`, {
    method: "PUT", body: JSON.stringify({ ...draft, assigneeId: draft.assigneeId || null }),
  }),
  updateStatus: (id: string, status: TaskStatus) => request<TaskItem>(`${taskApi}/api/tasks/${id}/status`, {
    method: "PATCH", body: JSON.stringify({ status }),
  }),
  deleteTask: (id: string) => request<void>(`${taskApi}/api/tasks/${id}`, { method: "DELETE" }),
  getTaskTypeStats: () => request<TaskTypeStat[]>(`${analyticsApi}/api/analytics/task-types`),
};
