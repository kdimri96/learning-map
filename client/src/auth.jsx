import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { api, session } from './api.js';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => session.getUser());

  useEffect(() => {
    const onLogout = () => setUser(null);
    window.addEventListener('lm:logout', onLogout);
    return () => window.removeEventListener('lm:logout', onLogout);
  }, []);

  const login = useCallback(async (email, password) => {
    const data = await api.login(email, password); // LoginResponseDto
    const u = { fullName: data.fullName, email: data.email, role: data.role, expiresAt: data.expiresAt };
    session.save(data.token, u);
    setUser(u);
    return u;
  }, []);

  const logout = useCallback(() => {
    session.clear();
    setUser(null);
  }, []);

  const value = useMemo(() => ({ user, isAdmin: user?.role === 'Admin', canEdit: Boolean(user), login, logout }), [user, login, logout]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export const useAuth = () => useContext(AuthContext);

/** Wrap a page that needs login. Optionally require the Admin role. */
export function RequireAuth({ children, admin = false }) {
  const { user, isAdmin } = useAuth();
  const location = useLocation();
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  if (admin && !isAdmin) return <Navigate to="/employees" replace />;
  return children;
}
