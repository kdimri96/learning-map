// Small shared UI pieces.

export function Avatar({ employee, size = 40 }) {
  const initials = `${employee.firstName?.[0] ?? ''}${employee.lastName?.[0] ?? ''}`.toUpperCase();
  return employee.imageUrl ? (
    <img className="avatar" src={employee.imageUrl} alt={employee.fullName} style={{ width: size, height: size }} />
  ) : (
    <span className="avatar avatar-initials" style={{ width: size, height: size, fontSize: size * 0.38 }}>{initials}</span>
  );
}

export function Alert({ error, kind = 'error' }) {
  if (!error) return null;
  const text = typeof error === 'string' ? error : error.message;
  return <div className={`alert alert-${kind}`} role={kind === 'error' ? 'alert' : 'status'}>{text}</div>;
}

export function Field({ label, error, children, hint }) {
  return (
    <label className={`field ${error ? 'has-error' : ''}`}>
      <span className="field-label">{label}</span>
      {children}
      {hint && !error && <span className="field-hint">{hint}</span>}
      {error && <span className="field-error">{error}</span>}
    </label>
  );
}

export const formatDate = (value) =>
  value ? new Date(value).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' }) : '—';

export const formatDateTime = (value) =>
  value ? new Date(value).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' }) : '—';
