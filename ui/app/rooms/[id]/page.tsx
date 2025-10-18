'use client';

import { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';

interface Artifact {
  name: string;
  type: string;
  version: number;
  sha256: string;
  created: string;
}

export default function RoomDetailPage() {
  const params = useParams();
  const router = useRouter();
  const roomId = params.id as string;

  const [roomName, setRoomName] = useState('');
  const [artifacts, setArtifacts] = useState<Artifact[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchRoomDetails();
  }, [roomId]);

  const fetchRoomDetails = async () => {
    try {
      setLoading(true);
      setError(null);

      // Try to fetch room details from backend
      const response = await fetch(`http://localhost:5000/rooms/${roomId}`, {
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        const data = await response.json();
        setRoomName(data.name || `Sala ${roomId}`);
      } else {
        setRoomName(`Sala ${roomId}`);
      }

      // Try to fetch artifacts
      const artifactsResponse = await fetch(
        `http://localhost:5000/rooms/${roomId}/artifacts`,
        {
          headers: {
            'Content-Type': 'application/json',
            'X-Entity-Id': 'demo-entity',
          },
        }
      );

      if (artifactsResponse.ok) {
        const data = await artifactsResponse.json();
        setArtifacts(data.items || []);
      } else {
        // Use mock data
        setArtifacts([
          {
            name: 'document.txt',
            type: 'text/plain',
            version: 1,
            sha256: 'abc123...',
            created: new Date().toISOString(),
          },
          {
            name: 'image.png',
            type: 'image/png',
            version: 2,
            sha256: 'def456...',
            created: new Date().toISOString(),
          },
        ]);
      }
    } catch (err) {
      console.log('Error fetching room details:', err);
      setRoomName(`Sala ${roomId}`);
      // Use mock data
      setArtifacts([
        {
          name: 'document.txt',
          type: 'text/plain',
          version: 1,
          sha256: 'abc123...',
          created: new Date().toISOString(),
        },
        {
          name: 'image.png',
          type: 'image/png',
          version: 2,
          sha256: 'def456...',
          created: new Date().toISOString(),
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="container">
        <div className="loading">
          <div className="spinner"></div>
        </div>
      </div>
    );
  }

  return (
    <div className="container">
      <div style={{ marginBottom: '2rem' }}>
        <Link href="/rooms" className="btn btn-secondary">
          ← Voltar para Salas
        </Link>
      </div>

      <h1>{roomName}</h1>
      <p style={{ color: '#666', marginBottom: '2rem' }}>ID: {roomId}</p>

      {error && (
        <div className="alert alert-error">
          {error}
        </div>
      )}

      <div className="alert alert-info" style={{ marginBottom: '2rem' }}>
        <strong>Integração Backend:</strong> Esta página busca artefatos do endpoint{' '}
        <code>GET /rooms/{'{roomId}'}/artifacts</code>
      </div>

      <h2 style={{ marginBottom: '1rem' }}>Artefatos</h2>

      {artifacts.length === 0 ? (
        <div className="card">
          <p>Nenhum artefato encontrado nesta sala.</p>
        </div>
      ) : (
        <div style={{ display: 'grid', gap: '1rem' }}>
          {artifacts.map((artifact, index) => (
            <div key={index} className="card">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start' }}>
                <div>
                  <h3 className="card-header">{artifact.name}</h3>
                  <div className="card-content">
                    <p><strong>Tipo:</strong> {artifact.type}</p>
                    <p><strong>Versão:</strong> {artifact.version}</p>
                    <p><strong>SHA256:</strong> {artifact.sha256.substring(0, 16)}...</p>
                    <p><strong>Criado:</strong> {new Date(artifact.created).toLocaleString('pt-BR')}</p>
                  </div>
                </div>
                <button className="btn btn-primary">
                  Download
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
