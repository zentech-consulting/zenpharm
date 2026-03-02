import { useEffect, useState, useCallback } from 'react'
import { Table, Button, Typography, Space, Tag, DatePicker, Popconfirm, message } from 'antd'
import { DeleteOutlined, BellOutlined } from '@ant-design/icons'
import type { Booking } from '../api/bookings'
import { fetchBookings, cancelBooking } from '../api/bookings'
import { sendBookingReminder } from '../api/notifications'
import dayjs from 'dayjs'

const statusColours: Record<string, string> = {
  pending: 'orange',
  confirmed: 'blue',
  cancelled: 'red',
  completed: 'green',
  no_show: 'default',
}

export default function BookingsPage() {
  const [bookings, setBookings] = useState<Booking[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [dateFilter, setDateFilter] = useState<string>()
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const data = await fetchBookings(page, 20, dateFilter)
      setBookings(data.items)
      setTotal(data.totalCount)
    } catch {
      message.error('Failed to load bookings')
    } finally {
      setLoading(false)
    }
  }, [page, dateFilter])

  useEffect(() => { load() }, [load])

  const handleCancel = async (id: string) => {
    try {
      await cancelBooking(id)
      message.success('Booking cancelled')
      load()
    } catch {
      message.error('Failed to cancel booking')
    }
  }

  const handleSendReminder = async (id: string) => {
    try {
      const result = await sendBookingReminder(id)
      if (result.success) {
        message.success('Reminder SMS sent')
      } else {
        message.warning(result.error ?? 'Failed to send reminder')
      }
    } catch {
      message.error('Failed to send reminder')
    }
  }

  const columns = [
    { title: 'Client', dataIndex: 'clientName', key: 'client' },
    { title: 'Service', dataIndex: 'serviceName', key: 'service' },
    { title: 'Employee', dataIndex: 'employeeName', key: 'employee', render: (v?: string) => v ?? '—' },
    { title: 'Date & Time', key: 'datetime', render: (_: unknown, r: Booking) =>
      `${dayjs(r.startTime).format('DD/MM/YYYY HH:mm')} – ${dayjs(r.endTime).format('HH:mm')}` },
    { title: 'Status', dataIndex: 'status', key: 'status',
      render: (v: string) => <Tag color={statusColours[v] ?? 'default'}>{v}</Tag> },
    {
      title: 'Actions', key: 'actions', render: (_: unknown, r: Booking) => (
        <Space>
          {r.status !== 'cancelled' && r.status !== 'completed' && (
            <>
              <Button size="small" icon={<BellOutlined />} onClick={() => handleSendReminder(r.id)}>
                Remind
              </Button>
              <Popconfirm title="Cancel this booking?" onConfirm={() => handleCancel(r.id)}>
                <Button size="small" danger icon={<DeleteOutlined />}>Cancel</Button>
              </Popconfirm>
            </>
          )}
        </Space>
      ),
    },
  ]

  return (
    <>
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Typography.Title level={4} style={{ margin: 0 }}>Bookings</Typography.Title>
        <DatePicker onChange={(d) => setDateFilter(d ? d.format('YYYY-MM-DD') : undefined)}
          placeholder="Filter by date" allowClear />
      </Space>

      <Table columns={columns} dataSource={bookings} rowKey="id" loading={loading}
        pagination={{ current: page, total, pageSize: 20, onChange: setPage }} />
    </>
  )
}
