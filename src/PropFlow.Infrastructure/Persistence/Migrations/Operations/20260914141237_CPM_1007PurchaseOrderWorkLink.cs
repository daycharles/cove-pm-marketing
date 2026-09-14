using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropFlow.Infrastructure.Persistence.Migrations.Operations
{
    /// <inheritdoc />
    public partial class CPM_1007PurchaseOrderWorkLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorkItemId",
                schema: "operations",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrganizationId_WorkItemId",
                schema: "operations",
                table: "PurchaseOrders",
                columns: new[] { "OrganizationId", "WorkItemId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_WorkItems_OrganizationId_WorkItemId",
                schema: "operations",
                table: "PurchaseOrders",
                columns: new[] { "OrganizationId", "WorkItemId" },
                principalSchema: "operations",
                principalTable: "WorkItems",
                principalColumns: new[] { "OrganizationId", "Id" },
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_WorkItems_OrganizationId_WorkItemId",
                schema: "operations",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_OrganizationId_WorkItemId",
                schema: "operations",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "WorkItemId",
                schema: "operations",
                table: "PurchaseOrders");
        }
    }
}
