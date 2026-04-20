import { useEffect, useMemo, useState } from "react";
import { Button } from "devextreme-react/button";
import DataGrid, { Column, Paging } from "devextreme-react/data-grid";
import DateBox from "devextreme-react/date-box";
import Popup from "devextreme-react/popup";
import SelectBox from "devextreme-react/select-box";
import TextArea from "devextreme-react/text-area";
import TextBox from "devextreme-react/text-box";
import { Modal } from "../../components/Modal";
import {
  createSprint,
  deleteSprint,
  getSprints,
  updateSprint,
} from "../../services/sprintService";
import type { BackendSprint } from "../../types/app";
import { getErrorMessage } from "../../utils/getErrorMessage";

type DetailPopupMode = "view" | "edit" | null;

type SprintForm = {
  name: string;
  goal: string;
  startDate: string;
  endDate: string;
  status: number;
};

const sprintStatusOptions = [
  { id: 0, label: "Planning" },
  { id: 1, label: "Active" },
  { id: 2, label: "Completed" },
];

const emptySprintForm: SprintForm = {
  name: "",
  goal: "",
  startDate: "",
  endDate: "",
  status: 0,
};

function toDateOnly(value: unknown): string {
  if (!value) return "";

  const date = new Date(value as string | number | Date);
  if (Number.isNaN(date.getTime())) return "";

  return date.toISOString().split("T")[0];
}

type SprintsSectionProps = {
  projectId: string;
  canCreateSprint: boolean;
  canUpdateSprint: boolean;
  canDeleteSprint: boolean;
};

