using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oypa.Crm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerLifecycleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNewCustomer",
                table: "Companies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastInteractionAtUtc",
                table: "Companies",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsNewCustomer",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "LastInteractionAtUtc",
                table: "Companies");
        }
    }
}
