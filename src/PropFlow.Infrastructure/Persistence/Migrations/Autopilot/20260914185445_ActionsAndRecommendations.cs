using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropFlow.Infrastructure.Persistence.Migrations.Autopilot
{
    /// <inheritdoc />
    public partial class ActionsAndRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Recommendations",
                schema: "autopilot",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FindingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ProposedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DecidedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecisionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recommendations", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_Recommendations_Findings_OrganizationId_FindingId",
                        columns: x => new { x.OrganizationId, x.FindingId },
                        principalSchema: "autopilot",
                        principalTable: "Findings",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionProposals",
                schema: "autopilot",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecommendationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ProposedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DecidedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecisionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExecutionOutcome = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PayloadFieldsJson = table.Column<string>(type: "jsonb", nullable: false),
                    PreviewDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PreviewChangesJson = table.Column<string>(type: "jsonb", nullable: false),
                    RequiredApprovalCapability = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequiredExecutionCapability = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequiredConsentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ConcurrencyToken = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionProposals", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_ActionProposals_Recommendations_OrganizationId_Recommendati~",
                        columns: x => new { x.OrganizationId, x.RecommendationId },
                        principalSchema: "autopilot",
                        principalTable: "Recommendations",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionProposals_OrganizationId_IdempotencyKey",
                schema: "autopilot",
                table: "ActionProposals",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActionProposals_OrganizationId_RecommendationId_Status",
                schema: "autopilot",
                table: "ActionProposals",
                columns: new[] { "OrganizationId", "RecommendationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_OrganizationId_FindingId_Status",
                schema: "autopilot",
                table: "Recommendations",
                columns: new[] { "OrganizationId", "FindingId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionProposals",
                schema: "autopilot");

            migrationBuilder.DropTable(
                name: "Recommendations",
                schema: "autopilot");
        }
    }
}
