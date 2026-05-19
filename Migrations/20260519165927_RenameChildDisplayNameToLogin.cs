using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningPlatform.Migrations
{
    /// <inheritdoc />
    public partial class RenameChildDisplayNameToLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "Children",
                newName: "Login");

            migrationBuilder.AlterColumn<string>(
                name: "Login",
                table: "Children",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.Sql(
                """
                UPDATE "Children"
                SET "Login" = LOWER(TRIM("Login"))
                WHERE "Login" IS NOT NULL AND "Login" <> '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Children_Login",
                table: "Children",
                column: "Login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Children_Login",
                table: "Children");

            migrationBuilder.AlterColumn<string>(
                name: "Login",
                table: "Children",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.RenameColumn(
                name: "Login",
                table: "Children",
                newName: "DisplayName");
        }
    }
}
