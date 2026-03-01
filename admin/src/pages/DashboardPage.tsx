import { useEffect, useState } from 'react'
import { Card, Col, Row, Statistic, Typography, Spin, message } from 'antd'
import {
  TeamOutlined,
  CalendarOutlined,
  UserOutlined,
  DollarOutlined,
} from '@ant-design/icons'
import { fetchDashboardSummary, type DashboardSummary } from '../api/reports'

const { Title } = Typography

export default function DashboardPage() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const load = async () => {
      try {
        const data = await fetchDashboardSummary()
        setSummary(data)
      } catch {
        message.error('Failed to load dashboard data')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  if (loading) return <Spin size="large" style={{ display: 'block', margin: '100px auto' }} />

  const stats = [
    { title: 'Total Clients', value: summary?.totalClients ?? 0, icon: <TeamOutlined />, colour: '#1a1a2e' },
    { title: 'Total Bookings', value: summary?.totalBookings ?? 0, icon: <CalendarOutlined />, colour: '#0f3460' },
    { title: 'Active Staff', value: summary?.totalEmployees ?? 0, icon: <UserOutlined />, colour: '#16213e' },
    { title: 'Revenue', value: summary?.revenue ?? 0, icon: <DollarOutlined />, colour: '#e94560', prefix: '$' },
  ]

  return (
    <>
      <Title level={4}>Dashboard</Title>
      <Row gutter={[16, 16]}>
        {stats.map((stat) => (
          <Col xs={24} sm={12} lg={6} key={stat.title}>
            <Card>
              <Statistic
                title={stat.title}
                value={stat.value}
                prefix={stat.icon}
                valueStyle={{ color: stat.colour }}
                precision={stat.prefix === '$' ? 2 : 0}
              />
            </Card>
          </Col>
        ))}
      </Row>
    </>
  )
}
