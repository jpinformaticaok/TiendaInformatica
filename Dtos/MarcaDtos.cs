namespace TiendaInformatica.Dtos;

/// <summary>Datos que acepta la API para crear una marca.</summary>
public sealed record MarcaInput(string Nombre);

/// <summary>Marca con la cantidad de productos que la usan.</summary>
public sealed record MarcaOutput(int Id, string Nombre, int TotalProductos);
