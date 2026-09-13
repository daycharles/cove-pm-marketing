using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropFlow.Infrastructure.Persistence.Migrations.Operations;

/// <summary>
/// Compatibility marker for the parallel FS-S11/S12 branch migration.
/// FS-S14Operations already creates the shared inspection/procurement schema in the
/// consolidated migration chain, so this later migration intentionally has no SQL.
/// </summary>
public partial class FS_S11S12Operations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) { }

    protected override void Down(MigrationBuilder migrationBuilder) { }
}
