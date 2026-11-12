namespace TiendaInformatica.Dtos;

/// <summary>Datos que acepta la API para crear una categoría.</summary>
public sealed record CategoriaInput(string Nombre, string? Descripcion);

/// <summary>Datos que devuelve la API para una categoría.</summary>
public sealed record CategoriaOutput(int Id, string Nombre, string? Descripcion, int TotalProductos);
