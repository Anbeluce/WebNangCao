using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebNangCao.Migrations
{
    /// <inheritdoc />
    public partial class AddSepayTransactionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SepayTransactionId",
                table: "Transactions",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SepayTransactionId",
                table: "Transactions");
        }
    }
}
