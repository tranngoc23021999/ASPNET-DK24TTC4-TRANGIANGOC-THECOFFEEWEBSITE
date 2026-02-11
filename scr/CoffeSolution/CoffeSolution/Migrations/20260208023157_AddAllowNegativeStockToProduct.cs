using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoffeSolution.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowNegativeStockToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowNegativeStock",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowNegativeStock",
                table: "Products");
        }
    }
}
