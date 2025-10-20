'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import { Room } from '@/lib/api';

export default function RoomsPage() {
  const [rooms, setRooms] = useState<Room[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [newRoomName, setNewRoomName] = useState('');

  useEffect(() => {
    fetchRooms();
  }, []);

  const fetchRooms = async () => {
    try {
      setLoading(true);
      setError(null);

      // Try to fetch from backend API
      const response = await fetch('http://localhost:5000/api/rooms', {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        const data = await response.json();
        setRooms(data.rooms || []);
      } else {
        // If backend is not available, use mock data
        console.log('Backend not available, using mock data');
        setRooms([
          {
            id: 'room-001',
            name: 'Development Room',
            created: new Date().toISOString(),
            artifactCount: 5,
          },
          {
            id: 'room-002',
            name: 'Test Room',
            created: new Date().toISOString(),
            artifactCount: 3,
          },
        ]);
      }
    } catch (err) {
      console.log('Error fetching rooms, using mock data:', err);
      // Use mock data when backend is not available
      setRooms([
        {
          id: 'room-001',
          name: 'Development Room',
          created: new Date().toISOString(),
          artifactCount: 5,
        },
        {
          id: 'room-002',
          name: 'Test Room',
          created: new Date().toISOString(),
          artifactCount: 3,
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateRoom = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newRoomName.trim()) return;

    try {
      const response = await fetch('http://localhost:5000/api/rooms', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ name: newRoomName }),
      });

      if (response.ok) {
        const data = await response.json();
        setRooms([...rooms, data.room]);
        setNewRoomName('');
        setShowCreateForm(false);
      } else {
        // Mock success when backend is not available
        const newRoom: Room = {
          id: `room-${Date.now()}`,
          name: newRoomName,
          created: new Date().toISOString(),
          artifactCount: 0,
        };
        setRooms([...rooms, newRoom]);
        setNewRoomName('');
        setShowCreateForm(false);
      }
    } catch (err) {
      // Mock success when backend is not available
      const newRoom: Room = {
        id: `room-${Date.now()}`,
        name: newRoomName,
        created: new Date().toISOString(),
        artifactCount: 0,
      };
      setRooms([...rooms, newRoom]);
      setNewRoomName('');
      setShowCreateForm(false);
    }
  };

  if (loading) {
    return (
      <div className="container">
        <h1>Rooms</h1>
        <div className="loading">
          <div className="spinner"></div>
        </div>
      </div>
    );
  }

  return (
    <div className="container">
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '2rem',
        }}
      >
        <h1>Available Rooms</h1>
        <button onClick={() => setShowCreateForm(!showCreateForm)} className="btn btn-primary">
          {showCreateForm ? 'Cancel' : 'New Room'}
        </button>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      {showCreateForm && (
        <div className="card" style={{ marginBottom: '2rem' }}>
          <h2 className="card-header">Create New Room</h2>
          <form onSubmit={handleCreateRoom} className="form">
            <div className="form-group">
              <label className="form-label">Room Name</label>
              <input
                type="text"
                value={newRoomName}
                onChange={(e) => setNewRoomName(e.target.value)}
                className="form-input"
                placeholder="Enter room name"
                required
              />
            </div>
            <div style={{ display: 'flex', gap: '1rem' }}>
              <button type="submit" className="btn btn-success">
                Create Room
              </button>
              <button
                type="button"
                onClick={() => setShowCreateForm(false)}
                className="btn btn-secondary"
              >
                Cancel
              </button>
            </div>
          </form>
        </div>
      )}

      <div className="alert alert-info" style={{ marginBottom: '1rem' }}>
        <strong>Backend Integration:</strong> This page attempts to connect to the backend at{' '}
        <code>http://localhost:5000/api/rooms</code>. When the backend is not available, sample data
        is displayed.
      </div>

      {rooms.length === 0 ? (
        <div className="card">
          <p>No rooms found. Create a new room to get started!</p>
        </div>
      ) : (
        <div className="room-list">
          {rooms.map((room) => (
            <div key={room.id} className="room-card">
              <div className="room-name">{room.name}</div>
              <div className="room-meta">
                ID: {room.id}
                {room.artifactCount !== undefined && <> • Artifacts: {room.artifactCount}</>}
                <> • Created: {new Date(room.created).toLocaleDateString('en-US')}</>
              </div>
              <div style={{ marginTop: '1rem' }}>
                <Link href={`/rooms/${room.id}`} className="btn btn-primary">
                  Access Room
                </Link>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
