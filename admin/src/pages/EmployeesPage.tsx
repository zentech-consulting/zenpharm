import { useEffect, useState, useCallback } from 'react'
import { Table, Button, Modal, Form, Input, Select, Switch, Typography, Space, Tag, message } from 'antd'
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons'
import type { Employee } from '../api/employees'
import { fetchEmployees, createEmployee, updateEmployee, deleteEmployee } from '../api/employees'

export default function EmployeesPage() {
  const [employees, setEmployees] = useState<Employee[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [roleFilter, setRoleFilter] = useState<string>()
  const [loading, setLoading] = useState(false)
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<Employee | null>(null)
  const [form] = Form.useForm()

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const data = await fetchEmployees(page, 20, roleFilter)
      setEmployees(data.items)
      setTotal(data.totalCount)
    } catch {
      message.error('Failed to load employees')
    } finally {
      setLoading(false)
    }
  }, [page, roleFilter])

  useEffect(() => { load() }, [load])

  const openModal = (record?: Employee) => {
    setEditing(record ?? null)
    form.setFieldsValue(record ?? { firstName: '', lastName: '', email: '', phone: '', role: 'staff', isActive: true })
    setModalOpen(true)
  }

  const handleSave = async () => {
    const values = await form.validateFields()
    try {
      if (editing) {
        await updateEmployee(editing.id, values)
        message.success('Employee updated')
      } else {
        await createEmployee(values)
        message.success('Employee created')
      }
      setModalOpen(false)
      load()
    } catch {
      message.error('Failed to save employee')
    }
  }

  const handleDelete = async (id: string) => {
    try {
      await deleteEmployee(id)
      message.success('Employee deleted')
      load()
    } catch {
      message.error('Failed to delete employee')
    }
  }

  const columns = [
    { title: 'Name', key: 'name', render: (_: unknown, r: Employee) => `${r.firstName} ${r.lastName}` },
    { title: 'Email', dataIndex: 'email', key: 'email' },
    { title: 'Phone', dataIndex: 'phone', key: 'phone' },
    { title: 'Role', dataIndex: 'role', key: 'role', render: (v: string) => <Tag>{v}</Tag> },
    { title: 'Active', dataIndex: 'isActive', key: 'active',
      render: (v: boolean) => <Tag color={v ? 'green' : 'red'}>{v ? 'Yes' : 'No'}</Tag> },
    {
      title: 'Actions', key: 'actions', render: (_: unknown, r: Employee) => (
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
        <Typography.Title level={4} style={{ margin: 0 }}>Employees</Typography.Title>
        <Space>
          <Select placeholder="Filter by role" allowClear style={{ width: 150 }} onChange={setRoleFilter}
            options={[
              { label: 'Staff', value: 'staff' },
              { label: 'Manager', value: 'manager' },
              { label: 'Admin', value: 'admin' },
            ]} />
          <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal()}>Add Employee</Button>
        </Space>
      </Space>

      <Table columns={columns} dataSource={employees} rowKey="id" loading={loading}
        pagination={{ current: page, total, pageSize: 20, onChange: setPage }} />

      <Modal title={editing ? 'Edit Employee' : 'Add Employee'} open={modalOpen}
        onOk={handleSave} onCancel={() => setModalOpen(false)} destroyOnClose>
        <Form form={form} layout="vertical">
          <Form.Item name="firstName" label="First Name" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="lastName" label="Last Name" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="email" label="Email"><Input type="email" /></Form.Item>
          <Form.Item name="phone" label="Phone"><Input /></Form.Item>
          <Form.Item name="role" label="Role" rules={[{ required: true }]}>
            <Select options={[
              { label: 'Staff', value: 'staff' },
              { label: 'Manager', value: 'manager' },
              { label: 'Admin', value: 'admin' },
            ]} />
          </Form.Item>
          <Form.Item name="isActive" label="Active" valuePropName="checked"><Switch /></Form.Item>
        </Form>
      </Modal>
    </>
  )
}
