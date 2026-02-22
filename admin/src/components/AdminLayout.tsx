import { useState } from 'react'
import { Outlet, useNavigate, useLocation } from 'react-router-dom'
import { logout } from '../api/auth'
import { Layout, Menu, Button, theme } from 'antd'
import {
  DashboardOutlined,
  TeamOutlined,
  ShopOutlined,
  CalendarOutlined,
  ScheduleOutlined,
  UserOutlined,
  RobotOutlined,
  BookOutlined,
  BarChartOutlined,
  LogoutOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
} from '@ant-design/icons'

const { Header, Sider, Content } = Layout

const menuItems = [
  { key: '/', icon: <DashboardOutlined />, label: 'Dashboard' },
  { key: '/clients', icon: <TeamOutlined />, label: 'Clients' },
  { key: '/services', icon: <ShopOutlined />, label: 'Services' },
  { key: '/bookings', icon: <CalendarOutlined />, label: 'Bookings' },
  { key: '/schedules', icon: <ScheduleOutlined />, label: 'Schedules' },
  { key: '/employees', icon: <UserOutlined />, label: 'Employees' },
  { key: '/ai-chat', icon: <RobotOutlined />, label: 'AI Chat' },
  { key: '/knowledge', icon: <BookOutlined />, label: 'Knowledge' },
  { key: '/reports', icon: <BarChartOutlined />, label: 'Reports' },
]

export default function AdminLayout() {
  const [collapsed, setCollapsed] = useState(false)
  const navigate = useNavigate()
  const location = useLocation()
  const { token } = theme.useToken()

  const handleLogout = async () => {
    try {
      await logout()
    } catch {
      // Continue with client-side cleanup even if server call fails
    }
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    navigate('/login')
  }

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider trigger={null} collapsible collapsed={collapsed} theme="dark">
        <div
          style={{
            height: 64,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'white',
            fontWeight: 700,
            fontSize: collapsed ? 16 : 18,
          }}
        >
          {collapsed ? 'ZB' : 'Zentech Biz'}
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[location.pathname]}
          items={menuItems}
          onClick={({ key }) => navigate(key)}
        />
      </Sider>
      <Layout>
        <Header
          style={{
            padding: '0 24px',
            background: token.colorBgContainer,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
          }}
        >
          <Button
            type="text"
            icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
            onClick={() => setCollapsed((prev) => !prev)}
          />
          <Button
            type="text"
            icon={<LogoutOutlined />}
            onClick={handleLogout}
          >
            Logout
          </Button>
        </Header>
        <Content style={{ margin: 24, padding: 24, background: token.colorBgContainer, borderRadius: 8 }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  )
}
