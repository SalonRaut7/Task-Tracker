import Chart, {
  CommonSeriesSettings,
  Legend,
  Series,
  Tooltip,
  ValueAxis,
} from "devextreme-react/chart";
import DataGrid, { Column, Paging } from "devextreme-react/data-grid";
import { PieChart, Series as PieSeries } from "devextreme-react/pie-chart";
import SelectBox from "devextreme-react/select-box";
import { useApp } from "../context/AppContext";
import { Status, TaskPriority } from "../types/task";

export function ReportsPage() {
  const { tasks, projects, projectsApiAvailable } = useApp();

  const completedTasks = tasks.filter((item) => item.status === Status.Completed).length;
  const inProgressTasks = tasks.filter((item) => item.status === Status.InProgress).length;
  const pendingTasks = tasks.filter((item) => item.status === Status.NotStarted).length;
  const totalTasks = tasks.length;

  const statusData = [
    { status: "Completed", value: completedTasks },
    { status: "In Progress", value: inProgressTasks },
    { status: "Not Started", value: pendingTasks },
    { status: "On Hold", value: tasks.filter((item) => item.status === Status.OnHold).length },
    { status: "Cancelled", value: tasks.filter((item) => item.status === Status.Cancelled).length },
  ];

  const priorityData = [
    { priority: "Highest", value: tasks.filter((item) => item.priority === TaskPriority.Highest).length },
    { priority: "High", value: tasks.filter((item) => item.priority === TaskPriority.High).length },
    { priority: "Medium", value: tasks.filter((item) => item.priority === TaskPriority.Medium).length },
    { priority: "Low", value: tasks.filter((item) => item.priority === TaskPriority.Low).length },
    { priority: "Lowest", value: tasks.filter((item) => item.priority === TaskPriority.Lowest).length },
  ];

  const projectRows = projects.map((project) => ({
    id: project.id,
    project: project.name,
    status: project.status ?? "-",
    updatedAt: project.updatedAt,
  }));

  const completionRate = totalTasks === 0 ? 0 : Math.round((completedTasks / totalTasks) * 100);

  return (
    <div className="page-stack">
      <section className="page-title-row">
        <div>
          <h1>Reports</h1>
          <p className="page-subtitle">Analytics and insights from backend task data</p>
        </div>
        <SelectBox
          dataSource={["Last 7 days", "Last 30 days", "Last 90 days", "All time"]}
          defaultValue="All time"
          width={170}
        />
      </section>

      <section className="kpi-grid">
        <article className="card kpi-card">
          <span>Total Tasks</span>
          <strong>{totalTasks}</strong>
        </article>
        <article className="card kpi-card">
          <span>Completion Rate</span>
          <strong>{completionRate}%</strong>
        </article>
        <article className="card kpi-card">
          <span>In Progress</span>
          <strong>{inProgressTasks}</strong>
        </article>
        <article className="card kpi-card">
          <span>Pending</span>
          <strong>{pendingTasks}</strong>
        </article>
      </section>

      <section className="dashboard-grid">
        <article className="card chart-card">
          <h2>Status Distribution</h2>
          <PieChart dataSource={statusData} height={260}>
            <PieSeries argumentField="status" valueField="value" />
            <Legend visible verticalAlignment="bottom" horizontalAlignment="center" />
            <Tooltip enabled />
          </PieChart>
        </article>

        <article className="card chart-card">
          <h2>Priority Breakdown</h2>
          <Chart dataSource={priorityData} height={260}>
            <CommonSeriesSettings argumentField="priority" type="bar" />
            <Series valueField="value" name="Tasks" color="#2f6de8" />
            <ValueAxis allowDecimals={false} />
            <Legend visible={false} />
            <Tooltip enabled />
          </Chart>
        </article>
      </section>

      <section className="card">
        <h2>Projects Overview</h2>
        {!projectsApiAvailable && (
          <p className="page-subtitle">
            Projects endpoint is not available in the current backend.
          </p>
        )}

        <DataGrid
          dataSource={projectRows}
          keyExpr="id"
          showBorders={false}
          rowAlternationEnabled
          noDataText={projectsApiAvailable ? "No projects returned by backend." : "Projects API not available."}
        >
          <Column dataField="project" caption="Project" />
          <Column dataField="status" caption="Status" />
          <Column
            dataField="updatedAt"
            caption="Updated"
            cellRender={({ data }) =>
              data.updatedAt ? new Date(data.updatedAt).toLocaleDateString() : "-"
            }
          />
          <Paging enabled={false} />
        </DataGrid>
      </section>
    </div>
  );
}
