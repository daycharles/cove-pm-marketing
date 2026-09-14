using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropFlow.Infrastructure.Persistence.Migrations.Autopilot
{
    /// <inheritdoc />
    public partial class AutopilotInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "autopilot");

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                schema: "autopilot",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubjectType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Detail = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => new { x.OrganizationId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "Runs",
                schema: "autopilot",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Trigger = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FindingCount = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runs", x => new { x.OrganizationId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "Findings",
                schema: "autopilot",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignalType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    SubjectType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FreshnessAsOf = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: true),
                    PortfolioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DismissedReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SnoozedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Findings", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_Findings_Runs_OrganizationId_RunId",
                        columns: x => new { x.OrganizationId, x.RunId },
                        principalSchema: "autopilot",
                        principalTable: "Runs",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Evidence",
                schema: "autopilot",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FindingId = table.Column<Guid>(type: "uuid", nullable: false),
                    InputsJson = table.Column<string>(type: "jsonb", nullable: false),
                    SourceLinksJson = table.Column<string>(type: "jsonb", nullable: false),
                    ImpactCategory = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    ImpactDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ImpactEstimatedAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    FreshnessAsOf = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evidence", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_Evidence_Findings_OrganizationId_FindingId",
                        columns: x => new { x.OrganizationId, x.FindingId },
                        principalSchema: "autopilot",
                        principalTable: "Findings",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Feedback",
                schema: "autopilot",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FindingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sentiment = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RecordedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedback", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_Feedback_Findings_OrganizationId_FindingId",
                        columns: x => new { x.OrganizationId, x.FindingId },
                        principalSchema: "autopilot",
                        principalTable: "Findings",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FindingReads",
                schema: "autopilot",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FindingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FindingReads", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_FindingReads_Findings_OrganizationId_FindingId",
                        columns: x => new { x.OrganizationId, x.FindingId },
                        principalSchema: "autopilot",
                        principalTable: "Findings",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_OrganizationId_SubjectType_SubjectId_OccurredAt",
                schema: "autopilot",
                table: "AuditEntries",
                columns: new[] { "OrganizationId", "SubjectType", "SubjectId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Evidence_OrganizationId_FindingId",
                schema: "autopilot",
                table: "Evidence",
                columns: new[] { "OrganizationId", "FindingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_OrganizationId_FindingId_RecordedBy_RecordedAt",
                schema: "autopilot",
                table: "Feedback",
                columns: new[] { "OrganizationId", "FindingId", "RecordedBy", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FindingReads_OrganizationId_FindingId_ViewerId",
                schema: "autopilot",
                table: "FindingReads",
                columns: new[] { "OrganizationId", "FindingId", "ViewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Findings_OrganizationId_PortfolioId_Status",
                schema: "autopilot",
                table: "Findings",
                columns: new[] { "OrganizationId", "PortfolioId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Findings_OrganizationId_PropertyId_Status",
                schema: "autopilot",
                table: "Findings",
                columns: new[] { "OrganizationId", "PropertyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Findings_OrganizationId_RunId",
                schema: "autopilot",
                table: "Findings",
                columns: new[] { "OrganizationId", "RunId" });

            migrationBuilder.CreateIndex(
                name: "IX_Findings_OrganizationId_SignalType",
                schema: "autopilot",
                table: "Findings",
                columns: new[] { "OrganizationId", "SignalType" });

            migrationBuilder.CreateIndex(
                name: "IX_Findings_OrganizationId_Status_Severity_DetectedAt",
                schema: "autopilot",
                table: "Findings",
                columns: new[] { "OrganizationId", "Status", "Severity", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Runs_OrganizationId_StartedAt",
                schema: "autopilot",
                table: "Runs",
                columns: new[] { "OrganizationId", "StartedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries",
                schema: "autopilot");

            migrationBuilder.DropTable(
                name: "Evidence",
                schema: "autopilot");

            migrationBuilder.DropTable(
                name: "Feedback",
                schema: "autopilot");

            migrationBuilder.DropTable(
                name: "FindingReads",
                schema: "autopilot");

            migrationBuilder.DropTable(
                name: "Findings",
                schema: "autopilot");

            migrationBuilder.DropTable(
                name: "Runs",
                schema: "autopilot");
        }
    }
}
