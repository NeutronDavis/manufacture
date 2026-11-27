# Plan: In-App Notifications & Price Change History View

**Status**: Implemented (2026-11-27)
**Created**: 2026-11-27
**Owner**: Production Pricing, Notifications, App Shell

## Summary (TL;DR)

Add a complete in-app notification system and a dedicated price-change history detail page.

- A Production Manager submits price changes → **all active SuperAdmins** receive one summary notification.
- A SuperAdmin approves or rejects a request → the **requester** receives a decision notification.
- The top-nav bell becomes a working dropdown with a red unread-count badge.
- Clicking a notification marks it read and opens its linked record.
- A new `/Notifications` page lists the signed-in user’s notifications, supports “Mark all as read”, and lets SuperAdmins send a general notification to either **every non-Customer user** or a **selected role**.
- The `/Production/Pricing` “Price Change History” table gets a **View** action linking to a new read-only detail page.

The codebase has no notification table, service, page, or working bell today. The existing bell is an inert button.

## Scope

**In scope**
- `Notifications` table + EF entity + DbContext mapping.
- `INotificationService` / `NotificationService` with per-recipient rows, unread counts, read actions, general fan-out, and price-change helpers.
- Price-change notification creation inside `ProductionService` so it is persisted with the existing price-change `SaveChanges`.
- Working notification bell dropdown with unread badge and unread items.
- `/Notifications` page with All/Unread filter, click-to-read, Mark all read, and SuperAdmin compose form with audience selection.
- Price Change History **View** action and `/Production/Pricing/History/{id}` detail page.
- Tests for pure notification text/type helpers and verification.

**Out of scope**
- Email, SMS, push, SignalR, or browser push notifications.
- Real-time badge updates without a page refresh/navigation.
- Notification deletion/archiving or per-user notification preferences.
- Notifications for modules other than price changes and manual general notices.
- A generic workflow/audience engine for future modules (the service can be extended later).
- Changing the existing price-change approval rules or RBAC.

## Current Findings

| Area | Current state | Change |
|---|---|---|
| Bell | `_Layout.cshtml:83-86` is an inert `<button>` with no id, href, dropdown, or badge | Replace with working daisyUI dropdown and unread badge |
| Notification storage | No table/entity/service/page exists | Add `Notifications`, entity, DbContext mapping, service, page |
| Price submission | `ProductionService.SubmitIngredientPriceChangeRequests` writes requests + `AuditLog` then saves | Add one summary notification per active SuperAdmin before `SaveChanges` |
| Price approval/rejection | `ApproveIngredientPriceChangeRequest` / `RejectIngredientPriceChangeRequest` update request + audit log | Add a decision notification to the requester before `SaveChanges` |
| Pricing history | `PriceHistory` table has no per-row action (`Index.cshtml:331-389`) | Add View action and detail page |
| Price detail fetch | Only a list method exists | Add `GetIngredientPriceChangeRequest(int id)` |
| General notices | No creation path | Add SuperAdmin compose form on `/Notifications` with “Everyone except Customers” or selected role |
| Roles | `SuperAdmin`, `ProductionManager`, `StoreManager`, `SalesRep`, `Customer`, `HrPayrollManager` | Use role names; Customers never receive notifications |
| Customer route gate | `RequireSessionPageFilter` already blocks `/Notifications` for Customers | Bell hidden for Customers + defensive page guard |

## Decisions (verbatim answers)

| Topic | Decision |
|---|---|
| Notification model | **“Per-recipient rows (Recommended)”** — one `Notifications` row per recipient; general notices fan out at send time. |
| General notices | **“I’ll go with option 1 but the form should also be able to send notification to some selected role too.”** — include compose form with audience = everyone except Customers **or** a selected role. |
| Submission alerts | **“One summary per submission (Recommended)”** — one notification per SuperAdmin per submit batch. |
| Admin recipients | **“All active SuperAdmins (Recommended).”** |
| Read behaviour | **“Click-to-read + mark all (Recommended)”** — bell shows unread; clicking marks read and navigates; page supports Mark all. |

## Data Model

### New table: `Notifications`

Follow `Manufacture.Database/README.md` conventions.

