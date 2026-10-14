using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarGruposNrcYDocenteSede : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GrupoNrcId",
                table: "Horarios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SedeId",
                table: "Docentes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GruposNrc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Turno = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AforoMaximo = table.Column<int>(type: "int", nullable: false),
                    Periodo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SedeId = table.Column<int>(type: "int", nullable: false),
                    CarreraId = table.Column<int>(type: "int", nullable: false),
                    CicloId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposNrc", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GruposNrc_Carreras_CarreraId",
                        column: x => x.CarreraId,
                        principalTable: "Carreras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GruposNrc_Ciclos_CicloId",
                        column: x => x.CicloId,
                        principalTable: "Ciclos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GruposNrc_Sedes_SedeId",
                        column: x => x.SedeId,
                        principalTable: "Sedes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Horarios_GrupoNrcId",
                table: "Horarios",
                column: "GrupoNrcId");

            migrationBuilder.CreateIndex(
                name: "IX_Docentes_SedeId",
                table: "Docentes",
                column: "SedeId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposNrc_CarreraId",
                table: "GruposNrc",
                column: "CarreraId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposNrc_CicloId",
                table: "GruposNrc",
                column: "CicloId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposNrc_Codigo",
                table: "GruposNrc",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GruposNrc_SedeId",
                table: "GruposNrc",
                column: "SedeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Docentes_Sedes_SedeId",
                table: "Docentes",
                column: "SedeId",
                principalTable: "Sedes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Horarios_GruposNrc_GrupoNrcId",
                table: "Horarios",
                column: "GrupoNrcId",
                principalTable: "GruposNrc",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Docentes_Sedes_SedeId",
                table: "Docentes");

            migrationBuilder.DropForeignKey(
                name: "FK_Horarios_GruposNrc_GrupoNrcId",
                table: "Horarios");

            migrationBuilder.DropTable(
                name: "GruposNrc");

            migrationBuilder.DropIndex(
                name: "IX_Horarios_GrupoNrcId",
                table: "Horarios");

            migrationBuilder.DropIndex(
                name: "IX_Docentes_SedeId",
                table: "Docentes");

            migrationBuilder.DropColumn(
                name: "GrupoNrcId",
                table: "Horarios");

            migrationBuilder.DropColumn(
                name: "SedeId",
                table: "Docentes");
        }
    }
}
