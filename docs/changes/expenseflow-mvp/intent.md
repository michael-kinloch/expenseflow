# Intent: ExpenseFlow MVP: expense submission and manager approval

**Feature ID:** `expenseflow-mvp`
**Author:** Michael Kinloch · **Date:** 2026-08-24 · **Status:** accepted
**GitHub issue:** <link once one exists — fill in retrospectively if the issue comes after this file>

## Problem
Employees currently submit expenses via email, spreadsheets, or paper, and managers approve them informally with no consistent record. This leaves no audit trail, no shared visibility into what's pending or decided, and no way for an employee to check the status of a claim without asking their manager directly.

## Proposed outcome
An employee can submit an expense claim digitally and see its status without having to ask. Their manager gets a queue of claims awaiting their decision and can approve or reject each one, with the outcome recorded and visible to the employee.

## Affected users and systems
- Users: employees submitting expense claims; their direct managers, who review and decide on those claims
- Systems: ExpenseFlow is a new, standalone application for the MVP — no integration with payroll, HR, or accounting systems at this stage

## Constraints
- None identified

## Out of scope
- Multi-level or finance-team approval chains (single manager approval only for the MVP)
- Integration with payroll, HR, or accounting systems
- Reimbursement/payment processing — approval status only, not payout

## Open questions
- None identified

## Success measure
80% of expense claims are submitted and approved through ExpenseFlow, rather than email/paper/spreadsheet, within 4 weeks of launch.
