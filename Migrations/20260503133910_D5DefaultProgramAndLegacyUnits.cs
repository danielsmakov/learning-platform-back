using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningPlatform.Migrations
{
    /// <inheritdoc />
    public partial class D5DefaultProgramAndLegacyUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Programs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // D5: одна программа по умолчанию — дорожка Beginner (как в AddLearningPrograms для legacy Units/Children).
            migrationBuilder.Sql("""
                UPDATE "Programs" SET "IsDefault" = true WHERE "DifficultyTrack" = 2;
                UPDATE "Programs" SET "IsDefault" = true WHERE "Id" = (
                  SELECT "Id" FROM "Programs" ORDER BY "DifficultyTrack" ASC LIMIT 1
                ) AND NOT EXISTS (SELECT 1 FROM "Programs" WHERE "IsDefault" = true);
                UPDATE "Units" u SET "ProgramId" = d."Id"
                FROM (SELECT "Id" FROM "Programs" WHERE "IsDefault" = true LIMIT 1) AS d
                WHERE NOT EXISTS (SELECT 1 FROM "Programs" p WHERE p."Id" = u."ProgramId");
                UPDATE "Children" c SET "CurrentProgramId" = d."Id"
                FROM (SELECT "Id" FROM "Programs" WHERE "IsDefault" = true LIMIT 1) AS d
                WHERE NOT EXISTS (SELECT 1 FROM "Programs" p WHERE p."Id" = c."CurrentProgramId");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Programs_IsDefault",
                table: "Programs",
                column: "IsDefault",
                unique: true,
                filter: "\"IsDefault\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Programs_IsDefault",
                table: "Programs");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Programs");
        }
    }
}
