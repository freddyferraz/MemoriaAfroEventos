using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechFlow.Eventos.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaFotoEvento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TemFoto",
                table: "Eventos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Fotos",
                columns: table => new
                {
                    EventoId = table.Column<int>(type: "integer", nullable: false),
                    Dados = table.Column<byte[]>(type: "bytea", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fotos", x => x.EventoId);
                    table.ForeignKey(
                        name: "FK_Fotos_Eventos_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fotos");

            migrationBuilder.DropColumn(
                name: "TemFoto",
                table: "Eventos");
        }
    }
}
