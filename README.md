# TiendaInformatica – API mínima con ASP.NET Core + PostgreSQL

API mínima (.NET 10) de una tienda de productos informáticos, con Entity Framework Core, PostgreSQL y datos iniciales (seeding).

## Requisitos
- .NET SDK 10
- PostgreSQL en `localhost:5432`
- Herramienta EF: `dotnet tool install --global dotnet-ef`
- VS Code con la extensión **C# Dev Kit** (y opcional **REST Client** para el archivo `.http`)

## Estructura
```
Models/Categoria.cs, Models/Producto.cs → entidades
Data/AppDbContext.cs                    → DbContext + seeding (HasData)
Migrations/                             → migración generada con el seed
Program.cs                              → configuración y endpoints
TiendaInformatica.http                  → peticiones de prueba
```

## Pasos
1. Pon la contraseña de tu usuario `postgres` en `appsettings.Development.json` → `ConnectionStrings:DefaultConnection`.
2. Ejecuta la API:
   ```
   dotnet run
   ```
   En Development, la API aplica sola las migraciones al arrancar: crea la base `tiendainformatica`, las tablas y los datos del seed.
   (También puedes hacerlo a mano con `dotnet ef database update`.)
3. Prueba: http://localhost:5020/productos — o abre `TiendaInformatica.http` y pulsa "Send Request".
4. En VS Code también puedes pulsar **F5** (configuración "API (.NET)").

## Endpoints
| Método | Ruta                       | Descripción |
|--------|----------------------------|-------------|
| GET    | /categorias                | Lista de categorías con su número de productos |
| GET    | /categorias/{id}           | Una categoría |
| GET    | /categorias/{id}/productos | Productos de una categoría |
| POST   | /categorias                | Crear categoría |
| GET    | /productos                 | Lista de productos |
| GET    | /productos/{id}            | Un producto |
| POST   | /productos                 | Crear producto |
| PUT    | /productos/{id}            | Actualizar |
| DELETE | /productos/{id}            | Eliminar |

Documento OpenAPI: http://localhost:5020/openapi/v1.json

## Datos del seed
- Categorías: Laptops, Periféricos, Componentes
- 6 productos (notebooks, teclado, mouse, SSD, RAM)

## Cambiar los datos del seed
1. Edita `HasData(...)` en `Data/AppDbContext.cs`.
2. `dotnet ef migrations add CambioSeed`
3. `dotnet run` (o `dotnet ef database update`).
