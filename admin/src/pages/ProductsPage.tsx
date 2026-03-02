import { useEffect, useState, useCallback } from 'react'
import {
  Table, Button, Modal, Form, Input, InputNumber, DatePicker, Switch,
  Typography, Space, Tag, Tabs, Alert, message, Checkbox, Select,
} from 'antd'
import { EditOutlined, DeleteOutlined, ImportOutlined } from '@ant-design/icons'
import type { Product } from '../api/products'
import type { MasterProduct } from '../api/masterProducts'
import { fetchProducts, updateProduct, deleteProduct, importProducts, recordStockMovement } from '../api/products'
import { fetchMasterProducts } from '../api/masterProducts'
import dayjs from 'dayjs'

const scheduleColours: Record<string, string> = {
  Unscheduled: 'default',
  S2: 'blue',
  S3: 'orange',
  S4: 'red',
}

export default function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(false)
  const [editModalOpen, setEditModalOpen] = useState(false)
  const [stockModalOpen, setStockModalOpen] = useState(false)
  const [editing, setEditing] = useState<Product | null>(null)
  const [stockProduct, setStockProduct] = useState<Product | null>(null)
  const [editForm] = Form.useForm()
  const [stockForm] = Form.useForm()

  const [catalogue, setCatalogue] = useState<MasterProduct[]>([])
  const [catalogueTotal, setCatalogueTotal] = useState(0)
  const [cataloguePage, setCataloguePage] = useState(1)
  const [catalogueSearch, setCatalogueSearch] = useState('')
  const [catalogueLoading, setCatalogueLoading] = useState(false)
  const [selectedIds, setSelectedIds] = useState<string[]>([])
  const [importing, setImporting] = useState(false)

  const loadProducts = useCallback(async () => {
    setLoading(true)
    try {
      const data = await fetchProducts(page, 20, search || undefined)
      setProducts(data.items)
      setTotal(data.totalCount)
    } catch {
      message.error('Failed to load products')
    } finally {
      setLoading(false)
    }
  }, [page, search])

  const loadCatalogue = useCallback(async () => {
    setCatalogueLoading(true)
    try {
      const data = await fetchMasterProducts(cataloguePage, 20, catalogueSearch || undefined)
      setCatalogue(data.items)
      setCatalogueTotal(data.totalCount)
    } catch {
      message.error('Failed to load catalogue')
    } finally {
      setCatalogueLoading(false)
    }
  }, [cataloguePage, catalogueSearch])

  useEffect(() => { loadProducts() }, [loadProducts])
  useEffect(() => { loadCatalogue() }, [loadCatalogue])

  const openEditModal = (record: Product) => {
    setEditing(record)
    editForm.setFieldsValue({
      ...record,
      expiryDate: record.expiryDate ? dayjs(record.expiryDate) : null,
    })
    setEditModalOpen(true)
  }

  const handleEdit = async () => {
    const values = await editForm.validateFields()
    if (!editing) return
    try {
      await updateProduct(editing.id, {
        ...values,
        expiryDate: values.expiryDate ? values.expiryDate.format('YYYY-MM-DD') : undefined,
      })
      message.success('Product updated')
      setEditModalOpen(false)
      loadProducts()
    } catch {
      message.error('Failed to update product')
    }
  }

  const handleDelete = async (id: string) => {
    try {
      await deleteProduct(id)
      message.success('Product deleted')
      loadProducts()
    } catch {
      message.error('Failed to delete product')
    }
  }

  const handleImport = async () => {
    if (selectedIds.length === 0) {
      message.warning('Select at least one product to import')
      return
    }
    setImporting(true)
    try {
      const imported = await importProducts(selectedIds)
      message.success(`Imported ${imported.length} product(s)`)
      setSelectedIds([])
      loadProducts()
    } catch {
      message.error('Failed to import products')
    } finally {
      setImporting(false)
    }
  }

  const openStockModal = (record: Product) => {
    setStockProduct(record)
    stockForm.setFieldsValue({ movementType: 'stock_in', quantity: 1, reference: '', notes: '', approvedBy: '' })
    setStockModalOpen(true)
  }

  const handleStockMovement = async () => {
    const values = await stockForm.validateFields()
    if (!stockProduct) return
    try {
      await recordStockMovement(stockProduct.id, values)
      message.success('Stock movement recorded')
      setStockModalOpen(false)
      loadProducts()
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to record stock movement'
      message.error(errorMsg)
    }
  }

  const stockMovementType = Form.useWatch('movementType', stockForm)

  const productColumns = [
    {
      title: 'Product', key: 'name',
      render: (_: unknown, r: Product) => r.customName ?? r.masterProductName,
    },
    { title: 'Brand', dataIndex: 'brand', key: 'brand' },
    { title: 'Category', dataIndex: 'category', key: 'category' },
    {
      title: 'Schedule', dataIndex: 'scheduleClass', key: 'scheduleClass',
      render: (v: string) => <Tag color={scheduleColours[v] ?? 'default'}>{v}</Tag>,
    },
    {
      title: 'Price', key: 'price',
      render: (_: unknown, r: Product) => `$${(r.customPrice ?? r.defaultPrice).toFixed(2)}`,
    },
    {
      title: 'Stock', dataIndex: 'stockQuantity', key: 'stockQuantity',
      render: (v: number, r: Product) =>
        v <= r.reorderLevel ? <Tag color="red">{v}</Tag> : v,
    },
    {
      title: 'Expiry', dataIndex: 'expiryDate', key: 'expiryDate',
      render: (v?: string) => v ? new Date(v).toLocaleDateString() : '-',
    },
    {
      title: 'Actions', key: 'actions',
      render: (_: unknown, r: Product) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEditModal(r)} />
          <Button size="small" onClick={() => openStockModal(r)}>Stock</Button>
          <Button size="small" danger icon={<DeleteOutlined />} onClick={() => handleDelete(r.id)} />
        </Space>
      ),
    },
  ]

  const catalogueColumns = [
    {
      title: '',
      key: 'select',
      render: (_: unknown, r: MasterProduct) => (
        <Checkbox
          checked={selectedIds.includes(r.id)}
          onChange={(e) => {
            setSelectedIds((prev) =>
              e.target.checked ? [...prev, r.id] : prev.filter((id) => id !== r.id)
            )
          }}
        />
      ),
    },
    { title: 'SKU', dataIndex: 'sku', key: 'sku' },
    { title: 'Name', dataIndex: 'name', key: 'name' },
    { title: 'Brand', dataIndex: 'brand', key: 'brand' },
    { title: 'Category', dataIndex: 'category', key: 'category' },
    {
      title: 'Schedule', dataIndex: 'scheduleClass', key: 'scheduleClass',
      render: (v: string) => <Tag color={scheduleColours[v] ?? 'default'}>{v}</Tag>,
    },
    { title: 'Price', dataIndex: 'unitPrice', key: 'unitPrice', render: (v: number) => `$${v.toFixed(2)}` },
  ]

  return (
    <>
      <Typography.Title level={4}>Products</Typography.Title>
      <Tabs
        items={[
          {
            key: 'my-products',
            label: 'My Products',
            children: (
              <>
                <Space style={{ marginBottom: 16 }}>
                  <Input.Search placeholder="Search products..." onSearch={setSearch} allowClear style={{ width: 300 }} />
                </Space>
                <Table columns={productColumns} dataSource={products} rowKey="id" loading={loading}
                  pagination={{ current: page, total, pageSize: 20, onChange: setPage }} />
              </>
            ),
          },
          {
            key: 'import',
            label: 'Import from Catalogue',
            children: (
              <>
                <Space style={{ marginBottom: 16 }}>
                  <Input.Search placeholder="Search catalogue..." onSearch={setCatalogueSearch} allowClear style={{ width: 300 }} />
                  <Button type="primary" icon={<ImportOutlined />} onClick={handleImport} loading={importing}
                    disabled={selectedIds.length === 0}>
                    Import Selected ({selectedIds.length})
                  </Button>
                </Space>
                <Table columns={catalogueColumns} dataSource={catalogue} rowKey="id" loading={catalogueLoading}
                  pagination={{ current: cataloguePage, total: catalogueTotal, pageSize: 20, onChange: setCataloguePage }} />
              </>
            ),
          },
        ]}
      />

      <Modal title="Edit Product" open={editModalOpen} onOk={handleEdit} onCancel={() => setEditModalOpen(false)} destroyOnClose>
        <Form form={editForm} layout="vertical">
          <Form.Item name="customName" label="Custom Name"><Input /></Form.Item>
          <Form.Item name="customPrice" label="Custom Price"><InputNumber prefix="$" min={0} style={{ width: '100%' }} /></Form.Item>
          <Form.Item name="reorderLevel" label="Reorder Level"><InputNumber min={0} style={{ width: '100%' }} /></Form.Item>
          <Form.Item name="expiryDate" label="Expiry Date"><DatePicker style={{ width: '100%' }} /></Form.Item>
          <Form.Item name="isVisible" label="Visible" valuePropName="checked"><Switch /></Form.Item>
          <Form.Item name="isFeatured" label="Featured" valuePropName="checked"><Switch /></Form.Item>
          <Form.Item name="sortOrder" label="Sort Order"><InputNumber min={0} style={{ width: '100%' }} /></Form.Item>
        </Form>
      </Modal>

      <Modal title={`Stock Movement — ${stockProduct?.customName ?? stockProduct?.masterProductName ?? ''}`}
        open={stockModalOpen}
        onOk={handleStockMovement}
        onCancel={() => setStockModalOpen(false)}
        okButtonProps={{
          disabled: stockProduct?.scheduleClass === 'S4' && stockMovementType === 'stock_out',
        }}
        destroyOnClose>
        <Form form={stockForm} layout="vertical">
          {stockProduct?.scheduleClass === 'S4' && stockMovementType === 'stock_out' && (
            <Alert
              type="error"
              showIcon
              style={{ marginBottom: 16 }}
              message="Prescription Only (S4)"
              description="S4 products cannot be dispensed via the admin panel. Prescription dispensing must be handled through the dispensary system."
            />
          )}
          {stockProduct?.scheduleClass === 'S3' && stockMovementType === 'stock_out' && (
            <Alert
              type="warning"
              showIcon
              style={{ marginBottom: 16 }}
              message="Pharmacist Only (S3)"
              description="This product requires pharmacist approval for stock out. Please enter the approving pharmacist's name below."
            />
          )}
          <Form.Item name="movementType" label="Type" rules={[{ required: true }]}>
            <Select options={[
              { value: 'stock_in', label: 'Stock In' },
              { value: 'stock_out', label: 'Stock Out' },
              { value: 'adjustment', label: 'Adjustment' },
              { value: 'expired', label: 'Expired' },
              { value: 'return', label: 'Return' },
            ]} />
          </Form.Item>
          <Form.Item name="quantity" label="Quantity" rules={[{ required: true }]}>
            <InputNumber min={1} style={{ width: '100%' }} />
          </Form.Item>
          {stockProduct?.scheduleClass === 'S3' && stockMovementType === 'stock_out' && (
            <Form.Item
              name="approvedBy"
              label="Approved By (Pharmacist)"
              rules={[{ required: true, message: 'Pharmacist approval is required for S3 products' }]}
            >
              <Input placeholder="Enter pharmacist name" />
            </Form.Item>
          )}
          <Form.Item name="reference" label="Reference"><Input /></Form.Item>
          <Form.Item name="notes" label="Notes"><Input.TextArea rows={2} /></Form.Item>
        </Form>
      </Modal>
    </>
  )
}
