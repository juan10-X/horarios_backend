using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEstructuraHorasYCarrerasSedes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoSesion",
                table: "Horarios",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HorasEvaluacionTotal",
                table: "Cursos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HorasTallerTotal",
                table: "Cursos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HorasTeoriaTotal",
                table: "Cursos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HorasVirtualTotal",
                table: "Cursos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Materia",
                table: "Cursos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalHoras",
                table: "Cursos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SedeCarreras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SedeId = table.Column<int>(type: "int", nullable: false),
                    CarreraId = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaAsignacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SedeCarreras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SedeCarreras_Carreras_CarreraId",
                        column: x => x.CarreraId,
                        principalTable: "Carreras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SedeCarreras_Sedes_SedeId",
                        column: x => x.SedeId,
                        principalTable: "Sedes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SedeCarreras_CarreraId",
                table: "SedeCarreras",
                column: "CarreraId");

            migrationBuilder.CreateIndex(
                name: "IX_SedeCarreras_SedeId_CarreraId",
                table: "SedeCarreras",
                columns: new[] { "SedeId", "CarreraId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SedeCarreras");

            migrationBuilder.DropColumn(
                name: "TipoSesion",
                table: "Horarios");

            migrationBuilder.DropColumn(
                name: "HorasEvaluacionTotal",
                table: "Cursos");

            migrationBuilder.DropColumn(
                name: "HorasTallerTotal",
                table: "Cursos");

            migrationBuilder.DropColumn(
                name: "HorasTeoriaTotal",
                table: "Cursos");

            migrationBuilder.DropColumn(
                name: "HorasVirtualTotal",
                table: "Cursos");

            migrationBuilder.DropColumn(
                name: "Materia",
                table: "Cursos");

            migrationBuilder.DropColumn(
                name: "TotalHoras",
                table: "Cursos");
        }
    }
}
