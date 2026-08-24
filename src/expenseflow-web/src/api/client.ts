export type ClaimStatus = 'Pending' | 'Approved' | 'Rejected';
export type DecisionOutcome = 'Approved' | 'Rejected';

export interface Decision {
  decidedBy: string;
  decision: DecisionOutcome;
  comment: string | null;
  decidedAt: string;
}

export interface Claim {
  id: string;
  employeeId: string;
  amount: number;
  currency: string;
  category: string;
  expenseDate: string;
  description: string;
  receiptUrl: string | null;
  status: ClaimStatus;
  submittedAt: string;
  decision: Decision | null;
}

export interface CurrentUser {
  id: string;
  name: string;
  email: string;
  role: 'Employee' | 'Manager';
}

export interface NewClaimInput {
  amount: number;
  currency: string;
  category: string;
  expenseDate: string;
  description: string;
  receiptUrl?: string | null;
}

export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
  });

  if (!response.ok) {
    let message = response.statusText;
    try {
      const body = await response.json();
      message = body.title ?? body.message ?? message;
    } catch {
      // response had no JSON body
    }
    throw new ApiError(response.status, message);
  }

  if (response.status === 204 || response.headers.get('content-length') === '0') {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function login(email: string, password: string): Promise<void> {
  return request('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) });
}

export function logout(): Promise<void> {
  return request('/api/auth/logout', { method: 'POST' });
}

export function getCurrentUser(): Promise<CurrentUser> {
  return request('/api/auth/me');
}

export function createClaim(input: NewClaimInput): Promise<Claim> {
  return request('/api/claims', { method: 'POST', body: JSON.stringify(input) });
}

export function getMyClaims(): Promise<Claim[]> {
  return request('/api/claims/mine');
}

export function getPendingClaims(): Promise<Claim[]> {
  return request('/api/claims/pending');
}

export function getClaim(id: string): Promise<Claim> {
  return request(`/api/claims/${id}`);
}

export function decideClaim(id: string, decision: DecisionOutcome, comment?: string): Promise<Claim> {
  return request(`/api/claims/${id}/decision`, {
    method: 'POST',
    body: JSON.stringify({ decision, comment }),
  });
}
