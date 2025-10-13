using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnInCompanyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "CodeSequence",
                startValue: 100L);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "Companies",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sequence",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValueSql: "NEXT VALUE FOR CodeSequence");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Sequence",
                table: "Companies");

            migrationBuilder.DropSequence(
                name: "CodeSequence");
        }
    }
}
