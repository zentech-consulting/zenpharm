import { Routes, Route, Navigate } from 'react-router-dom'
import { ConfigProvider } from 'antd'
import AdminLayout from './components/AdminLayout'
import RequireAuth from './components/RequireAuth'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import ClientsPage from './pages/ClientsPage'
import ServicesPage from './pages/ServicesPage'
import BookingsPage from './pages/BookingsPage'
import SchedulesPage from './pages/SchedulesPage'
import EmployeesPage from './pages/EmployeesPage'
import KnowledgePage from './pages/KnowledgePage'
import ProductsPage from './pages/ProductsPage'
import SessionsPage from './pages/SessionsPage'
import ReportsPage from './pages/ReportsPage'
import OrdersPage from './pages/OrdersPage'
import { useBranding } from './contexts/BrandingContext'

export default function App() {
  const { branding } = useBranding()

  const theme = {
    token: {
      colorPrimary: branding.primaryColour,
      borderRadius: 8,
    },
  }

  return (
    <ConfigProvider theme={theme}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route
          element={
            <RequireAuth>
              <AdminLayout />
            </RequireAuth>
          }
        >
          <Route path="/" element={<DashboardPage />} />
          <Route path="/clients" element={<ClientsPage />} />
          <Route path="/products" element={<ProductsPage />} />
          <Route path="/services" element={<ServicesPage />} />
          <Route path="/bookings" element={<BookingsPage />} />
          <Route path="/schedules" element={<SchedulesPage />} />
          <Route path="/employees" element={<EmployeesPage />} />
          <Route path="/knowledge" element={<KnowledgePage />} />
          <Route path="/reports" element={<ReportsPage />} />
          <Route path="/orders" element={<OrdersPage />} />
          <Route path="/sessions" element={<SessionsPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </ConfigProvider>
  )
}
