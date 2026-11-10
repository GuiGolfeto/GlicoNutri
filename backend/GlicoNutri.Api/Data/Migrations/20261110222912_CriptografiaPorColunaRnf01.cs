using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlicoNutri.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CriptografiaPorColunaRnf01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RNF01 — só o schema muda aqui: tipo de coluna (cifrado precisa de
            // mais espaço que o campo em claro) e o hash de busca do CPF. Os
            // dados que já existem continuam em texto puro até a API subir; quem
            // cifra e preenche o cpf_hash é o BackfillCriptografia, no start,
            // porque reproduzir o mesmo AES-GCM em SQL puro seria arriscado.
            migrationBuilder.DropIndex(
                name: "ix_usuarios_cpf",
                table: "usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "telefone",
                table: "usuarios",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cpf",
                table: "usuarios",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(14)",
                oldMaxLength: 14,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cpf_hash",
                table: "usuarios",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_cpf_hash",
                table: "usuarios",
                column: "cpf_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_usuarios_cpf_hash",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "cpf_hash",
                table: "usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "telefone",
                table: "usuarios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cpf",
                table: "usuarios",
                type: "character varying(14)",
                maxLength: 14,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_cpf",
                table: "usuarios",
                column: "cpf",
                unique: true);
        }
    }
}
