using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class HorizonEventItemUses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "max_uses",
                table: "horizon_admin_loadout",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "remaining_uses",
                table: "horizon_admin_loadout",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "max_uses",
                table: "horizon_admin_loadout");

            migrationBuilder.DropColumn(
                name: "remaining_uses",
                table: "horizon_admin_loadout");
        }
    }
}
