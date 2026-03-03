import { Link, Outlet, useLocation } from 'react-router-dom'
import { useEffect } from 'react'
import { ShoppingCart } from 'lucide-react'
import AiConsultant from '../features/ai-chat/AiConsultant'
import { useCart } from '../contexts/CartContext'

const navLinks = [
  { to: '/', label: 'Home' },
  { to: '/shop', label: 'Shop' },
  { to: '/services', label: 'Services' },
  { to: '/pricing', label: 'Pricing' },
  { to: '/about', label: 'About' },
  { to: '/contact', label: 'Contact' },
]

export default function Layout() {
  const { pathname } = useLocation()
  const { itemCount } = useCart()

  useEffect(() => {
    window.scrollTo(0, 0)
  }, [pathname])

  return (
    <div className="flex min-h-screen flex-col">
      <header className="border-b border-surface-dark bg-white">
        <nav className="mx-auto flex max-w-7xl items-center justify-between px-6 py-4">
          <Link to="/" className="text-xl font-bold text-primary">
            ZenPharm
          </Link>
          <div className="flex items-center gap-6">
            <ul className="flex gap-6">
              {navLinks.map((link) => (
                <li key={link.to}>
                  <Link
                    to={link.to}
                    className={`text-sm font-medium transition-colors hover:text-highlight ${
                      pathname === link.to || (link.to === '/shop' && pathname.startsWith('/shop'))
                        ? 'text-highlight'
                        : 'text-primary'
                    }`}
                  >
                    {link.label}
                  </Link>
                </li>
              ))}
            </ul>
            <Link to="/cart" className="relative text-primary transition-colors hover:text-highlight">
              <ShoppingCart size={22} />
              {itemCount > 0 && (
                <span className="absolute -right-2 -top-2 flex h-5 w-5 items-center justify-center rounded-full bg-highlight text-[10px] font-bold text-white">
                  {itemCount > 99 ? '99+' : itemCount}
                </span>
              )}
            </Link>
          </div>
        </nav>
      </header>

      <main className="flex-1">
        <Outlet />
      </main>

      <footer className="border-t border-surface-dark bg-primary py-8 text-white">
        <div className="mx-auto max-w-7xl px-6 text-center text-sm opacity-70">
          &copy; {new Date().getFullYear()} ZenPharm. All rights reserved.
        </div>
      </footer>

      <AiConsultant />
    </div>
  )
}