export function SprintsSection({
  projectId,
  canCreateSprint,
  canUpdateSprint,
  canDeleteSprint,
}: SprintsSectionProps) {
  const [sprints, setSprints] = useState<BackendSprint[]>([]);
  const [loading, setLoading] = useState(false);
  const [pageError, setPageError] = useState("");
  const [createError, setCreateError] = useState("");
  const [editError, setEditError] = useState("");

  const [showCreatePopup, setShowCreatePopup] = useState(false);
  const [createForm, setCreateForm] = useState<SprintForm>(emptySprintForm);

  const [selectedSprint, setSelectedSprint] = useState<BackendSprint | null>(null);
  const [popupMode, setPopupMode] = useState<DetailPopupMode>(null);
  const [editForm, setEditForm] = useState<SprintForm>(emptySprintForm);

  const sortedSprints = useMemo(
    () =>
      [...sprints].sort(
        (a, b) =>
          new Date(b.createdAt ?? "").getTime() -
          new Date(a.createdAt ?? "").getTime()
      ),
    [sprints]
  );

  useEffect(() => {
    const loadSprints = async () => {
      setLoading(true);
      setPageError("");

      try {
        const result = await getSprints(projectId);
        setSprints(result);
      } catch (error) {
        setPageError(getErrorMessage(error, "Failed to load sprints."));
      } finally {
        setLoading(false);
      }
    };

    void loadSprints();
  }, [projectId]);

  const openSprintDetails = (sprint: BackendSprint, mode: DetailPopupMode) => {
    setSelectedSprint(sprint);
    setPopupMode(mode);
    setEditForm({
      name: sprint.name,
      goal: sprint.goal ?? "",
      startDate: toDateOnly(sprint.startDate),
      endDate: toDateOnly(sprint.endDate),
      status: sprint.status,
    });
    setEditError("");
  };

  const closeSprintDetails = () => {
    setSelectedSprint(null);
    setPopupMode(null);
    setEditForm(emptySprintForm);
    setEditError("");
  };

  const handleCreateSprint = async () => {
    if (!canCreateSprint) {
      setCreateError("You do not have permission to create sprints.");
      return;
    }

    if (!createForm.name.trim()) {
      setCreateError("Sprint name is required.");
      return;
    }

    if (!createForm.startDate || !createForm.endDate) {
      setCreateError("Sprint start and end dates are required.");
      return;
    }

    setCreateError("");

    try {
      const created = await createSprint({
        projectId,
        name: createForm.name.trim(),
        goal: createForm.goal.trim() || undefined,
        startDate: createForm.startDate,
        endDate: createForm.endDate,
        status: createForm.status,
      });

      setSprints((prev) => [created, ...prev]);
      setCreateForm(emptySprintForm);
      setShowCreatePopup(false);
    } catch (error) {
      setCreateError(getErrorMessage(error, "Failed to create sprint."));
    }
  };

  const handleUpdateSprint = async () => {
    if (!selectedSprint) {
      return;
    }

    if (!canUpdateSprint) {
      setEditError("You do not have permission to update sprints.");
      return;
    }

    if (!editForm.name.trim()) {
      setEditError("Sprint name is required.");
      return;
    }

    if (!editForm.startDate || !editForm.endDate) {
      setEditError("Sprint start and end dates are required.");
      return;
    }

    if (editForm.startDate > editForm.endDate) {
      setEditError("Sprint start date cannot be after end date.");
      return;
    }

    setEditError("");

    try {
      const updated = await updateSprint(selectedSprint.id, {
        name: editForm.name.trim(),
        goal: editForm.goal.trim() || undefined,
        startDate: editForm.startDate,
        endDate: editForm.endDate,
        status: editForm.status,
      });

      setSprints((prev) =>
        prev.map((sprint) => (sprint.id === updated.id ? updated : sprint))
      );

      // Return to Project Details list view immediately after save.
      closeSprintDetails();
    } catch (error) {
      setEditError(getErrorMessage(error, "Failed to update sprint."));
    }
  };

  const handleDeleteSprint = async (sprint: BackendSprint) => {
    if (!canDeleteSprint) {
      setPageError("You do not have permission to delete sprints.");
      return;
    }

    const confirmed = window.confirm(`Delete sprint "${sprint.name}"?`);
    if (!confirmed) {
      return;
    }

    setPageError("");

    try {
      await deleteSprint(sprint.id);
      setSprints((prev) => prev.filter((item) => item.id !== sprint.id));

      if (selectedSprint?.id === sprint.id) {
        closeSprintDetails();
      }
    } catch (error) {
      setPageError(getErrorMessage(error, "Failed to delete sprint."));
    }
  };

  return (
    <section className="card">
      <div className="page-title-row">
        <h2>Sprints</h2>
        <Button
          text="New Sprint"
          icon="plus"
          disabled={!canCreateSprint}
          onClick={() => {
            setCreateError("");
            setShowCreatePopup(true);
          }}
        />
      </div>

      {pageError && <div className="form-error">{pageError}</div>}
      {loading && <div className="page-inline-info">Refreshing sprints...</div>}

      <DataGrid
        dataSource={sortedSprints}
        keyExpr="id"
        showBorders={false}
        noDataText="No sprints yet."
        onRowClick={(event) => {
          if (event.rowType !== "data" || !event.data) return;

          const target = event.event?.target as HTMLElement | null;
          if (target?.closest(".inline-actions")) return;

          openSprintDetails(event.data as BackendSprint, "view");
        }}
      >
        <Column dataField="name" caption="Sprint" />
        <Column dataField="startDate" caption="Start" width={120} />
        <Column dataField="endDate" caption="End" width={120} />
        <Column
          dataField="status"
          caption="Status"
          width={130}
          cellRender={({ data }: { data: BackendSprint }) =>
            sprintStatusOptions.find((option) => option.id === data.status)
              ?.label ?? String(data.status)
          }
        />
        <Column
          caption="Actions"
          width={180}
          cellRender={({ data }: { data: BackendSprint }) => (
            <div
              className="inline-actions"
              onClick={(event) => event.stopPropagation()}
              onMouseDown={(event) => event.stopPropagation()}
              onPointerDown={(event) => event.stopPropagation()}
            >
              <Button
                text="Edit"
                stylingMode="text"
                disabled={!canUpdateSprint}
                onClick={(event) => {
                  event?.event?.preventDefault?.();
                  event?.event?.stopPropagation?.();
                  openSprintDetails(data, "edit");
                }}
              />
              <Button
                text="Delete"
                type="danger"
                stylingMode="text"
                disabled={!canDeleteSprint}
                onClick={(event) => {
                  event?.event?.preventDefault?.();
                  event?.event?.stopPropagation?.();
                  void handleDeleteSprint(data);
                }}
              />
            </div>
          )}
        />
        <Paging enabled pageSize={8} />
      </DataGrid>

      <Popup
        visible={showCreatePopup}
        onHiding={() => {
          setShowCreatePopup(false);
          setCreateError("");
        }}
        title="Create Sprint"
        width={620}
        height="auto"
        showCloseButton
      >
        <div className="popup-form">
          {createError && <div className="form-error">{createError}</div>}

          <label>
            Name
            <TextBox
              value={createForm.name}
              onValueChanged={(event) =>
                setCreateForm((prev) => ({
                  ...prev,
                  name: String(event.value ?? ""),
                }))
              }
            />
          </label>

          <label>
            Goal
            <TextArea
              value={createForm.goal}
              minHeight={80}
              onValueChanged={(event) =>
                setCreateForm((prev) => ({
                  ...prev,
                  goal: String(event.value ?? ""),
                }))
              }
            />
          </label>

          <div className="form-grid-two">
            <label>
              Start Date
              <DateBox
                type="date"
                value={createForm.startDate || null}
                onValueChanged={(event) =>
                  setCreateForm((prev) => ({
                    ...prev,
                    startDate: toDateOnly(event.value),
                  }))
                }
              />
            </label>

            <label>
              End Date
              <DateBox
                type="date"
                value={createForm.endDate || null}
                onValueChanged={(event) =>
                  setCreateForm((prev) => ({
                    ...prev,
                    endDate: toDateOnly(event.value),
                  }))
                }
              />
            </label>
          </div>

          <label>
            Status
            <SelectBox
              dataSource={sprintStatusOptions}
              displayExpr="label"
              valueExpr="id"
              value={createForm.status}
              onValueChanged={(event) =>
                setCreateForm((prev) => ({
                  ...prev,
                  status:
                    typeof event.value === "number" ? event.value : prev.status,
                }))
              }
            />
          </label>

          <div className="popup-actions">
            <Button
              text="Create"
              type="default"
              disabled={!canCreateSprint}
              onClick={handleCreateSprint}
            />
            <Button
              text="Cancel"
              stylingMode="outlined"
              onClick={() => {
                setShowCreatePopup(false);
                setCreateError("");
              }}
            />
          </div>
        </div>
      </Popup>

      <Modal
        visible={selectedSprint !== null}
        onClose={closeSprintDetails}
        title={
          selectedSprint
            ? popupMode === "edit"
              ? `Edit Sprint: ${selectedSprint.name}`
              : `Sprint: ${selectedSprint.name}`
            : "Sprint"
        }
        width={680}
      >
        {selectedSprint && (
          <div className="popup-form">
            {popupMode === "edit" && editError && <div className="form-error">{editError}</div>}

            <label>
              Name
              <TextBox
                value={editForm.name}
                readOnly={popupMode === "view"}
                onValueChanged={(event) =>
                  setEditForm((prev) => ({
                    ...prev,
                    name: String(event.value ?? ""),
                  }))
                }
              />
            </label>

            <label>
              Goal
              <TextArea
                value={editForm.goal}
                minHeight={80}
                readOnly={popupMode === "view"}
                onValueChanged={(event) =>
                  setEditForm((prev) => ({
                    ...prev,
                    goal: String(event.value ?? ""),
                  }))
                }
              />
            </label>

            <div className="form-grid-two">
              <label>
                Start Date
                <DateBox
                  type="date"
                  value={editForm.startDate || null}
                  readOnly={popupMode === "view"}
                  onValueChanged={(event) =>
                    setEditForm((prev) => ({
                      ...prev,
                      startDate: toDateOnly(event.value),
                    }))
                  }
                />
              </label>

              <label>
                End Date
                <DateBox
                  type="date"
                  value={editForm.endDate || null}
                  readOnly={popupMode === "view"}
                  onValueChanged={(event) =>
                    setEditForm((prev) => ({
                      ...prev,
                      endDate: toDateOnly(event.value),
                    }))
                  }
                />
              </label>
            </div>

            <label>
              Status
              <SelectBox
                dataSource={sprintStatusOptions}
                displayExpr="label"
                valueExpr="id"
                value={editForm.status}
                readOnly={popupMode === "view"}
                dropDownOptions={{
                  wrapperAttr: { class: "modal-selectbox-overlay" },
                }}
                onValueChanged={(event) =>
                  setEditForm((prev) => ({
                    ...prev,
                    status:
                      typeof event.value === "number" ? event.value : prev.status,
                  }))
                }
              />
            </label>

            <div className="popup-actions">
              {popupMode === "edit" ? (
                <>
                  <Button
                    text="Cancel"
                    stylingMode="outlined"
                    onClick={closeSprintDetails}
                  />
                  <Button
                    text="Save"
                    type="default"
                    disabled={!canUpdateSprint}
                    onClick={handleUpdateSprint}
                  />
                </>
              ) : (
                <Button
                  text="Close"
                  stylingMode="outlined"
                  onClick={closeSprintDetails}
                />
              )}
            </div>
          </div>
        )}
      </Modal>
    </section>
  );
}
