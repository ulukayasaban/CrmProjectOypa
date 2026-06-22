using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oypa.Crm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Targets");

            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId",
                table: "SalesReps",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssigneeEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Segment = table.Column<int>(type: "int", nullable: false),
                    WeeklyTarget = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Goals_Employees_AssigneeEmployeeId",
                        column: x => x.AssigneeEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoalWeeks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeekStart = table.Column<DateOnly>(type: "date", nullable: false),
                    TargetValue = table.Column<int>(type: "int", nullable: false),
                    AchievedCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalWeeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalWeeks_Goals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "Goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesReps_EmployeeId",
                table: "SalesReps",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_AssigneeEmployeeId",
                table: "Goals",
                column: "AssigneeEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_IsActive",
                table: "Goals",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_GoalWeeks_GoalId_WeekStart",
                table: "GoalWeeks",
                columns: new[] { "GoalId", "WeekStart" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesReps_Employees_EmployeeId",
                table: "SalesReps",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesReps_Employees_EmployeeId",
                table: "SalesReps");

            migrationBuilder.DropTable(
                name: "GoalWeeks");

            migrationBuilder.DropTable(
                name: "Goals");

            migrationBuilder.DropIndex(
                name: "IX_SalesReps_EmployeeId",
                table: "SalesReps");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "SalesReps");

            migrationBuilder.CreateTable(
                name: "Targets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WeeklyMeetings = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Targets", x => x.Id);
                });
        }
    }
}
