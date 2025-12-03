using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatforIdFieldInOutward : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlatformId",
                table: "Outwards",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlatformId",
                table: "Outwards");
        }
    }
}
