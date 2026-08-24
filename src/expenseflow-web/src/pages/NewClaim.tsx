import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { ApiError, createClaim } from '../api/client';

function today(): string {
  return new Date().toISOString().slice(0, 10);
}

export function NewClaim() {
  const navigate = useNavigate();
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState('GBP');
  const [category, setCategory] = useState('');
  const [expenseDate, setExpenseDate] = useState(today());
  const [description, setDescription] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await createClaim({
        amount: Number(amount),
        currency,
        category,
        expenseDate,
        description,
      });
      navigate('/claims');
    } catch (err) {
      if (err instanceof ApiError) {
        setError(
          err.status === 400
            ? 'Please check the amount is positive and the expense date is not in the future.'
            : 'Something went wrong submitting your claim.',
        );
      } else {
        setError('Something went wrong submitting your claim.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main>
      <h1>New claim</h1>
      <form onSubmit={handleSubmit}>
        <div>
          <label htmlFor="amount">Amount</label>
          <input
            id="amount"
            type="number"
            step="0.01"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            required
          />
        </div>
        <div>
          <label htmlFor="currency">Currency</label>
          <input
            id="currency"
            type="text"
            maxLength={3}
            value={currency}
            onChange={(e) => setCurrency(e.target.value.toUpperCase())}
            required
          />
        </div>
        <div>
          <label htmlFor="category">Category</label>
          <input id="category" type="text" value={category} onChange={(e) => setCategory(e.target.value)} required />
        </div>
        <div>
          <label htmlFor="expenseDate">Expense date</label>
          <input
            id="expenseDate"
            type="date"
            max={today()}
            value={expenseDate}
            onChange={(e) => setExpenseDate(e.target.value)}
            required
          />
        </div>
        <div>
          <label htmlFor="description">Description</label>
          <textarea
            id="description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            required
          />
        </div>
        {error && <p role="alert">{error}</p>}
        <button type="submit" disabled={submitting}>
          {submitting ? 'Submitting…' : 'Submit claim'}
        </button>
      </form>
    </main>
  );
}
