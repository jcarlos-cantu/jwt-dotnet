# JWT con .NET 10 — Clean Architecture

## ¿Qué es JWT?

JWT (JSON Web Token) es un estándar abierto (RFC 7519) para transmitir información de forma segura entre dos partes como un objeto JSON. Se usa principalmente para **autenticación y autorización** en APIs REST.

Un token JWT tiene tres partes separadas por puntos:
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9        <- Header (algoritmo + tipo)
.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6Ikp.   <- Payload (claims / datos)
.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c <- Signature (verificación)
```

## Objetivo del Proyecto

Construir una Web API en .NET 10 con **Clean Architecture** que:
1. Expone un endpoint de **login** que valida credenciales y devuelve un JWT.
2. Protege otros endpoints con **autenticación Bearer JWT**.
3. Maneja **roles/claims** para autorización (ej. Admin vs User).
4. Implementa **Refresh Tokens** para renovar el acceso sin volver a loguearse.

---

## ¿Qué es Clean Architecture?

Clean Architecture (Robert C. Martin) organiza el código en capas concéntricas donde **las dependencias solo apuntan hacia adentro**. El núcleo del negocio no conoce ni depende de la infraestructura.

```
┌──────────────────────────────────────────┐
│              API / Presentation          │  ← Controllers, Middleware
│  ┌────────────────────────────────────┐  │
│  │           Application              │  │  ← Use Cases, DTOs, Interfaces
│  │  ┌──────────────────────────────┐  │  │
│  │  │          Domain              │  │  │  ← Entidades, Value Objects
│  │  │   (sin dependencias externas)│  │  │
│  │  └──────────────────────────────┘  │  │
│  └────────────────────────────────────┘  │
│              Infrastructure              │  ← DB, JWT impl, Repos
└──────────────────────────────────────────┘
```

**Regla de oro:** Domain no importa nada. Application solo importa Domain. Infrastructure e API implementan las interfaces definidas en capas internas.

---

## Stack Tecnológico

| Componente          | Tecnología                                       |
|---------------------|--------------------------------------------------|
| Framework           | ASP.NET Core 10 (Web API)                        |
| Autenticación       | Microsoft.AspNetCore.Authentication.JwtBearer    |
| Generación tokens   | System.IdentityModel.Tokens.Jwt                  |
| Hash contraseñas    | BCrypt.Net-Next                                  |
| Base de datos       | SQLite + EF Core                                 |
| Mapeo objetos       | Mapperly (o mapeo manual)                        |

---

## Estructura del Proyecto (solución multi-proyecto)

```
jwt_dotnet/
├── contexto.md
└── JwtApi/
    ├── JwtApi.sln
    │
    ├── src/
    │   ├── JwtApi.Domain/               <- Capa 1: núcleo puro
    │   │   ├── Entities/
    │   │   │   └── User.cs              <- entidad de dominio (sin ORM)
    │   │   ├── Enums/
    │   │   │   └── UserRole.cs
    │   │   └── Repositories/
    │   │       └── IUserRepository.cs   <- contrato (no implementación)
    │   │
    │   ├── JwtApi.Application/          <- Capa 2: casos de uso
    │   │   ├── DTOs/
    │   │   │   ├── LoginRequest.cs
    │   │   │   ├── RegisterRequest.cs
    │   │   │   └── TokenResponse.cs
    │   │   ├── Interfaces/
    │   │   │   ├── IAuthService.cs
    │   │   │   └── ITokenService.cs     <- contrato del token (sin JWT aquí)
    │   │   └── UseCases/
    │   │       ├── LoginUseCase.cs
    │   │       └── RefreshTokenUseCase.cs
    │   │
    │   ├── JwtApi.Infrastructure/       <- Capa 3: implementaciones concretas
    │   │   ├── Persistence/
    │   │   │   ├── AppDbContext.cs      <- EF Core DbContext
    │   │   │   └── UserRepository.cs   <- implementa IUserRepository
    │   │   ├── Auth/
    │   │   │   └── JwtTokenService.cs  <- implementa ITokenService con JWT
    │   │   └── DependencyInjection.cs  <- registra todos los servicios de infra
    │   │
    │   └── JwtApi.Api/                  <- Capa 4: punto de entrada HTTP
    │       ├── Program.cs
    │       ├── appsettings.json
    │       ├── Controllers/
    │       │   ├── AuthController.cs    <- POST /api/auth/login, /refresh
    │       │   └── UsersController.cs  <- GET /api/users/me, /admin
    │       └── requests.http           <- pruebas manuales desde el IDE
    │
    └── tests/
        └── JwtApi.Application.Tests/   <- unit tests de casos de uso
```

---

## Dependencias entre proyectos

```
JwtApi.Api  →  JwtApi.Application  →  JwtApi.Domain
JwtApi.Infrastructure  →  JwtApi.Application  →  JwtApi.Domain
```

- `Domain` no referencia nada.
- `Application` referencia solo `Domain`.
- `Infrastructure` referencia `Application` (para implementar interfaces).
- `Api` referencia `Application` + `Infrastructure` (solo para el registro DI).

---

## Plan Paso a Paso

### Paso 1 — Crear la solución y los proyectos
```bash
mkdir JwtApi && cd JwtApi
dotnet new sln -n JwtApi
dotnet new classlib -n JwtApi.Domain       -o src/JwtApi.Domain
dotnet new classlib -n JwtApi.Application  -o src/JwtApi.Application
dotnet new classlib -n JwtApi.Infrastructure -o src/JwtApi.Infrastructure
dotnet new webapi   -n JwtApi.Api          -o src/JwtApi.Api
dotnet new xunit    -n JwtApi.Application.Tests -o tests/JwtApi.Application.Tests

