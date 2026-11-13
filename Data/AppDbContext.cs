using Microsoft.EntityFrameworkCore;
using TiendaInformatica.Models;

namespace TiendaInformatica.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Producto> Productos => Set<Producto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categoria>(e =>
        {
            e.Property(c => c.Nombre).HasMaxLength(100);
            e.Property(c => c.Descripcion).HasMaxLength(250);
        });

        modelBuilder.Entity<Marca>(e =>
        {
            e.Property(m => m.Nombre).HasMaxLength(80);
            e.HasIndex(m => m.Nombre).IsUnique();
        });

        modelBuilder.Entity<Producto>(e =>
        {
            e.Property(p => p.Nombre).HasMaxLength(150);
            e.Property(p => p.Precio).HasPrecision(12, 2);

            // Una marca tiene muchos productos; no se puede borrar una marca en uso.
            e.HasOne(p => p.Marca)
                .WithMany(m => m.Productos)
                .HasForeignKey(p => p.MarcaId)
                .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<Marca>().HasData(
            new Marca { Id = 1, Nombre = "Lenovo" },
            new Marca { Id = 2, Nombre = "ASUS" },
            new Marca { Id = 3, Nombre = "Redragon" },
            new Marca { Id = 4, Nombre = "Logitech" },
            new Marca { Id = 5, Nombre = "Kingston" },
            new Marca { Id = 6, Nombre = "Corsair" }
        );

        modelBuilder.Entity<Producto>().HasData(
            new Producto { Id = 1, Nombre = "Notebook 15.6\" i5 16GB", MarcaId = 1, Precio = 850000m, Stock = 5,  CategoriaId = 1 },
            new Producto { Id = 2, Nombre = "Notebook 14\" Ryzen 7",   MarcaId = 2, Precio = 990000m, Stock = 3,  CategoriaId = 1 },
            new Producto { Id = 3, Nombre = "Teclado mecánico",        MarcaId = 3, Precio = 45000m,  Stock = 20, CategoriaId = 2 },
            new Producto { Id = 4, Nombre = "Mouse inalámbrico",       MarcaId = 4, Precio = 25000m,  Stock = 35, CategoriaId = 2 },
            new Producto { Id = 5, Nombre = "SSD NVMe 1TB",            MarcaId = 5, Precio = 70000m,  Stock = 15, CategoriaId = 3 },
            new Producto { Id = 6, Nombre = "Memoria RAM 16GB DDR4",   MarcaId = 6, Precio = 48000m,  Stock = 12, CategoriaId = 3 }
        );
    }
}
