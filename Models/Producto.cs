namespace TiendaInformatica.Models;

public class Producto
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public required string Marca { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }

    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
}
