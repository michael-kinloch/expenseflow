# Review instructions

Applies to every PR, whether opened by a human, Claude Code, or Copilot's coding agent.

## Passes (org-wide — do not remove or edit this section directly; it's maintained
centrally via the ai-native-sdlc plugin's policies/REVIEW-baseline.md, applied with
`/apply-baselines`)
Run all four passes and tag each finding with its pass:

- **Bugs** — logic errors, broken edge cases, subtle regressions
- **Security** — injection risks, authentication/authorization gaps, PII in logs or error
  messages, secrets in the diff
- **Compliance** — the change matches `spec.md` (acceptance criteria met, nothing extra
  in scope) and `plan.md` (diff matches the stated files/approach — flag any undocumented
  departure)
- **NFRs** — checked against the Non-functional requirements section of the linked `spec.md`

## What "Important" means here (org-wide)
Reserve Important for findings that would break behavior, leak data, breach a policy, or
mean the PR doesn't actually satisfy its linked spec's acceptance criteria. Style, naming,
and personal preference are Nits.

## Cap the nits (org-wide)
Report at most five nits per review; summarize the rest as a count so they don't bury the
Important findings.

## What review does NOT replace (org-wide)
A finding does not itself approve or block a PR — branch protection still requires a human
CODEOWNER approval regardless of review pass results. Review exists to make that human
approval faster and better-informed, not to replace it.

## When Claude opens a PR against its own review comments (org-wide)
If review flags something and `@claude` is tagged to fix it, the fix must not touch the
spec's acceptance criteria or delete/weaken a test to make a check pass. A fix that does
either gets flagged again, this time as Important regardless of the original finding's
severity.

## Do not report (repo-specific — fill this in for this repo, do not leave the example)
- <e.g. generated files under a specific path — customize per this repo's stack>
- Anything CI already enforces (formatting, lint — those are separate checks, not review
  findings; this line is safe to keep as-is in most repos)
