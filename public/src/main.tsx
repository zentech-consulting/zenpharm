import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { BrandingProvider } from './contexts/BrandingContext'
import { CartProvider } from './contexts/CartContext'
import './index.css'
import App from './App'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <BrandingProvider>
        <CartProvider>
          <App />
        </CartProvider>
      </BrandingProvider>
    </BrowserRouter>
  </StrictMode>,
)
