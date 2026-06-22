using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oypa.Crm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyAssignedSalesRep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedSalesRepId",
                table: "Companies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_AssignedSalesRepId",
                table: "Companies",
                column: "AssignedSalesRepId");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_SalesReps_AssignedSalesRepId",
                table: "Companies",
                column: "AssignedSalesRepId",
                principalTable: "SalesReps",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_SalesReps_AssignedSalesRepId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_AssignedSalesRepId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "AssignedSalesRepId",
                table: "Companies");
        }
    }
}
