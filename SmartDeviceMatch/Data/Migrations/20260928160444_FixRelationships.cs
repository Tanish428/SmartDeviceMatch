using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDeviceMatch.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AppUsers_UserId1",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_AppUsers_BuyerId1",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairShops_AppUsers_UserId1",
                table: "RepairShops");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_AppUsers_RevieweeId1",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_AppUsers_ReviewerId1",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_RevieweeId1",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ReviewerId1",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_RepairShops_UserId1",
                table: "RepairShops");

            migrationBuilder.DropIndex(
                name: "IX_Offers_BuyerId1",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId1",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RevieweeId1",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ReviewerId1",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "RepairShops");

            migrationBuilder.DropColumn(
                name: "BuyerId1",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Notifications");

            migrationBuilder.AlterColumn<int>(
                name: "ReviewerId",
                table: "Reviews",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "RevieweeId",
                table: "Reviews",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "RepairShops",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "BuyerId",
                table: "Offers",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Notifications",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_RevieweeId",
                table: "Reviews",
                column: "RevieweeId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ReviewerId",
                table: "Reviews",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairShops_UserId",
                table: "RepairShops",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_BuyerId",
                table: "Offers",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AppUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_AppUsers_BuyerId",
                table: "Offers",
                column: "BuyerId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairShops_AppUsers_UserId",
                table: "RepairShops",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_AppUsers_RevieweeId",
                table: "Reviews",
                column: "RevieweeId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_AppUsers_ReviewerId",
                table: "Reviews",
                column: "ReviewerId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AppUsers_UserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_AppUsers_BuyerId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairShops_AppUsers_UserId",
                table: "RepairShops");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_AppUsers_RevieweeId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_AppUsers_ReviewerId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_RevieweeId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ReviewerId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_RepairShops_UserId",
                table: "RepairShops");

            migrationBuilder.DropIndex(
                name: "IX_Offers_BuyerId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "ReviewerId",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "RevieweeId",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "RevieweeId1",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewerId1",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "RepairShops",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "RepairShops",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BuyerId",
                table: "Offers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "BuyerId1",
                table: "Offers",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_RevieweeId1",
                table: "Reviews",
                column: "RevieweeId1");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ReviewerId1",
                table: "Reviews",
                column: "ReviewerId1");

            migrationBuilder.CreateIndex(
                name: "IX_RepairShops_UserId1",
                table: "RepairShops",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_BuyerId1",
                table: "Offers",
                column: "BuyerId1");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId1",
                table: "Notifications",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AppUsers_UserId1",
                table: "Notifications",
                column: "UserId1",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_AppUsers_BuyerId1",
                table: "Offers",
                column: "BuyerId1",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairShops_AppUsers_UserId1",
                table: "RepairShops",
                column: "UserId1",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_AppUsers_RevieweeId1",
                table: "Reviews",
                column: "RevieweeId1",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_AppUsers_ReviewerId1",
                table: "Reviews",
                column: "ReviewerId1",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
