using Microsoft.EntityFrameworkCore;
using TiendaInformatica.Data;
using TiendaInformatica.Dtos;
using TiendaInformatica.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuración ----------
// La cadena de conexión NO vive en appsettings.json: se inyecta por variable de entorno
// (ConnectionStrings__DefaultConnection). En local se toma de appsettings.Development.json.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Falta 'ConnectionStrings:DefaultConnection'. Definila con la variable de entorno " +
        "ConnectionStrings__DefaultConnection (ver README).");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// CORS: orígenes permitidos separados por coma en Cors__AllowedOrigins.
// Si no se configura ninguno, se permite cualquier origen (API pública sin cookies).
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    }
    else
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    }
}));

var app = builder.Build();

// ---------- Pipeline ----------
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();

// Documento OpenAPI: /openapi/v1.json (también en producción).
app.MapOpenApi();

// ---------- Migraciones ----------
// Se aplican al arrancar (en cualquier entorno) con reintentos, porque al levantar
// con docker compose la base puede tardar unos segundos en aceptar conexiones.
if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
{
    await ApplyMigrationsAsync(app);
}

// ---------- Health ----------
app.MapGet("/health", async (AppDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "healthy", database = "up" })
        : Results.Json(new { status = "unhealthy", database = "down" },
            statusCode: StatusCodes.Status503ServiceUnavailable));

// ---------- Categorías ----------
var categorias = app.MapGroup("/categorias");

categorias.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
    await db.Categorias
        .AsNoTracking()
        .OrderBy(c => c.Id)
        .Select(c => new CategoriaOutput(c.Id, c.Nombre, c.Descripcion, c.Productos.Count))
        .ToListAsync(ct));

categorias.MapGet("/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
    await db.Categorias
        .AsNoTracking()
        .Where(c => c.Id == id)
        .Select(c => new CategoriaOutput(c.Id, c.Nombre, c.Descripcion, c.Productos.Count))
        .FirstOrDefaultAsync(ct) is CategoriaOutput categoria
        ? Results.Ok(categoria)
        : Results.NotFound());

categorias.MapGet("/{id:int}/productos", async (int id, AppDbContext db, CancellationToken ct) =>
    await db.Productos
        .AsNoTracking()
        .Where(p => p.CategoriaId == id)
        .OrderBy(p => p.Id)
        .Select(p => new ProductoResumen(p.Id, p.Nombre, p.MarcaId, p.Marca!.Nombre, p.Precio, p.Stock))
        .ToListAsync(ct));

categorias.MapPost("/", async (CategoriaInput input, AppDbContext db, CancellationToken ct) =>
{
    var error = ValidarCategoria(input);
    if (error is not null) return Results.BadRequest(new { error });

    var categoria = new Categoria
    {
        Nombre = input.Nombre.Trim(),
        Descripcion = string.IsNullOrWhiteSpace(input.Descripcion) ? null : input.Descripcion.Trim()
    };

    db.Categorias.Add(categoria);
    await db.SaveChangesAsync(ct);

    return Results.Created(
        $"/categorias/{categoria.Id}",
        new CategoriaOutput(categoria.Id, categoria.Nombre, categoria.Descripcion, 0));
});

// ---------- Marcas ----------
var marcas = app.MapGroup("/marcas");

marcas.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
    await db.Marcas
        .AsNoTracking()
        .OrderBy(m => m.Nombre)
        .Select(m => new MarcaOutput(m.Id, m.Nombre, m.Productos.Count))
        .ToListAsync(ct));

marcas.MapPost("/", async (MarcaInput input, AppDbContext db, CancellationToken ct) =>
{
    var error = ValidarMarca(input);
    if (error is not null) return Results.BadRequest(new { error });

    var nombre = input.Nombre.Trim();

    if (await db.Marcas.AnyAsync(m => m.Nombre == nombre, ct))
        return Results.Conflict(new { error = $"Ya existe la marca '{nombre}'." });

    var marca = new Marca { Nombre = nombre };
    db.Marcas.Add(marca);
    await db.SaveChangesAsync(ct);

    return Results.Created($"/marcas/{marca.Id}", new MarcaOutput(marca.Id, marca.Nombre, 0));
});

// ---------- Productos ----------
var productos = app.MapGroup("/productos");

productos.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
    await db.Productos
        .AsNoTracking()
        .OrderBy(p => p.Id)
        .Select(p => new ProductoOutput(
            p.Id, p.Nombre, p.MarcaId, p.Marca!.Nombre, p.Precio, p.Stock, p.CategoriaId, p.Categoria!.Nombre))
        .ToListAsync(ct));

