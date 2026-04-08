# Bikeapelago Admin Platform (BAP) Development Roadmap

## Prompt 1: Identity, Authorization, and Role Gating
**Objective:** Secure the existing .NET 9 API by enforcing a "User-by-default" registration policy and implementing Admin-specific authorization requirements.

**Technical Details:**
- **Namespace:** `Bikeapelago.Api.Admin.Authorization`
- **Libraries:** `Microsoft.AspNetCore.Identity`, `Microsoft.AspNetCore.Authorization`
- **Target Files:** `Program.cs`, `Controllers/AuthController.cs`, `Data/BikeapelagoDbContext.cs`

**Step-by-Step Instructions:**
1. **Modify Registration:** Update `AuthController.Register` (or the equivalent identity logic) to ensure all new `ApplicationUser` records are automatically assigned the "User" role upon creation using `UserManager.AddToRoleAsync`.
2. **Role Seeding:** Update the database initializer or `Program.cs` to ensure "Admin" and "User" roles exist in the `AspNetRoles` table.
3. **Admin Policy:** Define an "AdminOnly" authorization policy in `Program.cs` that requires the `Admin` role.
4. **Custom Attribute:** Create a `[AdminAuthorize]` attribute (inheriting from `AuthorizeAttribute`) that specifically sets the policy to "AdminOnly" for cleaner usage on controllers.
5. **Initial Admin Seed:** Create a temporary migration or a startup check that promotes a specific email (from environment variables) to the "Admin" role for initial setup.

**Security Check:** 
- Ensure `[Authorize]` is applied globally or to all new Admin-area controllers. 
- Validate that a standard "User" token receives a `403 Forbidden` when accessing Admin-gated endpoints.

---

## Prompt 2: Dynamic Schema Reflection API
**Objective:** Create a backend service that inspects the EF Core `IModel` to provide the frontend with a JSON description of the database structure.

**Technical Details:**
- **Namespace:** `Bikeapelago.Api.Admin.Services`
- **Target Files:** `Services/SchemaDiscoveryService.cs`, `Controllers/Admin/SchemaController.cs`
- **Output Schema:** `{ table: string, columns: { name: string, type: string, isNullable: bool, isPrimaryKey: bool, isSpatial: bool }[] }`

**Step-by-Step Instructions:**
1. **Service Implementation:** Create `SchemaDiscoveryService` and inject `BikeapelagoDbContext`.
2. **Reflection Logic:** Use `_context.Model.GetEntityTypes()` to iterate through registered models.
3. **Metadata Extraction:** For each entity, map EF Core types to a "Frontend-Friendly" type system (e.g., `string`, `number`, `date`, `boolean`, `uuid`, `geometry`).
4. **Spatial Detection:** Explicitly check if a property type belongs to `NetTopologySuite.Geometries` to flag it as `isSpatial`.
5. **API Endpoint:** Create `GET /api/admin/schema` gated by `[AdminAuthorize]` that returns a list of all entities and their metadata.

**Security Check:**
- Do not expose sensitive internal fields (e.g., `PasswordHash`, `ConcurrencyStamp`) in the schema JSON.
- Ensure the endpoint is strictly inaccessible to non-admin users.

---

## Prompt 3: The Admin Shell (React)
**Objective:** Initialize a separate Vite + React project for the Admin UI with a modular layout and authentication persistence.

**Technical Details:**
- **Framework:** Vite + React + TypeScript
- **UI Toolkit:** Shadcn/UI, Tailwind CSS, Lucide React
- **Location:** `/admin-ui` (Root sibling to main frontend)
- **Routing:** React Router v7

**Step-by-Step Instructions:**
1. **Project Setup:** Scaffold the Vite app. Configure `vite.config.ts` with a proxy to `http://localhost:5000/api` for development.
2. **Layout Foundation:** Create a `DashboardLayout` component featuring a collapsible `Sidebar` (using Shadcn) and a `MainContent` area.
3. **Auth State:** Implement a `useAuth` hook and an `AuthContext` that handles JWT storage (HttpOnly cookies preferred, or Secure LocalStorage) and role validation.
4. **Protected Routes:** Create a `ProtectedRoute` component that redirects to `/login` if no valid Admin session is found.
5. **Navigation:** Add a "Data Explorer" link to the sidebar that will eventually host the dynamic CRUD views.

**Security Check:**
- Verify that the Admin UI is completely separate from the main user app (no shared components/assets to prevent leakage).
- Set up a basic Content Security Policy (CSP) in the `index.html`.

---

## Prompt 4: Dynamic .NET Data Explorer
**Objective:** Build a dynamic React component that generates CRUD tables and forms based on the Schema API.

**Technical Details:**
- **Libraries:** `@tanstack/react-table`, `@tanstack/react-query`, `react-hook-form`, `zod`
- **Components:** `GenericDataTable.tsx`, `GenericDataForm.tsx`

