using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayIndexIconParentId_FormMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "Roles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayIndex",
                table: "FormMasters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "FormMasters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "FormMasters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentName",
                table: "FormMasters",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Remark",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "DisplayIndex",
                table: "FormMasters");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "FormMasters");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "FormMasters");

            migrationBuilder.DropColumn(
                name: "ParentName",
                table: "FormMasters");
        }
    }
}
