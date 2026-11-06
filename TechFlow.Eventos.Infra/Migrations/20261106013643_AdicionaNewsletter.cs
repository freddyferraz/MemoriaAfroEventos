using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechFlow.Eventos.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaNewsletter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assinantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Categorias = table.Column<int[]>(type: "integer[]", nullable: false),
                    Cidades = table.Column<string[]>(type: "text[]", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Confirmado = table.Column<bool>(type: "boolean", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataAceite = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmacaoEnviadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataConfirmacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assinantes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnviosNewsletter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventoId = table.Column<int>(type: "integer", nullable: false),
                    AssinanteId = table.Column<int>(type: "integer", nullable: false),
                    EnviadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnviosNewsletter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnviosNewsletter_Assinantes_AssinanteId",
                        column: x => x.AssinanteId,
                        principalTable: "Assinantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnviosNewsletter_Eventos_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assinantes_Email",
                table: "Assinantes",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assinantes_Token",
                table: "Assinantes",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnviosNewsletter_AssinanteId",
                table: "EnviosNewsletter",
                column: "AssinanteId");

            migrationBuilder.CreateIndex(
                name: "IX_EnviosNewsletter_EventoId_AssinanteId",
                table: "EnviosNewsletter",
                columns: new[] { "EventoId", "AssinanteId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnviosNewsletter");

            migrationBuilder.DropTable(
                name: "Assinantes");
        }
    }
}
