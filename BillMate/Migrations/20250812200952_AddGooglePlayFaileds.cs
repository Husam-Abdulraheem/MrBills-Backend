using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillMate.Migrations
{
    /// <inheritdoc />
    public partial class AddGooglePlayFaileds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PurchaseToken",
                table: "Subscriptions",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionId",
                table: "Subscriptions",
                type: "longtext",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchaseToken",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "Subscriptions");
        }
    }
}
