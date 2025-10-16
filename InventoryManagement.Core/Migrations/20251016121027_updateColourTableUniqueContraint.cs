using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class updateColourTableUniqueContraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Colour_Name_Code",
                table: "Colour");

            migrationBuilder.CreateIndex(
                name: "IX_Colour_Name_Code",
                table: "Colour",
                columns: new[] { "Name", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Colour_Name_Code",
                table: "Colour");

            migrationBuilder.CreateIndex(
                name: "IX_Colour_Name_Code",
                table: "Colour",
                columns: new[] { "Name", "Code" },
                unique: true);
        }
    }
}
