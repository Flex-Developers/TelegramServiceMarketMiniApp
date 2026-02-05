# Telegram Services Marketplace

Production-ready Telegram Mini App for a services marketplace with full e-commerce functionality.

## Features

- **Service Catalog**: Browse, search, and filter services
- **Shopping Cart**: Add services, apply promo codes
- **Multiple Payment Methods**: YooKassa, Robokassa, Telegram Stars
- **Order Management**: Track orders, update status
- **Reviews & Ratings**: 5-star reviews with seller responses
- **Real-time Notifications**: SignalR-powered updates
- **Seller Dashboard**: Manage services, view analytics
- **Multilingual Support**: Russian, English, German

## Tech Stack

### Backend
- ASP.NET Core 8.0
- PostgreSQL with Entity Framework Core
- Redis for caching
- SignalR for real-time features
- JWT + Telegram authentication

### Frontend
- React 18 with TypeScript
- Telegram Mini Apps SDK (@twa-dev/sdk)
- TanStack Query for data fetching
- Zustand for state management
- Tailwind CSS for styling
- Framer Motion for animations

### Testing
- Playwright for E2E tests
- xUnit for backend unit tests

## Quick Start

### Prerequisites
- Docker & Docker Compose
- Node.js 20+
- .NET 8 SDK (for local development)

### Environment Setup

1. Copy the environment template:
```bash
cp .env.example .env
```

2. Configure the required variables:
```env
TELEGRAM_BOT_TOKEN=your_bot_token
JWT_SECRET=your_super_secret_key_at_least_32_chars
YOOKASSA_SHOP_ID=your_shop_id
YOOKASSA_SECRET_KEY=your_secret_key
ROBOKASSA_LOGIN=your_login
ROBOKASSA_PASSWORD1=password1
ROBOKASSA_PASSWORD2=password2
DB_PASSWORD=your_db_password
WEBAPP_URL=https://your-webapp-url.com
```

### Running with Docker

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

The application will be available at:
- Frontend: http://localhost:80
- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger

### Local Development

**Backend:**
```bash
cd backend
dotnet restore
dotnet run --project src/WebAPI
```

**Frontend:**
```bash
cd frontend
npm install
npm run dev
```

## Project Structure

```
TelegramMarketplace/
├── backend/
│   ├── src/
│   │   ├── Domain/          # Entities, interfaces, domain events
│   │   ├── Application/     # Use cases, DTOs, services
│   │   ├── Infrastructure/  # Persistence, payments, auth
│   │   └── WebAPI/          # Controllers, middleware, hubs
│   └── tests/
├── frontend/
│   ├── src/
│   │   ├── components/      # React components
│   │   ├── pages/           # Page components
│   │   ├── hooks/           # Custom hooks
│   │   ├── services/        # API services
│   │   ├── store/           # Zustand stores
│   │   └── types/           # TypeScript types
│   └── e2e/                 # Playwright tests
├── docker-compose.yml
├── nginx.conf
└── README.md
```

## API Documentation

Swagger documentation is available at `/swagger` when running the backend.

### Key Endpoints

- `POST /api/auth/telegram` - Authenticate with Telegram
- `GET /api/services` - List services with filters
- `GET /api/services/{id}` - Get service details
- `POST /api/cart/items` - Add to cart
- `POST /api/orders` - Create order
- `POST /api/payments/{provider}/create` - Create payment

## Testing

### E2E Tests (Playwright)
```bash
cd frontend
npm run test:e2e
npm run test:e2e:ui  # With UI
```

### Backend Tests
```bash
cd backend
dotnet test
```

## Deployment

See [DEPLOYMENT.md](docs/DEPLOYMENT.md) for detailed deployment instructions.

### Production Checklist

- [ ] Set strong JWT secret (32+ characters)
- [ ] Configure HTTPS with valid SSL certificate
- [ ] Set up payment provider webhooks
- [ ] Configure Telegram bot menu button
- [ ] Enable database backups
- [ ] Set up monitoring (health checks)
- [ ] Configure CORS for production domain

## Documentation

- [Setup Guide](docs/SETUP.md)
- [Telegram Integration](docs/TELEGRAM_SETUP.md)
- [Payment Integration](docs/PAYMENT_INTEGRATION.md)
- [API Documentation](docs/API_DOCUMENTATION.md)
- [Deployment Guide](docs/DEPLOYMENT.md)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

MIT License - see [LICENSE](LICENSE) for details.

---

Built with Telegram Mini Apps SDK. Learn more at [core.telegram.org/bots/webapps](https://core.telegram.org/bots/webapps)
