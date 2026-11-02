# Warehouse Parcel Distribution System: Requirements

Status: DRAFT. The source document is not complete yet. Items marked **PROPOSED** are suggestions, not decided. Do not treat them as final; ask the user.

Source: `Warehouse_Parcel_Distribution_System_Specification.docx` (System Architecture, RBAC and Database Specification).

## 1. What this system is
A parcel hub / distribution system (not a stock inventory system). Parcels arrive from carriers, are received and scanned, sorted by destination, loaded onto vehicles on trips, dispatched, and delivered. Every parcel status change is recorded as an event, so each parcel has a full history.

Main flow:

```
Shipment arrives -> Receiving (scan) -> Sorting (batch by zone)
-> Loading (onto a trip) -> Dispatch -> Delivery / Return
```

## 2. Demo scope (this week, one client demo)
- One website, responsive: desktop and phone. No mobile app now. React Native or WebView may come later.
- Barcode is scanned with the phone camera in the browser (needs HTTPS). Manual barcode entry is always available as a fallback.
- One warehouse (one hub) only.
- All data is entered manually by users. No import or export.
- In scope: login and users, master data, shipments, receiving, parcels and tracking, sorting, trips and dispatch, simple delivery status, small dashboard.
- Out of scope (Phase 2): carrier contracts/rates/SLAs, invoices and payments, COD reconciliation, proof of delivery (signature/photo/GPS), returns, driver app, carrier and merchant portals, notifications, attachments, multi-hub, multi-tenant.

## 2a. Australian context (applies to everything)
This system is for warehouses in **Australia**. Build everything for Australia:
- **Time:** store all times in UTC. Each warehouse has an IANA time zone (for example `Australia/Sydney`, `Australia/Perth`) and times are shown in that zone. Daylight saving differs by state (none in QLD, NT, WA), so never hardcode an offset.
- **Address:** street, suburb, state, postcode, country (`AU`). States and territories: ACT, NSW, NT, QLD, SA, TAS, VIC, WA. Postcode is exactly 4 digits. **PROPOSED** state/postcode check (verify against Australia Post data before relying on it): NSW 1000-2599, 2619-2899, 2921-2999; ACT 0200-0299, 2600-2618, 2900-2920; VIC 3000-3999, 8000-8999; QLD 4000-4999, 9000-9999; SA 5000-5999; WA 6000-6999; TAS 7000-7999; NT 0800-0999.
- **Phone:** Australian format (`+61`, mobile `04xx xxx xxx`). **Dates:** `dd/MM/yyyy`. **Units:** metric (kg, cm). **Language:** English with Australian spelling.
- **Money (Phase 2):** AUD, GST 10%.
- **Privacy:** the Privacy Act 1988 and the Australian Privacy Principles apply. Recipient name, phone and address, and all staff details, are personal information. Collect only what is needed, restrict who can see it, never write it to logs, use fake data in demos, and plan for data to be hosted in Australia. Do NOT store Tax File Numbers. Do not store national ID, visa or student status unless the user confirms it is needed (see open questions).
- Compliance details (privacy, workplace, transport rules) must be confirmed with the user before being built. Do not assume legal rules.

## 3. Roles
Twelve roles are defined. Six are enough for the first release.

| # | Role | Main responsibilities | MVP |
|---|---|---|---|
| 1 | Super Admin | Platform-wide settings, security, all user access | Yes |
| 2 | Warehouse Admin | Hub setup: zones, locations, users, carrier accounts, reports | Yes |
| 3 | Warehouse Manager | Supervises receiving, sorting, dispatch; resolves discrepancies; daily throughput | Yes |
| 4 | Shipment Receiving Clerk | Processes inbound manifests, scans shipments, checks counts, flags missing/damaged | Yes |
| 5 | Sorting Operator | Sorts parcels by destination (city, zone, route) into batches | Yes (combined with Loader) |
| 6 | Loader / Dispatch Operator | Scans parcels into vehicles, checks the loading manifest, signs off departure readiness | Yes (combined with Sorting) |
| 7 | Dispatcher | Plans trips, assigns driver and vehicle, confirms dispatch | Yes |
| 8 | Driver / Delivery Rider | Runs assigned trips, records delivery or return | Phase 2 |
| 9 | Inventory / Audit Officer | Investigates damaged, missing or misrouted parcels, audits | Phase 2 |
| 10 | Finance Officer | Carrier contracts, billing, invoices, COD, payments | Phase 2 |
| 11 | Carrier Representative | External portal: track shipments, upload manifests | Phase 2 |
| 12 | Client / Merchant | External portal: book parcels, track, download invoices | Phase 2 |

