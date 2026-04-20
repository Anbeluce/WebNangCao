using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebNangCao.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInvoiceUsageBased : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WaterFee",
                table: "Invoices",
                newName: "WaterUsage");

            migrationBuilder.RenameColumn(
                name: "ManagementFee",
                table: "Invoices",
                newName: "WaterUnitPrice");

            migrationBuilder.RenameColumn(
                name: "ElectricityFee",
                table: "Invoices",
                newName: "ServiceFee");

            migrationBuilder.AddColumn<decimal>(
                name: "ElectricityUnitPrice",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ElectricityUsage",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ElectricityUnitPrice",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ElectricityUsage",
                table: "Invoices");

            migrationBuilder.RenameColumn(
                name: "WaterUsage",
                table: "Invoices",
                newName: "WaterFee");

            migrationBuilder.RenameColumn(
                name: "WaterUnitPrice",
                table: "Invoices",
                newName: "ManagementFee");

            migrationBuilder.RenameColumn(
                name: "ServiceFee",
                table: "Invoices",
                newName: "ElectricityFee");
        }
    }
}
