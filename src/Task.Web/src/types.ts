export const taskTypes = ["design", "develop", "review", "testing"] as const;
export const taskStatuses = ["to do", "in progress", "done"] as const;
export const taskPriorities = ["low", "normal", "high"] as const;

export type TaskType = (typeof taskTypes)[number];
export type TaskStatus = (typeof taskStatuses)[number];
export type TaskPriority = (typeof taskPriorities)[number];

export interface TaskItem {
  id: string;
  title: string;
  description: string;
  type: TaskType;
  status: TaskStatus;
  priority: TaskPriority;
  assigneeId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface TaskDraft {
  title: string;
  description: string;
  type: TaskType;
  status: TaskStatus;
  priority: TaskPriority;
  assigneeId: string;
}

export interface TaskTypeStat {
  taskType: TaskType;
  todo: number;
  inProgress: number;
  done: number;
}
