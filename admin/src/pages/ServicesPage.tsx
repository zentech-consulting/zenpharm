import { useEffect, useState, useCallback } from 'react'
import { Table, Button, Modal, Form, Input, InputNumber, Switch, Select, Typography, Space, Tag, message } from 'antd'
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons'
import type { Service } from '../api/services'
import { fetchServices, createService, updateService, deleteService } from '../api/services'

export default function ServicesPage() {
  const [services, setServices] = useState<Service[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [category, setCategory] = useState<string>()
  const [loading, setLoading] = useState(false)
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<Service | null>(null)
  const [form] = Form.useForm()

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const data = await fetchServices(page, 20, category)
      setServices(data.items)
      setTotal(data.totalCount)
    } catch {
      message.error('Failed to load services')
    } finally {
      setLoading(false)
    }
  }, [page, category])

  useEffect(() => { load() }, [load])

  const openModal = (record?: Service) => {
    setEditing(record ?? null)
    form.setFieldsValue(record ?? { name: '', description: '', category: '', price: 0, durationMinutes: 30, isActive: true })
    setModalOpen(true)
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    try {
      if (editing) {
        await updateService(editing.id, values)
        message.success('Service updated')
      } else {
        await createService(values)
        message.success('Service created')
      }
      setModalOpen(false)
      load()
    } catch {
      message.error('Failed to save service')
    }
  }

  const handleDelete = async (id: string) => {
    try {
      await deleteService(id)
      message.success('Service deleted')
      load()
    } catch {
      message.error('Failed to delete service')
    }
  }

  const categories = [...new Set(services.map(s => s.category))].filter(Boolean)

  const columns = [
    { title: 'Name', dataIndex: 'name', key: 'name' },
    { title: 'Category', dataIndex: 'category', key: 'category', render: (v: string) => <Tag>{v}</Tag> },
    { title: 'Price', dataIndex: 'price', key: 'price', render: (v: number) => `$${v.toFixed(2)}` },
    { title: 'Duration', dataIndex: 'durationMinutes', key: 'duration', render: (v: number) => `${v} min` },
    { title: 'Active', dataIndex: 'isActive', key: 'isActive', render: (v: boolean) => <Tag color={v ? 'green' : 'red'}>{v ? 'Yes' : 'No'}</Tag> },
    {
      title: 'Actions', key: 'actions', render: (_: unknown, r: Service) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openModal(r)} />
          <Button size="small" danger icon={<DeleteOutlined />} onClick={() => handleDelete(r.id)} />
        </Space>
      ),
    },
  ]

  return (
    <>
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Typography.Title level={4} style={{ margin: 0 }}>Services</Typography.Title>
        <Space>
          <Select placeholder="Filter by category" allowClear style={{ width: 180 }} onChange={setCategory}
            options={categories.map(c => ({ label: c, value: c }))} />
          <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>Add Service</Button>
        </Space>
      </Space>

      <Table columns={columns} dataSource={services} rowKey="id" loading={loading}
        pagination={{ current: page, total, pageSize: 20, onChange: setPage }} />

      <Modal title={editing ? 'Edit Service' : 'Add Service'} open={modalOpen}
        onOk={handleSave} onCancel={() => setModalOpen(false)} destroyOnClose>
        <Form form={form} layout="vertical">
          <Form.Item name="name" label="Name" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="description" label="Description"><Input.TextArea rows={3} /></Form.Item>
          <Form.Item name="category" label="Category" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="price" label="Price" rules={[{ required: true }]}><InputNumber min={0} step={0.01} style={{ width: '100%' }} prefix="$" /></Form.Item>
          <Form.Item name="durationMinutes" label="Duration (minutes)" rules={[{ required: true }]}><InputNumber min={5} step={5} style={{ width: '100%' }} /></Form.Item>
          <Form.Item name="isActive" label="Active" valuePropName="checked"><Switch /></Form.Item>
        </Form>
      </Modal>
    </>
  )
}
