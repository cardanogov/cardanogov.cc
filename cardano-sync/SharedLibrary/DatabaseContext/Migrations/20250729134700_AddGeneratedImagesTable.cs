using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedLibrary.DatabaseContext.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneratedImagesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "generated_image",
                columns: table => new
                {
                    Text = table.Column<string>(type: "text", nullable: false),
                    subtext = table.Column<string>(type: "text", nullable: false),
                    image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    cache_key = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "generated_image");
        }
    }
}
