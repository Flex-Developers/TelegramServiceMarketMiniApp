// User types
export interface User {
  id: string
  telegramId: number
  username?: string
  firstName: string
  lastName?: string
  photoUrl?: string
  role: 'Buyer' | 'Seller' | 'Both' | 'Admin'
  isVerified: boolean
  languageCode?: string
  createdAt: string
  lastActiveAt?: string
}

export interface AuthResult {
  success: boolean
  accessToken?: string
  refreshToken?: string
  expiresAt?: string
  user?: User
  error?: string
}

// Service types
export interface Service {
  id: string
  sellerId: string
  seller: SellerSummary
  title: string
  description: string
  categoryId: string
  categoryName: string
  price: number
  priceType: 'Fixed' | 'Hourly'
  deliveryDays: number
  isActive: boolean
  viewCount: number
  orderCount: number
  averageRating: number
  reviewCount: number
  responseTimeHours: number
  images: ServiceImage[]
  tags: string[]
  createdAt: string
  updatedAt: string
}

export interface ServiceListItem {
  id: string
  title: string
  price: number
  priceType: 'Fixed' | 'Hourly'
  deliveryDays: number
  averageRating: number
  reviewCount: number
  thumbnailUrl?: string
  seller: SellerSummary
}

export interface ServiceImage {
  id: string
  imageUrl: string
  thumbnailUrl?: string
  sortOrder: number
  isPrimary: boolean
}

export interface SellerSummary {
  id: string
  username?: string
  firstName: string
  photoUrl?: string
  isVerified: boolean
  averageRating: number
}

export interface ServiceFilter {
  page?: number
  pageSize?: number
  categoryId?: string
  minPrice?: number
  maxPrice?: number
  minRating?: number
  maxDeliveryDays?: number
  searchTerm?: string
  sortBy?: string
  sortDescending?: boolean
}

// Category types
export interface Category {
  id: string
  name: string
  nameEn: string
  nameDe: string
  icon?: string
  imageUrl?: string
  parentId?: string
  sortOrder: number
  isActive: boolean
  serviceCount: number
  children?: Category[]
}

// Cart types
export interface Cart {
  items: CartItem[]
  subTotal: number
  discountAmount?: number
  promoCode?: string
  total: number
  itemCount: number
}

export interface CartItem {
  id: string
  serviceId: string
  serviceTitle: string
  servicePrice: number
  thumbnailUrl?: string
  quantity: number
  totalPrice: number
  seller: SellerSummary
}

// Order types
export interface Order {
  id: string
  buyerId: string
  sellerId: string
  buyer: UserSummary
  seller: UserSummary
  status: OrderStatus
  subTotal: number
  commission: number
  totalAmount: number
  paymentMethod: PaymentMethod
  paymentStatus: PaymentStatus
  promoCode?: string
  discountAmount: number
  notes?: string
  items: OrderItem[]
  createdAt: string
  paidAt?: string
  completedAt?: string
  cancelledAt?: string
  cancellationReason?: string
}

export interface OrderListItem {
  id: string
  status: OrderStatus
  totalAmount: number
  paymentStatus: PaymentStatus
  itemCount: number
  firstItemTitle: string
  firstItemThumbnail?: string
  otherParty: UserSummary
  createdAt: string
}

export interface OrderItem {
  id: string
  serviceId: string
  serviceTitle: string
  serviceDescription?: string
  quantity: number
  unitPrice: number
  totalPrice: number
  thumbnailUrl?: string
}

export interface UserSummary {
  id: string
  username?: string
  firstName: string
  photoUrl?: string
}

export type OrderStatus =
  | 'Pending'
  | 'Paid'
  | 'Processing'
  | 'Delivered'
  | 'Completed'
  | 'Cancelled'
  | 'Refunded'
  | 'Disputed'

export type PaymentMethod = 'YooKassa' | 'Robokassa' | 'TelegramStars'

export type PaymentStatus =
  | 'Pending'
  | 'WaitingForCapture'
  | 'Completed'
  | 'Failed'
  | 'Cancelled'
  | 'Refunding'
  | 'Refunded'

// Payment types
export interface PaymentResult {
  paymentId: string
  orderId: string
  status: PaymentStatus
  confirmationUrl?: string
  message?: string
}

// Review types
export interface Review {
  id: string
  orderId: string
  serviceId: string
  reviewerId: string
  reviewer: UserSummary
  rating: number
  comment?: string
  images: string[]
  sellerResponse?: string
  responseDate?: string
  helpfulVotes: number
  isVerifiedPurchase: boolean
  createdAt: string
  updatedAt?: string
}

export interface ReviewStats {
  averageRating: number
  totalReviews: number
  fiveStarCount: number
  fourStarCount: number
  threeStarCount: number
  twoStarCount: number
  oneStarCount: number
}

// Notification types
export interface Notification {
  id: string
  type: NotificationType
  title: string
  message: string
  data?: string
  isRead: boolean
  createdAt: string
  readAt?: string
}

export type NotificationType =
  | 'OrderCreated'
  | 'OrderPaid'
  | 'OrderProcessing'
  | 'OrderDelivered'
  | 'OrderCompleted'
  | 'OrderCancelled'
  | 'NewReview'
  | 'NewMessage'
  | 'PaymentReceived'
  | 'PayoutProcessed'

// Pagination types
export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

// API Response types
export interface ApiError {
  error: string
  code?: string
  timestamp?: string
}
