using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrightPay.TakeHome.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class OfferConfigurationJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConfigurationJson",
                table: "CheckoutOffers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfigurationVersion",
                table: "CheckoutOffers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                UPDATE [CheckoutOffers]
                SET
                    [ConfigurationVersion] = 1,
                    [ConfigurationJson] = CONCAT(
                        '{"quantity":',
                        CONVERT(varchar(11), [Quantity]),
                        ',"fixedPrice":{"currency":"GBP","minorUnits":',
                        CONVERT(varchar(32), [FixedPriceAmount]),
                        '}}');
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ConfigurationJson",
                table: "CheckoutOffers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "FixedPriceAmount",
                table: "CheckoutOffers");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "CheckoutOffers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FixedPriceAmount",
                table: "CheckoutOffers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "CheckoutOffers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                UPDATE [CheckoutOffers]
                SET
                    [Quantity] = CONVERT(int, JSON_VALUE([ConfigurationJson], '$.quantity')),
                    [FixedPriceAmount] = CONVERT(decimal(18,2), JSON_VALUE([ConfigurationJson], '$.fixedPrice.minorUnits'));
                """);

            migrationBuilder.DropColumn(
                name: "ConfigurationJson",
                table: "CheckoutOffers");

            migrationBuilder.DropColumn(
                name: "ConfigurationVersion",
                table: "CheckoutOffers");
        }
    }
}
