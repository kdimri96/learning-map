import { useEffect, useState } from 'react';
import { Link, NavLink, Navigate, Route, Routes } from 'react-router-dom';
import { api } from './api.js';
import { RequireAuth, useAuth } from './auth.jsx';
import LoginPage from './pages/LoginPage.jsx';
import RegisterPage from './pages/RegisterPage.jsx';
import EmployeesPage from './pages/EmployeesPage.jsx';
import EmployeeDetailPage from './pages/EmployeeDetailPage.jsx';
import EmployeeFormPage from './pages/EmployeeFormPage.jsx';
import MePage from './pages/MePage.jsx';

export default function App() {
  const { user, canEdit, logout } = useAuth();
  const [apiUp, setApiUp] = useState(null);

  useEffect(() => {
    const check = () => api.health().then(setApiUp);
    check();
    const id = setInterval(check, 15000);
    return () => clearInterval(id);
  }, []);

  return (
    <div className="app">
      <header className="topbar">
        <Link to="/" className="brand">Learning Map</Link>
        {user && (
          <nav>
            <NavLink to="/employees" end>Employees</NavLink>
            {canEdit && <NavLink to="/employees/new">Add employee</NavLink>}
            <NavLink to="/me">My token</NavLink>
          </nav>
        )}
        <div className="topbar-right">
          <span className={`health ${apiUp ? 'up' : apiUp === false ? 'down' : ''}`} title="GET /health">
            API {apiUp ? 'online' : apiUp === false ? 'offline' : '…'}
          </span>
          {user && (
            <>
              <span className="who">{user.fullName} <span className={`badge ${user.role === 'Admin' ? 'badge-admin' : ''}`}>{user.role}</span></span>
              <button className="btn btn-ghost" onClick={logout}>Log out</button>
            </>
          )}
        </div>
      </header>

      <main className="content">
        <Routes>
          <Route path="/" element={<Navigate to={user ? '/employees' : '/login'} replace />} />
          <Route path="/login" element={user ? <Navigate to="/employees" replace /> : <LoginPage />} />
          <Route path="/register" element={user ? <Navigate to="/employees" replace /> : <RegisterPage />} />
          <Route path="/employees" element={<RequireAuth><EmployeesPage /></RequireAuth>} />
          <Route path="/employees/new" element={<RequireAuth><EmployeeFormPage /></RequireAuth>} />
          <Route path="/employees/:guid" element={<RequireAuth><EmployeeDetailPage /></RequireAuth>} />
          <Route path="/employees/:guid/edit" element={<RequireAuth><EmployeeFormPage /></RequireAuth>} />
          <Route path="/me" element={<RequireAuth><MePage /></RequireAuth>} />
          <Route path="*" element={<p>Page not found. <Link to="/">Go home</Link></p>} />
        </Routes>
      </main>
    </div>
  );
}