Role categories: Administration (1-3), Warehouse Operations (4-6, 9), Transport and Delivery (7-8), External (11-12), Finance (10).

For the demo the Driver is not a user. A staff member marks a parcel delivered. **PROPOSED**

## 4. Permissions (RBAC)
- Permissions are granular and are checked in middleware/policies. Role names are never hardcoded in business code.
- Roles are groups of permissions. A user can have one or more roles.
- Every permission is enforced on the API, and also hidden in the UI.

Permission names. From the spec: `shipment.create`, `parcel.scan`, `parcel.sort`, `trip.dispatch`, `carrier.manage`. **PROPOSED** (to complete the set): `user.manage`, `shipment.view`, `parcel.register`, `parcel.view`, `trip.create`, `trip.load`, `delivery.record`, `report.view`, `master.manage`.

Role / feature matrix from the spec ("Limited" is not defined yet, see open questions):

| Feature | Admin | Manager | Receiving | Sorting | Loader | Dispatcher | Driver |
|---|---|---|---|---|---|---|---|
| Manage users | Full | Limited | - | - | - | - | - |
| Manage carriers | Full | Limited | - | - | - | - | - |
| Create shipments | Full | Full | Full | - | - | - | - |
| Register parcels | Full | Full | Full | Limited | - | - | - |
| Sort parcels | Full | Full | Limited | Full | - | - | - |
| Create trips | Full | Full | - | - | - | Full | - |
| Load parcels | Full | Full | - | - | Full | Limited | - |
| Confirm dispatch | Full | Full | - | - | Limited | Full | Limited |
| View reports | Full | Full | Limited | Limited | Limited | Limited | Own trips |
| Record delivery | Full | Limited | - | - | - | - | Assigned |

## 5. Login and users
Spec table: `users` (id, name, email, password_hash, status, created_at), `roles`, `permissions`, `user_roles`, `role_permissions`.

Decisions for the demo (**PROPOSED**, please confirm):
- **Login:** email and password. Token-based (JWT). Passwords are stored only as a hash.
- **Signup (user decision):** the demo HAS a signup page. It creates a Super Admin. It is NOT linked anywhere on the website; it is reached only by typing its URL. Later the same idea serves multiple warehouses.
- **Organisation (PROPOSED):** signup creates an Organisation (the owner account) and its Super Admin. Organisation owns its warehouses, users and roles. Put an `OrganisationId` on organisation-owned tables from day one (cheap now, painful later). The demo uses one organisation with one warehouse.
- **Signup safety (PROPOSED):** a hidden URL is not security. The signup API must have rate limiting, strong password rules and the page must not be indexed by search engines. Email verification before production. Signup must never let a user pick a role; it only creates the Organisation owner (Super Admin).
- **Other staff:** created by Super Admin (or Warehouse Admin). Public signup for Client/Merchant is Phase 2.
- **After login (demo):** Super Admin goes to `/admin` (empty dashboard, plus the Roles screen). Everyone else goes to `/app` (empty dashboard). Admin pages and staff pages are separate areas (separate layout and menu) in the same React app. Admin API endpoints live under `/api/admin/...` and are Super Admin only.
- **Temporary role check:** until permissions exist, the admin area is guarded by ONE policy (for example `AdminArea`), so it can later be changed to a permission check in one place. Menu visibility by permission comes later.
- **Roles (user decision):** Super Admin can create roles. A role record has: name, description, status (active/inactive), system-role flag, created/updated dates. Permissions per role are designed later (the `role_permissions` table exists but stays empty for now). The 6 MVP roles are seeded as protected system roles.
- **User profile (PROPOSED):** name, email, phone, status, role(s), start date. The user also mentioned email, dates, national ID and international student status. Those describe a PERSON, not a role. They are held back until the user decides (open question 9).
- **Seed data:** one demo user per MVP role, created at setup.
- **User status:** Active / Inactive. Inactive users cannot log in. Users are never hard-deleted.
- **Password rules and lockout:** minimum length, lock after repeated failures. **PROPOSED**
- **Forgot/reset password:** not in the demo. **PROPOSED**

## 6. Shipments (incoming)
A shipment is one inbound delivery from a carrier that contains many parcels.

Fields (spec): `id, shipment_number, carrier_id, manifest_number, origin_location_id, destination_warehouse_id, expected_parcel_count, status, arrived_at, received_by, completed_at, created_at`.
Link table `shipment_parcels`: `id, shipment_id, parcel_id, scanned_at, scanned_by, receiving_status`.

