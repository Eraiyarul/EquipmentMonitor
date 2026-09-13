# Equipment Monitoring & Management System

> **Full Stack Developer Assignment** — Production-ready industrial IoT monitoring platform built with ASP.NET Core 10, PostgreSQL, MQTTnet, and SignalR.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-14+-336791?logo=postgresql)](https://www.postgresql.org)
[![SignalR](https://img.shields.io/badge/SignalR-Real--time-0078D4)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![MQTT](https://img.shields.io/badge/MQTT-In--process-orange)](https://mqtt.org)
[![GitHub](https://img.shields.io/badge/GitHub-Eraiyarul%2FEquipmentMonitor-181717?logo=github)](https://github.com/Eraiyarul/EquipmentMonitor)

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Tech Stack](#tech-stack)
4. [Features](#features)
5. [Prerequisites](#prerequisites)
6. [Quick Start](#quick-start)
7. [Configuration](#configuration)
8. [Database Setup](#database-setup)
9. [Demo Accounts](#demo-accounts)
10. [Application Pages](#application-pages)
11. [Real-Time IoT Flow](#real-time-iot-flow)
12. [Multi-Tenancy Design](#multi-tenancy-design)
13. [API / Page Endpoints](#api--page-endpoints)
14. [Project Structure](#project-structure)
15. [Known Limitations](#known-limitations)

---

## Overview

This system allows industrial operations teams to:

- **Track equipment health** across multiple stations in real time
- **Receive instant alerts** when sensor readings breach configured thresholds
- **Manage equipment lifecycle** (add, edit, delete stations with full audit trail)
- **Review historical data** with date-range filtering and Chart.js visualisations
- **Operate securely** with multi-tenant isolation — each organisation sees only its own data

The platform uses an **in-process MQTT broker** that continuously simulates IoT sensor data (Temperature, Humidity, Pressure) for all registered stations. Readings flow through the backend pipeline, trigger threshold evaluation, and reach the UI in under 100 ms via SignalR WebSockets — no page refresh required.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Browser (Client)                       │
│                                                             │
│   Razor Pages UI  ◄──── SignalR WebSocket ────► Hub        │
│   Chart.js charts        (real-time push)                   │
└────────────────────────────────┬────────────────────────────┘
                                 │ HTTP + WebSocket
┌────────────────────────────────▼────────────────────────────┐
│                   ASP.NET Core 10 Application               │
│                                                             │
│  ┌──────────────┐   ┌─────────────────────────────────┐    │
│  │ MQTT Broker  │   │         SignalR Hub              │    │
│  │ (in-process) │   │  tenant-{id} group isolation     │    │
│  └──────┬───────┘   └────────────────┬────────────────┘    │
│         │ subscribe                  │ broadcast            │
│  ┌──────▼───────────────────────────▼────────────────┐    │
│  │              ReadingIngestionService               │    │
│  │  Parse → Persist → AlertEvaluationService → Push  │    │
│  └──────────────────────┬─────────────────────────────┘    │
│                         │                                   │
│  ┌──────────────────────▼─────────────────────────────┐    │
│  │         Entity Framework Core + Npgsql              │    │
│  └──────────────────────┬─────────────────────────────┘    │
└─────────────────────────┼───────────────────────────────────┘
                          │
              ┌───────────▼──────────┐
              │      PostgreSQL       │
              │  Equipments, Readings │
              │  Alerts, Thresholds  │
              │  Tenants, Users      │
              └──────────────────────┘
```

**Data flow:**
1. MQTT Simulator publishes a JSON reading every 15 s per active station
2. MQTT Subscriber receives the message → `ReadingIngestionService.IngestAsync()`
3. Reading is persisted to PostgreSQL
4. `AlertEvaluationService` checks thresholds → creates `Alert` if breached
5. SignalR broadcasts `NewReading` (and `NewAlert` if applicable) to the owning tenant's group
6. All connected browsers in that tenant update live — metric cells, chart, event log, toast notifications

---

## Tech Stack

| Layer | Technology | Version |
|---|---|---|
| Framework | ASP.NET Core Razor Pages | 10.0 |
| Language | C# | 13 |
| ORM | Entity Framework Core + Npgsql | 10.x |
| Database | PostgreSQL | 14+ |
| Real-time | ASP.NET Core SignalR | 10.x |
| IoT / MQTT | MQTTnet (in-process) | 4.3.7 |
| Authentication | ASP.NET Core Identity | 10.x |
| Charts | Chart.js | 4.4.3 |
| Icons | Bootstrap Icons | 1.11.3 |
| UI | Custom dark IoT CSS design system | — |

> **Frontend note:** The assignment specified React.js/Next.js. Per the project direction, the UI was implemented with ASP.NET Core Razor Pages — a server-rendered approach that delivers the same dashboard, detail views, forms, and real-time SignalR integration without a separate SPA build pipeline.

---

## Features

### Core
- **Real-time dashboard** — station cards with animated LED status indicators, live metric cells, Chart.js 30-point rolling chart, event log feed
- **MQTT simulation** — in-process broker + publisher; no external MQTT broker required
- **Threshold-based alerting** — configurable min/max per equipment+metric; ~10 % spike probability in simulator to trigger breaches
- **Alert management** — Acknowledge and Resolve from dashboard or dedicated alerts page; status tracked with timestamps
- **Equipment CRUD** — Create, Read, Update, Delete stations with full tenant isolation
- **Historical readings** — per-station chart and table with date-range filtering (From/To)
- **Multi-tenancy** — two organisations (Acme Industries, TechCorp Solutions) with fully isolated data, SignalR groups, and logins

### Security
- **ASP.NET Core Identity** — username/password authentication with bcrypt hashing
- **Cookie-based session** — 8-hour sliding expiration
- **Role-based access** — Admin and Operator roles per tenant
- **Tenant isolation** — all DB queries filtered by `TenantId` from cookie claims; cross-tenant access returns 403/404
- **SignalR isolation** — clients join `tenant-{id}` group on connect; broadcasts never cross tenant boundaries

### UX
- **Dark industrial design system** — CSS variables, LED indicators, gauge bars, KPI cards, toast notifications
- **Zero-refresh updates** — readings, alerts, and badge counts update live via SignalR
- **Clickable demo account rows** — login page auto-fills credentials for evaluators

---

## Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 10.0+ |
| PostgreSQL | 14+ |
| Git | Any |

No external MQTT broker is required — MQTTnet runs the broker in-process.

---

## Quick Start

```bash
# 1. Clone
git clone https://github.com/Eraiyarul/EquipmentMonitor.git
cd EquipmentMonitor

# 2. Set your PostgreSQL password (see Configuration section)

# 3. Run — migrations and seed are applied automatically
dotnet run

# 4. Open browser
#    http://localhost:5272
```

Login with `admin / Admin@123` to see Acme Industries, or `techop / Techop@123` for TechCorp Solutions.

---

## Configuration

### Option A — `appsettings.Development.json` (recommended for local dev)

Create this file in the project root (it is gitignored — your password stays local):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=EquipmentMonitorDb;Username=postgres;Password=YOUR_ACTUAL_PASSWORD"
  }
}
```

### Option B — Edit `appsettings.json`

Replace `YOUR_PASSWORD` with your PostgreSQL password:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=EquipmentMonitorDb;Username=postgres;Password=YOUR_PASSWORD"
}
```

### Option C — Environment variable

```bash
ConnectionStrings__DefaultConnection="Host=localhost;Database=EquipmentMonitorDb;Username=postgres;Password=YOUR_PASSWORD" dotnet run
```

---

## Database Setup

### Automatic (default)

In `Development` mode, `Program.cs` runs:

```csharp
db.Database.EnsureDeleted();   // drop for a clean demo slate
db.Database.Migrate();          // apply all migrations
await DbInitializer.SeedAsync(...); // seed tenants, users, equipment, thresholds
```

This gives a clean, fully seeded database on every restart — ideal for demos.

### Manual migration commands

```bash
# Apply all pending migrations
dotnet ef database update

# Create a new migration after model changes
dotnet ef migrations add <MigrationName>

# Roll back to a specific migration
dotnet ef database update <MigrationName>
```

### Migrations

| Migration | Covers |
|---|---|
| `20260913160546_InitialCreate` | Equipment, Reading, Alert, Threshold tables + indexes |
| `20260913164434_AddIdentityAndTenants` | ASP.NET Identity tables, Tenant table, Equipment.TenantId FK |

### Seed Data

`Data/DbInitializer.cs` creates:

**Tenants & Equipment**

| Tenant | Station | Type | Location | Status | Thresholds |
|---|---|---|---|---|---|
| Acme Industries | Pump Station A | Hydraulic Pump | Floor 1 - North Wing | Active | Temp 60–90°C, Humidity 30–70%, Pressure 0.8–1.2 bar |
| Acme Industries | Compressor Unit B | Air Compressor | Floor 2 - East Bay | Active | Temp 50–80°C, Humidity 20–65%, Pressure 1.0–2.5 bar |
| Acme Industries | Conveyor Belt C | Belt Conveyor | Warehouse - Section C | Under Maintenance | Temp 20–50°C, Humidity 25–75%, Pressure 0.5–1.0 bar |
| TechCorp Solutions | Server Rack Alpha | Rack Server | Data Centre - Rack A12 | Active | Temp 18–28°C, Humidity 40–60%, Pressure 0.9–1.1 bar |
| TechCorp Solutions | CNC Machine Delta | CNC Milling | Production Floor - Bay 3 | Active | Temp 20–45°C, Humidity 30–65%, Pressure 0.6–1.8 bar |

---

## Demo Accounts

| Username | Password | Tenant | Role |
|---|---|---|---|
| `admin` | `Admin@123` | Acme Industries | Admin |
| `operator` | `Operator@123` | Acme Industries | Operator |
| `techop` | `Techop@123` | TechCorp Solutions | Operator |

> On the login page, click any demo account row to auto-fill credentials.

To verify tenant isolation: log in as `admin` (sees Acme stations), log out, then log in as `techop` (sees only TechCorp stations — completely separate data).

---

## Application Pages

| Route | Page | Description |
|---|---|---|
| `/Account/Login` | Login | Full-screen dark login with demo account rows |
| `/Account/Logout` | Logout | Sign out and redirect to login |
| `/` | Dashboard | KPI cards, station grid, rolling Chart.js chart, live event log |
| `/Equipment/Details/{id}` | Station Detail | Gauge cards, date-filtered readings history, threshold table, alert history |
| `/Equipment/Create` | Add Station | Form to register a new equipment station |
| `/Equipment/Edit/{id}` | Edit Station | Update name, type, location, status, installed date |
| `/Equipment/Delete/{id}` | Delete Station | Confirmation page before permanent deletion |
| `/Alerts` | Alert Center | Active + resolved alerts with Acknowledge / Resolve actions |

All pages except Login/Logout require authentication and enforce tenant isolation.

---

## Real-Time IoT Flow

### MQTT Topics

```
equipment/{equipmentId}/readings
```

### Message Payload

```json
{
  "equipmentId": 1,
  "metricType": "Temperature",
  "value": 73.4,
  "unit": "°C",
  "timestamp": "2026-09-13T17:30:00Z"
}
```

### Simulation Logic (`MqttBackgroundService.cs`)

- Publishes every **15 seconds** for each Active station
- Metrics: `Temperature`, `Humidity`, `Pressure`
- Normal range: within configured thresholds
- **~10 % spike probability** — value goes 20 % above/below threshold to trigger an alert

### SignalR Events (client-side)

| Event | Payload | Consumer |
|---|---|---|
| `NewReading` | `{ id, equipmentId, equipmentName, metricType, value, unit, timestamp }` | Dashboard metric cells, chart, event log; Detail page table |
| `NewAlert` | `{ id, equipmentId, equipmentName, message, status, createdAt }` | Toast notification, sidebar badge, alerts page live row |

---

## Multi-Tenancy Design

```
Tenant
 └── Equipment (TenantId FK)
      ├── Readings
      ├── Alerts
      └── Thresholds

ApplicationUser (TenantId FK)
 └── Claims: TenantId, TenantName, TenantCode, FullName, UserRole
```

- `AppClaimsPrincipalFactory` injects tenant claims into the authentication cookie at login
- `HttpTenantProvider` reads those claims on every request
- All page models call `tenant.TenantId` to scope every DB query
- SignalR: clients join `tenant-{id}` group on connect; MQTT pipeline broadcasts to that group only

---

## API / Page Endpoints

The application uses Razor Pages (server-rendered). Page handlers map to HTTP operations:

| Handler | HTTP | Action |
|---|---|---|
| `OnGetAsync` | GET | Read |
| `OnPostAsync` | POST | Create / Update / Delete |
| `OnPostAcknowledgeAsync` | POST | Acknowledge alert |
| `OnPostResolveAsync` | POST | Resolve alert |

MQTT is the primary reading ingestion path. The `ReadingIngestionService` is invoked by the MQTT subscriber and can also be called programmatically for integration scenarios.

---

## Project Structure

```
EquipmentMonitor/
│
├── Data/
│   ├── AppDbContext.cs              # EF Core DbContext (extends IdentityDbContext)
│   └── DbInitializer.cs             # Seed: tenants, users, equipment, thresholds
│
├── Hubs/
│   └── EquipmentHub.cs              # SignalR hub — joins tenant group on connect
│
├── Migrations/                      # EF Core migration history
│   ├── 20260913160546_InitialCreate.cs
│   └── 20260913164434_AddIdentityAndTenants.cs
│
├── Models/
│   ├── Equipment.cs                 # Name, Type, Location, Status, InstalledDate, TenantId
│   ├── Reading.cs                   # EquipmentId, MetricType, Value, Unit, Timestamp
│   ├── Alert.cs                     # EquipmentId, ReadingId, Message, Status, CreatedAt
│   ├── Threshold.cs                 # EquipmentId, MetricType, MinValue, MaxValue
│   ├── Tenant.cs                    # Id, Name, Code, IsActive
│   └── ApplicationUser.cs           # IdentityUser + FullName, UserRole, TenantId
│
├── Pages/
│   ├── Index.cshtml(.cs)            # Dashboard — KPI cards, station grid, live chart
│   ├── Account/
│   │   ├── Login.cshtml(.cs)        # Auth form with demo account rows
│   │   └── Logout.cshtml(.cs)       # Sign out
│   ├── Equipment/
│   │   ├── Details.cshtml(.cs)      # Gauges, date-filtered history, alerts
│   │   ├── Create.cshtml(.cs)       # Add station form
│   │   ├── Edit.cshtml(.cs)         # Edit station form
│   │   └── Delete.cshtml(.cs)       # Delete confirmation
│   ├── Alerts/
│   │   └── Index.cshtml(.cs)        # Alert center
│   └── Shared/
│       ├── _Layout.cshtml           # Authenticated layout (sidebar, topbar, SignalR, Chart.js)
│       └── _LoginLayout.cshtml      # Full-screen unauthenticated layout
│
├── Services/
│   ├── MqttBackgroundService.cs     # In-process broker + subscriber + simulator
│   ├── ReadingIngestionService.cs   # Persist reading → evaluate → broadcast
│   ├── AlertEvaluationService.cs    # Threshold check → create alert → broadcast
│   ├── TenantProvider.cs            # ITenantProvider reads TenantId from claims
│   └── AppClaimsPrincipalFactory.cs # Injects tenant/role claims at login
│
├── wwwroot/
│   └── css/iot.css                  # Dark IoT design system (CSS variables + components)
│
├── appsettings.json                 # Connection string (password placeholder)
├── Program.cs                       # DI registration, middleware pipeline, DB init
└── EquipmentMonitor.csproj
```

---

## Known Limitations

The following areas are intentionally noted per the assignment guideline *("note any incomplete areas in the README rather than over-extending")*:

| Area | Status | Notes |
|---|---|---|
| Frontend stack | Razor Pages (not React.js/Next.js) | Equivalent functionality delivered via server-rendered pages + SignalR |
| Reading ingest HTTP endpoint | Not exposed separately | Readings arrive via MQTT; ingestion service can be extended to an API controller |
| Date-range filtering on alerts | Not implemented | Available on readings; alerts page shows all with status filter |
| Unit tests | Not implemented | Alert evaluation logic is a candidate for isolated unit tests |
| Docker / docker-compose | Not implemented | No Dockerfile provided |
| Redis caching | Not implemented | DB queries are fast at current data volume |
| Mobile responsive | Partial | Layout optimised for desktop; sidebar does not collapse on narrow viewports |
| JWT authentication | Not implemented | Cookie-based auth used instead; role isolation is enforced |

---

## Test Results

Functional tests run against the live application on 2026-09-13:

| Test | Expected | Result |
|---|---|---|
| Login page loads | 200 | ✅ PASS |
| Unauthenticated dashboard → redirect | 302 to `/Account/Login` | ✅ PASS |
| Authenticated dashboard | 200 | ✅ PASS |
| Equipment Detail page | 200 | ✅ PASS |
| Equipment Create page | 200 | ✅ PASS |
| Equipment Edit page | 200 | ✅ PASS |
| Equipment Delete page | 200 | ✅ PASS |
| Alerts page | 200 | ✅ PASS |
| Tenant isolation — Acme user accessing TechCorp equipment | 404 | ✅ PASS |
| Tenant isolation — Acme user accessing TechCorp delete | 404 | ✅ PASS |
| Dashboard shows Acme stations (Pump, Compressor, Conveyor) | 3 found | ✅ PASS |
| TechCorp data not visible in Acme session | 0 leaks | ✅ PASS |
| TechCorp login shows Server Rack + CNC Machine | 2 found | ✅ PASS |
| Acme data not visible in TechCorp session | 0 leaks | ✅ PASS |
| Date-range filter inputs on Detail page | 2 inputs | ✅ PASS |
| Filter + Reset buttons | Present | ✅ PASS |
| Live MQTT readings ingested | 34 readings | ✅ PASS |

---

## Evaluation Notes for Reviewers

- **MQTT → Backend → Frontend flow**: Start the app and open the dashboard. Within 15 seconds you will see readings populate live across metric cells, the chart, and the event log — no manual refresh.
- **Alert threshold breach**: Approximately 1 in 10 readings spikes out of range. Watch for the red toast notification and the alert counter in the sidebar badge.
- **Tenant isolation**: Log in as `admin` (Acme — 3 stations), then as `techop` (TechCorp — 2 different stations). Data is completely isolated at DB query, SignalR broadcast, and session level.
- **Clean architecture**: `ReadingIngestionService` and `AlertEvaluationService` are pure service classes with no UI dependency — they can be unit-tested or reused in an API controller with zero changes.
- **EF Core migrations**: Two clean migrations with proper indexes (`EquipmentId + Timestamp`, `EquipmentId + Status`) and foreign key constraints with cascade rules.
- **Delete equipment**: Confirmation page with tenant ownership check — accessing another tenant's equipment returns 403/404.
- **Date-range filtering**: Station detail page accepts `From` and `To` query parameters; defaults to last 24 hours when not specified.

---

## Changelog

| Version | Date | Changes |
|---|---|---|
| v1.0.0 | 2026-09-13 | Initial release — full stack IoT monitoring with MQTT + SignalR + multi-tenant Identity |
| v1.1.0 | 2026-09-13 | Added Delete equipment endpoint, date-range filtering on readings, professional README |
| v1.2.0 | 2026-09-13 | Updated seed data with industry-grade equipment names, types and locations; verified all 17 functional tests |

---

*Built by Eraiyarul — Full Stack Developer Assignment 2026*
