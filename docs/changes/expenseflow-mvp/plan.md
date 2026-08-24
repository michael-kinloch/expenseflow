# Plan: ExpenseFlow MVP: expense submission and manager approval

**Feature ID:** `expenseflow-mvp`
**Source spec:** `./spec.md` (same folder) · **Engineer:** Michael Kinloch · **Date:** 2026-08-24 · **Status:** accepted
**GitHub issue/PR:** https://github.com/michael-kinloch/expenseflow/issues/5

## Context
This repo currently has no application code — this plan bootstraps the solution from scratch. Stack decisions (confirmed with the user, not guessed): ASP.NET Core Web API + EF Core + SQL Server on the backend, ASP.NET Core Identity with cookie auth for session-based login, and a React + TypeScript SPA frontend, all in a single solution under `src/`.

## Files that change
- `ExpenseFlow.sln` (new) — solution file referencing the three backend projects
- `src/ExpenseFlow.Api/ExpenseFlow.Api.csproj` (new) — ASP.NET Core Web API host project
- `src/ExpenseFlow.Api/Program.cs` (new) — app startup: DI registration, Identity + cookie auth, EF Core context, endpoint mapping
- `src/ExpenseFlow.Api/Endpoints/ClaimsEndpoints.cs` (new) — `POST /api/claims`, `GET /api/claims/mine`, `GET /api/claims/pending`, `POST /api/claims/{id}/decision`
- `src/ExpenseFlow.Api/Endpoints/AuthEndpoints.cs` (new) — `POST /api/auth/login`, `POST /api/auth/logout`
- `src/ExpenseFlow.Api/Authorization/ManagerOfClaimHandler.cs` (new) — authorization handler enforcing "caller is `manager_id` of the claim's employee" server-side, per spec's NFR
- `src/ExpenseFlow.Data/ExpenseFlow.Data.csproj` (new) — EF Core data project
- `src/ExpenseFlow.Data/ExpenseFlowDbContext.cs` (new) — `DbContext` with `Users`, `ExpenseClaims`, `ClaimDecisions`
- `src/ExpenseFlow.Data/Entities/User.cs`, `ExpenseClaim.cs`, `ClaimDecision.cs` (new) — entities matching spec's data model exactly
- `src/ExpenseFlow.Data/Migrations/*` (new, generated) — initial EF Core migration creating the three tables
- `src/ExpenseFlow.Domain/ExpenseFlow.Domain.csproj` (new) — domain project for validation/business rules independent of EF and HTTP
- `src/ExpenseFlow.Domain/ClaimValidator.cs` (new) — positive-amount and non-future-expense-date validation, shared by API and tests
- `src/ExpenseFlow.Api.Tests/ExpenseFlow.Api.Tests.csproj` (new) — xUnit integration test project
- `src/ExpenseFlow.Api.Tests/ClaimsEndpointsTests.cs` (new) — covers acceptance criteria (see Proof)
- `src/expenseflow-web/` (new) — React + TypeScript SPA, created via Vite (`package.json`, `src/main.tsx`, etc.)
- `src/expenseflow-web/src/pages/NewClaim.tsx` (new) — submission form (flow 1)
- `src/expenseflow-web/src/pages/MyClaims.tsx` (new) — employee claim list with status (flow 2)
- `src/expenseflow-web/src/pages/Approvals.tsx` (new) — manager pending queue + decision UI (flows 3–4)
- `src/expenseflow-web/src/api/client.ts` (new) — typed fetch wrapper for the four claims endpoints + auth
- `CLAUDE.md` (modified) — replace the "pre-scaffold" project-status section with real build/test/run commands once the solution exists (`dotnet build`, `dotnet test`, `npm run dev`)

## Order of work
1. Scaffold `ExpenseFlow.sln` and the four backend projects (`Api`, `Data`, `Domain`, `Api.Tests`) with project references: `Api` → `Data` + `Domain`; `Api.Tests` → `Api`.
2. Add `User`, `ExpenseClaim`, `ClaimDecision` entities and `ExpenseFlowDbContext` in `ExpenseFlow.Data`; generate and apply the initial migration against a local SQL Server (Docker container for dev).
3. Wire ASP.NET Core Identity (using `User` as the Identity user) with cookie auth in `Program.cs`; add `AuthEndpoints` for login/logout.
4. Implement `ClaimValidator` in `ExpenseFlow.Domain` (positive amount, non-future date) with unit tests before wiring it into the endpoint — this is the one piece of pure business logic worth testing in isolation from HTTP/EF.
5. Implement `ManagerOfClaimHandler` authorization handler and register it as a policy; implement `ClaimsEndpoints` using that policy on the manager-only routes.
6. Write `ClaimsEndpointsTests` against each acceptance criterion (see Proof) before considering the API done.
7. Scaffold `expenseflow-web` (Vite + React + TypeScript), build `NewClaim`, `MyClaims`, `Approvals` pages against the real API (not mocked), covering the empty-state and error paths from spec's user flows.
8. Manually walk through all four user flows end-to-end (two browser sessions/users) before calling this done.
9. Update `CLAUDE.md`'s project-status section to reflect the real stack and commands.

