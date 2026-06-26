using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oypa.Crm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyRevizeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirmType",
                table: "Companies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "DisFirma");

            migrationBuilder.AddColumn<Guid>(
                name: "LeadOwnerId",
                table: "Companies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceSector",
                table: "Companies",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceNote",
                table: "Companies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_LeadOwnerId",
                table: "Companies",
                column: "LeadOwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_SalesReps_LeadOwnerId",
                table: "Companies",
                column: "LeadOwnerId",
                principalTable: "SalesReps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_SalesReps_LeadOwnerId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_LeadOwnerId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "FirmType",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "LeadOwnerId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ServiceSector",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SourceNote",
                table: "Companies");
        }
    }
}
