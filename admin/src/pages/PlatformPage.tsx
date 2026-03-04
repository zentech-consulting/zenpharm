import { useState, useEffect } from 'react'
import { Tabs, Table, Button, Card, Statistic, Row, Col, Tag, message, Spin } from 'antd'
import { SyncOutlined, CheckCircleOutlined, CloseCircleOutlined } from '@ant-design/icons'
import {
  getTenants,
  getPendingSignups,
  runPbsSync,
  getPbsSummary,
} from '../api/platform'
import type { TenantSummary, PendingSignup, PbsSummary } from '../api/platform'

function TenantsTab() {
  const [tenants, setTenants] = useState<TenantSummary[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    getTenants()
      .then(setTenants)
      .catch(() => message.error('Failed to load tenants'))
      .finally(() => setLoading(false))
  }, [])

  const columns = [
    { title: 'Name', dataIndex: 'name', key: 'name' },
    { title: 'Subdomain', dataIndex: 'subdomain', key: 'subdomain' },
    {
      title: 'Status',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (active: boolean) =>
        active ? (
          <Tag icon={<CheckCircleOutlined />} color="success">Active</Tag>
        ) : (
          <Tag icon={<CloseCircleOutlined />} color="error">Inactive</Tag>
        ),
    },
    {
      title: 'Created',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (val: string) => new Date(val).toLocaleDateString(),
    },
  ]

  return <Table dataSource={tenants} columns={columns} rowKey="id" loading={loading} pagination={false} />
}

function PendingSignupsTab() {
  const [signups, setSignups] = useState<PendingSignup[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    getPendingSignups()
      .then(setSignups)
      .catch(() => message.error('Failed to load pending signups'))
      .finally(() => setLoading(false))
  }, [])

  const statusColour: Record<string, string> = {
    pending_payment: 'orange',
    provisioning: 'blue',
    active: 'green',
    failed: 'red',
    expired: 'default',
  }

  const columns = [
    { title: 'Pharmacy', dataIndex: 'pharmacyName', key: 'pharmacyName' },
    { title: 'Subdomain', dataIndex: 'subdomain', key: 'subdomain' },
    { title: 'Email', dataIndex: 'adminEmail', key: 'adminEmail' },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => <Tag color={statusColour[status] ?? 'default'}>{status}</Tag>,
    },
    {
      title: 'Created',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (val: string) => new Date(val).toLocaleDateString(),
    },
  ]

  return <Table dataSource={signups} columns={columns} rowKey="id" loading={loading} pagination={false} />
}

function PbsSyncTab() {
  const [summary, setSummary] = useState<PbsSummary | null>(null)
  const [syncing, setSyncing] = useState(false)
  const [loading, setLoading] = useState(true)

  const loadSummary = () => {
    setLoading(true)
    getPbsSummary()
      .then(setSummary)
      .catch(() => message.error('Failed to load PBS summary'))
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    loadSummary()
  }, [])

  const handleSync = async () => {
    setSyncing(true)
    try {
      const result = await runPbsSync()
      message.success(`PBS sync complete: ${result.matched} matched, ${result.updated} updated`)
      loadSummary()
    } catch {
      message.error('PBS sync failed')
    } finally {
      setSyncing(false)
    }
  }

  if (loading && !summary) return <Spin />

  return (
    <div>
      <Row gutter={16} style={{ marginBottom: 24 }}>
        <Col span={8}>
          <Card>
            <Statistic title="Total Products" value={summary?.totalProducts ?? 0} />
          </Card>
        </Col>
        <Col span={8}>
          <Card>
            <Statistic
              title="With PBS Code"
              value={summary?.withPbsCode ?? 0}
              valueStyle={{ color: '#3f8600' }}
            />
          </Card>
        </Col>
        <Col span={8}>
          <Card>
            <Statistic
              title="Without PBS Code"
              value={summary?.withoutPbsCode ?? 0}
              valueStyle={{ color: '#999' }}
            />
          </Card>
        </Col>
      </Row>
      <Button
        type="primary"
        icon={<SyncOutlined />}
        loading={syncing}
        onClick={handleSync}
        size="large"
      >
        Run PBS Sync
      </Button>
      <p style={{ marginTop: 12, color: '#666', fontSize: 13 }}>
        Matches active ingredient names to PBS item codes from the static mapping database.
      </p>
    </div>
  )
}

export default function PlatformPage() {
  const items = [
    { key: 'tenants', label: 'Tenants', children: <TenantsTab /> },
    { key: 'pending', label: 'Pending Signups', children: <PendingSignupsTab /> },
    { key: 'pbs', label: 'PBS Sync', children: <PbsSyncTab /> },
  ]

  return (
    <div>
      <h2 style={{ marginBottom: 16 }}>Platform Management</h2>
      <Tabs items={items} />
    </div>
  )
}
