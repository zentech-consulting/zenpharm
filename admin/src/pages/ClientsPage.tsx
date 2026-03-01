import { useEffect, useState, useCallback } from 'react'
import { Table, Button, Modal, Form, Input, Typography, Space, message } from 'antd'
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons'
import type { Client } from '../api/clients'
import { fetchClients, createClient, updateClient, deleteClient } from '../api/clients'

export default function ClientsPage() {
  const [clients, setClients] = useState<Client[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(false)
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<Client | null>(null)
  const [form] = Form.useForm()

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const data = await fetchClients(page, 20, search || undefined)
      setClients(data.items)
      setTotal(data.totalCount)
    } catch {
      message.error('Failed to load clients')
    } finally {
      setLoading(false)
    }
  }, [page, search])

  useEffect(() => { load() }, [load])

  const openModal = (record?: Client) => {
    setEditing(record ?? null)
    form.setFieldsValue(record ?? { firstName: '', lastName: '', email: '', phone: '', notes: '' })
    setModalOpen(true)
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    try {
      if (editing) {
        await updateClient(editing.id, values)
        message.success('Client updated')
      } else {
        await createClient(values)
        message.success('Client created')
      }
      setModalOpen(false)
      load()
    } catch {
      message.error('Failed to save client')
    }
  }

  const handleDelete = async (id: string) => {
    try {
      await deleteClient(id)
      message.success('Client deleted')
      load()
    } catch {
      message.error('Failed to delete client')
    }
  }

  const columns = [
    { title: 'Name', key: 'name', render: (_: unknown, r: Client) => `${r.firstName} ${r.lastName}` },
    { title: 'Email', dataIndex: 'email', key: 'email' },
    { title: 'Phone', dataIndex: 'phone', key: 'phone' },
    { title: 'Created', dataIndex: 'createdAt', key: 'createdAt', render: (v: string) => new Date(v).toLocaleDateString() },
    {
      title: 'Actions', key: 'actions', render: (_: unknown, r: Client) => (
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
        <Typography.Title level={4} style={{ margin: 0 }}>Clients</Typography.Title>
        <Space>
          <Input.Search placeholder="Search clients..." onSearch={setSearch} allowClear style={{ width: 250 }} />
          <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>Add Client</Button>
        </Space>
      </Space>

      <Table columns={columns} dataSource={clients} rowKey="id" loading={loading}
        pagination={{ current: page, total, pageSize: 20, onChange: setPage }} />

      <Modal title={editing ? 'Edit Client' : 'Add Client'} open={modalOpen}
        onOk={handleSave} onCancel={() => setModalOpen(false)} destroyOnClose>
        <Form form={form} layout="vertical">
          <Form.Item name="firstName" label="First Name" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="lastName" label="Last Name" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="email" label="Email"><Input type="email" /></Form.Item>
          <Form.Item name="phone" label="Phone"><Input /></Form.Item>
          <Form.Item name="notes" label="Notes"><Input.TextArea rows={3} /></Form.Item>
        </Form>
      </Modal>
    </>
  )
}
