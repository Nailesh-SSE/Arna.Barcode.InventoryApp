using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class removeUniqueRoute_FormMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FormMasters_Route",
                table: "FormMasters");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FormMasters_Route",
                table: "FormMasters",
                column: "Route",
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
