import { useEffect, useMemo, useState } from "react";
import { Button } from "devextreme-react/button";
import DataGrid, { Column, Paging } from "devextreme-react/data-grid";
import Popup from "devextreme-react/popup";
import SelectBox from "devextreme-react/select-box";
import TextArea from "devextreme-react/text-area";
import TextBox from "devextreme-react/text-box";
import { Modal } from "../../components/Modal";
import { createEpic, deleteEpic, getEpics, updateEpic } from "../../services/epicService";
import type { BackendEpic } from "../../types/app";
import { Status } from "../../types/task";
import { getErrorMessage } from "../../utils/getErrorMessage";

type DetailPopupMode = "view" | "edit" | null;

type EpicForm = {
  title: string;
  description: string;
  status: number;
};

const epicStatusOptions = [
  { id: Status.NotStarted, label: "Not Started" },
  { id: Status.InProgress, label: "In Progress" },
  { id: Status.Completed, label: "Completed" },
  { id: Status.OnHold, label: "On Hold" },
  { id: Status.Cancelled, label: "Cancelled" },
];

const emptyEpicForm: EpicForm = {
  title: "",
  description: "",
  status: Status.NotStarted,
};

type EpicsSectionProps = {
  projectId: string;
  canCreateEpic: boolean;
  canUpdateEpic: boolean;
  canDeleteEpic: boolean;
};

