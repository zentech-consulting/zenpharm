import { useState, useEffect, useCallback } from 'react'
import { Table, Tag, Button, Input, Tabs, Modal, message, Space, Descriptions } from 'antd'
import {
  CheckCircleOutlined,
  ShoppingCartOutlined,
  CloseCircleOutlined,
  SearchOutlined,
} from '@ant-design/icons'
import dayjs from 'dayjs'
import {
  fetchOrders,
  fetchOrder,
  markOrderReady,
  markOrderCollected,
  cancelOrder,
  type OrderSummary,
  type Order,
} from '../api/orders'

const statusColours: Record<string, string> = {
  pending: 'blue',
  ready: 'green',
  collected: 'default',
  cancelled: 'red',
}

export default function OrdersPage() {
  const [orders, setOrders] = useState<OrderSummary[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [statusFilter, setStatusFilter] = useState<string | undefined>()
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [expandedOrder, setExpandedOrder] = useState<Order | null>(null)
  const [cancelModalId, setCancelModalId] = useState<string | null>(null)
  const [cancelReason, setCancelReason] = useState('')
  const [actioning, setActioning] = useState(false)

  const loadOrders = useCallback(async () => {
    setLoading(true)
    try {
      const result = await fetchOrders(page, pageSize, statusFilter, search || undefined)
      setOrders(result.items)
      setTotalCount(result.totalCount)
    } catch (err) {
      message.error('Failed to load orders')
      console.error(err)
    } finally {
      setLoading(false)
    }
  }, [page, pageSize, statusFilter, search])

  useEffect(() => {
    loadOrders()
  }, [loadOrders])

  const handleMarkReady = async (id: string) => {
    setActioning(true)
    try {
      await markOrderReady(id)
      message.success('Order marked as ready — SMS sent to customer')
      loadOrders()
    } catch {
      message.error('Failed to mark order as ready')
    } finally {
      setActioning(false)
    }
  }

  const handleMarkCollected = async (id: string) => {
    setActioning(true)
    try {
      await markOrderCollected(id)
      message.success('Order marked as collected')
      loadOrders()
    } catch {
      message.error('Failed to mark order as collected')
    } finally {
      setActioning(false)
    }
  }

  const handleCancel = async () => {
    if (!cancelModalId || !cancelReason.trim()) return
    setActioning(true)
    try {
      await cancelOrder(cancelModalId, cancelReason)
      message.success('Order cancelled')
      setCancelModalId(null)
      setCancelReason('')
      loadOrders()
    } catch {
      message.error('Failed to cancel order')
    } finally {
      setActioning(false)
    }
  }

  const handleExpand = async (expanded: boolean, record: OrderSummary) => {
    if (expanded) {
      try {
        const order = await fetchOrder(record.id)
        setExpandedOrder(order)
      } catch {
        message.error('Failed to load order details')
      }
    } else {
      setExpandedOrder(null)
    }
  }

  const columns = [
    {
      title: 'Order #',
      dataIndex: 'orderNumber',
      key: 'orderNumber',
      render: (v: string) => <span className="font-mono font-medium">{v}</span>,
    },
    {
      title: 'Customer',
      dataIndex: 'clientName',
      key: 'clientName',
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => (
        <Tag color={statusColours[status] ?? 'default'}>
          {status.charAt(0).toUpperCase() + status.slice(1)}
        </Tag>
      ),
    },
    {
      title: 'Items',
      dataIndex: 'itemCount',
      key: 'itemCount',
      width: 80,
    },
    {
      title: 'Total',
      dataIndex: 'total',
      key: 'total',
      render: (v: number) => `$${v.toFixed(2)}`,
    },
    {
      title: 'Date',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (v: string) => dayjs(v).format('DD MMM YYYY HH:mm'),
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_: unknown, record: OrderSummary) => (
        <Space>
          {record.status === 'pending' && (
            <Button
              type="primary"
              size="small"
              icon={<CheckCircleOutlined />}
              loading={actioning}
              onClick={() => handleMarkReady(record.id)}
            >
              Ready
            </Button>
          )}
          {record.status === 'ready' && (
            <Button
              size="small"
              icon={<ShoppingCartOutlined />}
              loading={actioning}
              onClick={() => handleMarkCollected(record.id)}
            >
              Collected
            </Button>
          )}
          {(record.status === 'pending' || record.status === 'ready') && (
            <Button
              danger
              size="small"
              icon={<CloseCircleOutlined />}
              onClick={() => setCancelModalId(record.id)}
            >
              Cancel
            </Button>
          )}
        </Space>
      ),
    },
  ]

  const tabItems = [
    { key: '', label: 'All' },
    { key: 'pending', label: 'Pending' },
    { key: 'ready', label: 'Ready' },
    { key: 'collected', label: 'Collected' },
    { key: 'cancelled', label: 'Cancelled' },
  ]

  return (
    <div>
      <h1 style={{ fontSize: 24, fontWeight: 700, marginBottom: 16 }}>Orders</h1>

      <Tabs
        items={tabItems}
        activeKey={statusFilter ?? ''}
        onChange={(key) => {
          setStatusFilter(key || undefined)
          setPage(1)
        }}
      />

      <div style={{ marginBottom: 16 }}>
        <Input
          prefix={<SearchOutlined />}
          placeholder="Search by order number or customer name..."
          value={search}
          onChange={e => { setSearch(e.target.value); setPage(1) }}
          allowClear
          style={{ maxWidth: 400 }}
        />
      </div>

      <Table
        dataSource={orders}
        columns={columns}
        rowKey="id"
        loading={loading}
        pagination={{
          current: page,
          pageSize,
          total: totalCount,
          onChange: (p, ps) => { setPage(p); setPageSize(ps) },
          showSizeChanger: true,
          showTotal: (total) => `${total} orders`,
        }}
        expandable={{
          onExpand: handleExpand,
          expandedRowRender: () => {
            if (!expandedOrder) return <div>Loading...</div>
            return (
              <Descriptions column={1} size="small" bordered>
                {expandedOrder.items.map(item => (
                  <Descriptions.Item
                    key={item.id}
                    label={`${item.productName} x${item.quantity}`}
                  >
                    ${item.unitPrice.toFixed(2)} each = ${item.subtotal.toFixed(2)}
                  </Descriptions.Item>
                ))}
                <Descriptions.Item label="Subtotal">
                  ${expandedOrder.subtotal.toFixed(2)}
                </Descriptions.Item>
                <Descriptions.Item label="GST">
                  ${expandedOrder.taxAmount.toFixed(2)}
                </Descriptions.Item>
                <Descriptions.Item label="Total">
                  <strong>${expandedOrder.total.toFixed(2)}</strong>
                </Descriptions.Item>
                {expandedOrder.notes && (
                  <Descriptions.Item label="Notes">
                    {expandedOrder.notes}
                  </Descriptions.Item>
                )}
                {expandedOrder.estimatedReadyAt && (
                  <Descriptions.Item label="Est. Ready">
                    {dayjs(expandedOrder.estimatedReadyAt).format('DD MMM YYYY HH:mm')}
                  </Descriptions.Item>
                )}
                {expandedOrder.cancellationReason && (
                  <Descriptions.Item label="Cancellation Reason">
                    {expandedOrder.cancellationReason}
                  </Descriptions.Item>
                )}
              </Descriptions>
            )
          },
        }}
      />

      <Modal
        title="Cancel Order"
        open={!!cancelModalId}
        onOk={handleCancel}
        onCancel={() => { setCancelModalId(null); setCancelReason('') }}
        confirmLoading={actioning}
        okButtonProps={{ danger: true, disabled: !cancelReason.trim() }}
        okText="Cancel Order"
      >
        <p style={{ marginBottom: 8 }}>Please provide a reason for cancellation:</p>
        <Input.TextArea
          rows={3}
          value={cancelReason}
          onChange={e => setCancelReason(e.target.value)}
          placeholder="e.g. Customer requested cancellation"
        />
      </Modal>
    </div>
  )
}
