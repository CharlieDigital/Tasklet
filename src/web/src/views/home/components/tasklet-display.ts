import type { Color } from "@/api/generated/types/Color";
import type { Priority } from "@/api/generated/types/Priority";
import type { Status } from "@/api/generated/types/Status";

export const avatarColors: Record<Color, string> = {
  Amber: "#f59e0b",
  Lime: "#84cc16",
  Emerald: "#10b981",
  Cyan: "#06b6d4",
  Blue: "#3b82f6",
  Indigo: "#6366f1",
  Purple: "#a855f7",
  Pink: "#ec4899",
  Rose: "#f43f5e",
};

export const statusLabels: Record<Status, string> = {
  NotStarted: "Not started",
  InProgress: "In progress",
  Completed: "Completed",
  Blocked: "Blocked",
};

export const priorityLabels: Record<Priority, string> = {
  Eventually: "Eventually",
  Low: "Low",
  Medium: "Medium",
  High: "High",
  Critical: "Critical",
};

export const priorityTagTypes: Record<
  Priority,
  "default" | "error" | "success" | "warning" | "primary" | "info"
> = {
  Eventually: "default",
  Low: "info",
  Medium: "primary",
  High: "warning",
  Critical: "error",
};
