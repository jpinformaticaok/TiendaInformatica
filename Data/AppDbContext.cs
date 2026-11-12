using Microsoft.EntityFrameworkCore;
using TiendaInformatica.Models;

namespace TiendaInformatica.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categoria>(e =>
        {
            e.Property(c => c.Nombre).HasMaxLength(100);
            e.Property(c => c.Descripcion).HasMaxLength(250);
        });

        modelBuilder.Entity<Producto>(e =>
        {
            e.Property(p => p.Nombre).HasMaxLength(150);
            e.Property(p => p.Marca).HasMaxLength(80);
            e.Property(p => p.Precio).HasPrecision(12, 2);
        });

        // ---------- Seeding de datos ----------
        // HasData guarda estos registros dentro de la migración,
        // así se insertan al ejecutar "dotnet ef database update".
        // Los Id deben ir escritos a mano.
        modelBuilder.Entity<Categoria>().HasData(
            new Categoria { Id = 1, Nombre = "Laptops",     Descripcion = "Computadoras portátiles" },
            new Categoria { Id = 2, Nombre = "Periféricos", Descripcion = "Teclados, mouse, monitores" },
            new Categoria { Id = 3, Nombre = "Componentes", Descripcion = "Piezas para armar o mejorar una PC" }
        );

        modelBuilder.Entity<Producto>().HasData(
            new Producto { Id = 1, Nombre = "Notebook 15.6\" i5 16GB", Marca = "Lenovo",   Precio = 850000m, Stock = 5,  CategoriaId = 1 },
            new Producto { Id = 2, Nombre = "Notebook 14\" Ryzen 7",   Marca = "ASUS",     Precio = 990000m, Stock = 3,  CategoriaId = 1 },
            new Producto { Id = 3, Nombre = "Teclado mecánico",        Marca = "Redragon", Precio = 45000m,  Stock = 20, CategoriaId = 2 },
            new Producto { Id = 4, Nombre = "Mouse inalámbrico",       Marca = "Logitech", Precio = 25000m,  Stock = 35, CategoriaId = 2 },
            new Producto { Id = 5, Nombre = "SSD NVMe 1TB",            Marca = "Kingston", Precio = 70000m,  Stock = 15, CategoriaId = 3 },
            new Producto { Id = 6, Nombre = "Memoria RAM 16GB DDR4",   Marca = "Corsair",  Precio = 48000m,  Stock = 12, CategoriaId = 3 }
        );
    }
}
