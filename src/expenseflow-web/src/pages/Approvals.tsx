import { useEffect, useState } from 'react';
import { ApiError, decideClaim, getPendingClaims, type Claim, type DecisionOutcome } from '../api/client';

export function Approvals() {
  const [claims, setClaims] = useState<Claim[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [comments, setComments] = useState<Record<string, string>>({});
  const [busyId, setBusyId] = useState<string | null>(null);

  function load() {
    getPendingClaims()
      .then(setClaims)
      .catch(() => setError('Could not load the approval queue.'));
  }

  useEffect(load, []);

  async function handleDecision(claimId: string, decision: DecisionOutcome) {
    setBusyId(claimId);
    setError(null);
    try {
      await decideClaim(claimId, decision, comments[claimId]);
      setClaims((current) => current?.filter((c) => c.id !== claimId) ?? null);
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setError('That claim was already decided.');
        load();
      } else {
        setError('Could not record the decision.');
      }
    } finally {
      setBusyId(null);
    }
  }

  if (error) {
    return <p role="alert">{error}</p>;
  }

  if (claims === null) {
    return <p>Loading…</p>;
  }

  return (
    <main>
      <h1>Approvals</h1>
      {claims.length === 0 ? (
        <p>No claims are waiting for your decision.</p>
      ) : (
        <ul>
          {claims.map((claim) => (
            <li key={claim.id}>
              <p>
                {claim.category} — {claim.amount.toFixed(2)} {claim.currency} on {claim.expenseDate}
              </p>
              <p>{claim.description}</p>
              <label htmlFor={`comment-${claim.id}`}>Comment (optional)</label>
              <input
                id={`comment-${claim.id}`}
                type="text"
                value={comments[claim.id] ?? ''}
                onChange={(e) => setComments((c) => ({ ...c, [claim.id]: e.target.value }))}
              />
              <button type="button" disabled={busyId === claim.id} onClick={() => handleDecision(claim.id, 'Approved')}>
                Approve
              </button>
              <button type="button" disabled={busyId === claim.id} onClick={() => handleDecision(claim.id, 'Rejected')}>
                Reject
              </button>
            </li>
          ))}
        </ul>
      )}
    </main>
  );
}
