using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDeviceMatch.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    ShopId = table.Column<int>(type: "int", nullable: false),
                    CompatibilityScore = table.Column<int>(type: "int", nullable: false),
                    CategoryScore = table.Column<int>(type: "int", nullable: false),
                    BrandScore = table.Column<int>(type: "int", nullable: false),
                    ProximityScore = table.Column<int>(type: "int", nullable: false),
                    ExpertiseScore = table.Column<int>(type: "int", nullable: false),
                    RatingScore = table.Column<int>(type: "int", nullable: false),
                    DistanceKm = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_RepairShops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "RepairShops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_DeviceId",
                table: "Matches",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_ShopId",
                table: "Matches",
                column: "ShopId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Matches");
        }
    }
}
