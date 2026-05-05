# JWT API — .NET 10 + Clean Architecture

Web API de autenticación con JWT construida en .NET 10 siguiendo los principios de Clean Architecture.

## Características

- Registro y login de usuarios con contraseñas hasheadas (BCrypt)
- Autenticación con Access Tokens JWT (HMAC-SHA256)
- Refresh Tokens rotantes persistidos en base de datos
- Autorización por roles (`User` / `Admin`)
- Base de datos SQLite con EF Core y migraciones automáticas
- Documentación interactiva con Scalar UI
- Unit tests con xUnit, NSubstitute y FluentAssertions

## Estructura

```
src/
├── JwtApi.Domain/          # Entidades, enums, contratos (sin dependencias)
├── JwtApi.Application/     # Casos de uso, DTOs, interfaces
├── JwtApi.Infrastructure/  # JWT, EF Core, repositorios, DI
└── JwtApi.Api/             # Controllers, Program.cs, configuración

tests/
└── JwtApi.Application.Tests/  # Unit tests de los casos de uso
```

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Correr el proyecto

```bash
dotnet run --project src/JwtApi.Api
```

La API queda disponible en `http://localhost:5000`.  
La UI de Scalar en `http://localhost:5000/scalar/v1`.

## Correr los tests

```bash
dotnet test
```

## Endpoints

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/api/auth/register` | No | Registrar nuevo usuario |
| POST | `/api/auth/login` | No | Login, devuelve tokens |
| POST | `/api/auth/refresh` | No | Renovar access token |
| GET | `/api/users/me` | Bearer | Perfil del usuario actual |
| GET | `/api/users/admin` | Bearer + Admin | Área restringida a admins |

Ver ejemplos completos con curl y responses en [ENDPOINTS.md](./ENDPOINTS.md).

## Configuración

`src/JwtApi.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=jwtapi.db"
  },
  "Jwt": {
    "Key": "super-secret-key-minimum-32-characters-long!",
    "Issuer": "JwtApi",
    "Audience": "JwtApi",
    "ExpirationMinutes": "15",
    "RefreshTokenExpirationDays": "7"
  }
}
```

> En producción reemplaza `Jwt:Key` por una clave segura y nunca la subas al repositorio.

## Stack

| Componente | Tecnología |
|---|---|
| Framework | ASP.NET Core 10 |
| ORM | Entity Framework Core 9 + SQLite |
| Autenticación | JwtBearer + System.IdentityModel.Tokens.Jwt |
| Hash contraseñas | BCrypt.Net-Next |
| Documentación | Scalar + Microsoft.AspNetCore.OpenApi |
| Tests | xUnit + NSubstitute + FluentAssertions |
