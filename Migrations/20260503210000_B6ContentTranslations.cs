using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningPlatform.Migrations
{
    /// <inheritdoc />
    public class B6ContentTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContentTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTranslations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentTranslations_EntityType_EntityId_FieldName_Locale",
                table: "ContentTranslations",
                columns: new[] { "EntityType", "EntityId", "FieldName", "Locale" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ContentTranslations");
        }
    }
}
