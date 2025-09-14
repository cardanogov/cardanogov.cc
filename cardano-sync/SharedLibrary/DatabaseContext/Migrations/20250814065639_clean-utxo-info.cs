using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedLibrary.DatabaseContext.Migrations
{
    /// <inheritdoc />
    public partial class cleanutxoinfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "address",
                table: "md_utxo_info");

            migrationBuilder.DropColumn(
                name: "asset_list",
                table: "md_utxo_info");

            migrationBuilder.DropColumn(
                name: "block_height",
                table: "md_utxo_info");

            migrationBuilder.DropColumn(
                name: "datum_hash",
                table: "md_utxo_info");

            migrationBuilder.DropColumn(
                name: "inline_datum",
                table: "md_utxo_info");

            migrationBuilder.DropColumn(
                name: "is_spent",
                table: "md_utxo_info");

            migrationBuilder.DropColumn(
                name: "payment_cred",
                table: "md_utxo_info");

            migrationBuilder.DropColumn(
                name: "reference_script",
                table: "md_utxo_info");

            migrationBuilder.DropColumn(
                name: "value",
                table: "md_utxo_info");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "md_utxo_info",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "asset_list",
                table: "md_utxo_info",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "block_height",
                table: "md_utxo_info",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "datum_hash",
                table: "md_utxo_info",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "inline_datum",
                table: "md_utxo_info",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_spent",
                table: "md_utxo_info",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_cred",
                table: "md_utxo_info",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_script",
                table: "md_utxo_info",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "value",
                table: "md_utxo_info",
                type: "text",
                nullable: true);
        }
    }
}