- `Id BIGINT IDENTITY(1,1) NOT NULL`
- `RecipientUserId INT NOT NULL` → `Users.Id` (`ON DELETE CASCADE`)
- `Title NVARCHAR(200) NOT NULL`
- `Message NVARCHAR(1000) NOT NULL DEFAULT N''`
- `LinkUrl NVARCHAR(300) NULL`
- `Type INT NOT NULL DEFAULT 0` — `0 General`, `1 PriceChangeSubmitted`, `2 PriceChangeApproved`, `3 PriceChangeRejected`
- `IsRead BIT NOT NULL DEFAULT 0`
- `ReadAt DATETIME2(3) NULL`
- `CreatedByUserId INT NULL` → `Users.Id` (`NO ACTION`)
- `CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME()`
- `RowVersion ROWVERSION NOT NULL`

Constraints / indexes:
- `PK_Notifications` on `Id`
- `FK_Notifications_RecipientUser` → `Users`
- `FK_Notifications_CreatedByUser` → `Users` (nullable, no cascade)
- `CK_Notifications_Type` `Type IN (0,1,2,3)`
- `IX_Notifications_Recipient_IsRead_CreatedAt` on `(RecipientUserId, IsRead, CreatedAt DESC)`
- `IX_Notifications_CreatedAt` on `(CreatedAt DESC)`

Read-state rule: service sets `IsRead = 1` and `ReadAt = SYSUTCDATETIME`/`DateTime.UtcNow` together. A check constraint is optional; do not make it so strict that legacy/manual rows cannot be inserted.

### New DTOs / enum

`Manufacture.Library/Models/DTOs/NotificationDtos.cs`:

- `NotificationType` enum: `General = 0`, `PriceChangeSubmitted = 1`, `PriceChangeApproved = 2`, `PriceChangeRejected = 3`.
- `NotificationDto`: `Id`, `Title`, `Message`, `LinkUrl`, `Type`, `TypeLabel`, `IsRead`, `ReadAt`, `CreatedAt`.
- `NotificationBellDto`: `int UnreadCount`, `List<NotificationDto> Items`.
- `SendNotificationDto`: `[Required] Title`, `[Required] Message`, `LinkUrl`, `Audience` (default `"all"`; otherwise a role name).
- `PriceChangeDecisionNotificationDto`: request id, request number, inventory item id/name, previous/requested costs, `RequestedByUserId`, `RequestedByName`, `Approved`, `ReviewerName`, `Reason`.

`NotificationText` static helper (pure, testable):
- `TypeLabel(NotificationType)`
- `PriceChangeSubmittedMessage(string requesterName, int count)`
- `PriceChangeApprovedMessage(string itemName, decimal oldCost, decimal newCost)`
- `PriceChangeRejectedMessage(string itemName, string? reason)`
- `IsSupportedAudience(string audience)` / role label helpers.

## Implementation Steps

### Phase 1 — Schema and EF

1. **Add the `Notifications` table.**
   - *Details*: Create `Manufacture.Database/Tables/Notifications.sql` using the columns above. Add `<Build Include="Tables\Notifications.sql" />` to `Manufacture.Database/Manufacture.Database.sqlproj`.
   - *Dependencies*: None.

2. **Add the entity and DbContext mapping.**
   - *Details*: Create `Manufacture.Library/Models/Entities/Notification.cs` (scaffold style, `public partial class`, `null!`, `virtual User RecipientUser`, `virtual User? CreatedByUser`). Add `DbSet<Notification> Notifications` and a Fluent block in `Manufacture.Library/Data/ManufactureDbContext.cs` following `ProductRepository`/`AuditLog` patterns. Configure max lengths, defaults, rowversion concurrency token, indexes, and FKs.
   - *Dependencies*: Depends on Step 1.

### Phase 2 — Notification service

