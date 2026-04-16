import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import type { AppPermission } from "./permissions";
import { useApp } from "../context/AppContext";

type RequirePermissionProps = {
  permission: AppPermission;
  children: ReactNode;
};

export function RequirePermission({ permission, children }: RequirePermissionProps) {
  const location = useLocation();
  const { hasPermission, isAuthenticated, bootstrapping } = useApp();

  if (bootstrapping) {
    return <div className="page-loader">Loading workspace...</div>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  if (!hasPermission(permission)) {
    return <Navigate to="/forbidden" replace state={{ from: location.pathname }} />;
  }

  return <>{children}</>;
}