**Step-by-Step Instructions:**
1. **Schema Consumption:** Fetch the schema metadata from `/api/admin/schema` on load.
2. **Dynamic Routing:** Set up a route `/explorer/:tableName` that renders the data for the selected table.
3. **Generic Table:** Implement a TanStack Table that automatically maps schema columns to `TableHeader` and `TableCell`.
4. **CRUD Actions:** Create a sidebar panel or modal for "Create" and "Edit" that uses `react-hook-form` and `zod` to dynamically build input fields (e.g., `Input` for strings, `Checkbox` for booleans, `DatePicker` for dates).
5. **API Integration:** Implement generic `GET /api/admin/data/:tableName`, `POST`, `PUT`, and `DELETE` handlers.

**Security Check:**
- Ensure all dynamic API calls to `/api/admin/data/*` include the JWT in headers.
- Sanitize any user-provided data before sending to the backend.

---

## Prompt 5: Spatial Previews & Specialized Editors
**Objective:** Add PostGIS spatial data preview and editing capabilities to the Data Explorer.

**Technical Details:**
- **Libraries:** `MapLibre GL JS`, `net-topology-suite` (C#), `Well Known Text` (WKT)
- **Components:** `SpatialPreview.tsx`, `SpatialEditor.tsx`

**Step-by-Step Instructions:**
1. **Map Component:** Create a reusable MapLibre component for rendering spatial data.
2. **Cell Preview:** In the `GenericDataTable`, add a small map thumbnail or a "View Map" icon for any column flagged as `isSpatial`.
3. **WKT Integration:** Update the backend to serialize `Geometry` fields as WKT (Well-Known Text) for the frontend to consume.
4. **Spatial Form Field:** In the `GenericDataForm`, provide a map-based editor (drawing tools) for `isSpatial` columns.
5. **Coordinate Display:** Show lat/lng coordinates and a "Copy to Clipboard" feature for points.

**Security Check:**
- Use a secure tileset source (e.g., OpenStreetMap) and ensure no API keys are exposed.
- Limit spatial preview data to avoid overwhelming the browser for large geometries.

---

## Prompt 6: Scalable Extension Points
**Objective:** Implement a placeholder "Analytics" module to demonstrate how to extend the Admin shell beyond generic CRUD.

**Technical Details:**
- **Target Files:** `src/modules/Analytics/AnalyticsDashboard.tsx`, `src/config/modules.ts`
- **Charts:** `Recharts` or `Chart.js`

**Step-by-Step Instructions:**
1. **Module Registry:** Create a configuration file `src/config/modules.ts` that defines a list of "Extra Modules" with icons and routes.
2. **Analytics Component:** Create a static `AnalyticsDashboard` component with dummy data and a "Total Users" chart.
3. **Sidebar Integration:** Update the `Sidebar` component to map over the module registry and display extra links.
4. **Lazy Loading:** Use `React.lazy()` for these modules to keep the initial admin bundle small.
5. **Backend Extension:** Add a simple `GET /api/admin/stats` endpoint for the analytics module.

**Security Check:**
- Ensure even "Extra Modules" are wrapped in the `[AdminAuthorize]` policy on the backend.
- Verify that these extensions do not bypass the core `AuthContext` on the frontend.

---

## Prompt 7: Docker & Proxy Configuration
**Objective:** Finalize the dual-subdomain architecture (`admin.bikeapelago.com`) using Docker and Caddy/Nginx.

**Technical Details:**
- **Files:** `docker-compose.yml`, `Caddyfile` or `nginx.conf`, `Program.cs`
- **Domains:** `bikeapelago.alexkibler.com`, `admin.bikeapelago.com`

**Step-by-Step Instructions:**
1. **CORS Setup:** Update `Program.cs` in the .NET API to explicitly allow `admin.bikeapelago.com` with `AllowCredentials()` and `WithOrigins(...)`.
2. **Cookie Security:** Configure JWT/Identity cookies to use `SameSite = SameSiteMode.Lax` and `Domain = ".bikeapelago.com"` for cross-subdomain support.
3. **Docker Service:** Add a new `admin-ui` service to `docker-compose.yml` that builds the Vite app and serves it via Nginx/Caddy.
4. **Reverse Proxy:** Configure the reverse proxy (Caddy/Nginx) to route `admin.bikeapelago.com` to the `admin-ui` container.
5. **Network Connectivity:** Ensure the `admin-ui` container can communicate with the `api` container over the internal Docker network.

**Security Check:**
- Verify that `admin.bikeapelago.com` cannot access sensitive main-site cookies and vice versa (unless explicitly allowed).
- Disable root-level directory listing in the Nginx/Caddy configuration for the Admin UI.
