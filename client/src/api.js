// One place for every call to the .NET API.
// Every response is the ApiResponseData envelope: { responseStatus, errorMessage, statusCode, data }.

const TOKEN_KEY = 'lm_token';
const USER_KEY = 'lm_user';

export class ApiError extends Error {
  constructor(message, status, details) {
    super(message);
    this.status = status;
    this.details = details; // for 400s: [{ propertyName, errorMessage }]
  }
}

export const session = {
  getToken: () => safeGet(TOKEN_KEY),
  getUser: () => {
    const raw = safeGet(USER_KEY);
    try { return raw ? JSON.parse(raw) : null; } catch { return null; }
  },
  save(token, user) {
    safeSet(TOKEN_KEY, token);
    safeSet(USER_KEY, JSON.stringify(user));
  },
  clear() {
    safeRemove(TOKEN_KEY);
    safeRemove(USER_KEY);
  },
};

async function request(path, { method = 'GET', json, form } = {}) {
  const headers = {};
  const token = session.getToken();
  if (token) headers.Authorization = `Bearer ${token}`;

  let body;
  if (form) {
    body = form; // the browser sets multipart/form-data + boundary itself
  } else if (json !== undefined) {
    headers['Content-Type'] = 'application/json';
    body = JSON.stringify(json);
  }

  let res;
  try {
    res = await fetch(`/api${path}`, { method, headers, body });
  } catch {
    throw new ApiError('Cannot reach the API. Is it running on https://localhost:7080?', 0);
  }

  let payload = null;
  try { payload = await res.json(); } catch { /* empty body, e.g. 429 */ }

  if (res.status === 401 && token) {
    // Token expired or invalid: log out everywhere in the app.
    session.clear();
    window.dispatchEvent(new Event('lm:logout'));
  }

  if (!res.ok || payload?.responseStatus === false) {
    const fallback = res.status === 429 ? 'Too many attempts. Wait a minute and try again.' : `Request failed (${res.status})`;
    throw new ApiError(payload?.errorMessage || fallback, res.status, payload?.data);
  }
  return payload?.data;
}

function toQuery(params) {
  const q = new URLSearchParams();
  Object.entries(params).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') q.set(k, v);
  });
  const s = q.toString();
  return s ? `?${s}` : '';
}

export const api = {
  health: () => fetch('/api/health').then((r) => r.ok).catch(() => false),

  // ---- auth ----
  login: (email, password) => request('/auth/login', { method: 'POST', json: { email, password } }),
  register: (fullName, email, password) => request('/auth/register', { method: 'POST', json: { fullName, email, password } }),
  me: () => request('/auth/me'),

  // ---- employees ----
  listEmployees: ({ search, department, pageNumber, pageSize }) =>
    request(`/employees${toQuery({ search, department, pageNumber, pageSize })}`),
  getEmployee: (guid) => request(`/employees/${guid}`),
  createEmployee: (formData) => request('/employees', { method: 'POST', form: formData }),
  updateEmployee: (guid, formData) => request(`/employees/${guid}`, { method: 'PUT', form: formData }),
  deleteEmployee: (guid) => request(`/employees/${guid}`, { method: 'DELETE' }),
  uploadImage: (guid, file) => {
    const fd = new FormData();
    fd.append('Image', file);
    return request(`/employees/${guid}/image`, { method: 'PUT', form: fd });
  },
  removeImage: (guid) => request(`/employees/${guid}/image`, { method: 'DELETE' }),
  departmentSummary: () => request('/employees/department-summary'),
};

/** Turns [{propertyName:'Email', errorMessage:'...'}] into { Email: '...' } for showing under form fields. */
export function fieldErrors(err) {
  if (!(err instanceof ApiError) || !Array.isArray(err.details)) return {};
  return err.details.reduce((acc, d) => {
    const key = String(d.propertyName || '').split('.')[0]; // "Image.Length" -> "Image"
    if (key) acc[key.toLowerCase()] = acc[key.toLowerCase()] ? `${acc[key.toLowerCase()]} ${d.errorMessage}` : d.errorMessage;
    return acc;
  }, {});
}

// localStorage can throw (private mode, blocked storage), so wrap it.
function safeGet(k) { try { return localStorage.getItem(k); } catch { return null; } }
function safeSet(k, v) { try { localStorage.setItem(k, v); } catch { /* ignore */ } }
function safeRemove(k) { try { localStorage.removeItem(k); } catch { /* ignore */ } }
