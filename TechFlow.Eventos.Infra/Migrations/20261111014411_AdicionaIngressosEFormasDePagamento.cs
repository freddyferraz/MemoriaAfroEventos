using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechFlow.Eventos.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaIngressosEFormasDePagamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoBoleto",
                table: "Pagamentos",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoPix",
                table: "Pagamentos",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalCartao",
                table: "Pagamentos",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Metodo",
                table: "Pagamentos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Parcelas",
                table: "Pagamentos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "VenceEm",
                table: "Pagamentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComprasIngresso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventoId = table.Column<int>(type: "integer", nullable: false),
                    CompradorId = table.Column<string>(type: "text", nullable: false),
                    CompradorNome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprasIngresso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComprasIngresso_AspNetUsers_CompradorId",
                        column: x => x.CompradorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComprasIngresso_Eventos_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComprasIngresso_Codigo",
                table: "ComprasIngresso",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComprasIngresso_CompradorId",
                table: "ComprasIngresso",
                column: "CompradorId");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasIngresso_EventoId",
                table: "ComprasIngresso",
                column: "EventoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComprasIngresso");

            migrationBuilder.DropColumn(
                name: "CodigoBoleto",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "CodigoPix",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "FinalCartao",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "Metodo",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "Parcelas",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "VenceEm",
                table: "Pagamentos");
        }
    }
}