3. **Add `INotificationService` and `NotificationService`.**
   - *Details*: Create `Manufacture.Library/Interfaces/INotificationService.cs` and `Manufacture.Web/Services/NotificationService.cs`, registered scoped in `Program.cs` near the other scoped DB services.
   - Methods (synchronous, matching the existing synchronous `ProductionService`):
     - `NotificationBellDto GetBell(int userId, int take = 6)` — unread count + newest unread items.
     - `List<NotificationDto> GetForUser(int userId, bool unreadOnly = false, int take = 200)`.
     - `int GetUnreadCount(int userId)`.
     - `bool MarkRead(long notificationId, int userId)`.
     - `int MarkAllRead(int userId)`.
     - `int SendGeneral(SendNotificationDto dto, int createdByUserId)` — resolves audience and saves.
     - `int AddPriceChangeSubmitted(int requestedByUserId, string requestedByName, int submittedCount)` — **adds rows without saving**.
     - `void AddPriceChangeDecision(PriceChangeDecisionNotificationDto dto)` — **adds rows without saving**.
   - Audience resolution:
     - `"all"` / empty → active users whose role is not `Customer`.
     - otherwise → active users in the named role; reject `Customer`.
   - Price submission recipients: active `SuperAdmin` users. If none, add nothing.
   - Decision recipients: `RequestedByUserId` when present/active; otherwise all active `ProductionManager` users.
   - `LinkUrl` is app-relative (`/Production/Pricing` or `/Production/Pricing/History/{id}`). Validate it starts with `/` before storing; empty/null allowed.
   - *Dependencies*: Depends on Step 2.

4. **Add pure notification text tests.**
   - *Details*: Add `Manufacture.Tests/NotificationTextTests.cs` covering type labels, submitted count message, approved/rejected messages, and audience validation. Use xUnit only; there is no EF test harness.
   - *Dependencies*: Can run parallel with Step 3 once `NotificationText` exists.

### Phase 3 — Price-change notifications and history detail

5. **Extend `ProductionService` to create notifications.**
   - *Details*:
     - Inject `INotificationService` into `ProductionService`.
     - In `SubmitIngredientPriceChangeRequests`, after the request loop and before `_db.SaveChanges()`, call `_notifications.AddPriceChangeSubmitted(requestedByUserId, requestedByName, SubmittedCount)` when `SubmittedCount > 0`.
     - In `ApproveIngredientPriceChangeRequest`, before `_db.SaveChanges()`, call `AddPriceChangeDecision` with request/item/cost fields, `Approved = true`, reviewer name, and no reason.
     - In `RejectIngredientPriceChangeRequest`, before `_db.SaveChanges()`, call `AddPriceChangeDecision` with `Approved = false`, reviewer name, and the rejection reason.
     - Notifications are added to the same scoped `ManufactureDbContext`, so the existing `SaveChanges` persists them atomically with the price request and audit log.
   - *Dependencies*: Depends on Step 3.

6. **Add single-request lookup and history detail page.**
   - *Details*:
     - Add `IngredientPriceChangeRequestDto? GetIngredientPriceChangeRequest(int id)` to `IProductionService`/`ProductionService`.
     - Create `Manufacture.Web/Pages/Production/Pricing/History.cshtml` and `.cshtml.cs` with `@page "{id:int}"`.
     - Page guard: same as Pricing (`ProductionManager` or `SuperAdmin`), otherwise redirect to Dashboard.
     - Show: request number/status badge, material + item code + unit, previous/requested/current price, difference and percentage, requester/requested date, reviewer/reviewed date, applied date, review notes, and a short workflow timeline.
     - Add a Back to Pricing link.
   - *Dependencies*: Depends on Step 5.

7. **Add View action to Price Change History.**
   - *Details*: In `Pages/Production/Pricing/Index.cshtml` (history card around lines 331-389), add an `Action` header and a per-row `View` link to `/Production/Pricing/History/{id}`. Update the empty-state colspan and keep the existing row search/status filters.
   - *Dependencies*: Depends on Step 6.

### Phase 4 — Bell and notifications page

8. **Wire the bell dropdown in `_Layout.cshtml`.**
   - *Details*:
     - Add `@inject Manufacture.Library.Interfaces.INotificationService Notifications`.
     - Read `UserId` from session next to the existing role/name/email reads.
     - Only render the bell for `userRole != "Customer"` and a valid `UserId`.
     - Build `var bell = userId.HasValue ? Notifications.GetBell(userId.Value) : new NotificationBellDto();`.
     - Replace the inert button (`_Layout.cshtml:83-86`) with a daisyUI `dropdown dropdown-end` modeled on the avatar menu (`_Layout.cshtml:89-114`).
     - Trigger: bell icon + red `indicator`/`badge badge-error badge-xs` only when `bell.UnreadCount > 0`; show the count.
     - Dropdown: up to 6 unread notifications with type icon/tone, title, short message, relative/absolute date; each links to `/Notifications?handler=Open&id=@item.Id`.
     - Empty state: “No unread notifications.”
     - Footer: “View all notifications” → `/Notifications`.
   - *Dependencies*: Depends on Step 3.

