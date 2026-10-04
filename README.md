# 🐾 Pet Shop Management System — Backend API

An enterprise-grade RESTful Web API built with **.NET 8**, **Dapper (Micro-ORM)**, and a **Two-Way Synchronized JSON Mock Database**.

---

## 🛠️ Tech Stack & Architecture

- **Framework**: .NET 8 (ASP.NET Core Web API)
- **Data Access (Micro-ORM)**: Dapper 2.1.89 (High-performance parameterized SQL)
- **Database Engine**: SQLite (`petshop_database.db`) for ACID compliance and lightning-fast SQL queries
- **Mock Database Persistence**: Live two-way sync with JSON tables in `Data/Tables/`
- **Authentication**: JWT Bearer Tokens with Cryptographic Digital Signatures & Role-Based Access Control (RBAC)
- **Password Security**: BCrypt password hashing with Salt (`BCrypt.Net-Next`)
- **API Documentation**: Swagger / OpenAPI with interactive Swagger UI

### Architecture Overview

```text
Controllers/              # API Endpoints & Request/Response Contracts
├── AuthController.cs     # Controller 1: Authentication & User Profiles
└── PetShopController.cs  # Controller 2: CRUD Data Management & Admin Dashboard

Services/                 # Business Logic & Validation Layer
├── AuthService.cs        # Login, Registration, BCrypt Verification
├── TokenService.cs       # JWT Token Issuance & Role Claims
└── PetShopService.cs     # Pet Inventory, Adoption Orders & Dashboard KPIs

Repositories/             # Data Access Layer using Dapper SQL Queries
├── PetRepository.cs      # Full CRUD, Pagination, Filters & Status Toggles
├── UserRepository.cs     # User Queries & Account Creation
├── OrderRepository.cs    # Adoption Transactions & Revenue Queries
└── CategoryRepository.cs # Pet Categories Management

Data/                     # Database Engine & Mock Tables
├── Connections/          # Connection Factories (IDbConnectionFactory)
├── Tables/               # Mock Database JSON Tables (Users, Pets, Categories, Orders)
└── JsonDatabaseManager.cs# 2-Way Synchronization Engine (SQLite ⟷ JSON)

DTOs/                     # Data Transfer Objects & ApiResponse<T> Wrapper
Models/                   # Core Domain Entities (User, Pet, Category, Order)
```

---

## 📊 Database & Tables Schema

The database consists of **4 core tables** persisted in SQLite and mirrored into `Data/Tables/*.json`:

| Table | File | Description | Key Fields |
|---|---|---|---|
| `Users` | `Data/Tables/Users.json` | System accounts & roles | `Id`, `Username`, `PasswordHash`, `FullName`, `Telephone`, `Email`, `Role` |
| `Categories` | `Data/Tables/Categories.json` | Animal species categories | `Id`, `Name`, `Description`, `Icon`, `CreatedAt` |
| `Pets` | `Data/Tables/Pets.json` | Pet inventory & adoption status | `Id`, `Name`, `CategoryId`, `Breed`, `Age`, `Gender`, `Price`, `Status`, `ImageUrl` |
| `Orders` | `Data/Tables/Orders.json` | Adoption transactions | `Id`, `OrderNumber`, `CustomerName`, `PetId`, `TotalAmount`, `PaymentMethod`, `Status` |

---

## 🔒 Security & Role-Based Access Control (RBAC)

The system enforces strict multi-layered security:
- **Public**: Anyone can browse available pets and categories (`/api/petshop/pets`, `/api/petshop/categories`).
- **Authenticated (`User`)**: Registered members can adopt pets and view their own profile (`/api/petshop/orders`, `/api/auth/me`).
- **Admin Only**: Full CRUD operations on inventory, status management, order reviews, and executive analytics are guarded with `[Authorize(Roles = "Admin")]`. Non-admin requests receive **`HTTP 403 Forbidden`**.

### Pre-configured Accounts

| Username | Password | Role | Permissions |
|---|---|---|---|
| **`admin`** | `admin123` | **Admin** | Full Management, CRUD Pets, Dashboard Analytics |
| **`user`** | `user123` | **User** | Browse Catalog, Submit Adoption Orders |

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher

### Installation & Run

1. Clone the repository and navigate to the backend directory:
   ```bash
   cd pet-shop-service
   ```

2. Restore NuGet dependencies:
   ```bash
   dotnet restore
   ```

3. Build the solution:
   ```bash
   dotnet build
   ```

4. Run the API:
   ```bash
   dotnet run
   ```

5. The API will start listening on:
   - **API Base URL**: `http://localhost:5000`
   - **Interactive Swagger UI**: `http://localhost:5000/swagger`

---

## 📑 API Endpoints Reference

All endpoints support both `/api/petshop/*` and `/api/crud/*` for complete compatibility.

### 1. Authentication Controller (`/api/auth`)

| Method | Endpoint | Access | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | Public | Register a new member account |
| `POST` | `/api/auth/login` | Public | Authenticate user & return JWT token |
| `GET` | `/api/auth/me` | Authenticated | Retrieve current user profile |

### 2. Main Data & CRUD Controller (`/api/petshop` or `/api/crud`)

| Method | Endpoint | Access | Description |
|---|---|---|---|
| `GET` | `/api/petshop/pets` | Public | Get pets with pagination (`pageSize=20`), search & category filters |
| `GET` | `/api/petshop/pets/{id}` | Public | Get pet details by ID |
| `POST` | `/api/petshop/pets` | **Admin** | Create new pet record |
| `PUT` | `/api/petshop/pets/{id}` | **Admin** | Update existing pet record or toggle status |
| `DELETE` | `/api/petshop/pets/{id}` | **Admin** | Delete pet record from inventory |
| `GET` | `/api/petshop/categories` | Public | List all pet categories |
| `POST` | `/api/petshop/categories` | **Admin** | Create new pet category |
| `POST` | `/api/petshop/orders` | Authenticated | Submit pet adoption order |
| `GET` | `/api/petshop/orders` | **Admin** | View all adoption transactions |
| `GET` | `/api/petshop/dashboard/summary` | **Admin** | Executive statistics, revenue KPIs & category breakdown |
