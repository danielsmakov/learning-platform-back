using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningPlatform.Migrations;

/// <summary>
/// Ранее файлы B6/H2 были без Designer — EF их не применял. Идемпотентный DDL для существующих БД.
/// </summary>
public partial class BackfillB6H2Schema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""ContentTranslations"" (
    ""Id"" uuid NOT NULL,
    ""EntityType"" character varying(64) NOT NULL,
    ""EntityId"" uuid NOT NULL,
    ""FieldName"" character varying(64) NOT NULL,
    ""Locale"" character varying(16) NOT NULL,
    ""Value"" character varying(2000) NOT NULL,
    CONSTRAINT ""PK_ContentTranslations"" PRIMARY KEY (""Id"")
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ContentTranslations_EntityType_EntityId_FieldName_Locale""
    ON ""ContentTranslations"" (""EntityType"", ""EntityId"", ""FieldName"", ""Locale"");
");

        migrationBuilder.Sql(@"
DO $h2$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'Lessons' AND column_name = 'Description'
  ) THEN
    ALTER TABLE ""Lessons"" ADD COLUMN ""Description"" character varying(2000) NOT NULL DEFAULT '';
  END IF;
END
$h2$;
");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ""IX_ContentTranslations_EntityType_EntityId_FieldName_Locale"";
DROP TABLE IF EXISTS ""ContentTranslations"";
");

        migrationBuilder.Sql(@"
DO $h2$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'Lessons' AND column_name = 'Description'
  ) THEN
    ALTER TABLE ""Lessons"" DROP COLUMN ""Description"";
  END IF;
END
$h2$;
");
    }
}
