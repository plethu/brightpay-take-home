using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrightPay.TakeHome.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class OfferPlanningMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CombinationRule",
                table: "CheckoutOffers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "CheckoutOffers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "CheckoutOffers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CombinationRule",
                table: "CheckoutOffers");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "CheckoutOffers");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "CheckoutOffers");
        }
    }
}
