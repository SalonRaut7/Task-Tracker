# TaskTracker 🚀

TaskTracker is a modern, full-stack enterprise task management platform built to streamline team collaboration. It features a robust **.NET 9** backend utilizing **Clean Architecture** and **CQRS**, paired with a dynamic **React + Vite** frontend.

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat&logo=dotnet&logoColor=white)
![React](https://img.shields.io/badge/React-20232A?style=flat&logo=react&logoColor=61DAFB)
![Vite](https://img.shields.io/badge/Vite-B73BFE?style=flat&logo=vite&logoColor=FFD62E)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=flat&logo=postgresql&logoColor=white)
![CQRS](https://img.shields.io/badge/Architecture-CQRS-blue)

## 📌 What It Does

TaskTracker brings powerful project management tools to your team. It natively supports hierarchal agile principles including **Organizations, Projects, Epics, Sprints, and Tasks**. With built-in features like rich comments, user mentions, live presence, real-time push notifications, and seamless file attachments, teams can stay aligned effortlessly.

## 🛠️ The Tech Stack

### **Backend**
- **Framework:** ASP.NET Core 9 API using top-tier RESTful standards.
- **Architecture:** Clean Architecture + CQRS with MediatR.
- **Data Access:** Entity Framework Core (EF Core) communicating with PostgreSQL.
- **Security:** ASP.NET Identity, JWT Authentication, and pipeline-enforced fine-grained permissions.
- **Real-Time:** SignalR Hubs for live data changes. 
- **Storage & Extras:** Cloudinary for attachments, FluentValidation, Serilog for extensive rolling logs.

### **Frontend**
- **Framework:** React 18, TypeScript, and Vite.
- **UI Components:** Powerful DevExtreme grids, forms, and layout elements.
- **Real-Time Context:** SignalR browser client for instant notification and table refreshes.
- **Tooling:** ESLint, React Compiler.

---

## ✨ Key Features

- 🔐 **Secure Identity:** Auth utilizing secure JWT tokens, account verification, password resets, and OTPs.
- 🏢 **Multi-Tenancy:** Role-based access control inside Organizations and nested Projects.
- 📋 **Agile Artifacts:** Track scopes using Epics, manage work via Sprints, and issue Task assignments with deadlines/expiry rules.
- 💬 **Collaborative Feedback:** Comment histories on tasks with `@user` mentions and rich content.
- 📡 **Real-time Synced:** Instantly receive in-app notifications and grid updates through SignalR web-socket connections.
- 📎 **Cloud Attachments:** Direct cloud upload integration for files with previews rendering directly in task view.
- 🗃️ **Systematic Background Jobs:** Automatic date expiration monitors evaluating due tasks passively via Hosted Services.

---

## 🏗️ Architecture Design

The backend fundamentally enforces the **Dependency Inversion Principle** across decoupled layers:

1. **`TaskTracker.API` (Presentation Edge):** ASP.NET Controllers mapped to application paths, Startup bindings, middleware, global error handling, API endpoints, Swagger details, SignalR push endpoints.
2. **`TaskTracker.Application` (Use Cases):** Centralizes commands/queries via MediatR mapping. DTO wrappers, mapping logic, validation with FluentValidation, and authorization pipeline behaviors. Business logic operates cleanly here.
3. **`TaskTracker.Domain` (Core Rules):** Heart of the platform holding pure Entity blueprints, Enums, ReadModels, domain events (e.g. `TaskChangedDomainEvent`), and interface contracts.
4. **`TaskTracker.Infrastructure` (Persistence & Outside Integrations):** Handles PostgreSQL `AppDbContext` state representations with EF Core code-first mappings, implementations of all Respositories mapping, Cloudinary Cloud storage integration, and Background Service threads.

---

## ⚙️ Configuration & Setup

### Prerequisites
To run the setup locally, you will need:
- **.NET 9.0 SDK**
- **Node.js (LTS)** & **npm**
- **PostgreSQL Server** (Local or Cloud/Docker equivalent)
- **Cloudinary Account** (for file attachments)

### Step 1: Environment Variables
Backend configuration dictates API environment configurations primarily in `TaskTracker.API/appsettings.json` (or `appsettings.Development.json`).
Key configurations include:
- `ConnectionStrings:DefaultConnection`: Map to your PostgreSQL DB instance.
- `JwtSettings`: Application generic Secret Key and access-token intervals.
- `Cloudinary`: Add target `CloudName`, `ApiKey`, and `ApiSecret`.
- `SmtpSettings`: Target your local or internet SMTP email sender details (necessary for Invites/OTP).

For the Frontend `.env`:
- Provide `VITE_API_BASE_URL` pointing strictly towards your API path if separated (otherwise defaults to local).

### Step 2: Bootstrapping the Backend
The initial setup includes resolving dependencies, mapping tables, migrating contexts, and seeding.

```bash
# 1. Restore packages
dotnet restore

# 2. Complete DB Migration targeting correct infrastructure path
dotnet ef database update --project TaskTracker.Infrastructure --startup-project TaskTracker.API

# 3. Fire up the platform
cd TaskTracker.API
dotnet run
```

### Step 3: Bootstrapping the Frontend
Operate the React local Vite dev-server:

```bash
cd TaskTracker.Web

# 1. Provide all npm packages
npm install

# 2. Spool development server (Available usually at http://localhost:5173)
npm run dev
```

## 📝 Developer API Notes

- **Swagger GUI:** Swagger UI natively injects into the web path during development mappings.
- **SignalR Websockets:** Live notification endpoints are accessible by establishing handshakes at `/hubs/notifications`.
- **Exceptions:** General exceptions automatically shape into consistent API `ConflictException` and `ForbiddenAccessException` responses securely by custom middleware.
- **CORS Default:** Backend natively clears CORS pre-flags specifically targeting standard `http://localhost:5173` ports. Adjust app settings to modify.
