import { useMemo, useState } from "react";
import DataGrid, { Column, Paging } from "devextreme-react/data-grid";
import TextBox from "devextreme-react/text-box";
import { useNavigate } from "react-router-dom";
import { useApp } from "../context/AppContext";
import type { BackendProject } from "../types/app";

export function ProjectsPage() {
  const navigate = useNavigate();
  const {
    projects,
    loadingData,
    projectsApiAvailable,
    projectsApiMessage,
  } = useApp();
  const [query, setQuery] = useState("");

  const filteredProjects = useMemo(() => {
    const term = query.trim().toLowerCase();

    return projects
      .filter((project) => {
        if (!term) {
          return true;
        }

        return (
          project.name.toLowerCase().includes(term) ||
          (project.key ?? "").toLowerCase().includes(term)
        );
      });
  }, [projects, query]);

  return (
    <div className="page-stack">
      <section className="page-title-row">
        <div>
          <h1>Projects</h1>
          <p className="page-subtitle">Projects loaded directly from backend APIs</p>
        </div>
      </section>

      {!projectsApiAvailable && (
        <div className="card state-card warning-state">
          <h3>Projects endpoint unavailable</h3>
          <p>
            {projectsApiMessage ||
              "No /api/Projects endpoint is currently exposed by the backend."}
          </p>
        </div>
      )}

      {loadingData && <div className="page-inline-info">Refreshing projects...</div>}

      <section className="toolbar-row">
        <TextBox
          mode="search"
          placeholder="Search projects..."
          value={query}
          onValueChanged={(event) => setQuery(String(event.value ?? ""))}
          width={320}
          showClearButton
        />
      </section>

      <section className="card">
        <DataGrid
          dataSource={filteredProjects}
          keyExpr="id"
          rowAlternationEnabled
          hoverStateEnabled
          showBorders={false}
          onRowClick={(event) => navigate(`/projects/${(event.data as BackendProject).id}`)}
          noDataText={projectsApiAvailable ? "No projects returned by backend." : "Projects API not available."}
        >
          <Column dataField="name" caption="Project" />
          <Column dataField="key" caption="Key" width={120} />
          <Column dataField="status" caption="Status" width={140} />
          <Column
            dataField="updatedAt"
            caption="Updated"
            width={140}
            cellRender={({ data }: { data: BackendProject }) =>
              data.updatedAt ? new Date(data.updatedAt).toLocaleDateString() : "-"
            }
          />
          <Paging enabled pageSize={10} />
        </DataGrid>
      </section>
    </div>
  );
}
