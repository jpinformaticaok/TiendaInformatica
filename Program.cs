using Microsoft.EntityFrameworkCore;
using TiendaInformatica.Data;
using TiendaInformatica.Models;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL con EF Core (cadena de conexión en appsettings.json)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // http://localhost:5020/openapi/v1.json

    // Aplica las migraciones pendientes (y por lo tanto el seeding) al arrancar.
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

// ---------- Categorías ----------
var categorias = app.MapGroup("/categorias");

categorias.MapGet("/", async (AppDbContext db) =>
    await db.Categorias
        .OrderBy(c => c.Id)
        .Select(c => new { c.Id, c.Nombre, c.Descripcion, TotalProductos = c.Productos.Count })
        .ToListAsync());

categorias.MapGet("/{id:int}", async (int id, AppDbContext db) =>
    await db.Categorias.FindAsync(id) is Categoria c ? Results.Ok(c) : Results.NotFound());

categorias.MapGet("/{id:int}/productos", async (int id, AppDbContext db) =>
    await db.Productos
        .Where(p => p.CategoriaId == id)
        .OrderBy(p => p.Id)
        .Select(p => new { p.Id, p.Nombre, p.Marca, p.Precio, p.Stock })
        .ToListAsync());

categorias.MapPost("/", async (Categoria categoria, AppDbContext db) =>
{
    db.Categorias.Add(categoria);
    await db.SaveChangesAsync();
    return Results.Created($"/categorias/{categoria.Id}", categoria);
});

// ---------- Productos ----------
var productos = app.MapGroup("/productos");

productos.MapGet("/", async (AppDbContext db) =>
    await db.Productos
        .OrderBy(p => p.Id)
        .Select(p => new { p.Id, p.Nombre, p.Marca, p.Precio, p.Stock, p.CategoriaId, Categoria = p.Categoria!.Nombre })
        .ToListAsync());

productos.MapGet("/{id:int}", async (int id, AppDbContext db) =>
    await db.Productos.FindAsync(id) is Producto p ? Results.Ok(p) : Results.NotFound());

productos.MapPost("/", async (Producto producto, AppDbContext db) =>
{
    if (!await db.Categorias.AnyAsync(c => c.Id == producto.CategoriaId))
        return Results.BadRequest($"No existe la categoría {producto.CategoriaId}");

    db.Productos.Add(producto);
    await db.SaveChangesAsync();
    return Results.Created($"/productos/{producto.Id}", producto);
});

productos.MapPut("/{id:int}", async (int id, Producto datos, AppDbContext db) =>
{
    var p = await db.Productos.FindAsync(id);
    if (p is null) return Results.NotFound();

    p.Nombre = datos.Nombre;
    p.Marca = datos.Marca;
    p.Precio = datos.Precio;
    p.Stock = datos.Stock;
    p.CategoriaId = datos.CategoriaId;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

productos.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
{
    var p = await db.Productos.FindAsync(id);
    if (p is null) return Results.NotFound();

    db.Productos.Remove(p);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
