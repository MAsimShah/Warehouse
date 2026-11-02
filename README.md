# Warehouse Parcel Distribution System

A web system for an Australian parcel distribution hub. It tracks every parcel from the moment a carrier shipment arrives until the parcel is delivered: receiving, sorting, loading, dispatch and delivery, with a full history for each parcel.

> **Status: early development.** The solution and project structure are in place. Features are being built for a first demo. Nothing here is production ready.

## Goals
- Receive carrier shipments and scan parcels (phone camera barcode scanning in the browser).
- Sort parcels into batches by destination zone, load them onto trips, and dispatch.
- Keep a complete, tamper-resistant history of every parcel status change.
- Role-based access with fine-grained permissions.
- Built for Australia: warehouse time zones, Australian addresses and postcodes, AUD and GST (later phase), and the Privacy Act.

## Parcel flow
```
Shipment arrives -> Receiving (scan) -> Sorting (batch by zone)
-> Loading (onto a trip) -> Dispatch -> Delivery / Return
```

## Tech stack
| Layer | Technology |
|---|---|
| Frontend | React, TypeScript, Vite (one responsive website: desktop and phone) |
| Backend | ASP.NET Core Web API (.NET 10), Clean Architecture |
| Database | SQL Server with EF Core |
| Tests | xUnit (backend) |

A mobile app (React Native or WebView) may come later. The API is designed so it can be reused.

## Project structure
```
.
├── backend/
│   ├── Warehouse.slnx
│   ├── src/
│   │   ├── Warehouse.Domain          Entities and business rules
│   │   ├── Warehouse.Application     Use cases, DTOs, interfaces
│   │   ├── Warehouse.Infrastructure  EF Core, SQL Server
│   │   └── Warehouse.Api             Controllers, startup, configuration
│   └── tests/
│       ├── Warehouse.Domain.Tests
│       └── Warehouse.Application.Tests
├── frontend/                          React app (Vite + TypeScript)
├── docs/
│   └── requirements.md                Business requirements (draft)
├── AGENTS.md                          Rules for AI coding agents
└── .agents/, .claude/, .codex/        Agent configuration and guardrail hooks
```

Dependencies point inward: `Api -> Application -> Domain`, and `Infrastructure -> Application`. The Domain project depends on nothing.

## Getting started

### Requirements
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org) (current LTS) and npm
- SQL Server (local or remote) for when the database is added

### Backend
```bash
cd backend
dotnet build Warehouse.slnx
dotnet run --project src/Warehouse.Api
```

### Frontend
```bash
cd frontend
npm install
npm run dev
```

Phone camera scanning needs HTTPS. It works on `localhost`, but testing from a phone needs an HTTPS address.

### Configuration and secrets
Do not commit secrets. Keep connection strings and keys in `dotnet user-secrets` or environment variables, never in `appsettings.Production.json` or `.env` files (both are git-ignored).

## Roadmap
**First demo (one warehouse, manual data entry, no import or export)**
- Login, roles, and an admin area separate from the staff area
- Master data: warehouse, zones, carriers, vehicles
- Shipments, receiving scans, parcels and tracking timeline
- Sorting batches, trips, loading and dispatch
- Simple delivery status and a small dashboard

**Later phases**
- Permission management screens and menu permissions
- Carrier contracts, invoices, payments, COD
- Proof of delivery, returns, driver app
- Carrier and merchant portals, notifications
- Multiple warehouses and organisations

## Documentation
- [Business requirements](docs/requirements.md) (draft; items marked PROPOSED are not decided yet)

## Working with AI agents
This repository includes shared rules and guardrails for AI coding tools (Claude, Codex, Grok, Antigravity). Read [AGENTS.md](AGENTS.md) before using one. In short: agents must ask before changing or running anything, stay inside this folder, and never use git, touch the database, delete files, read secrets, or install packages on their own.

## Branches
Work happens on `develop`.

## Licence
Not decided yet. All rights reserved until a licence is added.
