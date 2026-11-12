namespace TiendaInformatica.Dtos;

/// <summary>Datos que acepta la API para crear o actualizar un producto.</summary>
public sealed record ProductoInput(string Nombre, string Marca, decimal Precio, int Stock, int CategoriaId);

/// <summary>Producto con el nombre de su categoría.</summary>
public sealed record ProductoOutput(
    int Id,
    string Nombre,
    string Marca,
    decimal Precio,
    int Stock,
    int CategoriaId,
    string? Categoria);

/// <summary>Vista reducida de un producto (listado por categoría).</summary>
public sealed record ProductoResumen(int Id, string Nombre, string Marca, decimal Precio, int Stock);
