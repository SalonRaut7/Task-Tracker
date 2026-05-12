import React, { useEffect, useMemo, useState } from "react";
import { Button } from "devextreme-react/button";
import DataGrid, { Column, Paging } from "devextreme-react/data-grid";
import DateBox from "devextreme-react/date-box";
import DropDownButton from "devextreme-react/drop-down-button";
import Popup from "devextreme-react/popup";
import TextArea from "devextreme-react/text-area";
import TextBox from "devextreme-react/text-box";
import { Modal } from "../../components/Modal";
import {
  archiveSprint,
  cancelSprint,
  completeSprint,   
  createSprint,
  deleteSprint,
  getSprints,
  startSprint,
  updateSprint,
} from "../../services/sprintService";
import { SprintStatus, sprintStatusLabel, type BackendSprint } from "../../types/app";
import { getErrorMessage } from "../../utils/getErrorMessage";
import { toDateOnly } from "../../utils/toDateOnly";

type DetailPopupMode = "view" | "edit" | null;

type SprintForm = {
  name: string;
  goal: string;
  startDate: string;
  endDate: string;
};

const emptySprintForm: SprintForm = {
  name: "",
  goal: "",
  startDate: "",
  endDate: "",
};

const dropDownOptions = {
  wrapperAttr: { class: "modal-selectbox-overlay" },
};

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
  const [pageSuccess, setPageSuccess] = useState("");
  const [createError, setCreateError] = useState("");
  const [editError, setEditError] = useState("");
  const [actionLoading, setActionLoading] = useState(false);

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

  const statusLabel = (status: number): string =>
    sprintStatusLabel[status as SprintStatus] ?? String(status);

  const updateSprintInList = (updated: BackendSprint) =>
    setSprints((prev) =>
      prev.map((s) => (s.id === updated.id ? updated : s))
    );

  const clearMessages = () => {
    setPageError("");
    setPageSuccess("");
  };

  const openSprintDetails = (sprint: BackendSprint, mode: DetailPopupMode) => {
    setSelectedSprint(sprint);
    setPopupMode(mode);
    setEditForm({
      name: sprint.name,
      goal: sprint.goal ?? "",
      startDate: toDateOnly(sprint.startDate),
      endDate: toDateOnly(sprint.endDate),
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

    if (createForm.startDate >= createForm.endDate) {
      setCreateError("End date must be after start date (minimum 1 day duration).");
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
      });

      setSprints((prev) => [created, ...prev]);
      setCreateForm(emptySprintForm);
      setShowCreatePopup(false);
    } catch (error) {
      setCreateError(getErrorMessage(error, "Failed to create sprint."));
    }
  };

  const handleUpdateSprint = async () => {
    if (!selectedSprint) return;

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

    if (editForm.startDate >= editForm.endDate) {
      setEditError("End date must be after start date (minimum 1 day duration).");
      return;
    }

    setEditError("");

    try {
      const updated = await updateSprint(selectedSprint.id, {
        name: editForm.name.trim(),
        goal: editForm.goal.trim() || undefined,
        startDate: editForm.startDate,
        endDate: editForm.endDate,
      });

      updateSprintInList(updated);
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
    if (!confirmed) return;

    clearMessages();

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

  const handleStartSprint = async (sprint: BackendSprint) => {
    clearMessages();
    setActionLoading(true);
    try {
      const updated = await startSprint(sprint.id);
      updateSprintInList(updated);
      setPageSuccess(`Sprint "${updated.name}" is now Active.`);
      if (selectedSprint?.id === sprint.id)
        setSelectedSprint(updated);
    } catch (error) {
      setPageError(getErrorMessage(error, "Failed to start sprint."));
    } finally {
      setActionLoading(false);
    }
  };

  const handleCompleteSprint = async (sprint: BackendSprint) => {
    clearMessages();
    const confirmed = window.confirm(
      `Complete sprint "${sprint.name}"?\n\nIncomplete tasks will be moved back to the backlog.`
    );
    if (!confirmed) return;

    setActionLoading(true);
    try {
      const result = await completeSprint(sprint.id);
      updateSprintInList(result.sprint);
      const msg = result.rolledOverTaskCount > 0
        ? `Sprint "${result.sprint.name}" completed. ${result.rolledOverTaskCount} incomplete task(s) moved to backlog.`
        : `Sprint "${result.sprint.name}" completed. All tasks were finished!`;
      setPageSuccess(msg);
      if (selectedSprint?.id === sprint.id)
        setSelectedSprint(result.sprint);
    } catch (error) {
      setPageError(getErrorMessage(error, "Failed to complete sprint."));
    } finally {
      setActionLoading(false);
    }
  };

  const handleCancelSprint = async (sprint: BackendSprint) => {
    clearMessages();
    const confirmed = window.confirm(
      `Cancel sprint "${sprint.name}"?\n\nAll tasks will be moved back to the backlog.`
    );
    if (!confirmed) return;

    setActionLoading(true);
    try {
      const updated = await cancelSprint(sprint.id);
      updateSprintInList(updated);
      setPageSuccess(`Sprint "${updated.name}" has been cancelled.`);
      if (selectedSprint?.id === sprint.id)
        setSelectedSprint(updated);
    } catch (error) {
      setPageError(getErrorMessage(error, "Failed to cancel sprint."));
    } finally {
      setActionLoading(false);
    }
  };

  const handleArchiveSprint = async (sprint: BackendSprint) => {
    clearMessages();
    const promptValue = window.prompt(`Archive reason for sprint "${sprint.name}":`);
    if (promptValue === null) return;
    
    const reason = promptValue.trim();
    if (!reason) {
      setPageError("Archive reason is required.");
      return;
    }

    setActionLoading(true);
    try {
      const updated = await archiveSprint(sprint.id, reason);
      updateSprintInList(updated);
      setPageSuccess(`Sprint "${updated.name}" has been archived.`);
      if (selectedSprint?.id === sprint.id)
        setSelectedSprint(updated);
    } catch (error) {
      setPageError(getErrorMessage(error, "Failed to archive sprint."));
    } finally {
      setActionLoading(false);
    }
  };

  const renderLifecycleActions = (sprint: BackendSprint) => {
    const status = sprint.status as SprintStatus;
    const btns: React.ReactElement[] = [];

    if (status === SprintStatus.Planning) {
      btns.push(
        <Button
          key="start"
          text="Start Sprint"
          type="success"
          stylingMode="outlined"
          disabled={actionLoading || !canUpdateSprint}
          onClick={(e) => {
            e?.event?.stopPropagation?.();
            void handleStartSprint(sprint);
          }}
        />
      );
      btns.push(
        <Button
          key="cancel"
          text="Cancel"
          type="danger"
          stylingMode="text"
          disabled={actionLoading || !canUpdateSprint}
          onClick={(e) => {
            e?.event?.stopPropagation?.();
            void handleCancelSprint(sprint);
          }}
        />
      );
    }

    if (status === SprintStatus.Active) {
      btns.push(
        <Button
          key="complete"
          text="Complete"
          type="success"
          stylingMode="outlined"
          disabled={actionLoading || !canUpdateSprint}
          onClick={(e) => {
            e?.event?.stopPropagation?.();
            void handleCompleteSprint(sprint);
          }}
        />
      );
      btns.push(
        <Button
          key="cancel"
          text="Cancel"
          type="danger"
          stylingMode="text"
          disabled={actionLoading || !canUpdateSprint}
          onClick={(e) => {
            e?.event?.stopPropagation?.();
            void handleCancelSprint(sprint);
          }}
        />
      );
    }

    if (status === SprintStatus.Completed || status === SprintStatus.Cancelled) {
      btns.push(
        <Button
          key="archive"
          text="Archive"
          stylingMode="outlined"
          disabled={actionLoading || !canUpdateSprint}
          onClick={(e) => {
            e?.event?.stopPropagation?.();
            void handleArchiveSprint(sprint);
          }}
        />
      );
    }

    return btns;
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
      {pageSuccess && <div className="form-success">{pageSuccess}</div>}
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
            statusLabel(data.status)
          }
        />
        <Column
          caption="Actions"
          width={320}
          cellRender={({ data }: { data: BackendSprint }) => {
            const isEditAllowed = canUpdateSprint && ![SprintStatus.Completed, SprintStatus.Cancelled, SprintStatus.Archived].includes(data.status);
            const isDeleteAllowed = canDeleteSprint && [SprintStatus.Planning, SprintStatus.Cancelled].includes(data.status);

            const dropdownItems = [];
            
            if (isEditAllowed) {
              dropdownItems.push({
                id: "edit",
                text: "Edit...",
                icon: "edit",
              });
            }
            
            if (isDeleteAllowed) {
              dropdownItems.push({
                id: "delete",
                text: "Delete",
                icon: "trash",
              });
            }

            return (
              <div
                className="inline-actions"
                onClick={(event) => event.stopPropagation()}
                onMouseDown={(event) => event.stopPropagation()}
                onPointerDown={(event) => event.stopPropagation()}
              >
                <Button
                  key="view"
                  text="View"
                  icon="eye"
                  stylingMode="text"
                  onClick={(e) => {
                    e?.event?.stopPropagation?.();
                    openSprintDetails(data, "view");
                  }}
                />
                {renderLifecycleActions(data)}
                
                {dropdownItems.length > 0 && (
                  <DropDownButton
                    icon="overflow"
                    showArrowIcon={false}
                    items={dropdownItems}
                    displayExpr="text"
                    keyExpr="id"
                    stylingMode="text"
                    dropDownOptions={{ width: 140, container: "body" }}
                    onButtonClick={(e) => {
                      e.event?.preventDefault?.();
                      e.event?.stopPropagation?.();
                    }}
                    onItemClick={(e) => {
                      e.event?.preventDefault?.();
                      e.event?.stopPropagation?.();
                      if (e.itemData?.id === "edit") {
                        openSprintDetails(data, "edit");
                      } else if (e.itemData?.id === "delete") {
                        void handleDeleteSprint(data);
                      }
                    }}
                  />
                )}
              </div>
            );
          }}
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
              maxLength={200}
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
              maxLength={1000}
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
                dropDownOptions={dropDownOptions}
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
                dropDownOptions={dropDownOptions}
                onValueChanged={(event) =>
                  setCreateForm((prev) => ({
                    ...prev,
                    endDate: toDateOnly(event.value),
                  }))
                }
              />
            </label>
          </div>

          <p style={{ color: "var(--text-muted)", fontSize: "0.82rem", margin: 0 }}>
            New sprints always start in <strong>Planning</strong> status. Use lifecycle actions to start, complete, cancel, or archive.
          </p>

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
            <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
              <span style={{ fontWeight: 600, fontSize: "0.9rem" }}>Status:</span>
              <span className="role-chip">{statusLabel(selectedSprint.status)}</span>
            </div>

            <label>
              Name
              <TextBox
                value={editForm.name}
                maxLength={200}
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
                maxLength={1000}
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
                  dropDownOptions={dropDownOptions}
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
                  dropDownOptions={dropDownOptions}
                  onValueChanged={(event) =>
                    setEditForm((prev) => ({
                      ...prev,
                      endDate: toDateOnly(event.value),
                    }))
                  }
                />
              </label>
            </div>

            {popupMode === "view" && (
              <div className="inline-actions" style={{ gap: "0.5rem", flexWrap: "wrap" }}>
                {renderLifecycleActions(selectedSprint)}
              </div>
            )}

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
