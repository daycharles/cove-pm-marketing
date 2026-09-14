using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PropFlow.Infrastructure.Autopilot;

namespace PropFlow.Infrastructure.Persistence.Migrations.Autopilot;

// Standard tenant isolation for every Autopilot table: an organization foreign key, forced RLS,
// and a policy bound to app.organization_id — same pattern as every other business table
// (operations."Timeline"'s TenantSecurity, integrations."Connections"'s IntegrationsTenantSecurity).
// Evidence/Feedback/AuditEntries additionally get the append-only trigger operations."Timeline"
// has: the GRANT in DatabaseProvisioner already withholds UPDATE/DELETE, and the trigger is the
// backstop that a GRANT alone is (a widened GRANT is a one-line mistake; a trigger is not).
[DbContext(typeof(AutopilotStore))]
[Migration("20260914161500_AutopilotTenantSecurity")]
public sealed class AutopilotTenantSecurity : Migration
{
    private static readonly string[] AllTables = ["Runs", "Findings", "Evidence", "Feedback", "AuditEntries", "FindingReads"];
    private static readonly string[] AppendOnlyTables = ["Evidence", "Feedback", "AuditEntries"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var table in AllTables)
        {
            migrationBuilder.Sql($"""
                ALTER TABLE autopilot."{table}"
                  ADD CONSTRAINT "FK_{table}_Organization" FOREIGN KEY ("OrganizationId")
                  REFERENCES identity."Organizations" ("Id") ON DELETE RESTRICT;
                ALTER TABLE autopilot."{table}" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE autopilot."{table}" FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON autopilot."{table}"
                  USING ("OrganizationId" = nullif(current_setting('app.organization_id', true), '')::uuid)
                  WITH CHECK ("OrganizationId" = nullif(current_setting('app.organization_id', true), '')::uuid);
                """);
        }

        foreach (var table in AppendOnlyTables)
        {
            var functionName = $"reject_{table.ToLowerInvariant()}_mutation";
            migrationBuilder.Sql($"""
                CREATE FUNCTION autopilot.{functionName}() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                  RAISE EXCEPTION '{table} rows are append-only' USING ERRCODE = '55000';
                END; $$;
                CREATE TRIGGER {table.ToLowerInvariant()}_append_only BEFORE UPDATE OR DELETE ON autopilot."{table}"
                  FOR EACH ROW EXECUTE FUNCTION autopilot.{functionName}();
                """);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in AppendOnlyTables)
        {
            var functionName = $"reject_{table.ToLowerInvariant()}_mutation";
            migrationBuilder.Sql($"""
                DROP TRIGGER {table.ToLowerInvariant()}_append_only ON autopilot."{table}";
                DROP FUNCTION autopilot.{functionName}();
                """);
        }

        foreach (var table in AllTables)
            migrationBuilder.Sql($"""
                DROP POLICY tenant_isolation ON autopilot."{table}";
                ALTER TABLE autopilot."{table}" DISABLE ROW LEVEL SECURITY;
                ALTER TABLE autopilot."{table}" DROP CONSTRAINT "FK_{table}_Organization";
                """);
    }
}
