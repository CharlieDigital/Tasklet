using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tasklet.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialTaskletSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tasklets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    ExplicitOrder = table.Column<string>(
                        type: "TEXT",
                        maxLength: 64,
                        nullable: true
                    ),
                    Pinned = table.Column<bool>(type: "INTEGER", nullable: false),
                    Color = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DueAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasklets", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Tasklets_UserId",
                table: "Tasklets",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Tasklets_UserId_CreatedAtUtc",
                table: "Tasklets",
                columns: new[] { "UserId", "CreatedAtUtc" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Tasklets_UserId_DueAtUtc",
                table: "Tasklets",
                columns: new[] { "UserId", "DueAtUtc" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Tasklets_UserId_Pinned_ExplicitOrder",
                table: "Tasklets",
                columns: new[] { "UserId", "Pinned", "ExplicitOrder" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Tasklets_UserId_Priority",
                table: "Tasklets",
                columns: new[] { "UserId", "Priority" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Tasklets_UserId_Status",
                table: "Tasklets",
                columns: new[] { "UserId", "Status" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Tasklets");
        }
    }
}