9. **Create the `/Notifications` page.**
   - *Details*: Create `Manufacture.Web/Pages/Notifications/Index.cshtml` + `.cshtml.cs` (default route `/Notifications`).
     - Read `UserId`/`UserRole` from session; redirect Customers defensively (the global filter already blocks them).
     - `OnGet(bool unreadOnly = false)`: load the signed-in user’s notifications, newest first. Show All/Unread filter chips and an unread count.
     - `OnGetOpen(long id)`: mark the notification read if it belongs to the user, then redirect to a local `LinkUrl` or `/Notifications`.
     - `OnPostMarkAllRead()`: mark all the user’s unread notifications read; TempData success; redirect.
     - `OnPostSendGeneral(SendNotificationDto dto)`: SuperAdmin only; `SendGeneral`; TempData success; redirect.
     - SuperAdmin-only compose card: Title, Message, optional Link, audience select with `Everyone (except Customers)` plus each non-Customer role. Load roles from `Roles`/`Users` via the service or DbContext-backed service.
     - Notification list: type icon/tone, title, message, created time, read/unread badge, action link. Use TempData alerts consistent with the layout.
   - *Dependencies*: Depends on Steps 3 and 8.

### Phase 5 — Deploy and verify

10. **Build, test, deploy, and walk the flows.**
    - *Details*: Build solution/tests, build the database project, deploy, then verify the manual flows below. Stop any dev server afterwards.
    - *Dependencies*: Depends on all previous steps.

## Relevant Files & Symbols

**Database / EF**
- `Manufacture.Database/Tables/Notifications.sql` — new.
- `Manufacture.Database/Manufacture.Database.sqlproj` — register the table.
- `Manufacture.Library/Models/Entities/Notification.cs` — new.
- `Manufacture.Library/Data/ManufactureDbContext.cs` — `DbSet<Notification>` + Fluent mapping.
- `Manufacture.Database/README.md` — add Notifications to the table map/ER diagram.

**Contracts / services**
- `Manufacture.Library/Models/DTOs/NotificationDtos.cs` — new enum/DTOs.
- `Manufacture.Library/Interfaces/INotificationService.cs` — new.
- `Manufacture.Web/Services/NotificationService.cs` — new.
- `Manufacture.Web/Services/NotificationText.cs` — pure formatting helper.
- `Manufacture.Web/Program.cs` — register scoped `INotificationService`.
- `Manufacture.Library/Interfaces/IProductionService.cs` — add single-request lookup.
- `Manufacture.Web/Services/ProductionService.cs` — inject notifications; add writes in submit/approve/reject; add lookup.

**Pricing UI**
- `Manufacture.Web/Pages/Production/Pricing/Index.cshtml` — history Action column + View link; line refs around 309-395.
- `Manufacture.Web/Pages/Production/Pricing/Index.cshtml.cs` — optionally reuse role guard; no notification logic here.
- `Manufacture.Web/Pages/Production/Pricing/History.cshtml` + `.cshtml.cs` — new detail page.

**App shell / notifications UI**
- `Manufacture.Web/Pages/Shared/_Layout.cshtml` — bell injection/dropdown at 83-86; session UserId read near 3-8.
- `Manufacture.Web/Pages/Notifications/Index.cshtml` + `.cshtml.cs` — new page.
- `Manufacture.Web/Pages/_ViewImports.cshtml` — optionally add `@using Manufacture.Library.Interfaces` (or use the full namespace in `@inject`).

**Tests**
- `Manufacture.Tests/NotificationTextTests.cs` — new pure tests.
- Existing pricing service tests are mock-only; do not assume EF tests.

## Verification Plan

