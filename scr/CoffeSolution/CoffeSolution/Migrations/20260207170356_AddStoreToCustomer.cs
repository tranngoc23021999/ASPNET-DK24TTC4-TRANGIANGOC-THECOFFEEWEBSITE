using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoffeSolution.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreToCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_StoreId",
                table: "Customers",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Stores_StoreId",
                table: "Customers",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Stores_StoreId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_StoreId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Customers");
        }
    }
}
