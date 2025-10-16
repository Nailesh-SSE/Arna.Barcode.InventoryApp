using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Core.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnrequiredFieldsonInward : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChallanNo",
                table: "Inwards");

            migrationBuilder.DropColumn(
                name: "InvoiceDate",
                table: "Inwards");

            migrationBuilder.DropColumn(
                name: "InvoiceNo",
                table: "Inwards");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChallanNo",
                table: "Inwards",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoiceDate",
                table: "Inwards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNo",
                table: "Inwards",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
