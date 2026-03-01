import { useEffect, useState, useCallback } from 'react'
import { Table, Button, Modal, Form, Input, Select, Typography, Space, Tag, message } from 'antd'
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons'
import type { KnowledgeEntry } from '../api/knowledge'
import { fetchKnowledgeEntries, createKnowledgeEntry, updateKnowledgeEntry, deleteKnowledgeEntry } from '../api/knowledge'

export default function KnowledgePage() {
  const [entries, setEntries] = useState<KnowledgeEntry[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [category, setCategory] = useState<string>()
  const [loading, setLoading] = useState(false)
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<KnowledgeEntry | null>(null)
  const [form] = Form.useForm()

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const data = await fetchKnowledgeEntries(page, 20, category)
      setEntries(data.items)
      setTotal(data.totalCount)
    } catch {
      message.error('Failed to load knowledge entries')
    } finally {
      setLoading(false)
    }
  }, [page, category])

  useEffect(() => { load() }, [load])

  const openModal = (record?: KnowledgeEntry) => {
    setEditing(record ?? null)
    form.setFieldsValue(record
      ? { ...record, tags: record.tags?.join(', ') }
      : { title: '', content: '', category: 'general', tags: '' })
    setModalOpen(true)
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    const data = {
      ...values,
      tags: values.tags ? values.tags.split(',').map((t: string) => t.trim()).filter(Boolean) : [],
    }
    try {
      if (editing) {
        await updateKnowledgeEntry(editing.id, data)
        message.success('Entry updated')
      } else {
        await createKnowledgeEntry(data)
        message.success('Entry created')
      }
      setModalOpen(false)
      load()
    } catch {
      message.error('Failed to save entry')
    }
  }

  const handleDelete = async (id: string) => {
    try {
      await deleteKnowledgeEntry(id)
      message.success('Entry deleted')
      load()
    } catch {
      message.error('Failed to delete entry')
    }
  }

  const columns = [
    { title: 'Title', dataIndex: 'title', key: 'title' },
    { title: 'Category', dataIndex: 'category', key: 'category', render: (v: string) => <Tag>{v}</Tag> },
    { title: 'Tags', dataIndex: 'tags', key: 'tags',
      render: (tags: string[]) => tags.map(t => <Tag key={t}>{t}</Tag>) },
    { title: 'Updated', dataIndex: 'updatedAt', key: 'updated',
      render: (v: string) => new Date(v).toLocaleDateString() },
    {
      title: 'Actions', key: 'actions', render: (_: unknown, r: KnowledgeEntry) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openModal(r)} />
          <Button size="small" danger icon={<DeleteOutlined />} onClick={() => handleDelete(r.id)} />
        </Space>
      ),
    },
  ]

  const categories = [...new Set(entries.map(e => e.category))].filter(Boolean)

  return (
    <>
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Typography.Title level={4} style={{ margin: 0 }}>Knowledge Base</Typography.Title>
        <Space>
          <Select placeholder="Filter by category" allowClear style={{ width: 180 }} onChange={setCategory}
            options={categories.map(c => ({ label: c, value: c }))} />
          <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>Add Entry</Button>
        </Space>
      </Space>

      <Table columns={columns} dataSource={entries} rowKey="id" loading={loading}
        pagination={{ current: page, total, pageSize: 20, onChange: setPage }} />

      <Modal title={editing ? 'Edit Entry' : 'Add Entry'} open={modalOpen}
        onOk={handleSave} onCancel={() => setModalOpen(false)} destroyOnClose width={640}>
        <Form form={form} layout="vertical">
          <Form.Item name="title" label="Title" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="content" label="Content" rules={[{ required: true }]}><Input.TextArea rows={8} /></Form.Item>
          <Form.Item name="category" label="Category" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="tags" label="Tags (comma-separated)"><Input placeholder="faq, pricing, hours" /></Form.Item>
        </Form>
      </Modal>
    </>
  )
}
