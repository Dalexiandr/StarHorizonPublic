using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class HorizonAdminLoadout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "horizon_admin_loadout",
                columns: table => new
                {
                    horizon_admin_loadout_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    player_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prototype_id = table.Column<string>(type: "text", nullable: false),
                    component_overrides_yaml = table.Column<string>(type: "text", nullable: true),
                    custom_name = table.Column<string>(type: "text", nullable: true),
                    custom_description = table.Column<string>(type: "text", nullable: true),
                    credit_cost = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    granted_by = table.Column<string>(type: "text", nullable: false),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_horizon_admin_loadout", x => x.horizon_admin_loadout_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_horizon_admin_loadout_player_user_id",
                table: "horizon_admin_loadout",
                column: "player_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "horizon_admin_loadout");
        }
    }
}
