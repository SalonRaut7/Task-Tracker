import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "devextreme-react/button";
import DataGrid, { Column } from "devextreme-react/data-grid";
import TextBox from "devextreme-react/text-box";
import { Modal } from "../components/Modal";
import { PaginationControls } from "../components/PaginationControls";
import { useDebouncedValue } from "../hooks/useDebouncedValue";
import { usePagination } from "../hooks/usePagination";
import {
  archiveUser,
  getUserDetails,
  loadUsers,
} from "../services/userService";
import type { BackendUserDetails, BackendUserSummary } from "../types/app";
import { getErrorMessage } from "../utils/getErrorMessage";

function formatDate(value?: string): string {
  if (!value) {
    return "-";
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "-" : date.toLocaleString();
}

export function UsersPage() {
  const navigate = useNavigate();

  const [users, setUsers] = useState<BackendUserSummary[]>([]);
  const [query, setQuery] = useState("");
  const debouncedQuery = useDebouncedValue(query, 250);
  const [loading, setLoading] = useState(false);
  const [pageError, setPageError] = useState("");
  const [totalCount, setTotalCount] = useState(0);
  const [reloadTick, setReloadTick] = useState(0);

  const { page, pageSize, skip, setPage, setPageSize, resetPage } = usePagination({
    totalCount,
    initialPageSize: 10,
  });

  const [selectedUser, setSelectedUser] = useState<BackendUserSummary | null>(null);
  const [userDetails, setUserDetails] = useState<BackendUserDetails | null>(null);
  const [detailsLoading, setDetailsLoading] = useState(false);
  const [detailsError, setDetailsError] = useState("");

  useEffect(() => {
    resetPage();
  }, [debouncedQuery, resetPage]);

  const loadUsersPage = useCallback(async () => {
    setLoading(true);
    setPageError("");

    try {
      const result = await loadUsers({
        search: debouncedQuery.trim() || undefined,
        skip,
        take: pageSize,
      });

      setUsers(result.data);
      setTotalCount(result.totalCount);
    } catch (error) {
      setUsers([]);
      setTotalCount(0);
      setPageError(getErrorMessage(error, "Failed to load users."));
    } finally {
      setLoading(false);
    }
  }, [debouncedQuery, pageSize, skip]);

  useEffect(() => {
    void loadUsersPage();
  }, [loadUsersPage, reloadTick]);

  const closeDetails = useCallback(() => {
    setSelectedUser(null);
    setUserDetails(null);
    setDetailsError("");
    setDetailsLoading(false);
  }, []);

  const openDetails = useCallback(async (user: BackendUserSummary) => {
    setSelectedUser(user);
    setUserDetails(null);
    setDetailsError("");
    setDetailsLoading(true);

    try {
      const details = await getUserDetails(user.userId);
      if (!details) {
        setDetailsError("User details were not found.");
        return;
      }

      setUserDetails(details);
    } catch (error) {
      setDetailsError(getErrorMessage(error, "Failed to load user details."));
    } finally {
      setDetailsLoading(false);
    }
  }, []);

  const handleArchive = useCallback(async (user: BackendUserSummary) => {
    const confirmed = window.confirm(`Archive user "${user.firstName} ${user.lastName}"?`);
    if (!confirmed) {
      return;
    }

    const reasonInput = window.prompt("Optional archive reason:", "");
    const reason = reasonInput ? reasonInput.trim() : undefined;

    setPageError("");

    try {
      await archiveUser(user.userId, reason);

      if (selectedUser?.userId === user.userId) {
        closeDetails();
      }

      setReloadTick((value) => value + 1);
    } catch (error) {
      setPageError(getErrorMessage(error, "Failed to archive user."));
    }
  }, [closeDetails, selectedUser?.userId]);

  return (
    <div className="page-stack">
      <section className="page-title-row">
        <div>
          <h1>Users</h1>
          <p className="page-subtitle">SuperAdmin user directory and account lifecycle controls</p>
        </div>

        <Button
          text="Open Archive"
          icon="folder"
          type="normal"
          onClick={() => navigate("/users/archive")}
        />
      </section>

      {loading && <div className="page-inline-info">Refreshing users...</div>}
      {pageError && <div className="form-error">{pageError}</div>}

      <section className="toolbar-row">
        <TextBox
          mode="search"
          placeholder="Search users by name or email..."
          value={query}
          onValueChanged={(event) => setQuery(String(event.value ?? ""))}
          width="min(100%, 360px)"
          showClearButton
        />
      </section>

      <section className="card">
        <DataGrid
          dataSource={users}
          keyExpr="userId"
          showBorders={false}
          rowAlternationEnabled
          hoverStateEnabled
          columnAutoWidth
          columnHidingEnabled
          wordWrapEnabled
          columnMinWidth={120}
          noDataText="No users found."
          onRowClick={(event) => {
            const target = event.event?.target;
            if (target instanceof HTMLElement && target.closest("[data-grid-actions]")) {
              return;
            }

            void openDetails(event.data as BackendUserSummary);
          }}
        >
          <Column
            caption="Name"
            cellRender={({ data }: { data: BackendUserSummary }) => `${data.firstName} ${data.lastName}`.trim()}
          />
          <Column dataField="email" caption="Email" />
          <Column dataField="organizationCount" caption="Orgs" width={90} />
          <Column dataField="projectCount" caption="Projects" width={90} />
          <Column dataField="assignedTaskCount" caption="Assigned" width={110} />
          <Column dataField="reportedTaskCount" caption="Reported" width={110} />
          <Column
            dataField="updatedAt"
            caption="Updated"
            width={190}
            cellRender={({ data }: { data: BackendUserSummary }) => formatDate(data.updatedAt)}
          />
          <Column
            caption="Actions"
            width={220}
            allowSorting={false}
            allowFiltering={false}
            cellRender={({ data }: { data: BackendUserSummary }) => (
              <div
                className="inline-actions"
                data-grid-actions
                onClick={(event) => event.stopPropagation()}
                onMouseDown={(event) => event.stopPropagation()}
                onPointerDown={(event) => event.stopPropagation()}
              >
                <Button
                  text="View"
                  stylingMode="text"
                  onClick={(event) => {
                    event.event?.stopPropagation();
                    void openDetails(data);
                  }}
                />
                {!data.isSuperAdmin && (
                  <Button
                    text="Archive"
                    stylingMode="text"
                    elementAttr={{ class: "danger" }}
                    onClick={(event) => {
                      event.event?.stopPropagation();
                      void handleArchive(data);
                    }}
                  />
                )}
              </div>
            )}
          />
        </DataGrid>

        <PaginationControls
          page={page}
          pageSize={pageSize}
          totalCount={totalCount}
          loading={loading}
          onPageChange={setPage}
          onPageSizeChange={setPageSize}
        />
      </section>

      <Modal
        visible={selectedUser !== null}
        onClose={closeDetails}
        title={selectedUser ? `User Details: ${selectedUser.firstName} ${selectedUser.lastName}` : "User Details"}
        width={900}
      >
        {detailsLoading && <div className="page-inline-info">Loading user details...</div>}
        {detailsError && <div className="form-error">{detailsError}</div>}

        {!detailsLoading && !detailsError && userDetails && (
          <div className="page-stack" style={{ gap: "0.9rem" }}>
            <section className="card state-card">
              <strong>General</strong>
              <div className="simple-table-wrap">
                <table className="simple-table">
                  <tbody>
                    <tr>
                      <th>Name</th>
                      <td>{`${userDetails.firstName} ${userDetails.lastName}`.trim()}</td>
                    </tr>
                    <tr>
                      <th>Email</th>
                      <td>{userDetails.email}</td>
                    </tr>
                    <tr>
                      <th>Status</th>
                      <td>
                        {userDetails.isArchived
                          ? "Archived"
                          : userDetails.isActive
                          ? "Active"
                          : "Inactive"}
                      </td>
                    </tr>
                    <tr>
                      <th>Created</th>
                      <td>{formatDate(userDetails.createdAt)}</td>
                    </tr>
                    <tr>
                      <th>Updated</th>
                      <td>{formatDate(userDetails.updatedAt)}</td>
                    </tr>
                    <tr>
                      <th>Assigned Tasks</th>
                      <td>{userDetails.assignedTaskCount}</td>
                    </tr>
                    <tr>
                      <th>Reported Tasks</th>
                      <td>{userDetails.reportedTaskCount}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </section>

            <section className="card state-card">
              <strong>Organization Memberships ({userDetails.organizationMemberships.length})</strong>
              <div className="simple-table-wrap">
                <table className="simple-table">
                  <thead>
                    <tr>
                      <th>Organization</th>
                      <th>Role</th>
                      <th>Joined</th>
                    </tr>
                  </thead>
                  <tbody>
                    {userDetails.organizationMemberships.map((membership) => (
                      <tr key={`${membership.organizationId}-${membership.role}`}>
                        <td>{membership.organizationName}</td>
                        <td>{membership.role}</td>
                        <td>{formatDate(membership.joinedAt)}</td>
                      </tr>
                    ))}
                    {userDetails.organizationMemberships.length === 0 && (
                      <tr>
                        <td colSpan={3}>No organization memberships.</td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </section>

            <section className="card state-card">
              <strong>Project Memberships ({userDetails.projectMemberships.length})</strong>
              <div className="simple-table-wrap">
                <table className="simple-table">
                  <thead>
                    <tr>
                      <th>Project</th>
                      <th>Organization</th>
                      <th>Role</th>
                      <th>Joined</th>
                    </tr>
                  </thead>
                  <tbody>
                    {userDetails.projectMemberships.map((membership) => (
                      <tr key={`${membership.projectId}-${membership.role}`}>
                        <td>{membership.projectName}</td>
                        <td>{membership.organizationName}</td>
                        <td>{membership.role}</td>
                        <td>{formatDate(membership.joinedAt)}</td>
                      </tr>
                    ))}
                    {userDetails.projectMemberships.length === 0 && (
                      <tr>
                        <td colSpan={4}>No project memberships.</td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </section>
          </div>
        )}

        <div className="popup-actions" style={{ marginTop: "0.75rem" }}>
          <Button text="Close" stylingMode="outlined" onClick={closeDetails} />
        </div>
      </Modal>
    </div>
  );
}
