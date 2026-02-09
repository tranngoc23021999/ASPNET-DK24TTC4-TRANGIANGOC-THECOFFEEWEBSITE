using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoffeSolution.Migrations
{
    /// <inheritdoc />
    public partial class UpdateScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Suppliers",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "StoreId",
                table: "Products",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_StoreId",
                table: "Suppliers",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Stores_StoreId",
                table: "Suppliers",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Stores_StoreId",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_StoreId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Suppliers");

            migrationBuilder.AlterColumn<int>(
                name: "StoreId",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
