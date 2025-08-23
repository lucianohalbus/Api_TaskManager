# Api TaskManager

REST API for task management with JWT authentication.

## Technologies

- .NET 9.0
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Authentication
- BCrypt for password hashing
- xUnit for testing

## Setup

### 1. Configure settings files

Copy the template files and configure with your values:

```bash
# In the src/ folder
cp appsettings.Template.json appsettings.json
cp appsettings.Development.Template.json appsettings.Development.json
```

### 2. Configure required values

Edit the copied files and replace the placeholders:

- `Jwt:Key`: Secret key for JWT (minimum 32 characters)
- `ConnectionStrings:DefaultConnection`: Database connection string

Example for local development:
```json
{
  "Jwt": {
    "Key": "your-secret-key-here-with-at-least-32-characters"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaskManagerDb;Trusted_Connection=true;"
  }
}
```

### 3. Database

```bash
# Install EF Tools (if needed)
dotnet tool install --global dotnet-ef

# Apply migrations
dotnet ef database update --project src
```

## Run

```bash
# Development
dotnet run --project src

# Or in the src/ folder
cd src
dotnet run
```

The API will be available at `https://localhost:xxxx` with Swagger documentation at the root.

## Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test --filter "UnitTests"

# Integration tests only
dotnet test --filter "IntegrationTests"
```

## Structure

```
├── src/                     # Main API code
│   ├── Controllers/         # API Controllers
│   ├── Models/             # Domain models
│   ├── Data/               # DbContext and configurations
│   ├── Dtos/               # Data Transfer Objects
│   ├── Middlewares/        # Custom middlewares
│   └── Extensions/         # Extension methods
├── tests/
│   ├── unittests/          # Unit tests
│   └── integrationtests/   # Integration tests
```

## Main endpoints

- `POST /api/auth/login` - Authentication
- `POST /api/auth/refresh` - Refresh token
- `GET /api/tasks` - List tasks (authenticated)
- `POST /api/tasks` - Create task (authenticated)