productos.MapGet("/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
    await db.Productos
        .AsNoTracking()
        .Where(p => p.Id == id)
        .Select(p => new ProductoOutput(
            p.Id, p.Nombre, p.MarcaId, p.Marca!.Nombre, p.Precio, p.Stock, p.CategoriaId, p.Categoria!.Nombre))
        .FirstOrDefaultAsync(ct) is ProductoOutput producto
        ? Results.Ok(producto)
        : Results.NotFound());

productos.MapPost("/", async (ProductoInput input, AppDbContext db, CancellationToken ct) =>
{
    var error = ValidarProducto(input);
    if (error is not null) return Results.BadRequest(new { error });

    var nombreCategoria = await db.Categorias
        .Where(c => c.Id == input.CategoriaId)
        .Select(c => c.Nombre)
        .FirstOrDefaultAsync(ct);

    if (nombreCategoria is null)
        return Results.BadRequest(new { error = $"No existe la categoría {input.CategoriaId}." });

    var nombreMarca = await db.Marcas
        .Where(m => m.Id == input.MarcaId)
        .Select(m => m.Nombre)
        .FirstOrDefaultAsync(ct);

    if (nombreMarca is null)
        return Results.BadRequest(new { error = $"No existe la marca {input.MarcaId}." });

    var producto = new Producto
    {
        Nombre = input.Nombre.Trim(),
        MarcaId = input.MarcaId,
        Precio = input.Precio,
        Stock = input.Stock,
        CategoriaId = input.CategoriaId
    };

    db.Productos.Add(producto);
    await db.SaveChangesAsync(ct);

    return Results.Created(
        $"/productos/{producto.Id}",
        new ProductoOutput(producto.Id, producto.Nombre, producto.MarcaId, nombreMarca,
            producto.Precio, producto.Stock, producto.CategoriaId, nombreCategoria));
});

productos.MapPut("/{id:int}", async (int id, ProductoInput input, AppDbContext db, CancellationToken ct) =>
{
    var error = ValidarProducto(input);
    if (error is not null) return Results.BadRequest(new { error });

    var producto = await db.Productos.FindAsync([id], ct);
    if (producto is null) return Results.NotFound();

    if (!await db.Categorias.AnyAsync(c => c.Id == input.CategoriaId, ct))
        return Results.BadRequest(new { error = $"No existe la categoría {input.CategoriaId}." });

    if (!await db.Marcas.AnyAsync(m => m.Id == input.MarcaId, ct))
        return Results.BadRequest(new { error = $"No existe la marca {input.MarcaId}." });

    producto.Nombre = input.Nombre.Trim();
    producto.MarcaId = input.MarcaId;
    producto.Precio = input.Precio;
    producto.Stock = input.Stock;
    producto.CategoriaId = input.CategoriaId;

    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

productos.MapDelete("/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
{
    var producto = await db.Productos.FindAsync([id], ct);
    if (producto is null) return Results.NotFound();

    db.Productos.Remove(producto);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

app.Run();

// ---------- Helpers ----------

static string? ValidarCategoria(CategoriaInput input)
{
    if (string.IsNullOrWhiteSpace(input.Nombre))
        return "El nombre es obligatorio.";
    if (input.Nombre.Trim().Length > 100)
        return "El nombre no puede superar los 100 caracteres.";
    if (input.Descripcion?.Trim().Length > 250)
        return "La descripción no puede superar los 250 caracteres.";

    return null;
}

static string? ValidarMarca(MarcaInput input)
{
    if (string.IsNullOrWhiteSpace(input.Nombre))
        return "El nombre de la marca es obligatorio.";
    if (input.Nombre.Trim().Length > 80)
        return "El nombre de la marca no puede superar los 80 caracteres.";

    return null;
}

static string? ValidarProducto(ProductoInput input)
{
    if (string.IsNullOrWhiteSpace(input.Nombre))
        return "El nombre es obligatorio.";
    if (input.Nombre.Trim().Length > 150)
        return "El nombre no puede superar los 150 caracteres.";
    if (input.MarcaId <= 0)
        return "La marca es obligatoria.";
    if (input.Precio < 0)
        return "El precio no puede ser negativo.";
    if (input.Stock < 0)
        return "El stock no puede ser negativo.";

    return null;
}

static async Task ApplyMigrationsAsync(WebApplication app)
{
    const int maxAttempts = 10;

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            app.Logger.LogInformation("Migraciones aplicadas correctamente.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            var seconds = Math.Min(30, (int)Math.Pow(2, attempt));
            app.Logger.LogWarning(ex,
                "No se pudieron aplicar las migraciones (intento {Attempt}/{Max}). Reintentando en {Seconds}s.",
                attempt, maxAttempts, seconds);
            await Task.Delay(TimeSpan.FromSeconds(seconds));
        }
    }
}
