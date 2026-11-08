using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillMate.Migrations
{
    /// <inheritdoc />
    public partial class AddShopierFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                table: "Subscriptions",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RandomNumber",
                table: "Subscriptions",
                type: "longtext",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderNumber",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "RandomNumber",
                table: "Subscriptions");
        }
    }
}
