using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSerialNumberAndRemarkInComapny : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sequence",
                table: "Companies");

            migrationBuilder.DropSequence(
                name: "CodeSequence");

            migrationBuilder.RenameColumn(
                name: "Remarks",
                table: "Companies",
                newName: "Remark");

            migrationBuilder.AddColumn<int>(
                name: "SerialNumber",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "Companies");

            migrationBuilder.RenameColumn(
                name: "Remark",
                table: "Companies",
                newName: "Remarks");

            migrationBuilder.CreateSequence<int>(
                name: "CodeSequence",
                startValue: 100L);

            migrationBuilder.AddColumn<int>(
                name: "Sequence",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValueSql: "NEXT VALUE FOR CodeSequence");
        }
    }
}
