using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrawlData.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coupon",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    type = table.Column<bool>(type: "bit", nullable: false),
                    supplier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    discount = table.Column<double>(type: "float", nullable: false),
                    min_value_apply = table.Column<double>(type: "float", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    available = table.Column<double>(type: "float", nullable: true),
                    expired_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    url_apply_list = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    code = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coupon", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_coupon",
                columns: table => new
                {
                    userid = table.Column<int>(type: "int", nullable: false),
                    couponid = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_coupon", x => new { x.userid, x.couponid });
                    table.ForeignKey(
                        name: "FK_user_coupon_coupon_couponid",
                        column: x => x.couponid,
                        principalTable: "coupon",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_coupon_user_userid",
                        column: x => x.userid,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_username",
                table: "user",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_coupon_couponid",
                table: "user_coupon",
                column: "couponid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_coupon");

            migrationBuilder.DropTable(
                name: "coupon");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
