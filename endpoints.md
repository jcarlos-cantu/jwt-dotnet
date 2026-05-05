# Endpoints — JWT API

Base URL: `http://localhost:5192`

---

## POST /api/auth/register

Registra un nuevo usuario. Por defecto se crea con rol `User`.

```bash
curl -X POST http://localhost:5192/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "usuario@example.com",
    "password": "Password123!"
  }'
```

**Response 200 OK**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI4ZjNhMW...",
  "refreshToken": "k9Z2mXq7vB3nLpRwT1oYsU6jHdCeAiGfNbKxOtMlWuVhQySzPrDcE..."
}
```

**Response 409 Conflict** — email ya registrado
```json
{
  "message": "El email ya está registrado."
}
```

---

## POST /api/auth/login

Autentica un usuario existente y devuelve los tokens.

```bash
curl -X POST http://localhost:5192/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "usuario@example.com",
    "password": "Password123!"
  }'
```

**Response 200 OK**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI4ZjNhMW...",
  "refreshToken": "k9Z2mXq7vB3nLpRwT1oYsU6jHdCeAiGfNbKxOtMlWuVhQySzPrDcE..."
}
```

**Response 401 Unauthorized** — email no existe o contraseña incorrecta
```json
{
  "message": "Credenciales inválidas."
}
```

---

## POST /api/auth/refresh

Emite un nuevo `accessToken` usando el `refreshToken`. El refresh token también rota.

```bash
curl -X POST http://localhost:5192/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "k9Z2mXq7vB3nLpRwT1oYsU6jHdCeAiGfNbKxOtMlWuVhQySzPrDcE..."
  }'
```

**Response 200 OK**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI4ZjNhMW...",
  "refreshToken": "xT4nVqRs2wAkPdYhBmCuZjOeLfGiNbKoMrWyXsUvDgHpQcEaTzIlJ..."
}
```

**Response 401 Unauthorized** — token no existe en DB
```json
{
  "message": "Refresh token inválido."
}
```

**Response 401 Unauthorized** — token vencido (más de 7 días)
```json
{
  "message": "Refresh token expirado."
}
```

---

## GET /api/users/me

Devuelve el perfil del usuario autenticado extraído de los claims del JWT.  
Requiere: `Authorization: Bearer <accessToken>`

```bash
curl http://localhost:5192/api/users/me \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response 200 OK**
```json
{
  "userId": "8f3a1b2c-4d5e-6f7a-8b9c-0d1e2f3a4b5c",
  "email": "usuario@example.com",
  "role": "User"
}
```

**Response 401 Unauthorized** — token ausente o inválido
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```

---

## GET /api/users/admin

Endpoint exclusivo para usuarios con rol `Admin`.  
Requiere: `Authorization: Bearer <accessToken>` + rol `Admin`

```bash
curl http://localhost:5192/api/users/admin \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response 200 OK** — solo si el token tiene rol `Admin`
```json
{
  "message": "Bienvenido, Admin. Área restringida."
}
```

**Response 401 Unauthorized** — token ausente o inválido
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```

**Response 403 Forbidden** — token válido pero rol `User`
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403
}
```

---

## Flujo completo de ejemplo

```bash
# 1. Registrar
curl -X POST http://localhost:5192/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"yo@test.com","password":"Pass123!"}'

# 2. Guardar el accessToken y hacer una petición autenticada
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

curl http://localhost:5192/api/users/me \
  -H "Authorization: Bearer $TOKEN"

# 3. Cuando el accessToken vence, refrescarlo
REFRESH="k9Z2mXq7vB3nLpRwT1oYsU6..."

curl -X POST http://localhost:5192/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"$REFRESH\"}"
```

---

## Cambiar un usuario a rol Admin

Por ahora no hay endpoint para esto. Se hace directamente en la DB:

```sql
UPDATE Users SET Role = 'Admin' WHERE Email = 'admin@example.com';
```
