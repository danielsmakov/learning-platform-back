using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningProgramsAndUnitChildLinks : Migration
    {
        private static readonly Guid IdElementary = Guid.Parse("a1000000-0000-4000-8000-000000000001");
        private static readonly Guid IdBeginner = Guid.Parse("a1000000-0000-4000-8000-000000000002");
        private static readonly Guid IdPreIntermediate = Guid.Parse("a1000000-0000-4000-8000-000000000003");
        private static readonly Guid IdIntermediate = Guid.Parse("a1000000-0000-4000-8000-000000000004");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Programs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DifficultyTrack = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Programs",
                columns: ["Id", "DifficultyTrack", "Title", "Description", "IsPublished"],
                values: [IdElementary, 1, "Elementary", "Начальный уровень программы.", true]);

            migrationBuilder.InsertData(
                table: "Programs",
                columns: ["Id", "DifficultyTrack", "Title", "Description", "IsPublished"],
                values: [IdBeginner, 2, "Beginner", "Базовый уровень программы.", true]);

            migrationBuilder.InsertData(
                table: "Programs",
                columns: ["Id", "DifficultyTrack", "Title", "Description", "IsPublished"],
                values: [IdPreIntermediate, 3, "Pre-Intermediate", "Средний подготовительный уровень.", true]);

            migrationBuilder.InsertData(
                table: "Programs",
                columns: ["Id", "DifficultyTrack", "Title", "Description", "IsPublished"],
                values: [IdIntermediate, 4, "Intermediate", "Средний уровень программы.", true]);

            migrationBuilder.CreateIndex(
                name: "IX_Programs_DifficultyTrack",
                table: "Programs",
                column: "DifficultyTrack",
                unique: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProgramId",
                table: "Units",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql($"""UPDATE "Units" SET "ProgramId" = '{IdBeginner}' WHERE "ProgramId" IS NULL""");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProgramId",
                table: "Units",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentProgramId",
                table: "Children",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql($"""UPDATE "Children" SET "CurrentProgramId" = '{IdBeginner}' WHERE "CurrentProgramId" IS NULL""");

            migrationBuilder.AlterColumn<Guid>(
                name: "CurrentProgramId",
                table: "Children",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Units_ProgramId",
                table: "Units",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_Children_CurrentProgramId",
                table: "Children",
                column: "CurrentProgramId");

            migrationBuilder.AddForeignKey(
                name: "FK_Children_Programs_CurrentProgramId",
                table: "Children",
                column: "CurrentProgramId",
                principalTable: "Programs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Units_Programs_ProgramId",
                table: "Units",
                column: "ProgramId",
                principalTable: "Programs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Children_Programs_CurrentProgramId",
                table: "Children");

            migrationBuilder.DropForeignKey(
                name: "FK_Units_Programs_ProgramId",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Units_ProgramId",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Children_CurrentProgramId",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "ProgramId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "CurrentProgramId",
                table: "Children");

            migrationBuilder.DropTable(
                name: "Programs");
        }
    }
}
