using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oypa.Crm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMailDraftCc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cc",
                table: "MailDrafts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cc",
                table: "MailDrafts");
        }
    }
}
