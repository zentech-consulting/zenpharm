import { Card, Col, Row, Statistic, Typography } from 'antd'
import {
  TeamOutlined,
  CalendarOutlined,
  UserOutlined,
  DollarOutlined,
} from '@ant-design/icons'

const { Title } = Typography

const stats = [
  { title: 'Total Clients', value: '—', icon: <TeamOutlined />, colour: '#1a1a2e' },
  { title: 'Bookings Today', value: '—', icon: <CalendarOutlined />, colour: '#0f3460' },
  { title: 'Active Staff', value: '—', icon: <UserOutlined />, colour: '#16213e' },
  { title: 'Revenue (Month)', value: '—', icon: <DollarOutlined />, colour: '#e94560' },
]

export default function DashboardPage() {
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
              />
            </Card>
          </Col>
        ))}
      </Row>
      <Card style={{ marginTop: 24 }}>
        <Typography.Text type="secondary">
          Dashboard statistics will populate once modules are implemented.
          Connect to the API and configure your industry package to see real data.
        </Typography.Text>
      </Card>
    </>
  )
}
