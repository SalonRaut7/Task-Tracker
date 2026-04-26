import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import type { AppPermission } from "./permissions";
import { useApp } from "../context/AppContext";
import type { ScopeType } from "../types/invitation";

type RequirePermissionProps = {
  permission: AppPermission;
  /** Optional scope for scoped permission checks. */
  scopeType?: ScopeType;
  scopeId?: string;
  children: ReactNode;
};

export function RequirePermission({
  permission,
  scopeType,
  scopeId,
  children,
}: RequirePermissionProps) {
  const location = useLocation();
  const { hasPermission, isAuthenticated, bootstrapping, permissionsLoaded } = useApp();

  if (bootstrapping || (isAuthenticated && !permissionsLoaded)) {
    return <div className="page-loader">Loading workspace...</div>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  if (!hasPermission(permission, scopeType, scopeId)) {
    return (
      <Navigate to="/forbidden" replace state={{ from: location.pathname }} />
    );
  }

  return <>{children}</>;
}
