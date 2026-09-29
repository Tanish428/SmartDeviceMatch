using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDeviceMatch.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRepairShopToOffer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "BuyerId",
                table: "Offers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "RepairShopId",
                table: "Offers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_RepairShopId",
                table: "Offers",
                column: "RepairShopId");

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_RepairShops_RepairShopId",
                table: "Offers",
                column: "RepairShopId",
                principalTable: "RepairShops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Offers_RepairShops_RepairShopId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Offers_RepairShopId",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "RepairShopId",
                table: "Offers");

            migrationBuilder.AlterColumn<int>(
                name: "BuyerId",
                table: "Offers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
