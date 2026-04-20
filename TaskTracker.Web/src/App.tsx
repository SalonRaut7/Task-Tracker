import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AppProvider, useApp } from "./context/AppContext";
import { MainLayout } from "./layouts/MainLayout";
import { DashboardPage } from "./pages/DashboardPage";
import { ForgotPasswordPage } from "./pages/ForgotPasswordPage";
import { LoginPage } from "./pages/LoginPage";
import { ProjectDetailsPage } from "./pages/ProjectDetailsPage";
import { ProjectsPage } from "./pages/ProjectsPage";
import { RegisterPage } from "./pages/RegisterPage";
import { ReportsPage } from "./pages/ReportsPage";
import { SettingsPage } from "./pages/SettingsPage";
import { TaskDetailsPage } from "./pages/TaskDetailsPage";
import { TasksPage } from "./pages/TasksPage";
import { ForbiddenPage } from "./pages/ForbiddenPage";
import { OrganizationsPage } from "./pages/OrganizationsPage";
import { CommentsPage } from "./pages/CommentsPage";
import { RequirePermission } from "./security/RequirePermission";
import { AppPermissions } from "./security/permissions";

function HomeRedirect() {
  const { isAuthenticated, bootstrapping } = useApp();

  if (bootstrapping) {
    return <div className="page-loader">Loading workspace...</div>;
  }

  return <Navigate to={isAuthenticated ? "/dashboard" : "/login"} replace />;
}

export default function App() {
  return (
    <AppProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<HomeRedirect />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />

          <Route element={<MainLayout />}>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route
              path="/organizations"
              element={
                <RequirePermission permission={AppPermissions.OrganizationsView}>
                  <OrganizationsPage />
                </RequirePermission>
              }
            />
            <Route
              path="/projects"
              element={
                <RequirePermission permission={AppPermissions.ProjectsView}>
                  <ProjectsPage />
                </RequirePermission>
              }
            />
            <Route
              path="/projects/:id"
              element={
                <RequirePermission permission={AppPermissions.ProjectsView}>
                  <ProjectDetailsPage />
                </RequirePermission>
              }
            />
            <Route
              path="/tasks"
              element={
                <RequirePermission permission={AppPermissions.TasksView}>
                  <TasksPage />
                </RequirePermission>
              }
            />
            <Route
              path="/tasks/:projectId/:taskId"
              element={
                <RequirePermission permission={AppPermissions.TasksView}>
                  <TaskDetailsPage />
                </RequirePermission>
              }
            />
            <Route
              path="/tasks/details/:taskId"
              element={
                <RequirePermission permission={AppPermissions.TasksView}>
                  <TaskDetailsPage />
                </RequirePermission>
              }
            />
            <Route
              path="/reports"
              element={
                <RequirePermission permission={AppPermissions.TasksView}>
                  <ReportsPage />
                </RequirePermission>
              }
            />
            <Route
              path="/comments"
              element={
                <RequirePermission permission={AppPermissions.CommentsView}>
                  <CommentsPage />
                </RequirePermission>
              }
            />
            <Route path="/settings" element={<SettingsPage />} />
            <Route path="/forbidden" element={<ForbiddenPage />} />
          </Route>

          <Route path="*" element={<HomeRedirect />} />
        </Routes>
      </BrowserRouter>
    </AppProvider>
  );
}