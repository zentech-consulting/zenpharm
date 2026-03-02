import { useEffect, useState, useCallback } from 'react'
import { Table, Button, Typography, Space, Tag, Popconfirm, Card, Col, Row, Statistic, message } from 'antd'
import { DeleteOutlined, SyncOutlined, LaptopOutlined } from '@ant-design/icons'
import type { ActiveSession, SessionListResponse } from '../api/auth'
import { fetchActiveSessions, revokeSession } from '../api/auth'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'

dayjs.extend(relativeTime)

const { Title } = Typography

export default function SessionsPage() {
  const [data, setData] = useState<SessionListResponse | null>(null)
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await fetchActiveSessions()
      setData(result)
    } catch {
      message.error('Failed to load sessions')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  const handleRevoke = async (id: string) => {
    try {
      await revokeSession(id)
      message.success('Session revoked')
      load()
    } catch {
      message.error('Failed to revoke session')
    }
  }

  const activeCount = data?.sessions.length ?? 0
  const maxSessions = data?.maxSessions ?? 0
  const usagePercent = maxSessions > 0 ? Math.round((activeCount / maxSessions) * 100) : 0
  const usageColour = usagePercent >= 90 ? '#ee5a24' : usagePercent >= 70 ? '#f39c12' : '#27ae60'

  const columns = [
    {
      title: 'User',
      dataIndex: 'username',
      key: 'username',
      render: (v: string) => <Tag icon={<LaptopOutlined />}>{v}</Tag>,
    },
    {
      title: 'IP Address',
      dataIndex: 'createdByIp',
      key: 'ip',
      render: (v: string | null) => v ?? '—',
    },
    {
      title: 'Created',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (v: string) => dayjs(v).format('DD/MM/YYYY HH:mm'),
    },
    {
      title: 'Last Active',
      dataIndex: 'lastUsedAt',
      key: 'lastUsedAt',
      render: (v: string | null) => v ? dayjs(v).fromNow() : 'Just created',
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_: unknown, record: ActiveSession) => (
        <Popconfirm
          title="Revoke this session?"
          description="The user will be logged out."
          onConfirm={() => handleRevoke(record.id)}
        >
          <Button size="small" danger icon={<DeleteOutlined />}>
            Revoke
          </Button>
        </Popconfirm>
      ),
    },
  ]

  return (
    <>
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Active Sessions</Title>
        <Button icon={<SyncOutlined />} onClick={load} loading={loading}>
          Refresh
        </Button>
      </Space>

      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={8}>
          <Card>
            <Statistic
              title="Active Sessions"
              value={activeCount}
              suffix={`/ ${maxSessions}`}
              valueStyle={{ color: usageColour }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={8}>
          <Card>
            <Statistic
              title="Usage"
              value={usagePercent}
              suffix="%"
              valueStyle={{ color: usageColour }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={8}>
          <Card>
            <Statistic
              title="Available Slots"
              value={Math.max(0, maxSessions - activeCount)}
              valueStyle={{ color: maxSessions - activeCount > 0 ? '#27ae60' : '#ee5a24' }}
            />
          </Card>
        </Col>
      </Row>

      <Table
        columns={columns}
        dataSource={data?.sessions ?? []}
        rowKey="id"
        loading={loading}
        pagination={false}
      />
    </>
  )
}