export function EpicsSection({
  projectId,
  canCreateEpic,
  canUpdateEpic,
  canDeleteEpic,
}: EpicsSectionProps) {
  const [epics, setEpics] = useState<BackendEpic[]>([]);
  const [loading, setLoading] = useState(false);
  const [pageError, setPageError] = useState("");
  const [createError, setCreateError] = useState("");
  const [editError, setEditError] = useState("");

  const [showCreatePopup, setShowCreatePopup] = useState(false);
  const [createForm, setCreateForm] = useState<EpicForm>(emptyEpicForm);

  const [selectedEpic, setSelectedEpic] = useState<BackendEpic | null>(null);
  const [popupMode, setPopupMode] = useState<DetailPopupMode>(null);
  const [editForm, setEditForm] = useState<EpicForm>(emptyEpicForm);

  const sortedEpics = useMemo(
    () =>
      [...epics].sort(
        (a, b) =>
          new Date(b.createdAt ?? "").getTime() -
          new Date(a.createdAt ?? "").getTime()
      ),
    [epics]
  );

  useEffect(() => {
    const loadEpics = async () => {
      setLoading(true);
      setPageError("");

      try {
        const result = await getEpics(projectId);
        setEpics(result);
      } catch (error) {
        setPageError(getErrorMessage(error, "Failed to load epics."));
      } finally {
        setLoading(false);
      }
    };

    void loadEpics();
  }, [projectId]);

  const openEpicDetails = (epic: BackendEpic, mode: DetailPopupMode) => {
    setSelectedEpic(epic);
    setPopupMode(mode);
    setEditForm({
      title: epic.title,
      description: epic.description ?? "",
      status: epic.status,
    });
    setEditError("");
  };

  const closeEpicDetails = () => {
    setSelectedEpic(null);
    setPopupMode(null);
    setEditForm(emptyEpicForm);
    setEditError("");
  };

  const handleCreateEpic = async () => {
    if (!canCreateEpic) {
      setCreateError("You do not have permission to create epics.");
      return;
    }

    if (!createForm.title.trim()) {
      setCreateError("Epic title is required.");
      return;
    }

    setCreateError("");

    try {
      const created = await createEpic({
        projectId,
        title: createForm.title.trim(),
        description: createForm.description.trim() || undefined,
        status: createForm.status,
      });

      setEpics((prev) => [created, ...prev]);
      setCreateForm(emptyEpicForm);
      setShowCreatePopup(false);
    } catch (error) {
      setCreateError(getErrorMessage(error, "Failed to create epic."));
    }
  };

  const handleUpdateEpic = async () => {
    if (!selectedEpic) {
      return;
    }

    if (!canUpdateEpic) {
      setEditError("You do not have permission to update epics.");
      return;
    }

    if (!editForm.title.trim()) {
      setEditError("Epic title is required.");
      return;
    }

    setEditError("");

    try {
      const updated = await updateEpic(selectedEpic.id, {
        title: editForm.title.trim(),
        description: editForm.description.trim() || undefined,
        status: editForm.status,
      });

      setEpics((prev) =>
        prev.map((epic) => (epic.id === updated.id ? updated : epic))
      );

      // Return to Project Details list view immediately after save.
      closeEpicDetails();
    } catch (error) {
      setEditError(getErrorMessage(error, "Failed to update epic."));
    }
  };

  const handleDeleteEpic = async (epic: BackendEpic) => {
    if (!canDeleteEpic) {
      setPageError("You do not have permission to delete epics.");
      return;
    }

    const confirmed = window.confirm(`Delete epic "${epic.title}"?`);
    if (!confirmed) {
      return;
    }

    setPageError("");

    try {
      await deleteEpic(epic.id);
      setEpics((prev) => prev.filter((item) => item.id !== epic.id));

      if (selectedEpic?.id === epic.id) {
        closeEpicDetails();
      }
    } catch (error) {
      setPageError(getErrorMessage(error, "Failed to delete epic."));
    }
  };

  return (
    <section className="card">
      <div className="page-title-row">
        <h2>Epics</h2>
        <Button
          text="New Epic"
          icon="plus"
          disabled={!canCreateEpic}
          onClick={() => {
            setCreateError("");
            setShowCreatePopup(true);
          }}
        />
      </div>

      {pageError && <div className="form-error">{pageError}</div>}
      {loading && <div className="page-inline-info">Refreshing epics...</div>}

      <DataGrid
        dataSource={sortedEpics}
        keyExpr="id"
        showBorders={false}
        noDataText="No epics yet."
        onRowClick={(event) => {
          if (event.rowType !== "data" || !event.data) return;

          const target = event.event?.target as HTMLElement | null;
          if (target?.closest(".inline-actions")) return;

          openEpicDetails(event.data as BackendEpic, "view");
        }}
      >
        <Column dataField="title" caption="Title" />
        <Column
          dataField="status"
          caption="Status"
          width={140}
          cellRender={({ data }: { data: BackendEpic }) =>
            epicStatusOptions.find((option) => option.id === data.status)?.label ??
            String(data.status)
          }
        />
        <Column
          dataField="updatedAt"
          caption="Updated"
          width={170}
          cellRender={({ data }: { data: BackendEpic }) =>
            data.updatedAt ? new Date(data.updatedAt).toLocaleString() : "-"
          }
        />
        <Column
          caption="Actions"
          width={180}
          cellRender={({ data }: { data: BackendEpic }) => (
            <div
              className="inline-actions"
              onClick={(event) => event.stopPropagation()}
              onMouseDown={(event) => event.stopPropagation()}
              onPointerDown={(event) => event.stopPropagation()}
            >
              <Button
                text="Edit"
                stylingMode="text"
                disabled={!canUpdateEpic}
                onClick={(event) => {
                  event?.event?.preventDefault?.();
                  event?.event?.stopPropagation?.();
                  openEpicDetails(data, "edit");
                }}
              />
              <Button
                text="Delete"
                type="danger"
                stylingMode="text"
                disabled={!canDeleteEpic}
                onClick={(event) => {
                  event?.event?.preventDefault?.();
                  event?.event?.stopPropagation?.();
                  void handleDeleteEpic(data);
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
        title="Create Epic"
        width={560}
        height="auto"
        showCloseButton
      >
        <div className="popup-form">
          {createError && <div className="form-error">{createError}</div>}

          <label>
            Title
            <TextBox
              value={createForm.title}
              maxLength={500}
              onValueChanged={(event) =>
                setCreateForm((prev) => ({
                  ...prev,
                  title: String(event.value ?? ""),
                }))
              }
            />
          </label>

          <label>
            Description
            <TextArea
              value={createForm.description}
              maxLength={5000}
              minHeight={90}
              onValueChanged={(event) =>
                setCreateForm((prev) => ({
                  ...prev,
                  description: String(event.value ?? ""),
                }))
              }
            />
          </label>

          <label>
            Status
            <SelectBox
              dataSource={epicStatusOptions}
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
              disabled={!canCreateEpic}
              onClick={handleCreateEpic}
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
        visible={selectedEpic !== null}
        onClose={closeEpicDetails}
        title={
          selectedEpic
            ? popupMode === "edit"
              ? `Edit Epic: ${selectedEpic.title}`
              : `Epic: ${selectedEpic.title}`
            : "Epic"
        }
        width={620}
      >
        {selectedEpic && (
          <div className="popup-form">
            {popupMode === "edit" && editError && <div className="form-error">{editError}</div>}

            <label>
              Title
              <TextBox
                value={editForm.title}
                maxLength={500}
                readOnly={popupMode === "view"}
                onValueChanged={(event) =>
                  setEditForm((prev) => ({
                    ...prev,
                    title: String(event.value ?? ""),
                  }))
                }
              />
            </label>

            <label>
              Description
              <TextArea
                value={editForm.description}
                maxLength={5000}
                minHeight={90}
                readOnly={popupMode === "view"}
                onValueChanged={(event) =>
                  setEditForm((prev) => ({
                    ...prev,
                    description: String(event.value ?? ""),
                  }))
                }
              />
            </label>

            <label>
              Status
              <SelectBox
                dataSource={epicStatusOptions}
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
                    onClick={closeEpicDetails}
                  />
                  <Button
                    text="Save"
                    type="default"
                    disabled={!canUpdateEpic}
                    onClick={handleUpdateEpic}
                  />
                </>
              ) : (
                <Button
                  text="Close"
                  stylingMode="outlined"
                  onClick={closeEpicDetails}
                />
              )}
            </div>
          </div>
        )}
      </Modal>
    </section>
  );
}