## Risks
- **EF Core migration on `User`/Identity integration**: mapping a custom `manager_id` self-referencing FK onto ASP.NET Core Identity's `IdentityUser` can produce a circular/awkward migration if done carelessly (Identity's own schema plus a self-referencing FK in the same table). Mitigated by modeling `manager_id` as a plain nullable FK column added via a normal migration after Identity's own tables are scaffolded, not by trying to customize Identity's base schema.
- **Authorization bypass via client-supplied ids**: `GET /api/claims/pending` and `POST /api/claims/{id}/decision` must derive the caller's identity from the authenticated cookie/session, never from a request parameter — this is the exact case spec's Security NFR calls out, and it's the most security-sensitive part of this plan. Mitigated by the dedicated `ManagerOfClaimHandler` policy applied to both routes, tested explicitly (see Proof) rather than left to ad hoc checks in each endpoint.
- **No receipt-upload implementation decided yet**: spec.md's "Flagged concerns" left receipt storage/PII handling open. This plan treats `receipt_url` as an optional field with no upload endpoint built in this pass — attaching a receipt is out of scope for this plan's implementation, even though the column exists in the data model. This must be called out again explicitly at review so it isn't mistaken for an oversight.
- **No password-reset/account-provisioning flow decided yet** (also flagged open in spec.md): this plan assumes an admin manually seeds `User` rows (including `manager_id`) via a to-be-written seed script or direct DB insert for testing — there is no self-service signup in this plan.

## Proof
- `ClaimValidatorTests.cs` (new, in `ExpenseFlow.Api.Tests` or a dedicated `ExpenseFlow.Domain.Tests`): positive-amount and future-date rejection, matching acceptance criteria 2 and 3.
- `ClaimsEndpointsTests.cs`:
  - `PostClaims_WithValidData_Returns201AndPendingStatus` — criterion 1
  - `PostClaims_WithNonPositiveAmount_ReturnsValidationError` — criterion 2
  - `PostClaims_WithFutureExpenseDate_ReturnsValidationError` — criterion 3
  - `GetPendingClaims_ReturnsOnlyDirectReportsClaims` — criterion 4
  - `GetPendingClaims_WithNoPendingClaims_ReturnsEmptyList` — criterion 5
  - `PostDecision_ByDirectManager_Returns200AndUpdatesClaim` — criterion 6
  - `PostDecision_ByNonManager_Returns403` — criterion 7
  - `GetClaim_ByOtherEmployee_Returns403` — criterion 8
  - `PostDecision_OnAlreadyDecidedClaim_Returns409AndDoesNotChangeDecision` — criterion 9
  - `GetClaim_AfterDecision_ShowsOutcomeAndComment` — criterion 10
- Manual end-to-end walkthrough of all four spec user flows in the running app (documented as a checklist in the PR description), since the React SPA has no automated tests in this MVP pass.

## Departures from spec (fill in only if they occur during implementation)
- **Added `GET /api/claims/{id}`, not listed in spec.md's "API changes" section.** spec.md's acceptance criteria 8 ("An employee attempting to view another employee's claim returns 403") and 10 ("The employee can see the decision outcome and manager comment... after it's decided") both require a way to fetch a single claim by id, and this plan's own Proof section already named tests assuming one exists (`GetClaim_ByOtherEmployee_Returns403`, `GetClaim_AfterDecision_ShowsOutcomeAndComment`). Implemented as an authenticated `GET /api/claims/{id}` that allows the claim's employee or their manager, and returns 403 otherwise — this was implicit in spec.md's acceptance criteria even though the API list omitted it, so treated as a gap to fill rather than a design change.
- **Solution file format**: `dotnet new sln` in the SDK available in this environment (10.0.400) defaults to the new `.slnx` XML format. Regenerated with `--format sln` to match plan.md's stated `ExpenseFlow.sln` filename exactly.
- **EF Core migration generation uses a design-time factory** (`ExpenseFlowDbContextFactory` in `ExpenseFlow.Data`, implementing `IDesignTimeDbContextFactory`) rather than relying on `ExpenseFlow.Api`'s `Program.cs`, so the migration could be generated (and the data model finished) before wiring up Identity/auth in step 3, matching plan.md's stated order of work.
- **Endpoint tests use EF Core's InMemory provider, not a real SQL Server instance.** No SQL Server (Docker or otherwise) was available in this sandbox. `ClaimsEndpointsTestFactory` swaps `ExpenseFlowDbContext`'s registration to `UseInMemoryDatabase` inside a custom `WebApplicationFactory<Program>`, keeping the real SQL Server provider for actual runtime use (`Program.cs` is unchanged). This is a common, low-risk substitution for integration tests since the entities/queries used don't rely on SQL Server-specific behavior.
- **Manual end-to-end walkthrough was not completed against a live browser + real SQL Server.** This sandbox has Docker installed, but every `docker run` (needed to stand up a SQL Server container) requires an interactive approval step that has no human available to grant during this unattended run; the same is true of `curl`/browser automation against the locally-running servers. `dotnet run --project src/ExpenseFlow.Api` and `npm run dev` were both confirmed to start cleanly (no startup errors), but a real interactive two-browser click-through of the four user flows could not be performed here. The 10 `ClaimsEndpointsTests` (run via `WebApplicationFactory`'s in-process test server, which doesn't need network/Docker approval) exercise the same HTTP request/response flows as the acceptance criteria and are the functional proof for this PR; a human should still do a live click-through before merging.
