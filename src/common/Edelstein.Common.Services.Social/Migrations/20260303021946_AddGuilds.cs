using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Edelstein.Common.Services.Social.Migrations
{
    /// <inheritdoc />
    public partial class AddGuilds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "guilds",
                columns: table => new
                {
                    ID = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    Name = table.Column<string>(type: "text", nullable: false),
                    GradeName1 = table.Column<string>(type: "text", nullable: false),
                    GradeName2 = table.Column<string>(type: "text", nullable: false),
                    GradeName3 = table.Column<string>(type: "text", nullable: false),
                    GradeName4 = table.Column<string>(type: "text", nullable: false),
                    GradeName5 = table.Column<string>(type: "text", nullable: false),
                    MaxMemberNum = table.Column<int>(type: "integer", nullable: false),
                    MasterCharacterID = table.Column<int>(type: "integer", nullable: false),
                    MarkBg = table.Column<short>(type: "smallint", nullable: false),
                    MarkBgColor = table.Column<byte>(type: "smallint", nullable: false),
                    Mark = table.Column<short>(type: "smallint", nullable: false),
                    MarkColor = table.Column<byte>(type: "smallint", nullable: false),
                    Notice = table.Column<string>(type: "text", nullable: false),
                    Point = table.Column<int>(type: "integer", nullable: false),
                    GuildLevel = table.Column<byte>(type: "smallint", nullable: false),
                    AllianceID = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guilds", x => x.ID);
                }
            );

            migrationBuilder.CreateTable(
                name: "guild_invitations",
                columns: table => new
                {
                    ID = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    GuildID = table.Column<int>(type: "integer", nullable: false),
                    InviterID = table.Column<int>(type: "integer", nullable: false),
                    CharacterID = table.Column<int>(type: "integer", nullable: false),
                    DateExpire = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_invitations", x => x.ID);
                    table.ForeignKey(
                        name: "FK_guild_invitations_guilds_GuildID",
                        column: x => x.GuildID,
                        principalTable: "guilds",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "guild_members",
                columns: table => new
                {
                    ID = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    GuildID = table.Column<int>(type: "integer", nullable: false),
                    CharacterID = table.Column<int>(type: "integer", nullable: false),
                    CharacterName = table.Column<string>(type: "text", nullable: false),
                    Job = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Grade = table.Column<int>(type: "integer", nullable: false),
                    ChannelID = table.Column<int>(type: "integer", nullable: false),
                    Commitment = table.Column<int>(type: "integer", nullable: false),
                    AllianceGrade = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_members", x => x.ID);
                    table.ForeignKey(
                        name: "FK_guild_members_guilds_GuildID",
                        column: x => x.GuildID,
                        principalTable: "guilds",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "guild_skills",
                columns: table => new
                {
                    ID = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    GuildID = table.Column<int>(type: "integer", nullable: false),
                    SkillID = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    DateExpire = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    BuyerName = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_skills", x => x.ID);
                    table.ForeignKey(
                        name: "FK_guild_skills_guilds_GuildID",
                        column: x => x.GuildID,
                        principalTable: "guilds",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_guild_invitations_GuildID",
                table: "guild_invitations",
                column: "GuildID"
            );

            migrationBuilder.CreateIndex(
                name: "IX_guild_members_GuildID",
                table: "guild_members",
                column: "GuildID"
            );

            migrationBuilder.CreateIndex(
                name: "IX_guild_members_CharacterID",
                table: "guild_members",
                column: "CharacterID",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_guild_skills_GuildID_SkillID",
                table: "guild_skills",
                columns: new[] { "GuildID", "SkillID" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_guilds_Name",
                table: "guilds",
                column: "Name",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "guild_invitations");

            migrationBuilder.DropTable(name: "guild_members");

            migrationBuilder.DropTable(name: "guild_skills");

            migrationBuilder.DropTable(name: "guilds");
        }
    }
}
