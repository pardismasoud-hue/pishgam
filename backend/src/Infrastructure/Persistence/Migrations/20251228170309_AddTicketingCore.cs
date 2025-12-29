using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketingCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "CompanyExpertLinks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PrimaryExpertProfileId",
                table: "Assets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TicketMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorRole = table.Column<int>(type: "int", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsInternal = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCatalogItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedExpertProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SlaFirstResponseMinutes = table.Column<int>(type: "int", nullable: false),
                    SlaResolutionMinutes = table.Column<int>(type: "int", nullable: false),
                    FirstResponseDueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolutionDueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FirstResponseAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FirstResponseBreached = table.Column<bool>(type: "bit", nullable: false),
                    ResolutionBreached = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TicketTimeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpertProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Minutes = table.Column<int>(type: "int", nullable: false),
                    WorkType = table.Column<int>(type: "int", nullable: false),
                    LoggedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketTimeLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyExpertLinks_CompanyProfileId_IsPrimary",
                table: "CompanyExpertLinks",
                columns: new[] { "CompanyProfileId", "IsPrimary" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_PrimaryExpertProfileId",
                table: "Assets",
                column: "PrimaryExpertProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketMessages_CreatedAtUtc",
                table: "TicketMessages",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TicketMessages_IsInternal",
                table: "TicketMessages",
                column: "IsInternal");

            migrationBuilder.CreateIndex(
                name: "IX_TicketMessages_TicketId",
                table: "TicketMessages",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssetId",
                table: "Tickets",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssignedExpertProfileId",
                table: "Tickets",
                column: "AssignedExpertProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CompanyProfileId",
                table: "Tickets",
                column: "CompanyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CreatedAtUtc",
                table: "Tickets",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ServiceCatalogItemId",
                table: "Tickets",
                column: "ServiceCatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Status",
                table: "Tickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTimeLogs_ExpertProfileId",
                table: "TicketTimeLogs",
                column: "ExpertProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTimeLogs_LoggedAtUtc",
                table: "TicketTimeLogs",
                column: "LoggedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTimeLogs_TicketId",
                table: "TicketTimeLogs",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketMessages");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "TicketTimeLogs");

            migrationBuilder.DropIndex(
                name: "IX_CompanyExpertLinks_CompanyProfileId_IsPrimary",
                table: "CompanyExpertLinks");

            migrationBuilder.DropIndex(
                name: "IX_Assets_PrimaryExpertProfileId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "CompanyExpertLinks");

            migrationBuilder.DropColumn(
                name: "PrimaryExpertProfileId",
                table: "Assets");
        }
    }
}
