import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api, fieldErrors } from '../api.js';
import { useAuth } from '../auth.jsx';
import { Alert, Avatar, formatDate, formatDateTime } from '../components.jsx';

export default function EmployeeDetailPage() {
  const { guid } = useParams();
  const { canEdit } = useAuth(); // every logged-in user can add, edit and delete
  const navigate = useNavigate();
  const fileInput = useRef(null);
  const [employee, setEmployee] = useState(null);
  const [error, setError] = useState(null);
  const [notice, setNotice] = useState(null);
  const [busy, setBusy] = useState(false);
  const [pickedFile, setPickedFile] = useState(null);
  const [preview, setPreview] = useState(null);

  useEffect(() => {
    api.getEmployee(guid).then(setEmployee).catch(setError);
  }, [guid]);

  // Local preview of the picked file before uploading it.
  useEffect(() => {
    if (!pickedFile) { setPreview(null); return undefined; }
    const url = URL.createObjectURL(pickedFile);
    setPreview(url);
    return () => URL.revokeObjectURL(url);
  }, [pickedFile]);

  async function run(action, successText) {
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      const updated = await action();
      if (updated) setEmployee(updated);
      setNotice(successText);
      return true;
    } catch (err) {
      setError(err);
      return false;
    } finally {
      setBusy(false);
    }
  }

  async function upload() {
    const ok = await run(() => api.uploadImage(guid, pickedFile), 'Image uploaded.');
    if (ok) {
      setPickedFile(null);
      if (fileInput.current) fileInput.current.value = '';
    }
  }

  async function onDelete() {
    if (!window.confirm(`Delete ${employee.fullName} (${employee.employeeCode})?`)) return;
    const ok = await run(() => api.deleteEmployee(guid), 'Deleted.');
    if (ok) navigate('/employees', { replace: true });
  }

  if (!employee) {
    return error ? <><Alert error={error} /><Link to="/employees">← Back to employees</Link></> : <p className="muted">Loading…</p>;
  }

  const imageError = fieldErrors(error).image;

  return (
    <div className="stack-lg">
      <Link to="/employees" className="muted small">← Back to employees</Link>

      <div className="page-head">
        <div className="person person-lg">
          <Avatar employee={employee} size={64} />
          <div>
            <h1>{employee.fullName}</h1>
            <p className="muted"><code>{employee.employeeCode}</code> · {employee.designation}, {employee.department}</p>
          </div>
        </div>
        {canEdit && (
          <div className="actions">
            <Link className="btn" to={`/employees/${guid}/edit`}>Edit details</Link>
            <button className="btn btn-danger" onClick={onDelete} disabled={busy}>Delete</button>
          </div>
        )}
      </div>

      <Alert error={notice} kind="success" />
      <Alert error={error && !imageError ? error : null} />

      <div className="detail-grid">
        <section className="card">
          <h2>Details</h2>
          <dl className="details">
            <dt>Employee ID</dt><dd><code>{employee.employeeCode}</code></dd>
            <dt>First name</dt><dd>{employee.firstName}</dd>
            <dt>Last name</dt><dd>{employee.lastName}</dd>
            <dt>Email</dt><dd>{employee.email}</dd>
            <dt>Phone</dt><dd>{employee.phoneNumber || '—'}</dd>
            <dt>Department</dt><dd>{employee.department}</dd>
            <dt>Designation</dt><dd>{employee.designation}</dd>
            <dt>Date of joining</dt><dd>{formatDate(employee.dateOfJoining)}</dd>
            <dt>Created</dt><dd>{formatDateTime(employee.createdDate)}</dd>
            <dt>Last modified</dt><dd>{formatDateTime(employee.modifiedDate)}</dd>
            <dt>System GUID</dt><dd><code className="small">{employee.employeeGuid}</code></dd>
          </dl>
        </section>

        <section className="card">
          <h2>Profile image</h2>
          <div className="image-box">
            {preview ? (
              <img src={preview} alt="Preview of new upload" />
            ) : employee.imageUrl ? (
              <a href={employee.imageUrl} target="_blank" rel="noreferrer" title="Open full image (temporary SAS link)">
                <img src={employee.imageUrl} alt={employee.fullName} />
              </a>
            ) : (
              <div className="image-empty">No image uploaded</div>
            )}
          </div>
          <dl className="details">
            <dt>Status</dt>
            <dd>{employee.hasImage ? <span className="badge badge-ok">Uploaded</span> : <span className="badge">None</span>}</dd>
            <dt>Uploaded on</dt><dd>{formatDateTime(employee.imageUploadedDate)}</dd>
          </dl>

          {canEdit && (
            <div className="stack">
              <input
                ref={fileInput}
                type="file"
                accept="image/png,image/jpeg,image/webp"
                onChange={(e) => setPickedFile(e.target.files?.[0] ?? null)}
              />
              <span className="field-hint">JPEG, PNG or WebP, up to 2 MB.</span>
              {imageError && <span className="field-error">{imageError}</span>}
              <div className="actions">
                <button className="btn btn-primary" onClick={upload} disabled={!pickedFile || busy}>
                  {employee.hasImage ? 'Replace image' : 'Upload image'}
                </button>
                {employee.hasImage && (
                  <button className="btn" onClick={() => run(() => api.removeImage(guid), 'Image removed.')} disabled={busy}>Remove image</button>
                )}
              </div>
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
