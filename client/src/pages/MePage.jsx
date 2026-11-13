import { useEffect, useState } from 'react';
import { api, session } from '../api.js';
import { Alert, formatDateTime } from '../components.jsx';
import { useAuth } from '../auth.jsx';

/** Shows GET /auth/me (what the API reads from your JWT) and the decoded token itself. */
export default function MePage() {
  const { user } = useAuth();
  const [me, setMe] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => { api.me().then(setMe).catch(setError); }, []);

  const token = session.getToken() ?? '';
  const [header, payload] = token.split('.').slice(0, 2).map(decodePart);

  return (
    <div className="stack-lg">
      <div>
        <h1>My token</h1>
        <p className="muted">A JWT has three Base64 parts: header.payload.signature. Anyone can READ it; only the server can SIGN it.</p>
      </div>
      <Alert error={error} />

      <div className="detail-grid">
        <section className="card">
          <h2>GET /auth/me</h2>
          {me && (
            <dl className="details">
              <dt>User ID (sub)</dt><dd>{me.userId}</dd>
              <dt>Name</dt><dd>{me.name}</dd>
              <dt>Email</dt><dd>{me.email}</dd>
              <dt>Role</dt><dd>{me.role}</dd>
              <dt>Expires</dt><dd>{formatDateTime(user?.expiresAt)}</dd>
            </dl>
          )}
        </section>
        <section className="card">
          <h2>Decoded token</h2>
          <pre className="code">{JSON.stringify({ header, payload }, null, 2)}</pre>
        </section>
      </div>

      <section className="card">
        <h2>Raw token</h2>
        <pre className="code wrap">{token}</pre>
      </section>
    </div>
  );
}

function decodePart(part) {
  try {
    const json = atob(part.replace(/-/g, '+').replace(/_/g, '/'));
    return JSON.parse(json);
  } catch {
    return null;
  }
}
