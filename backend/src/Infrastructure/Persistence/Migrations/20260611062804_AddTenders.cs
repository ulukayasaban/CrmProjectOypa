using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oypa.Crm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TenderNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Sector = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TenderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PersonnelCount = table.Column<int>(type: "int", nullable: true),
                    EstimatedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Volume = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AssignedSalesRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApproachNotifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tenders_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tenders_SalesReps_AssignedSalesRepId",
                        column: x => x.AssignedSalesRepId,
                        principalTable: "SalesReps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_AssignedSalesRepId",
                table: "Tenders",
                column: "AssignedSalesRepId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_CompanyId",
                table: "Tenders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_Sector",
                table: "Tenders",
                column: "Sector");

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_Status",
                table: "Tenders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_TenderDate",
                table: "Tenders",
                column: "TenderDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenders");
        }
    }
}
