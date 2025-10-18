import Link from 'next/link';

export default function HomePage() {
  return (
    <div className="container">
      <h1 style={{ fontSize: '2.5rem', marginBottom: '1rem' }}>
        Bem-vindo à Metaplataforma
      </h1>
      <p style={{ fontSize: '1.25rem', color: '#666', marginBottom: '2rem' }}>
        Plataforma de Sala Viva para colaboração em tempo real
      </p>

      <div className="card">
        <h2 className="card-header">Funcionalidades Principais</h2>
        <div className="card-content">
          <ul style={{ lineHeight: '2', paddingLeft: '1.5rem' }}>
            <li>Gerenciamento de Salas de Colaboração</li>
            <li>Sistema de Autenticação Seguro</li>
            <li>Artefatos e Compartilhamento de Dados</li>
            <li>Integração em Tempo Real via SignalR</li>
          </ul>
        </div>
      </div>

      <div style={{ display: 'flex', gap: '1rem', marginTop: '2rem' }}>
        <Link href="/rooms" className="btn btn-primary">
          Ver Salas
        </Link>
        <Link href="/login" className="btn btn-secondary">
          Fazer Login
        </Link>
      </div>

      <div className="alert alert-info" style={{ marginTop: '2rem' }}>
        <strong>Status:</strong> Sistema operacional. Backend disponível em{' '}
        <code>http://localhost:5000</code>
      </div>
    </div>
  );
}
