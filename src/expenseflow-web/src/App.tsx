import type { ReactNode } from 'react';
import { Link, Navigate, Route, Routes } from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import { Login } from './pages/Login';
import { MyClaims } from './pages/MyClaims';
import { NewClaim } from './pages/NewClaim';
import { Approvals } from './pages/Approvals';

function RequireAuth({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth();

  if (loading) {
    return <p>Loading…</p>;
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}

function Layout({ children }: { children: ReactNode }) {
  const { user, logout } = useAuth();

  return (
    <div>
      <nav>
        <Link to="/claims">My claims</Link>
        {user?.role === 'Manager' && <Link to="/approvals">Approvals</Link>}
        {user && (
          <button type="button" onClick={() => logout()}>
            Log out
          </button>
        )}
      </nav>
      {children}
    </div>
  );
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route
        path="/claims"
        element={
          <RequireAuth>
            <Layout>
              <MyClaims />
            </Layout>
          </RequireAuth>
        }
      />
      <Route
        path="/claims/new"
        element={
          <RequireAuth>
            <Layout>
              <NewClaim />
            </Layout>
          </RequireAuth>
        }
      />
      <Route
        path="/approvals"
        element={
          <RequireAuth>
            <Layout>
              <Approvals />
            </Layout>
          </RequireAuth>
        }
      />
      <Route path="*" element={<Navigate to="/claims" replace />} />
    </Routes>
  );
}
