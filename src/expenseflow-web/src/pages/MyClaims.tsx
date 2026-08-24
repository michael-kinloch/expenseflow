import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getMyClaims, type Claim } from '../api/client';

export function MyClaims() {
  const [claims, setClaims] = useState<Claim[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getMyClaims()
      .then(setClaims)
      .catch(() => setError('Could not load your claims.'));
  }, []);

  if (error) {
    return <p role="alert">{error}</p>;
  }

  if (claims === null) {
    return <p>Loading…</p>;
  }

  return (
    <main>
      <h1>My claims</h1>
      <p>
        <Link to="/claims/new">Submit a new claim</Link>
      </p>
      {claims.length === 0 ? (
        <p>You haven't submitted any claims yet.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Date</th>
              <th>Category</th>
              <th>Amount</th>
              <th>Status</th>
              <th>Comment</th>
            </tr>
          </thead>
          <tbody>
            {claims.map((claim) => (
              <tr key={claim.id}>
                <td>{claim.expenseDate}</td>
                <td>{claim.category}</td>
                <td>
                  {claim.amount.toFixed(2)} {claim.currency}
                </td>
                <td>{claim.status}</td>
                <td>{claim.decision?.comment ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  );
}
