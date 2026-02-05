# Setup Guide

## Prerequisites

- **Docker & Docker Compose** (for containerized deployment)
- **Node.js 20+** (for frontend development)
- **.NET 8 SDK** (for backend development)
- **PostgreSQL 16** (if running locally without Docker)
- **Redis 7** (if running locally without Docker)

## Local Development Setup

### 1. Clone the Repository

```bash
git clone https://github.com/your-org/telegram-marketplace.git
cd telegram-marketplace
```

### 2. Environment Configuration

Create environment files:

```bash
# Root .env for Docker
cp .env.example .env

# Backend appsettings
cp backend/src/WebAPI/appsettings.json backend/src/WebAPI/appsettings.Development.json
```

Edit `.env` and `appsettings.Development.json` with your configuration.

### 3. Database Setup

**With Docker:**
```bash
docker-compose up postgres redis -d
```

**Without Docker:**
1. Install PostgreSQL and create database:
   ```sql
   CREATE DATABASE telegram_marketplace;
   ```
2. Install Redis and start the service

### 4. Backend Setup

```bash
cd backend

# Restore packages
dotnet restore

# Apply database migrations
dotnet ef database update --project src/Infrastructure --startup-project src/WebAPI

# Run the API
dotnet run --project src/WebAPI
```

The API will be available at `http://localhost:5000`

### 5. Frontend Setup

```bash
cd frontend

# Install dependencies
npm install

# Start development server
npm run dev
```

The frontend will be available at `http://localhost:5173`

## Database Migrations

### Create a new migration

```bash
cd backend
dotnet ef migrations add MigrationName --project src/Infrastructure --startup-project src/WebAPI
```

### Apply migrations

```bash
dotnet ef database update --project src/Infrastructure --startup-project src/WebAPI
```

### Revert migration

```bash
dotnet ef database update PreviousMigrationName --project src/Infrastructure --startup-project src/WebAPI
```

## Running Tests

### Backend Unit Tests

```bash
cd backend
dotnet test tests/Unit
```

### Backend Integration Tests

```bash
cd backend
dotnet test tests/Integration
```

### Frontend E2E Tests

```bash
cd frontend

# Install Playwright browsers (first time only)
npx playwright install

# Run tests
npm run test:e2e

# Run with UI
npm run test:e2e:ui
```

## Seed Data

To populate the database with sample data:

```bash
cd backend
dotnet run --project src/WebAPI -- --seed
```

Or manually execute the seed SQL:

```sql
-- Categories
INSERT INTO "Categories" ("Id", "Name", "NameEn", "NameDe", "Icon", "IsActive", "SortOrder")
VALUES
  (gen_random_uuid(), 'Дизайн', 'Design', 'Design', '🎨', true, 1),
  (gen_random_uuid(), 'Разработка', 'Development', 'Entwicklung', '💻', true, 2),
  (gen_random_uuid(), 'Маркетинг', 'Marketing', 'Marketing', '📢', true, 3);
```

## Troubleshooting

### Database Connection Issues

1. Verify PostgreSQL is running: `docker-compose ps` or `pg_isready`
2. Check connection string in appsettings
3. Ensure database exists and migrations are applied

### Redis Connection Issues

1. Verify Redis is running: `docker-compose ps` or `redis-cli ping`
2. Check Redis connection string

### Frontend Build Issues

1. Clear node_modules: `rm -rf node_modules && npm install`
2. Clear Vite cache: `rm -rf node_modules/.vite`

### Telegram WebApp Not Loading

1. Ensure HTTPS is configured (Telegram requires HTTPS)
2. Check CORS settings in backend
3. Verify bot token and web app URL configuration
