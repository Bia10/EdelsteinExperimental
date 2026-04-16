using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edelstein.Common.Services.Social.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGuildBBSSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop LastCommenterID — not present in client ENTRYLIST/CURENTRY structs.
            migrationBuilder.DropColumn(name: "LastCommenterID", table: "guild_bbs_posts");

            // Add EmoticonID — nEmoticon field in ENTRYLIST and CURENTRY.
            migrationBuilder.AddColumn<int>(
                name: "EmoticonID",
                table: "guild_bbs_posts",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            // Shrink Title from 50 → 25 — client nHorzMax = 25 on CCtrlEdit.
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "guild_bbs_posts",
                type: "character varying(25)",
                maxLength: 25,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "EmoticonID", table: "guild_bbs_posts");

            migrationBuilder.AddColumn<int>(
                name: "LastCommenterID",
                table: "guild_bbs_posts",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "guild_bbs_posts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(25)",
                oldMaxLength: 25
            );
        }
    }
}
