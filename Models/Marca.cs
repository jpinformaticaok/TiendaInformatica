namespace TiendaInformatica.Models;

public class Marca
{
    public int Id { get; set; }
    public required string Nombre { get; set; }

    public List<Producto> Productos { get; set; } = [];
}
