import type { TaskletResponse } from "@/api/generated/types/TaskletResponse";
import { colorEnum } from "@/api/generated/types/Color";
import { priorityEnum } from "@/api/generated/types/Priority";
import { statusEnum } from "@/api/generated/types/Status";

const fixtureUserId = "00000000-0000-0000-0000-000000000001";

/**
 * Local visual data for the pre-API checkpoint.
 *
 * These values use generated API types so the card layout is exercised against
 * the same shape the Tasklet store will receive later.
 */
export const taskletFixtures: TaskletResponse[] = [
  {
    id: "11111111-1111-1111-1111-111111111111",
    userId: fixtureUserId,
    title: "Prepare project notes",
    body: "Collect the implementation details and open questions before the next planning pass.",
    status: statusEnum.NotStarted,
    priority: priorityEnum.High,
    explicitOrder: null,
    pinned: true,
    color: colorEnum.Blue,
    createdAtUtc: new Date("2026-06-06T15:30:00.000Z"),
    completedAtUtc: null,
    dueAtUtc: new Date("2026-06-09T14:00:00.000Z"),
  },
  {
    id: "22222222-2222-2222-2222-222222222222",
    userId: fixtureUserId,
    title: "Review API client contracts",
    body: "Confirm generated request and response models cover create, edit, pin, and delete flows.",
    status: statusEnum.InProgress,
    priority: priorityEnum.Medium,
    explicitOrder: null,
    pinned: false,
    color: colorEnum.Emerald,
    createdAtUtc: new Date("2026-06-05T19:45:00.000Z"),
    completedAtUtc: null,
    dueAtUtc: null,
  },
  {
    id: "33333333-3333-3333-3333-333333333333",
    userId: fixtureUserId,
    title: "Document auth baseline",
    body: "",
    status: statusEnum.Completed,
    priority: priorityEnum.Low,
    explicitOrder: null,
    pinned: false,
    color: colorEnum.Purple,
    createdAtUtc: new Date("2026-06-03T13:15:00.000Z"),
    completedAtUtc: new Date("2026-06-04T16:20:00.000Z"),
    dueAtUtc: null,
  },
];
