using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebNangCao.Migrations
{
    /// <inheritdoc />
    public partial class ClarifyInvoiceServiceFees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ServiceFee",
                table: "Invoices",
                newName: "WasteFee");

            migrationBuilder.AddColumn<decimal>(
                name: "ManagementFee",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ParkingFee",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManagementFee",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ParkingFee",
                table: "Invoices");

            migrationBuilder.RenameColumn(
                name: "WasteFee",
                table: "Invoices",
                newName: "ServiceFee");
        }
    }
}
