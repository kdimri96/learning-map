import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api } from '../api.js';
import { useAuth } from '../auth.jsx';
import { Alert, Avatar, formatDate } from '../components.jsx';

export default function EmployeesPage() {
  const { canEdit } = useAuth(); // every logged-in user can add, edit and delete
  const navigate = useNavigate();
  const [filters, setFilters] = useState({ search: '', department: '', pageNumber: 1, pageSize: 10 });
  const [searchText, setSearchText] = useState('');
  const [result, setResult] = useState(null);
  const [summary, setSummary] = useState([]);
  const [error, setError] = useState(null);
  const [notice, setNotice] = useState(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [page, summaryRows] = await Promise.all([api.listEmployees(filters), api.departmentSummary()]);
      setResult(page);
      setSummary(summaryRows);
    } catch (err) {
      setError(err);
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => { load(); }, [load]);

  // Debounce the search box: query the API 350 ms after the user stops typing.
  useEffect(() => {
    const id = setTimeout(() => setFilters((f) => (f.search === searchText ? f : { ...f, search: searchText, pageNumber: 1 })), 350);
    return () => clearTimeout(id);
  }, [searchText]);

  async function onDelete(emp) {
    if (!window.confirm(`Delete ${emp.fullName} (${emp.employeeCode})?`)) return;
    try {
      await api.deleteEmployee(emp.employeeGuid);
      setNotice(`${emp.fullName} was deleted.`);
      load();
    } catch (err) {
      setError(err);
    }
  }

  const totalHeadcount = summary.reduce((n, d) => n + d.employeeCount, 0);

  return (
    <div className="stack-lg">
      <div className="page-head">
        <div>
          <h1>Employees</h1>
          <p className="muted">GET /employees with search and paging. Department totals come from a stored procedure via Dapper.</p>
        </div>
        {canEdit && <Link className="btn btn-primary" to="/employees/new">+ Add employee</Link>}
      </div>

      <section className="summary-grid">
        <div className="card stat">
          <span className="stat-label">Total employees</span>
          <span className="stat-value">{totalHeadcount}</span>
        </div>
        {summary.map((d) => (
          <button
            key={d.department}
            className={`card stat stat-button ${filters.department === d.department ? 'active' : ''}`}
            onClick={() => setFilters((f) => ({ ...f, department: f.department === d.department ? '' : d.department, pageNumber: 1 }))}
            title="Click to filter by this department"
          >
            <span className="stat-label">{d.department}</span>
            <span className="stat-value">{d.employeeCount}</span>
          </button>
        ))}
      </section>

      <Alert error={notice} kind="success" />
      <Alert error={error} />

      <div className="toolbar">
        <input
          className="search"
          placeholder="Search by ID (EMP-00001), name, email or designation…"
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
        />
        <select value={filters.department} onChange={(e) => setFilters({ ...filters, department: e.target.value, pageNumber: 1 })}>
          <option value="">All departments</option>
          {summary.map((d) => <option key={d.department} value={d.department}>{d.department}</option>)}
        </select>
        <select value={filters.pageSize} onChange={(e) => setFilters({ ...filters, pageSize: Number(e.target.value), pageNumber: 1 })}>
          {[5, 10, 20, 50].map((n) => <option key={n} value={n}>{n} per page</option>)}
        </select>
      </div>

      <div className="card table-wrap">
        <table>
          <thead>
            <tr>
              <th>Employee</th>
              <th>ID</th>
              <th>Department</th>
              <th>Designation</th>
              <th>Joined</th>
              <th>Image</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {loading && !result && <tr><td colSpan={7} className="muted center">Loading…</td></tr>}
            {result?.items.length === 0 && (
              <tr><td colSpan={7} className="muted center">No employees found.{canEdit && <> <Link to="/employees/new">Add one</Link>.</>}</td></tr>
            )}
            {result?.items.map((e) => (
              <tr key={e.employeeGuid} className="clickable" onClick={() => navigate(`/employees/${e.employeeGuid}`)}>
                <td>
                  <div className="person">
                    <Avatar employee={e} />
                    <div>
                      <div className="strong">{e.fullName}</div>
                      <div className="muted small">{e.email}</div>
                    </div>
                  </div>
                </td>
                <td><code>{e.employeeCode}</code></td>
                <td>{e.department}</td>
                <td>{e.designation}</td>
                <td>{formatDate(e.dateOfJoining)}</td>
                <td>{e.hasImage ? <span className="badge badge-ok">Uploaded</span> : <span className="badge">None</span>}</td>
                <td className="actions" onClick={(ev) => ev.stopPropagation()}>
                  <Link className="btn btn-small" to={`/employees/${e.employeeGuid}`}>View</Link>
                  {canEdit && <Link className="btn btn-small" to={`/employees/${e.employeeGuid}/edit`}>Edit</Link>}
                  {canEdit && <button className="btn btn-small btn-danger" onClick={() => onDelete(e)}>Delete</button>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {result && result.totalPages > 1 && (
        <div className="pager">
          <button className="btn btn-small" disabled={filters.pageNumber <= 1} onClick={() => setFilters({ ...filters, pageNumber: filters.pageNumber - 1 })}>← Prev</button>
          <span className="muted">Page {result.pageNumber} of {result.totalPages} · {result.totalCount} employees</span>
          <button className="btn btn-small" disabled={filters.pageNumber >= result.totalPages} onClick={() => setFilters({ ...filters, pageNumber: filters.pageNumber + 1 })}>Next →</button>
        </div>
      )}
    </div>
  );
}