Rules:
- `shipment_number` is globally unique.
- A parcel appears once per shipment: `UNIQUE(shipment_id, parcel_id)`.
- `expected_parcel_count` comes from the manifest. The received count is always calculated (COUNT of scanned parcels), never typed by hand.
- Receiving, sorting batch creation and vehicle loading run inside database transactions, so there is no half-finished scan.
- Shipments are never hard-deleted. Use a status such as CANCELLED or VOID.
- Parcels on a manifest are registered first with status Expected, so missing parcels can be found. **PROPOSED**

Shipment status. **PROPOSED**: `Expected -> Arrived -> Receiving -> Completed` (or `Completed with discrepancies`), and `Cancelled`.

## 7. Parcels
Fields (spec): `id, tracking_number, carrier_id, recipient_name, recipient_phone, destination_city_id, destination_zone_id, delivery_address, weight, dimensions, cod_amount, status, current_location_id, created_at, updated_at`.

Rules:
- Tracking number is unique per carrier: `UNIQUE(carrier_id, tracking_number)`.
- `status` and `current_location_id` are the latest known state. The full history is in `parcel_events`.
- Parcels are never hard-deleted (status flags / soft delete).
- `cod_amount` is stored but COD handling is Phase 2.
- Split `dimensions` into length, width, height. **PROPOSED**

### Parcel events (history)
`parcel_events`: `id, parcel_id, event_type, from_status, to_status, location_id, performed_by, notes, created_at`.
- Never overwrite a status silently. Every change adds a new event row.
- The table is insert-only: no updates and no deletes.
- The status change and its event are saved in the same transaction.
- The event list is the tracking timeline shown to users.

### Parcel status machine (**PROPOSED**, must be confirmed)
```
Expected -> Received -> Sorted -> Loaded -> InTransit -> Delivered
Expected -> Missing            Received -> Damaged
InTransit -> DeliveryFailed -> (retry) or Returned
any open status -> Cancelled
```
Only the allowed transitions are accepted by the system. Everything else is rejected.

## 8. Sorting
Sorting groups parcels by destination into batches.
- `sorting_batches`: `id, warehouse_id, destination_zone_id, status, created_by, created_at, completed_at`.
- `sorting_batch_parcels`: `id, sorting_batch_id, parcel_id, sorted_by, sorted_at`.

## 9. Vehicles, trips and dispatch
- `vehicles`: `id, registration_number (unique), vehicle_type, capacity, status`.
- `trips`: `id, trip_number, origin_warehouse_id, destination_location_id, vehicle_id, driver_id, status, planned_departure_at, actual_departure_at, arrived_at, created_by`.
- `trip_parcels`: `id, trip_id, parcel_id, loaded_by, loaded_at, unloaded_at`.
- `trip_events`: `id, trip_id, event_type, location_id, performed_by, notes, created_at` (Phase 2 for the demo).
- Trips are never hard-deleted (status flags).

## 10. Master data
`warehouses` (code unique), `locations` (tree through `parent_location_id`), `cities`, `delivery_zones`, `carriers` (code unique). Entered manually. One warehouse in the demo.

## 11. Other tables
`audit_logs` (insert-only): `id, user_id, entity_type, entity_id, action, old_values, new_values, created_at`.
Phase 2: `attachments`, `notifications`, carrier contracts/rates/SLAs, invoices, invoice items, payments, delivery attempts, proofs of delivery, returns.

## 12. Technical rules
- Backend: ASP.NET Core Web API, Clean Architecture, EF Core, SQL Server. Frontend: React (responsive web).
- Granular permission-based authorization.
- Atomic transactions for receiving, sorting and loading.
- Event-based status history. Soft deletes.
- Insert-only: `parcel_events`, `audit_logs`.
- All times in UTC.

## 13. Open questions
1. What does "Limited" mean in the permission matrix (which parts, own records only, view only)?
2. Do parcels get registered from a manifest before arrival, or created at the first scan?
3. How does a sorting batch relate to a trip? (One batch on one trip, or many?)
4. Barcode: use the carrier tracking number, or an internal label as well?
5. Is the parcel status machine above right?
6. Signup, password reset and password rules: confirm the proposals in section 5.
7. Language of the UI: English only, or Urdu as well?
8. The rest of the document (sections still to be written).
9. Staff profile fields: are national ID and international student status really needed? Australia has no national ID card (options are driver licence, passport, TFN, Medicare, and TFN must not be stored). Student visa holders have limited work hours, so the real need may be "right to work" and "working hours limit". Decide what is needed, why, who can see it, and how it is protected.
10. Signup: confirm that it creates an Organisation plus Super Admin, and that other roles never self-register.
11. Which Australian time zone(s) do the warehouses use? Which states?
