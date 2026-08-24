# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## AI-native SDLC (org-wide — do not remove or edit this section directly; it's maintained
centrally via the ai-native-sdlc plugin's policies/CLAUDE-baseline.md, applied with
`/apply-baselines`)
- No implementation without a linked, accepted `plan.md` under
  `docs/changes/<feature-slug>/`, which itself links an accepted `spec.md` and `intent.md`
  in the same folder. If any is missing for the change you're working on, say so and stop —
  don't infer intent and proceed.
- Use the `ai-native-sdlc` plugin's `/new-intent`, `/new-spec`, `/new-plan`, `/new-bugfix`,
  and `/sdlc-status` commands rather than creating these files free-hand.
- For a customer-reported or otherwise external bug: use `/new-bugfix`, not `/new-intent` —
  it's the lighter path for restoring documented expected behavior. Write a failing
  regression test before changing any code, confirm it fails for the right reason, then fix
  — never edit or delete that test to make it pass. If the fix turns out to need a real
  design decision, stop and escalate to `/new-intent` instead of forcing it into the bugfix.
- Verify your own work before reporting a task complete: run this repo's actual build/test
  commands (see the repo-specific section below) and paste the output. If a test fails, fix
  the code, not the test.
- Never commit secrets, credentials, or connection strings. Flag it immediately rather than
  attempting to work around a missing secret.
- No PII beyond what's in the agreed data model appears in logs or error messages.
- When you make the same mistake twice in this repo, the correction belongs in this file's
  repo-specific "Things Claude gets wrong here" section below — say so explicitly when it
  happens, so a human adds it.

## Project status

`expenseflow` implements the MVP from `docs/changes/expenseflow-mvp/` (intent → spec → plan, all accepted): expense claim submission and single-manager approval.

**Stack:** ASP.NET Core Web API + EF Core + SQL Server backend, ASP.NET Core Identity with cookie-based auth, and a React + TypeScript SPA (Vite) frontend.

**Solution layout** (`ExpenseFlow.sln`, all under `src/`):
- `ExpenseFlow.Api` — Web API host: `Program.cs` (DI, Identity/cookie auth, EF Core, endpoint mapping), `Endpoints/` (`ClaimsEndpoints`, `AuthEndpoints`), `Authorization/ManagerOfClaimHandler` (resource-based policy enforcing "caller is the claim owner's manager" server-side).
- `ExpenseFlow.Data` — EF Core: `ExpenseFlowDbContext` (extends `IdentityDbContext<User, ...>`), `Entities/` (`User`, `ExpenseClaim`, `ClaimDecision`), `Migrations/`, and `ExpenseFlowDbContextFactory` (design-time factory used by `dotnet ef`).
- `ExpenseFlow.Domain` — framework-independent business logic: `ClaimValidator` (positive-amount, non-future-date rules).
- `ExpenseFlow.Api.Tests` — xUnit; `ClaimValidatorTests` (unit) and `ClaimsEndpointsTests` (integration, via `WebApplicationFactory<Program>` with EF Core InMemory swapped in for `ExpenseFlowDbContext` — see `ClaimsEndpointsTestFactory`).
- `expenseflow-web` — React + TypeScript SPA (Vite): `src/pages/` (`Login`, `NewClaim`, `MyClaims`, `Approvals`), `src/api/client.ts` (typed fetch wrapper, cookie-based), `src/auth/AuthContext.tsx`. Vite dev server proxies `/api` to the backend (see `vite.config.ts`) so cookies stay same-origin in dev.

**Build/test commands** (from repo root):
- `dotnet build` — builds the whole solution.
- `dotnet test` — runs all unit + integration tests.
- `dotnet tool restore` — restores `dotnet-ef` (manifest at `.config/dotnet-tools.json`) before running `dotnet ef` commands.
- `dotnet ef migrations add <Name> --project src/ExpenseFlow.Data --startup-project src/ExpenseFlow.Data --output-dir Migrations` — add a migration (uses `ExpenseFlowDbContextFactory`, not `ExpenseFlow.Api`, as the design-time context source).
- `dotnet run --project src/ExpenseFlow.Api` — runs the API (needs a real SQL Server reachable via the `ExpenseFlow` connection string in `appsettings.json`/`appsettings.Development.json`; no seed/migration-apply step is wired up yet — run `dotnet ef database update` against a real server first).
- `npm install && npm run dev` (from `src/expenseflow-web/`) — runs the frontend against the API at `http://localhost:5099` (proxied).

## Known gaps / out of scope for this pass
- No receipt-upload endpoint (accepted as a data column only — see `docs/changes/expenseflow-mvp/plan.md`'s Risks).
- No password-reset or self-service signup; accounts and `manager_id` are assumed to be seeded directly (no seed script exists yet).
- No SQL Server instance is provisioned in CI/dev sandboxes by default — integration tests use EF Core InMemory instead (see `docs/changes/expenseflow-mvp/plan.md`'s "Departures from spec").
