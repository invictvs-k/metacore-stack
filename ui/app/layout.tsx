import Link from 'next/link';
import './globals.css';

export const metadata = {
  title: 'Metaplataforma — Sala Viva',
  description: 'UI para inspeção da plataforma Metacore',
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="pt-BR">
      <body>
        <nav className="nav">
          <div className="nav-container">
            <Link href="/" className="nav-logo">
              Metaplataforma
            </Link>
            <ul className="nav-menu">
              <li className="nav-item">
                <Link href="/" className="nav-link">Home</Link>
              </li>
              <li className="nav-item">
                <Link href="/rooms" className="nav-link">Salas</Link>
              </li>
              <li className="nav-item">
                <Link href="/login" className="nav-link">Login</Link>
              </li>
            </ul>
          </div>
        </nav>
        <main className="main-content">
          {children}
        </main>
        <footer className="footer">
          <p>&copy; 2025 Metaplataforma — Sala Viva</p>
        </footer>
      </body>
    </html>
  )
}
