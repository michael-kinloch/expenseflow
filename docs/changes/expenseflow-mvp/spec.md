# Spec: ExpenseFlow MVP: expense submission and manager approval

**Feature ID:** `expenseflow-mvp`
**Source intent:** `./intent.md` (same folder) · **Author (session driver):** Michael Kinloch · **Date:** 2026-08-24 · **Status:** accepted
**Design:** N/A — no Figma file exists yet for this feature
**GitHub issue:** https://github.com/michael-kinloch/expenseflow/issues/3

## Summary
ExpenseFlow will let an employee log in, submit an expense claim with amount, category, date, description, and an optional receipt attachment, and track its status. Their manager logs in, sees a queue of claims awaiting decision, and approves or rejects each one with the outcome recorded and visible to the employee.

## User flows

1. **Employee submits a claim**
   1. Employee logs in.
   2. Employee opens "New claim" and enters amount, currency, category, expense date, description, and optionally attaches a receipt.
   3. Employee submits. System validates required fields and rejects the submission with a clear error if amount is not positive, expense date is in the future, or a required field is missing.
   4. On success, the claim is created with status `pending` and the employee sees it in their claim list with that status.

2. **Employee views claim status**
   1. Employee logs in and opens "My claims."
   2. System lists their claims with current status (`pending`, `approved`, `rejected`) and, for decided claims, the decision date.

3. **Manager reviews the approval queue**
   1. Manager logs in and opens "Approvals."
   2. System lists claims from the manager's direct reports that are `pending`.
   3. If there are no pending claims, the queue shows an empty state, not an error.

4. **Manager approves or rejects a claim**
   1. Manager opens a claim from the queue and selects Approve or Reject (optionally with a comment).
   2. System records the decision, decided-by, and decision timestamp, and removes the claim from the pending queue.
   3. Employee sees the updated status and comment (if any) next time they view the claim.

5. **Unhappy paths**
   - An employee attempting to view or act on another employee's claim is denied.
   - A manager attempting to decide on a claim that isn't one of their direct reports' is denied.
   - A manager attempting to decide on a claim that has already been decided gets a clear "already decided" error, not a silent overwrite.

## Data model changes
- **User**: `id`, `name`, `email`, `password_hash`, `role` (`employee` | `manager` — a manager can also submit their own claims), `manager_id` (nullable FK to `User.id`, set by an admin for the MVP — no HR system integration)
- **ExpenseClaim**: `id`, `employee_id` (FK to `User.id`), `amount`, `currency`, `category`, `expense_date`, `description`, `receipt_url` (nullable), `status` (`pending` | `approved` | `rejected`), `submitted_at`
- **ClaimDecision**: `id`, `claim_id` (FK to `ExpenseClaim.id`), `decided_by` (FK to `User.id`), `decision` (`approved` | `rejected`), `comment` (nullable), `decided_at`

## API changes
- `POST /api/claims` — employee creates a claim. Body: `amount`, `currency`, `category`, `expense_date`, `description`, `receipt_url?`. Returns the created claim with `status: pending`. Rejects unknown fields.
- `GET /api/claims/mine` — employee lists their own claims.
- `GET /api/claims/pending` — manager lists pending claims from their direct reports only.
- `POST /api/claims/{id}/decision` — manager approves/rejects. Body: `decision`, `comment?`. Returns 409 if the claim is already decided.
- All endpoints require an authenticated session. `GET /api/claims/pending` and `POST /api/claims/{id}/decision` additionally require the caller to be the `manager_id` of the claim's employee — enforced server-side, never inferred from client-supplied identifiers.

## Acceptance criteria
- [ ] Submitting a claim with a positive amount, non-future expense date, and all required fields returns 201 and the claim appears in the employee's claim list with status `pending`.
- [ ] Submitting a claim with a negative or zero amount returns a validation error and creates no claim.
- [ ] Submitting a claim with a future expense date returns a validation error and creates no claim.
- [ ] `GET /api/claims/pending` for a manager returns only pending claims belonging to that manager's direct reports.
- [ ] `GET /api/claims/pending` returns an empty list (not an error) when a manager has no pending claims.
- [ ] A manager approving or rejecting a claim from a direct report returns 200 and updates the claim's status, decided-by, and decided-at.
- [ ] A manager attempting to decide on a claim that is not one of their direct reports' returns 403.
- [ ] An employee attempting to view another employee's claim returns 403.
- [ ] Deciding on an already-decided claim returns 409 and does not change the existing decision.
- [ ] The employee can see the decision outcome and manager comment (if any) on their claim after it's decided.

## Non-functional requirements
- Performance: no special requirement beyond existing SLOs — MVP scale is expected to be low volume (single-digit requests/sec).
- Security: session-based authentication required for every endpoint; authorization checked server-side against `manager_id`/`employee_id`, never trusted from the client (per org-wide secure-API baseline). Passwords stored as hashes, never plaintext.
- Accessibility: claim submission form and approval queue must be usable via keyboard and screen reader (standard form labeling, focus order) — no numeric target set for this MVP.
- Observability: every state-changing endpoint (`POST /api/claims`, `POST /api/claims/{id}/decision`) logs actor id, action, claim id, and timestamp. No claim amount/description/receipt content in logs beyond the claim id.

## Flagged concerns
- **Concern:** Receipt uploads (`receipt_url`) introduce a file-storage and file-type validation surface not covered by intent.md, and could carry PII (e.g. a receipt photo showing a card number or home address). — **Owner:** Engineering lead — **Resolution:** open — needs a decision on storage location, access control, and accepted file types before `/new-plan`.
- **Concern:** `manager_id` is set manually by an admin with no HR integration for the MVP (per intent.md's out-of-scope). This means manager assignment can drift or be wrong with no source of truth to reconcile against. — **Owner:** Product owner — **Resolution:** accepted as an MVP limitation; intent.md already scopes out HR integration, so no action needed now, but worth revisiting if adoption grows.
- **Concern:** No password-reset or account-provisioning flow is specified, but login is required for both roles. — **Owner:** Product owner — **Resolution:** open — needs a decision on whether this MVP needs self-service account creation or admin-provisioned accounts only.

## Open questions carried from intent.md
- None identified — intent.md listed no open questions.

## Out of scope
- Multi-level or finance-team approval chains (single manager approval only for the MVP)
- Integration with payroll, HR, or accounting systems (including automatic manager-hierarchy sync)
- Reimbursement/payment processing — approval status only, not payout
- Password reset / self-service account provisioning (flagged above as needing a decision)