# Agregar todos a la solución
dotnet sln add src/JwtApi.Domain src/JwtApi.Application src/JwtApi.Infrastructure src/JwtApi.Api tests/JwtApi.Application.Tests

# Referenciar proyectos entre sí
dotnet add src/JwtApi.Application/JwtApi.Application.csproj   reference src/JwtApi.Domain/JwtApi.Domain.csproj
dotnet add src/JwtApi.Infrastructure/JwtApi.Infrastructure.csproj reference src/JwtApi.Application/JwtApi.Application.csproj
dotnet add src/JwtApi.Api/JwtApi.Api.csproj                   reference src/JwtApi.Application/JwtApi.Application.csproj
dotnet add src/JwtApi.Api/JwtApi.Api.csproj                   reference src/JwtApi.Infrastructure/JwtApi.Infrastructure.csproj
dotnet add tests/JwtApi.Application.Tests/JwtApi.Application.Tests.csproj reference src/JwtApi.Application/JwtApi.Application.csproj
```

### Paso 2 — Instalar paquetes NuGet
```bash
# Infrastructure
dotnet add src/JwtApi.Infrastructure reference Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/JwtApi.Infrastructure reference Microsoft.EntityFrameworkCore.Design
dotnet add src/JwtApi.Infrastructure reference BCrypt.Net-Next
dotnet add src/JwtApi.Infrastructure reference System.IdentityModel.Tokens.Jwt

# Api
dotnet add src/JwtApi.Api reference Microsoft.AspNetCore.Authentication.JwtBearer
```

### Paso 3 — Capa Domain
- Crear `User.cs`: entidad con `Id`, `Email`, `PasswordHash`, `Role`, `RefreshToken`, `RefreshTokenExpiry`
- Crear `UserRole.cs`: enum con `User` y `Admin`
- Crear `IUserRepository.cs`: interfaz con `GetByEmailAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`

### Paso 4 — Capa Application
- Crear DTOs: `LoginRequest`, `RegisterRequest`, `TokenResponse`
- Crear `ITokenService`: interfaz con `GenerateAccessToken`, `GenerateRefreshToken`, `GetPrincipalFromExpiredToken`
- Crear `IAuthService`: interfaz con `LoginAsync` y `RefreshAsync`
- Crear `LoginUseCase` y `RefreshTokenUseCase` que implementan la lógica usando las interfaces

### Paso 5 — Capa Infrastructure
- `AppDbContext`: DbContext de EF Core con `DbSet<User>`
- `UserRepository`: implementa `IUserRepository` con EF Core
- `JwtTokenService`: implementa `ITokenService` generando tokens JWT firmados con HMAC-SHA256
- `DependencyInjection.cs`: método de extensión `AddInfrastructure(this IServiceCollection)` que registra todo

### Paso 6 — Capa API
- Configurar `appsettings.json` con la sección `Jwt` (Key, Issuer, Audience, ExpirationMinutes)
- `Program.cs`: llamar a `AddInfrastructure()`, registrar autenticación JWT, `UseAuthentication/Authorization`
- `AuthController`: `POST /api/auth/login` y `POST /api/auth/refresh`
- `UsersController`: `GET /api/users/me` (`[Authorize]`) y `GET /api/users/admin` (`[Authorize(Roles="Admin")]`)

### Paso 7 — Probar con `requests.http`
- Registrar usuario (si aplica)
- Login → capturar `accessToken` y `refreshToken`
- Llamar endpoint protegido con el token
- Llamar endpoint de Admin (debe fallar con rol User)
- Refresh del token

---

## Conceptos Clave

### Claims
Datos dentro del payload del JWT:
- `sub` → ID del usuario
- `email` → email
- `role` → rol (Admin / User)
- `exp` → expiración (Unix timestamp)
- `jti` → ID único del token (para revocación)

### Firma del Token
El servidor firma el token con la clave secreta. Al recibir un request:
1. El middleware decodifica header y payload.
2. Recalcula la firma con la clave local.
3. Si coincide → token válido. Si no → `401 Unauthorized`.

### Access Token vs Refresh Token
|                    | Access Token       | Refresh Token       |
|--------------------|--------------------|---------------------|
| Duración           | Corta (15-60 min)  | Larga (7-30 días)   |
| Dónde se guarda    | Memory / header    | DB + HttpOnly Cookie|
| Uso                | Cada request       | Solo para renovar   |

### ¿Por qué Clean Architecture aquí?
- El `JwtTokenService` (que usa la librería JWT) está en Infrastructure → si mañana cambias la librería, solo tocas esa clase.
- Los casos de uso (`LoginUseCase`) son 100% testeables sin necesidad de HTTP ni DB.
- El dominio (`User`) es una clase C# pura sin atributos de EF ni JWT.

---

## Resultado Esperado

Al terminar tendrás una API donde:
- Un usuario hace login y recibe `{ accessToken, refreshToken }`.
- Accede a endpoints protegidos con `Authorization: Bearer <token>`.
- Un token vencido se renueva con el refresh token.
- Solo usuarios con rol `Admin` acceden a ciertos endpoints.
- El código está organizado en 4 proyectos con dependencias controladas.
