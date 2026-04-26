import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { Button } from "devextreme-react/button";
import DataGrid, { Column, Paging } from "devextreme-react/data-grid";
import TextArea from "devextreme-react/text-area";
import TextBox from "devextreme-react/text-box";
import { useNavigate } from "react-router-dom";
import { Modal } from "../components/Modal";
import { useApp } from "../context/AppContext";
import { AppPermissions } from "../security/permissions";
import {
  createOrganization,
  deleteOrganization,
  getOrganizations,
  updateOrganization,
} from "../services/organizationService";
import type { BackendOrganization } from "../types/app";
import { getErrorMessage } from "../utils/getErrorMessage";

type OrganizationForm = {
  name: string;
  slug: string;
  description: string;
};

type PopupMode = "view" | "edit" | null;

const emptyForm: OrganizationForm = {
  name: "",
  slug: "",
  description: "",
};

function toSlug(input: string): string {
  return input
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9\s-]/g, "")
    .replace(/\s+/g, "-")
    .replace(/-+/g, "-");
}

export function OrganizationsPage() {
  const navigate = useNavigate();
  const { hasPermission } = useApp();

  const canCreate = hasPermission(AppPermissions.OrganizationsCreate);
  const canUpdateOrg = useCallback((orgId: string) => hasPermission(AppPermissions.OrganizationsUpdate, "Organization", orgId), [hasPermission]);
  const canDeleteOrg = useCallback((orgId: string) => hasPermission(AppPermissions.OrganizationsDelete, "Organization", orgId), [hasPermission]);
  const canViewOrgMembers = useCallback((orgId: string) => hasPermission(AppPermissions.MembersView, "Organization", orgId), [hasPermission]);

  const [organizations, setOrganizations] = useState<BackendOrganization[]>([]);
  const [query, setQuery] = useState("");
  const [loading, setLoading] = useState(false);
  const [pageError, setPageError] = useState("");
  const [createError, setCreateError] = useState("");
  const [editError, setEditError] = useState("");

  const [showCreate, setShowCreate] = useState(false);
  const [createForm, setCreateForm] = useState<OrganizationForm>(emptyForm);

  const [selectedOrganization, setSelectedOrganization] = useState<BackendOrganization | null>(null);
  const [popupMode, setPopupMode] = useState<PopupMode>(null);
  const [editForm, setEditForm] = useState<OrganizationForm>(emptyForm);

  const filteredOrganizations = useMemo(() => {
    const term = query.trim().toLowerCase();

    if (!term) {
      return organizations;
    }

    return organizations.filter((organization) => {
      const description = organization.description ?? "";
      return (
        organization.name.toLowerCase().includes(term) ||
        organization.slug.toLowerCase().includes(term) ||
        description.toLowerCase().includes(term)
      );
    });
  }, [organizations, query]);

  const loadOrganizations = useCallback(async () => {
    setLoading(true);
    setPageError("");

    try {
      const result = await getOrganizations();
      setOrganizations(result);
    } catch (error) {
      setPageError(getErrorMessage(error, "Failed to load organizations."));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadOrganizations();
  }, [loadOrganizations]);

  const openOrganization = useCallback((organization: BackendOrganization, mode: PopupMode) => {
    setSelectedOrganization(organization);
    setPopupMode(mode);
    setEditForm({
      name: organization.name,
      slug: organization.slug,
      description: organization.description ?? "",
    });
    setEditError("");
  }, []);

  const closeDetailsPopup = useCallback(() => {
    setSelectedOrganization(null);
    setPopupMode(null);
    setEditError("");
  }, []);

  const closeCreatePopup = useCallback(() => {
    setShowCreate(false);
    setCreateForm(emptyForm);
    setCreateError("");
  }, []);

  const handleCreate = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      setCreateError("");

      if (!canCreate) {
        setCreateError("You do not have permission to create organizations.");
        return;
      }

      if (!createForm.name.trim()) {
        setCreateError("Organization name is required.");
        return;
      }

      if (!createForm.slug.trim()) {
        setCreateError("Organization slug is required.");
        return;
      }

      try {
        const created = await createOrganization({
          name: createForm.name.trim(),
          slug: createForm.slug.trim(),
          description: createForm.description.trim() || undefined,
        });

        setOrganizations((prev) => [created, ...prev]);
        closeCreatePopup();
      } catch (error) {
        setCreateError(getErrorMessage(error, "Failed to create organization."));
      }
    },
    [canCreate, closeCreatePopup, createForm]
  );

  const handleSave = useCallback(async () => {
    if (!selectedOrganization) {
      return;
    }

    if (!canUpdateOrg(selectedOrganization.id)) {
      setEditError("You do not have permission to update this organization.");
      return;
    }

    if (!editForm.name.trim()) {
      setEditError("Organization name is required.");
      return;
    }

    if (!editForm.slug.trim()) {
      setEditError("Organization slug is required.");
      return;
    }

    setEditError("");

    try {
      const updated = await updateOrganization(selectedOrganization.id, {
        name: editForm.name.trim(),
        slug: editForm.slug.trim(),
        description: editForm.description.trim() || undefined,
      });

      setOrganizations((prev) =>
        prev.map((item) => (item.id === updated.id ? updated : item))
      );

      setSelectedOrganization(updated);
      setEditForm({
        name: updated.name,
        slug: updated.slug,
        description: updated.description ?? "",
      });
      setPopupMode("view");
    } catch (error) {
      setEditError(getErrorMessage(error, "Failed to update organization."));
    }
  }, [canUpdateOrg, editForm, selectedOrganization]);

  const handleDeleteFromGrid = useCallback(
    async (organization: BackendOrganization) => {
      if (!canDeleteOrg(organization.id)) {
        setPageError("You do not have permission to delete this organization.");
        return;
      }

      const confirmed = window.confirm(`Delete organization "${organization.name}"?`);
      if (!confirmed) {
        return;
      }

      setPageError("");

      try {
        await deleteOrganization(organization.id);
        setOrganizations((prev) => prev.filter((item) => item.id !== organization.id));

        if (selectedOrganization?.id === organization.id) {
          closeDetailsPopup();
        }
      } catch (error) {
        setPageError(getErrorMessage(error, "Failed to delete organization."));
      }
    },
    [canDeleteOrg, closeDetailsPopup, selectedOrganization]
  );

  return (
    <div className="page-stack">
      <section className="page-title-row">
        <div>
          <h1>Organizations</h1>
          <p className="page-subtitle">Manage tenant organizations and ownership boundaries</p>
        </div>

        <Button
          text="New Organization"
          icon="plus"
          type="default"
          disabled={!canCreate}
          onClick={() => {
            setCreateError("");
            setShowCreate(true);
          }}
        />
      </section>

      {loading && <div className="page-inline-info">Refreshing organizations...</div>}
      {pageError && <div className="form-error">{pageError}</div>}

      <section className="toolbar-row">
        <TextBox
          mode="search"
          placeholder="Search organizations..."
          value={query}
          onValueChanged={(event) => setQuery(String(event.value ?? ""))}
          width="min(100%, 340px)"
          showClearButton
        />
      </section>

      <section className="card">
        <DataGrid
          dataSource={filteredOrganizations}
          keyExpr="id"
          showBorders={false}
          rowAlternationEnabled
          hoverStateEnabled
          columnAutoWidth
          columnHidingEnabled
          wordWrapEnabled
          columnMinWidth={120}
          noDataText="No organizations found."
          onRowClick={(event) => {
            const target = event.event?.target;
            if (target instanceof HTMLElement && target.closest("[data-grid-actions]")) {
              return;
            }

            openOrganization(event.data as BackendOrganization, "view");
          }}
        >
          <Column dataField="name" caption="Name" />
          <Column dataField="slug" caption="Slug" width={180} />
          <Column
            dataField="updatedAt"
            caption="Updated"
            width={150}
            cellRender={({ data }: { data: BackendOrganization }) =>
              data.updatedAt ? new Date(data.updatedAt).toLocaleDateString() : "-"
            }
          />
          <Column
            caption="Actions"
            width={320}
            allowSorting={false}
            allowFiltering={false}
            cellRender={({ data }: { data: BackendOrganization }) => (
              <div
                className="inline-actions"
                data-grid-actions
                onClick={(e) => e.stopPropagation()}
                onMouseDown={(e) => e.stopPropagation()}
                onPointerDown={(e) => e.stopPropagation()}
              >
                <Button
                  text="Team"
                  stylingMode="text"
                  disabled={!canViewOrgMembers(data.id)}
                  onClick={(event) => {
                    event.event?.stopPropagation();
                    navigate(`/organizations/${data.id}/members`);
                  }}
                />
                <Button
                  text="View"
                  stylingMode="text"
                  onClick={(event) => {
                    event.event?.stopPropagation();
                    openOrganization(data, "view");
                  }}
                />
                <Button
                  text="Edit"
                  stylingMode="text"
                  disabled={!canUpdateOrg(data.id)}
                  onClick={(event) => {
                    event.event?.stopPropagation();
                    openOrganization(data, "edit");
                  }}
                />
                <Button
                  text="Delete"
                  stylingMode="text"
                  elementAttr={{ class: "danger" }}
                  disabled={!canDeleteOrg(data.id)}
                  onClick={(event) => {
                    event.event?.stopPropagation();
                    void handleDeleteFromGrid(data);
                  }}
                />
              </div>
            )}
          />
          <Paging enabled pageSize={10} />
        </DataGrid>
      </section>

      <Modal
        visible={showCreate}
        onClose={closeCreatePopup}
        title="Create Organization"
        width={600}
      >
        <form className="popup-form" onSubmit={handleCreate}>
          {createError && <div className="form-error">{createError}</div>}

          <label>
            Name
            <TextBox
              value={createForm.name}
              maxLength={200}
              onValueChanged={(event) => {
                const value = String(event.value ?? "");
                setCreateForm((prev) => ({
                  ...prev,
                  name: value,
                  slug: prev.slug ? prev.slug : toSlug(value),
                }));
              }}
            />
          </label>

          <label>
            Slug
            <TextBox
              value={createForm.slug}
              maxLength={200}
              onValueChanged={(event) =>
                setCreateForm((prev) => ({
                  ...prev,
                  slug: toSlug(String(event.value ?? "")),
                }))
              }
            />
          </label>

          <label>
            Description
            <TextArea
              value={createForm.description}
              maxLength={1000}
              onValueChanged={(event) =>
                setCreateForm((prev) => ({
                  ...prev,
                  description: String(event.value ?? ""),
                }))
              }
              minHeight={80}
            />
          </label>

          <div className="popup-actions">
            <Button text="Cancel" stylingMode="outlined" onClick={closeCreatePopup} />
            <Button text="Create" type="default" useSubmitBehavior disabled={!canCreate} />
          </div>
        </form>
      </Modal>

      <Modal
        visible={selectedOrganization !== null}
        onClose={closeDetailsPopup}
        title={
          selectedOrganization
            ? popupMode === "edit"
              ? `Edit Organization: ${selectedOrganization.name}`
              : `Organization: ${selectedOrganization.name}`
            : "Organization"
        }
        width={680}
      >
        {selectedOrganization && (
          <div className="popup-form">
            {popupMode === "edit" && editError && <div className="form-error">{editError}</div>}

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
              Slug
              <TextBox
                value={editForm.slug}
                maxLength={200}
                readOnly={popupMode === "view"}
                onValueChanged={(event) =>
                  setEditForm((prev) => ({
                    ...prev,
                    slug: toSlug(String(event.value ?? "")),
                  }))
                }
              />
            </label>

            <label>
              Description
              <TextArea
                value={editForm.description}
                maxLength={1000}
                readOnly={popupMode === "view"}
                onValueChanged={(event) =>
                  setEditForm((prev) => ({
                    ...prev,
                    description: String(event.value ?? ""),
                  }))
                }
                minHeight={90}
              />
            </label>

            <div className="popup-actions">
              {popupMode === "edit" ? (
                <>
                  <Button
                    text="Cancel"
                    stylingMode="outlined"
                    onClick={() => {
                      // cancel = close popup
                      closeDetailsPopup();
                    }}
                  />
                  <Button
                    text="Save"
                    type="default"
                    disabled={!canUpdateOrg(selectedOrganization.id)}
                    onClick={() => void handleSave()}
                  />
                </>
              ) : (
                <>
                  {canViewOrgMembers(selectedOrganization.id) && (
                    <Button
                      text="Manage Team"
                      stylingMode="outlined"
                      onClick={() => {
                        closeDetailsPopup();
                        navigate(`/organizations/${selectedOrganization.id}/members`);
                      }}
                    />
                  )}
                  <Button
                    text="Close"
                    stylingMode="outlined"
                    onClick={closeDetailsPopup}
                  />
                </>
              )}
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}