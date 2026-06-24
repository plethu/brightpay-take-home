using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BrightPay.TakeHome.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCheckoutCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CheckoutProducts",
                columns: table => new
                {
                    Sku = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: false),
                    UnitPriceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutProducts", x => x.Sku);
                });

            migrationBuilder.CreateTable(
                name: "CheckoutOffers",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Sku = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    FixedPriceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutOffers", x => x.Code);
                    table.ForeignKey(
                        name: "FK_CheckoutOffers_CheckoutProducts_Sku",
                        column: x => x.Sku,
                        principalTable: "CheckoutProducts",
                        principalColumn: "Sku",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "CheckoutProducts",
                columns: new[] { "Sku", "IsActive", "UnitPriceAmount" },
                values: new object[,]
                {
                    { "A", true, 50m },
                    { "B", true, 30m },
                    { "C", true, 20m },
                    { "D", true, 15m }
                });

            migrationBuilder.InsertData(
                table: "CheckoutOffers",
                columns: new[] { "Code", "FixedPriceAmount", "Quantity", "Sku", "State", "Type" },
                values: new object[,]
                {
                    { "A-3-FOR-130", 130m, 3, "A", 1, 1 },
                    { "B-2-FOR-45", 45m, 2, "B", 1, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutOffers_Sku_State",
                table: "CheckoutOffers",
                columns: new[] { "Sku", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutProducts_IsActive",
                table: "CheckoutProducts",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckoutOffers");

            migrationBuilder.DropTable(
                name: "CheckoutProducts");
        }
    }
}
