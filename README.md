# TiendaInformatica – API mínima con ASP.NET Core + PostgreSQL

API mínima (.NET 10) de una tienda de productos informáticos, con Entity Framework Core,
PostgreSQL, datos iniciales (seeding) y despliegue con Docker Compose.

## Requisitos

**Para desarrollo local**
- .NET SDK 10
- PostgreSQL en `localhost:5432`
- VS Code con la extensión **C# Dev Kit** (y opcional **REST Client** para el archivo `.http`)

**Para desplegar**
- Docker y Docker Compose

## Estructura

```
Models/            → entidades de EF Core
Dtos/              → contratos de entrada y salida de la API
Data/AppDbContext.cs → DbContext + seeding (HasData)
Migrations/        → migración generada con el seed
Program.cs         → configuración, pipeline y endpoints
Dockerfile         → build multi-etapa (SDK → runtime alpine, usuario no root)
docker-compose.yml → API + PostgreSQL con volumen persistente
.env.example       → plantilla de variables de entorno
TiendaInformatica.http → peticiones de prueba
```

## Desarrollo local

1. Poné la contraseña de tu usuario `postgres` en `appsettings.Development.json`
   (`ConnectionStrings:DefaultConnection`). Ese archivo es solo para desarrollo local.
2. Ejecutá la API:

   ```bash
   dotnet run
   ```

   Al arrancar se aplican las migraciones pendientes y el seed.
   También podés hacerlo a mano con `dotnet ef database update`.
3. Probá `http://localhost:5020/productos` o abrí `TiendaInformatica.http`.

## Despliegue con Docker

### 1. Preparar el entorno

```bash
cp .env.example .env
# Editar .env y poner una POSTGRES_PASSWORD segura
```

> La password se usa en la cadena de conexión, así que conviene que sea larga y
> **sin** caracteres especiales (`;`, `=`, comillas, `$`).

### 2. Levantar todo

```bash
docker compose up -d --build
```

Esto crea:
- `db`: PostgreSQL 17 con volumen `pgdata` (los datos sobreviven a los reinicios).
- `api`: la API .NET, que espera a que la base esté sana y aplica las migraciones.

La API queda publicada en el puerto `API_PORT` (por defecto `5001`) del host.
El puerto `8080` queda reservado para el panel administrativo.

### 3. Verificar

```bash
docker compose ps
curl http://localhost:5001/health      # {"status":"healthy","database":"up"}
curl http://localhost:5001/productos
```

### Comandos útiles

```bash
docker compose logs -f api       # logs de la API
docker compose logs -f db        # logs de PostgreSQL
docker compose restart api       # reiniciar solo la API
docker compose down              # detener (los datos se conservan en el volumen)
docker compose down -v           # detener y BORRAR la base de datos
```

### Backup y restauración

```bash
# Backup
docker compose exec -T db pg_dump -U tienda -d tiendainformatica > backup.sql

# Restaurar
cat backup.sql | docker compose exec -T db psql -U tienda -d tiendainformatica
```

## Variables de entorno

| Variable | Descripción | Default |
|---|---|---|
| `POSTGRES_DB` | Nombre de la base | `tiendainformatica` |
| `POSTGRES_USER` | Usuario de PostgreSQL | `tienda` |
| `POSTGRES_PASSWORD` | Password (obligatoria) | — |
| `API_PORT` | Puerto del host para la API de productos | `5001` |
| `CORS_ALLOWED_ORIGINS` | Orígenes permitidos, separados por coma. Vacío = todos | — |
| `ConnectionStrings__DefaultConnection` | Cadena de conexión completa (alternativa) | — |
| `Database__ApplyMigrationsOnStartup` | Aplicar migraciones al arrancar | `true` |

La configuración de .NET también se puede sobrescribir con variables de entorno usando
doble guion bajo, por ejemplo `Cors__AllowedOrigins=https://midominio.com`.

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/health` | Estado de la API y de la base |
| GET | `/categorias` | Lista de categorías con su número de productos |
| GET | `/categorias/{id}` | Una categoría |
| GET | `/categorias/{id}/productos` | Productos de una categoría |
| POST | `/categorias` | Crear categoría |
| GET | `/productos` | Lista de productos |
| GET | `/productos/{id}` | Un producto |
| POST | `/productos` | Crear producto |
| PUT | `/productos/{id}` | Actualizar |
| DELETE | `/productos/{id}` | Eliminar |

Documento OpenAPI: `/openapi/v1.json`

Los errores se devuelven como `ProblemDetails` (RFC 7807). Las validaciones de entrada
responden `400` con `{ "error": "..." }`.

## Datos del seed

- Categorías: Laptops, Periféricos, Componentes
- 6 productos (notebooks, teclado, mouse, SSD, RAM)

## Cambiar los datos del seed

1. Editá `HasData(...)` en `Data/AppDbContext.cs`.
2. `dotnet ef migrations add CambioSeed`
3. `dotnet run` (o `docker compose up -d --build` en el servidor).

## Notas de seguridad

- No hay credenciales en el repositorio: la cadena de conexión se inyecta por variable
  de entorno y `appsettings.Development.json` solo aplica en desarrollo local.
- El contenedor de la API corre con un usuario no root.
- PostgreSQL no publica puertos al host: solo es accesible desde la red interna de compose.
- En un VPS conviene poner un reverse proxy (Caddy/Nginx) adelante para servir HTTPS y
  cerrar el puerto de la API con el firewall.
