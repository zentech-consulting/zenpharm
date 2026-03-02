import { useEffect, useState } from 'react'
import { Card, Col, Row, Table, Typography, DatePicker, Space, Statistic, Spin, message, Tabs } from 'antd'
import {
  ShoppingCartOutlined,
  DollarOutlined,
  WarningOutlined,
  UserOutlined,
} from '@ant-design/icons'
import {
  fetchTopSellingProducts,
  fetchRevenueByCategory,
  fetchExpiryWaste,
  fetchEmployeeUtilisation,
  type TopSellingProductsReport,
  type RevenueByCategoryReport,
  type ExpiryWasteReport,
  type EmployeeUtilisationReport,
} from '../api/reports'
import dayjs from 'dayjs'

const { RangePicker } = DatePicker

export default function ReportsPage() {
  const [dateRange, setDateRange] = useState<[string?, string?]>([undefined, undefined])
  const [loading, setLoading] = useState(true)
  const [topSelling, setTopSelling] = useState<TopSellingProductsReport | null>(null)
  const [revenueByCategory, setRevenueByCategory] = useState<RevenueByCategoryReport | null>(null)
  const [expiryWaste, setExpiryWaste] = useState<ExpiryWasteReport | null>(null)
  const [employeeUtil, setEmployeeUtil] = useState<EmployeeUtilisationReport | null>(null)

  useEffect(() => {
    const load = async () => {
      setLoading(true)
      const [from, to] = dateRange
      try {
        const [ts, rc, ew, eu] = await Promise.allSettled([
          fetchTopSellingProducts(from, to),
          fetchRevenueByCategory(from, to),
          fetchExpiryWaste(from, to),
          fetchEmployeeUtilisation(from, to),
        ])
        if (ts.status === 'fulfilled') setTopSelling(ts.value)
        if (rc.status === 'fulfilled') setRevenueByCategory(rc.value)
        if (ew.status === 'fulfilled') setExpiryWaste(ew.value)
        if (eu.status === 'fulfilled') setEmployeeUtil(eu.value)
      } catch {
        message.error('Failed to load reports')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [dateRange])

  const handleDateChange = (_: unknown, dateStrings: [string, string]) => {
    setDateRange([dateStrings[0] || undefined, dateStrings[1] || undefined])
  }

  if (loading) return <Spin size="large" style={{ display: 'block', margin: '100px auto' }} />

  return (
    <>
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Typography.Title level={4} style={{ margin: 0 }}>Reports</Typography.Title>
        <RangePicker onChange={handleDateChange} />
      </Space>

      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="Total Stock-Out Movements"
              value={topSelling?.totalStockOutMovements ?? 0}
              prefix={<ShoppingCartOutlined />}
              valueStyle={{ color: '#2e86de' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="Service Revenue"
              value={revenueByCategory?.totalRevenue ?? 0}
              prefix={<DollarOutlined />}
              valueStyle={{ color: '#27ae60' }}
              precision={2}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="Expiry Waste Value"
              value={expiryWaste?.totalWasteValue ?? 0}
              prefix={<WarningOutlined />}
              valueStyle={{ color: '#ee5a24' }}
              precision={2}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="Employee Bookings"
              value={employeeUtil?.totalBookings ?? 0}
              prefix={<UserOutlined />}
              valueStyle={{ color: '#0f3460' }}
            />
          </Card>
        </Col>
      </Row>

      <Tabs items={[
        {
          key: 'top-selling',
          label: 'Top Selling Products',
          children: (
            <Table
              dataSource={topSelling?.items ?? []}
              rowKey="productId"
              pagination={false}
              columns={[
                { title: 'Product', dataIndex: 'productName', key: 'productName' },
                { title: 'Brand', dataIndex: 'brand', key: 'brand', render: (v?: string) => v ?? '—' },
                { title: 'Category', dataIndex: 'category', key: 'category' },
                { title: 'Units Sold', dataIndex: 'totalSold', key: 'totalSold' },
                { title: 'Revenue', dataIndex: 'totalRevenue', key: 'totalRevenue',
                  render: (v: number) => `$${v.toFixed(2)}` },
              ]}
            />
          ),
        },
        {
          key: 'revenue-category',
          label: 'Revenue by Category',
          children: (
            <Table
              dataSource={revenueByCategory?.items ?? []}
              rowKey="category"
              pagination={false}
              columns={[
                { title: 'Category', dataIndex: 'category', key: 'category' },
                { title: 'Bookings', dataIndex: 'bookingCount', key: 'bookingCount' },
                { title: 'Revenue', dataIndex: 'revenue', key: 'revenue',
                  render: (v: number) => `$${v.toFixed(2)}` },
              ]}
            />
          ),
        },
        {
          key: 'expiry-waste',
          label: 'Expiry Waste',
          children: (
            <Table
              dataSource={expiryWaste?.items ?? []}
              rowKey="productId"
              pagination={false}
              columns={[
                { title: 'Product', dataIndex: 'productName', key: 'productName' },
                { title: 'Brand', dataIndex: 'brand', key: 'brand', render: (v?: string) => v ?? '—' },
                { title: 'Expired Qty', dataIndex: 'expiredQuantity', key: 'expiredQuantity' },
                { title: 'Waste Value', dataIndex: 'estimatedWasteValue', key: 'estimatedWasteValue',
                  render: (v: number) => `$${v.toFixed(2)}` },
              ]}
            />
          ),
        },
        {
          key: 'employee-util',
          label: 'Employee Utilisation',
          children: (
            <Table
              dataSource={employeeUtil?.items ?? []}
              rowKey="employeeId"
              pagination={false}
              columns={[
                { title: 'Employee', dataIndex: 'employeeName', key: 'employeeName' },
                { title: 'Role', dataIndex: 'role', key: 'role' },
                { title: 'Total Bookings', dataIndex: 'totalBookings', key: 'totalBookings' },
                { title: 'Completed', dataIndex: 'completedBookings', key: 'completedBookings' },
                { title: 'Revenue', dataIndex: 'revenue', key: 'revenue',
                  render: (v: number) => `$${v.toFixed(2)}` },
              ]}
            />
          ),
        },
      ]} />
    </>
  )
}
