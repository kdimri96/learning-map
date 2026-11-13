import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, fieldErrors } from '../api.js';
import { useAuth } from '../auth.jsx';
import { Alert, Field } from '../components.jsx';

export default function RegisterPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ fullName: '', email: '', password: '' });
  const [error, setError] = useState(null);
  const [busy, setBusy] = useState(false);
  const errs = fieldErrors(error);
  const set = (k) => (e) => setForm({ ...form, [k]: e.target.value });

  async function onSubmit(e) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await api.register(form.fullName, form.email, form.password);
      await login(form.email, form.password);
      navigate('/employees', { replace: true });
    } catch (err) {
      setError(err);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="auth-card card">
      <h1>Register</h1>
      <p className="muted">Once registered you can add, edit and delete employees.</p>
      <Alert error={error} />
      <form onSubmit={onSubmit} className="stack">
        <Field label="Full name" error={errs.fullname}>
          <input value={form.fullName} onChange={set('fullName')} required />
        </Field>
        <Field label="Email" error={errs.email}>
          <input type="email" value={form.email} onChange={set('email')} required autoComplete="username" />
        </Field>
        <Field label="Password" error={errs.password} hint="8+ characters with upper, lower, digit and symbol.">
          <input type="password" value={form.password} onChange={set('password')} required autoComplete="new-password" />
        </Field>
        <button className="btn btn-primary" disabled={busy}>{busy ? 'Creating…' : 'Create account'}</button>
      </form>
      <p className="muted small">Already registered? <Link to="/login">Log in</Link></p>
    </div>
  );
}
