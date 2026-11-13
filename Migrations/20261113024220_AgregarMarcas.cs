using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TiendaInformatica.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMarcas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Crear la tabla de marcas con su índice único.
            migrationBuilder.CreateTable(
                name: "Marcas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marcas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_Nombre",
                table: "Marcas",
                column: "Nombre",
                unique: true);

            // 2. Datos semilla de marcas.
            migrationBuilder.InsertData(
                table: "Marcas",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 1, "Lenovo" },
                    { 2, "ASUS" },
                    { 3, "Redragon" },
                    { 4, "Logitech" },
                    { 5, "Kingston" },
                    { 6, "Corsair" }
                });

            // 3. Ajustar la secuencia de identidad tras insertar Ids explícitos
            //    y agregar marcas que ya existan en productos fuera del seed.
            migrationBuilder.Sql("""
                SELECT setval(
                    pg_get_serial_sequence('"Marcas"', 'Id'),
                    (SELECT COALESCE(MAX("Id"), 1) FROM "Marcas"));
                """);

            migrationBuilder.Sql("""
                INSERT INTO "Marcas" ("Nombre")
                SELECT DISTINCT TRIM(p."Marca")
                FROM "Productos" p
                WHERE TRIM(p."Marca") <> ''
                  AND NOT EXISTS (
                      SELECT 1 FROM "Marcas" m WHERE m."Nombre" = TRIM(p."Marca")
                  );
                """);

            // 4. Agregar MarcaId nullable y rellenarlo desde el nombre viejo.
            migrationBuilder.AddColumn<int>(
                name: "MarcaId",
                table: "Productos",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Productos" p
                SET "MarcaId" = m."Id"
                FROM "Marcas" m
                WHERE m."Nombre" = TRIM(p."Marca");
                """);

            // 5. Red de seguridad: productos con marca vacía pasan a "Sin marca".
            migrationBuilder.Sql("""
                INSERT INTO "Marcas" ("Nombre")
                SELECT 'Sin marca'
                WHERE EXISTS (SELECT 1 FROM "Productos" WHERE "MarcaId" IS NULL)
                  AND NOT EXISTS (SELECT 1 FROM "Marcas" WHERE "Nombre" = 'Sin marca');
                """);

            migrationBuilder.Sql("""
                UPDATE "Productos"
                SET "MarcaId" = (SELECT "Id" FROM "Marcas" WHERE "Nombre" = 'Sin marca')
                WHERE "MarcaId" IS NULL;
                """);

            // 6. MarcaId obligatorio y eliminación de la columna vieja.
            migrationBuilder.AlterColumn<int>(
                name: "MarcaId",
                table: "Productos",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Marca",
                table: "Productos");

            // 7. Índice y clave foránea.
            migrationBuilder.CreateIndex(
                name: "IX_Productos_MarcaId",
                table: "Productos",
                column: "MarcaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Marcas_MarcaId",
                table: "Productos",
                column: "MarcaId",
                principalTable: "Marcas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restaurar la columna de texto y copiar el nombre de la marca.
            migrationBuilder.AddColumn<string>(
                name: "Marca",
                table: "Productos",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Productos" p
                SET "Marca" = m."Nombre"
                FROM "Marcas" m
                WHERE m."Id" = p."MarcaId";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Marcas_MarcaId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_MarcaId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "MarcaId",
                table: "Productos");

            migrationBuilder.DropTable(
                name: "Marcas");

            migrationBuilder.AlterColumn<string>(
                name: "Marca",
                table: "Productos",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80,
                oldNullable: true);
        }
    }
}
