using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PropFlow.Infrastructure.Autopilot;

namespace PropFlow.Infrastructure.Persistence.Migrations.Autopilot;

// CPM-8.08. Same tenant isolation as AutopilotTenantSecurity gives every other Autopilot table —
// organization foreign key, forced RLS, a policy bound to app.organization_id. Recommendations
// and ActionProposals are mutable (Approve/Reject/MarkExecuted change their own row), the same as
// Findings and Runs, so neither gets the append-only trigger Evidence/Feedback/AuditEntries have.
[DbContext(typeof(AutopilotStore))]
[Migration("20260914185500_ActionsAndRecommendationsTenantSecurity")]
public sealed class ActionsAndRecommendationsTenantSecurity : Migration
{
    private static readonly string[] Tables = ["Recommendations", "ActionProposals"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var table in Tables)
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
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in Tables)
            migrationBuilder.Sql($"""
                DROP POLICY tenant_isolation ON autopilot."{table}";
                ALTER TABLE autopilot."{table}" DISABLE ROW LEVEL SECURITY;
                ALTER TABLE autopilot."{table}" DROP CONSTRAINT "FK_{table}_Organization";
                """);
    }
}
