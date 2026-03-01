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

const theme = {
  token: {
    colorPrimary: '#1a1a2e',
    borderRadius: 8,
  },
}

export default function App() {
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
          <Route path="/services" element={<ServicesPage />} />
          <Route path="/bookings" element={<BookingsPage />} />
          <Route path="/schedules" element={<SchedulesPage />} />
          <Route path="/employees" element={<EmployeesPage />} />
          <Route path="/knowledge" element={<KnowledgePage />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </ConfigProvider>
  )
}
