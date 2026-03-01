import { useEffect, useState, useCallback } from 'react'
import { Table, Button, Typography, Space, DatePicker, Modal, Form, message } from 'antd'
import { PlusOutlined, DeleteOutlined } from '@ant-design/icons'
import type { Schedule } from '../api/schedules'
import { fetchSchedules, generateSchedules, deleteSchedule } from '../api/schedules'
import dayjs from 'dayjs'

const { RangePicker } = DatePicker

export default function SchedulesPage() {
  const [schedules, setSchedules] = useState<Schedule[]>([])
  const [loading, setLoading] = useState(false)
  const [dateRange, setDateRange] = useState<[string?, string?]>([])
  const [genModalOpen, setGenModalOpen] = useState(false)
  const [genForm] = Form.useForm()

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const data = await fetchSchedules(dateRange[0], dateRange[1])
      setSchedules(data.items)
    } catch {
      message.error('Failed to load schedules')
    } finally {
      setLoading(false)
    }
  }, [dateRange])

  useEffect(() => { load() }, [load])

  const handleGenerate = async () => {
    const values = await genForm.validateFields()
    try {
      const result = await generateSchedules({
        startDate: values.range[0].format('YYYY-MM-DD'),
        endDate: values.range[1].format('YYYY-MM-DD'),
      })
      message.success(`Generated ${result.length} schedule entries`)
      setGenModalOpen(false)
      load()
    } catch {
      message.error('Failed to generate schedules')
    }
  }

  const handleDelete = async (id: string) => {
    try {
      await deleteSchedule(id)
      message.success('Schedule deleted')
      load()
    } catch {
      message.error('Failed to delete schedule')
    }
  }

  const columns = [
    { title: 'Employee', dataIndex: 'employeeName', key: 'employee' },
    { title: 'Date', dataIndex: 'date', key: 'date', render: (v: string) => dayjs(v).format('DD/MM/YYYY') },
    { title: 'Start', dataIndex: 'startTime', key: 'start' },
    { title: 'End', dataIndex: 'endTime', key: 'end' },
    { title: 'Location', dataIndex: 'location', key: 'location', render: (v?: string) => v ?? '—' },
    {
      title: 'Actions', key: 'actions', render: (_: unknown, r: Schedule) => (
        <Button size="small" danger icon={<DeleteOutlined />} onClick={() => handleDelete(r.id)} />
      ),
    },
  ]

  return (
    <>
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Typography.Title level={4} style={{ margin: 0 }}>Schedules</Typography.Title>
        <Space>
          <RangePicker onChange={(dates) => setDateRange([
            dates?.[0]?.format('YYYY-MM-DD'),
            dates?.[1]?.format('YYYY-MM-DD'),
          ])} />
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setGenModalOpen(true)}>Generate</Button>
        </Space>
      </Space>

      <Table columns={columns} dataSource={schedules} rowKey="id" loading={loading} />

      <Modal title="Generate Schedules" open={genModalOpen}
        onOk={handleGenerate} onCancel={() => setGenModalOpen(false)} destroyOnClose>
        <Form form={genForm} layout="vertical">
          <Form.Item name="range" label="Date Range" rules={[{ required: true }]}>
            <RangePicker style={{ width: '100%' }} />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
