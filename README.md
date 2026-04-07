# Clean Architecture — Student Enrollment System

A RESTful API built with ASP.NET Core following **Clean Architecture** principles, featuring Redis caching, RabbitMQ messaging, email notifications, and fully containerized using Docker for consistent deployment and scalability across environments.

---

## 🏗️ Architecture

- **CleanArchitecture/**                  → API Layer (Controllers, Program.cs)
- **CleanArchitecture.Application/**      → Business Logic (Services, Interfaces, DTOs, Events)
- **CleanArchitecture.Domain/**           → Core Entities & Enums
- **CleanArchitecture.Infrastructure/**   → Data, Caching, Messaging, Repositories
- **CleanArchitecture.UnitTests/**        → Unit Tests (Services)
- **CleanArchitecture.IntegrationTests/** → Integration Tests (Controllers)

---

## ⚙️ Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 10 |
| Database | PostgreSQL + Entity Framework Core |
| Caching | Redis (StackExchange.Redis) |
| Messaging | RabbitMQ (RabbitMQ.Client) |
| Email | SMTP (System.Net.Mail) |
| ORM | Entity Framework Core |
| DI Decoration | Scrutor |
| Testing | xUnit + Moq + FluentAssertions |

---

## 📦 Features

- **CRUD** for Students, Courses, and Enrollments
- **Redis Caching** with cache invalidation on mutations (decorator pattern)
- **RabbitMQ Messaging** for async event-driven communication
- **Email Notifications** triggered via RabbitMQ consumers
- **Unit Tests** for all services with mocked dependencies
- **Integration Tests** for all controllers using in-memory database

---

## 📨 RabbitMQ Event Flow

| Trigger | Queue | Consumer | Action |
|---|---|---|---|
| Student created | `student.registered` | `StudentRegisteredConsumer` | Welcome email |
| Enrollment created | `enrollment.created` | `EnrollmentCreatedConsumer` | Confirmation email |
| Enrollment deleted | `enrollment.cancelled` | `EnrollmentCancelledConsumer` | Cancellation email |
| Course updated | `course.updated` | `CourseUpdatedConsumer` | Notify enrolled students |
| Any mutation | `audit.log` | `AuditLogConsumer` | Audit log entry |

---

## 🧪 Tests

| Project | Type | Count |
|---|---|---|
| `CleanArchitecture.UnitTests` | Unit Tests | 27 |
| `CleanArchitecture.IntegrationTests` | Integration Tests | 13 |

Run all tests:
```bash
dotnet test
```

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/)
- [Redis](https://redis.io/)
- [RabbitMQ](https://www.rabbitmq.com/)

### 1 — Clone the repository
```bash
git clone https://github.com/ayubhatta/CleanArchitecture.git
cd CleanArchitecture
```

### 2 — Configure secrets

Create `appsettings.Development.json` in the `CleanArchitecture` project root:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=cleanArchi;Port=5432;User Id=postgres;Password=yourpassword;",
    "Redis": "localhost:6379"
  },
  "Caching": {
    "Enabled": true
  },
  "EmailSettings": {
    "Email": "your-email@gmail.com",
    "Password": "your-app-password",
    "Host": "smtp.gmail.com",
    "DisplayName": "Your App Name",
    "Port": 587,
    "UseSSL": true,
    "FullName": "your-email@gmail.com"
  },
  "Messaging": {
    "Enabled": true,
    "RabbitMq": {
      "Host": "localhost",
      "Username": "admin",
      "Password": "your-rabbitmq-password"
    }
  }
}
```

### 3 — Run database migrations
```bash
cd CleanArchitecture
dotnet ef database update
```

### 4 — Start Redis and RabbitMQ (WSL)
```bash
sudo service redis-server start
sudo service rabbitmq-server start
```

### 5 — Run the API
```bash
dotnet run
```

API will be available at `https://localhost:7176` and Swagger at `https://localhost:7176/swagger`.

---

## 🔌 API Endpoints

### Students
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/students` | Get all students |
| GET | `/api/students/{id}` | Get student by ID |
| POST | `/api/students` | Create student |
| PUT | `/api/students/{id}` | Update student |
| DELETE | `/api/students/{id}` | Delete student |

### Courses
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/courses` | Get all courses |
| GET | `/api/courses/{id}` | Get course by ID |
| POST | `/api/courses` | Create course |
| PUT | `/api/courses/{id}` | Update course |
| DELETE | `/api/courses/{id}` | Delete course |

### Enrollments
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/enrollments` | Get all enrollments |
| GET | `/api/enrollments/student/{studentId}` | Get by student |
| GET | `/api/enrollments/course/{courseId}` | Get by course |
| POST | `/api/enrollments` | Create enrollment |
| DELETE | `/api/enrollments/{studentId}/{courseId}` | Delete enrollment |

---

## 📝 Notes

- `appsettings.Development.json` is gitignored — never commit secrets
- Caching and Messaging can be toggled via `appsettings.json` using `Caching:Enabled` and `Messaging:Enabled`
- Gmail requires an **App Password** (not your account password) — generate one at [myaccount.google.com/apppasswords](https://myaccount.google.com/apppasswords)
- RabbitMQ guest user only works on localhost — create a dedicated user for remote connections
