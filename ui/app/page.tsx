import Link from 'next/link';

export default function HomePage() {
  return (
    <div className="container">
      <h1 style={{ fontSize: '2.5rem', marginBottom: '1rem' }}>Welcome to the Meta-Platform</h1>
      <p style={{ fontSize: '1.25rem', color: '#666', marginBottom: '2rem' }}>
        Living Room Platform for real-time collaboration
      </p>

      <div className="card">
        <h2 className="card-header">Key Features</h2>
        <div className="card-content">
          <ul style={{ lineHeight: '2', paddingLeft: '1.5rem' }}>
            <li>Collaboration Room Management</li>
            <li>Secure Authentication System</li>
            <li>Artifacts and Data Sharing</li>
            <li>Real-time Integration via SignalR</li>
          </ul>
        </div>
      </div>

      <div style={{ display: 'flex', gap: '1rem', marginTop: '2rem' }}>
        <Link href="/rooms" className="btn btn-primary">
          View Rooms
        </Link>
        <Link href="/login" className="btn btn-secondary">
          Login
        </Link>
      </div>

      <div className="alert alert-info" style={{ marginTop: '2rem' }}>
        <strong>Status:</strong> System operational. Backend available at{' '}
        <code>http://localhost:5000</code>
      </div>
    </div>
  );
}
