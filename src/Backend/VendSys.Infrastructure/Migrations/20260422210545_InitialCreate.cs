using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendSys.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "DEXMeter",
                schema: "dbo",
                columns: table => new
                {
                    DexMeterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Machine = table.Column<string>(type: "nvarchar(1)", nullable: false),
                    DEXDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MachineSerialNumber = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ValueOfPaidVends = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEXMeter", x => x.DexMeterId);
                });

            migrationBuilder.CreateTable(
                name: "DEXLaneMeter",
                schema: "dbo",
                columns: table => new
                {
                    DexLaneMeterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DexMeterId = table.Column<int>(type: "int", nullable: false),
                    ProductIdentifier = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    NumberOfVends = table.Column<int>(type: "int", nullable: false),
                    ValueOfPaidSales = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEXLaneMeter", x => x.DexLaneMeterId);
                    table.ForeignKey(
                        name: "FK_DEXLaneMeter_DEXMeter",
                        column: x => x.DexMeterId,
                        principalSchema: "dbo",
                        principalTable: "DEXMeter",
                        principalColumn: "DexMeterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DEXLaneMeter_DexMeterId",
                schema: "dbo",
                table: "DEXLaneMeter",
                column: "DexMeterId");

            migrationBuilder.CreateIndex(
                name: "UQ_DEXMeter_Machine_DEXDateTime",
                schema: "dbo",
                table: "DEXMeter",
                columns: new[] { "Machine", "DEXDateTime" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DEXLaneMeter",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "DEXMeter",
                schema: "dbo");
        }
    }
}