1. **Automated**
   - `dotnet build Manufacture.slnx`
   - `dotnet test Manufacture.Tests/Manufacture.Tests.csproj`
   - `dotnet build Manufacture.Database/Manufacture.Database.sqlproj`
   - `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/deploy-database.ps1`
   - Use the temp output path if files are locked: `-p:BaseOutputPath=C:\Users\user\AppData\Local\Temp\opencode\ManufactureBuild\`.

2. **Manual flows**
   - Sign in as `admin@bakery.com` (SuperAdmin): bell shows unread count and dropdown items.
   - Sign in as `production@bakery.com` (ProductionManager): open `/Production/Pricing`, submit 2+ price changes. One summary notification appears per active SuperAdmin (not one per item).
   - As SuperAdmin: approve one request and reject another with a reason. The requester receives one notification per decision with the correct item/price/reason and links to the history detail page.
   - Bell: unread badge shows the count; dropdown lists only unread; clicking an item marks it read and opens its link; count decreases after navigation.
   - `/Notifications`: All/Unread filter, Mark all as read, read/unread states, and action links work.
   - SuperAdmin compose: send to “Everyone (except Customers)” and verify all non-Customer roles receive it; send to a selected role and verify only that role receives it; Customers never see it.
   - Price Change History: each row has View; the detail page matches the submitted/approved/rejected values and notes.
   - Customer login: bell hidden; `/Notifications` redirects to `/Sales/PreOrderSchedule/Index` via the existing filter.
   - Verify `Notifications` rows are per recipient and `IsRead/ReadAt` update correctly.

3. **Regression**
   - Price submission still skips unchanged/duplicate pending items and keeps the existing one-pending-per-item rule.
   - Approval still applies inventory/recipe costs atomically.
   - Rejection still requires a reason and does not change prices.
   - Existing RBAC/Pricing role guards remain unchanged.

## Context & Decisions Log

- **2026-11-27**: User requested notifications when a Production Manager changes prices, when a SuperAdmin approves/rejects, a bell dropdown with unread items and a red count badge, a notification service/interface/page, general notifications for everyone except Customers, and a View action for Price Change History.
- **2026-11-27**: Storage = **per-recipient rows**; general notices fan out at send time.
- **2026-11-27**: Compose form must support **everyone except Customers** or a **selected role**.
- **2026-11-27**: Price submission creates **one summary notification per submission** for **all active SuperAdmins**.
- **2026-11-27**: Read behaviour = **click-to-read + Mark all as read**.
- **Note**: User wrote `/Production/Pricing1`; the actual page is `/Production/Pricing`.

## Key Decisions & Trade-offs

| Topic | Decision | Why |
|---|---|---|
| Notification storage | Per-recipient rows | Read state is simple; no receipt table; fits existing scoped DbContext and page queries. |
| General notices | Fan out at send time to active non-Customer users | Matches per-recipient model; simplest reliable unread counts. Users created after send time will not see past general notices. |
| Price submission alerts | One summary per batch | Avoids flooding admins when a PM submits many items. |
| Decision alerts | Per request to the requester | The requester needs item-specific approval/rejection details. |
| Notification writes | Added to `ProductionService` before its existing `SaveChanges` | Keeps price request, audit log, and notification in one transaction. |
| Compose audience | Everyone-except-Customers or one selected role | Matches the explicit request; avoids a full audience engine. |
| Bell data | Sync service query in `_Layout` | Layout already uses sync `RbacCache`; no ViewComponent pattern exists. Skip query for unauthenticated/Customer sessions. |
| History detail | New `/Production/Pricing/History/{id}` page | Gives a stable link target for notifications and a clean read-only view. |
| Read state | Click marks read; page has Mark all | Matches the chosen behaviour and keeps the bell unread-only. |

## Implementation Record (2026-11-27)

- **Build**: `dotnet build Manufacture.slnx` — passed.
- **Tests**: `dotnet test Manufacture.Tests/Manufacture.Tests.csproj` — 268 passed, 0 failed.
- **Database**: `Notifications` table deployed to LocalDB with the documented columns, FKs, and indexes. (The deploy script's sqlpackage publish hung on one run; the table script was applied directly with `sqlcmd` and the dacpac still contains the table for future deploys.)
- **Manual verification**:
  - SuperAdmin sent a general notification to “Everyone (except Customers)” → 5 recipient rows created (one per active non-Customer user); Customers excluded.
  - Admin bell displayed the unread notice; `/Notifications` page displayed it.
  - “Mark all as read” cleared the admin unread count.
  - ProductionManager submitted a price change → one “Price change approval needed” notification appeared for the SuperAdmin.
  - SuperAdmin approved the request → requester received a “Price change approved” notification.
  - SuperAdmin rejected a second request with a reason → requester received a “Price change rejected” notification containing the reason.
  - `/Production/Pricing/History/{id}` returned 200 and displayed the price change detail.
- **Cleanup**: Verification notifications, test price-change requests, audit-log rows, and test price changes were removed; inventory price and recipe cost snapshots were restored. The verification server was stopped.

