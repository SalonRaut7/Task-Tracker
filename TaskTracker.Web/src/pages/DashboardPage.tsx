import Chart, {
  ArgumentAxis,
  CommonSeriesSettings,
  Legend,
  Series,
  Tooltip,
  ValueAxis,
} from "devextreme-react/chart";
import { PieChart, Series as PieSeries } from "devextreme-react/pie-chart";
import { Link, useNavigate } from "react-router-dom";
import { useApp } from "../context/AppContext";
import { Status } from "../types/task";
import { priorityLabel, statusLabel, taskKey } from "../utils/taskPresentation";
import { formatDistanceToNow } from "../utils/time";

const ACTIVITY_ICON: Record<string, string> = {
  TaskCreated: "add",
  TaskUpdated: "edit",
  TaskDeleted: "trash",
  TaskReassigned: "user",
  TaskDueSoon: "clock",
  TaskOverdue: "warning",
  ProjectUpdated: "edit",
  ProjectDeleted: "trash",
  OrganizationUpdated: "edit",
  OrganizationDeleted: "trash",
};

function normalizeDay(date: string): string {
  return new Date(date).toLocaleDateString(undefined, {
    month: "short",
    day: "numeric",
  });
}

export function DashboardPage() {
  const { tasks, notifications, user, loadingData } = useApp();
  const navigate = useNavigate();

  const totalTasks = tasks.length;
  const completedTasks = tasks.filter((item) => item.status === Status.Completed).length;
  const inProgressTasks = tasks.filter((item) => item.status === Status.InProgress).length;
  const overdueTasks = tasks.filter(
    (item) =>
      item.endDate &&
      new Date(`${item.endDate}T00:00:00`).getTime() < Date.now() &&
      item.status !== Status.Completed
  ).length;

  const recentTasks = [...tasks]
    .sort(
      (a, b) =>
        new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
    )
    .slice(0, 5);

  // Use notifications as the activity feed (last 10)
  const recentActivity = notifications.slice(0, 10);

  const trendMap = new Map<string, { day: string; created: number; done: number }>();
  for (const task of tasks) {
    const day = normalizeDay(task.createdAt);
    const entry = trendMap.get(day) ?? { day, created: 0, done: 0 };
    entry.created += 1;
    if (task.status === Status.Completed) {
      entry.done += 1;
    }
    trendMap.set(day, entry);
  }

  const trendData = [...trendMap.values()];

  const distributionData = [
    { status: "Done", value: completedTasks },
    { status: "In Progress", value: inProgressTasks },
    { status: "To Do", value: totalTasks - completedTasks - inProgressTasks },
  ];

  return (
    <div className="page-stack">
      <section>
        <h1>Dashboard</h1>
        <p className="page-subtitle">
          Welcome back, {user?.firstName}. Here is your task overview.
        </p>
      </section>

      {loadingData && <div className="page-inline-info">Refreshing dashboard data...</div>}

      <section className="kpi-grid">
        <article className="card kpi-card">
          <span>Total Tasks</span>
          <strong>{totalTasks}</strong>
        </article>
        <article className="card kpi-card">
          <span>Completed</span>
          <strong>{completedTasks}</strong>
        </article>
        <article className="card kpi-card">
          <span>In Progress</span>
          <strong>{inProgressTasks}</strong>
        </article>
        <article className="card kpi-card">
          <span>Overdue</span>
          <strong>{overdueTasks}</strong>
        </article>
      </section>

      <section className="dashboard-grid">
        <article className="card chart-card">
          <div className="card-head">
            <h2>Task Progress</h2>
          </div>
          <Chart dataSource={trendData} height={280}>
            <CommonSeriesSettings argumentField="day" type="spline" />
            <Series valueField="created" name="Created" color="#2563eb" />
            <Series valueField="done" name="Done" color="#10b981" />
            <ArgumentAxis discreteAxisDivisionMode="crossLabels" />
            <ValueAxis allowDecimals={false} />
            <Legend visible />
            <Tooltip enabled />
          </Chart>
        </article>

        <article className="card chart-card">
          <div className="card-head">
            <h2>Task Distribution</h2>
          </div>
          <PieChart dataSource={distributionData} height={280}>
            <PieSeries argumentField="status" valueField="value" />
            <Legend visible verticalAlignment="bottom" horizontalAlignment="center" />
            <Tooltip enabled />
          </PieChart>
        </article>
      </section>

      <section className="dashboard-grid">
        <article className="card">
          <div className="card-head">
            <h2>Recent Activity</h2>
            <span className="live-badge">● Live</span>
          </div>
          <div className="activity-list">
            {recentActivity.map((item) => (
              <button
                key={item.id}
                type="button"
                className="activity-item"
                onClick={() => {
                  if (item.taskId && item.projectId) {
                    navigate(`/tasks/${item.projectId}/${item.taskId}`);
                  }
                }}
              >
                <div className="activity-icon">
                  <i
                    className={`dx-icon dx-icon-${ACTIVITY_ICON[item.type] ?? "info"}`}
                    aria-hidden="true"
                  />
                </div>
                <div className="activity-content">
                  <div className="activity-message">{item.message}</div>
                  <small>{formatDistanceToNow(item.createdAt)}</small>
                </div>
              </button>
            ))}
            {recentActivity.length === 0 && (
              <p className="activity-empty">No recent activity. Task events will appear here in real time.</p>
            )}
          </div>
        </article>

        <article className="card">
          <div className="card-head split">
            <h2>Latest Tasks</h2>
            <Link to="/tasks">View all</Link>
          </div>
          <div className="simple-table-wrap">
            <table className="simple-table">
              <thead>
                <tr>
                  <th>Task</th>
                  <th>Status</th>
                  <th>Priority</th>
                  <th>End Date</th>
                </tr>
              </thead>
              <tbody>
                {recentTasks.map((task) => (
                  <tr key={task.id}>
                    <td>
                      {task.title || taskKey(task)}
                      <small>{taskKey(task)}</small>
                    </td>
                    <td>{statusLabel(task.status)}</td>
                    <td>{priorityLabel(task.priority)}</td>
                    <td>{task.endDate ? new Date(`${task.endDate}T00:00:00`).toLocaleDateString() : "-"}</td>
                  </tr>
                ))}
                {recentTasks.length === 0 && (
                  <tr>
                    <td colSpan={4}>No tasks available yet.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </article>
      </section>
    </div>
  );
}
