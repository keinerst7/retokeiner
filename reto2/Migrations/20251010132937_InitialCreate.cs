using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace reto2.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reto_keiner");

            migrationBuilder.CreateTable(
                name: "recaudos",
                schema: "reto_keiner",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Estacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sentido = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Hora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValorTabulado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recaudos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recaudos",
                schema: "reto_keiner");
        }
    }
}
