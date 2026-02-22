import { Routes, Route, Navigate } from 'react-router-dom'
import { ConfigProvider } from 'antd'
import AdminLayout from './components/AdminLayout'
import RequireAuth from './components/RequireAuth'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'

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
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </ConfigProvider>
  )
}
