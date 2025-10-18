'use client';

import { useState, FormEvent } from 'react';
import { useRouter } from 'next/navigation';

export default function LoginPage() {
  const router = useRouter();
  const [isLogin, setIsLogin] = useState(true);
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    name: '',
  });
  const [errors, setErrors] = useState({
    email: '',
    password: '',
    name: '',
  });
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const validateEmail = (email: string): boolean => {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
  };

  const validatePassword = (password: string): boolean => {
    return password.length >= 6;
  };

  const validateForm = (): boolean => {
    const newErrors = {
      email: '',
      password: '',
      name: '',
    };

    if (!formData.email) {
      newErrors.email = 'Email é obrigatório';
    } else if (!validateEmail(formData.email)) {
      newErrors.email = 'Email inválido';
    }

    if (!formData.password) {
      newErrors.password = 'Senha é obrigatória';
    } else if (!validatePassword(formData.password)) {
      newErrors.password = 'Senha deve ter no mínimo 6 caracteres';
    }

    if (!isLogin && !formData.name) {
      newErrors.name = 'Nome é obrigatório';
    }

    setErrors(newErrors);
    return !Object.values(newErrors).some((error) => error !== '');
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setMessage(null);

    if (!validateForm()) {
      return;
    }

    setLoading(true);

    try {
      const endpoint = isLogin ? '/api/auth/login' : '/api/auth/register';
      const response = await fetch(`http://localhost:5000${endpoint}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(formData),
      });

      if (response.ok) {
        const data = await response.json();
        setMessage({ type: 'success', text: isLogin ? 'Login realizado com sucesso!' : 'Cadastro realizado com sucesso!' });
        
        // Store token or session info
        if (data.token) {
          localStorage.setItem('auth_token', data.token);
        }

        // Redirect after successful login
        setTimeout(() => {
          router.push('/rooms');
        }, 1500);
      } else {
        const error = await response.json();
        setMessage({ type: 'error', text: error.message || 'Erro ao processar requisição' });
      }
    } catch (err) {
      console.log('Backend not available, simulating authentication:', err);
      
      // Simulate successful authentication when backend is not available
      if (isLogin) {
        // Validate credentials (mock)
        if (formData.email === 'demo@example.com' && formData.password === 'demo123') {
          setMessage({ type: 'success', text: 'Login realizado com sucesso!' });
          localStorage.setItem('auth_token', 'mock-token-123');
          setTimeout(() => {
            router.push('/rooms');
          }, 1500);
        } else {
          setMessage({ type: 'error', text: 'Credenciais inválidas. Use: demo@example.com / demo123' });
        }
      } else {
        // Simulate registration
        setMessage({ type: 'success', text: 'Cadastro realizado com sucesso!' });
        setTimeout(() => {
          setIsLogin(true);
          setMessage(null);
        }, 1500);
      }
    } finally {
      setLoading(false);
    }
  };

  const handleInputChange = (field: string, value: string) => {
    setFormData({ ...formData, [field]: value });
    // Clear error for this field
    setErrors({ ...errors, [field]: '' });
  };

  return (
    <div className="container" style={{ maxWidth: '500px' }}>
      <div className="card">
        <h1 style={{ marginBottom: '1.5rem' }}>
          {isLogin ? 'Login' : 'Cadastro'}
        </h1>

        {message && (
          <div className={`alert alert-${message.type === 'success' ? 'success' : 'error'}`}>
            {message.text}
          </div>
        )}

        <form onSubmit={handleSubmit} className="form">
          {!isLogin && (
            <div className="form-group">
              <label className="form-label">Nome</label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => handleInputChange('name', e.target.value)}
                className="form-input"
                placeholder="Seu nome completo"
                disabled={loading}
              />
              {errors.name && <span className="form-error">{errors.name}</span>}
            </div>
          )}

          <div className="form-group">
            <label className="form-label">Email</label>
            <input
              type="email"
              value={formData.email}
              onChange={(e) => handleInputChange('email', e.target.value)}
              className="form-input"
              placeholder="seu@email.com"
              disabled={loading}
            />
            {errors.email && <span className="form-error">{errors.email}</span>}
          </div>

          <div className="form-group">
            <label className="form-label">Senha</label>
            <input
              type="password"
              value={formData.password}
              onChange={(e) => handleInputChange('password', e.target.value)}
              className="form-input"
              placeholder="••••••••"
              disabled={loading}
            />
            {errors.password && <span className="form-error">{errors.password}</span>}
          </div>

          <button 
            type="submit" 
            className="btn btn-primary" 
            disabled={loading}
            style={{ width: '100%' }}
          >
            {loading ? 'Processando...' : (isLogin ? 'Entrar' : 'Cadastrar')}
          </button>
        </form>

        <div style={{ marginTop: '1.5rem', textAlign: 'center' }}>
          <button
            onClick={() => {
              setIsLogin(!isLogin);
              setMessage(null);
              setErrors({ email: '', password: '', name: '' });
            }}
            style={{ background: 'none', border: 'none', color: '#0070f3', cursor: 'pointer', textDecoration: 'underline' }}
          >
            {isLogin ? 'Não tem conta? Cadastre-se' : 'Já tem conta? Faça login'}
          </button>
        </div>

        {isLogin && (
          <div className="alert alert-info" style={{ marginTop: '1.5rem' }}>
            <strong>Demo:</strong> Use <code>demo@example.com</code> / <code>demo123</code> quando o backend não estiver disponível
          </div>
        )}
      </div>

      <div className="alert alert-info" style={{ marginTop: '1rem' }}>
        <strong>Integração Backend:</strong> Esta página tenta autenticar via{' '}
        <code>POST /api/auth/login</code> ou <code>POST /api/auth/register</code>. 
        Quando o backend não está disponível, uma autenticação simulada é utilizada.
      </div>

      <div className="card" style={{ marginTop: '1rem' }}>
        <h3 className="card-header">Casos de Teste</h3>
        <div className="card-content">
          <p><strong>Dados Válidos (modo demo):</strong></p>
          <ul style={{ lineHeight: '1.8', paddingLeft: '1.5rem' }}>
            <li>Email: demo@example.com</li>
            <li>Senha: demo123</li>
          </ul>
          <p style={{ marginTop: '1rem' }}><strong>Dados Inválidos:</strong></p>
          <ul style={{ lineHeight: '1.8', paddingLeft: '1.5rem' }}>
            <li>Email sem @</li>
            <li>Senha com menos de 6 caracteres</li>
            <li>Campos vazios</li>
          </ul>
        </div>
      </div>
    </div>
  );
}
