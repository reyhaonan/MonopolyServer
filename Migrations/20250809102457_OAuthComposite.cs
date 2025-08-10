using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonopolyServer.Migrations
{
    /// <inheritdoc />
    public partial class OAuthComposite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserOAuth_ProviderName_OAuthID",
                table: "UserOAuth",
                columns: new[] { "ProviderName", "OAuthID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserOAuth_ProviderName_OAuthID",
                table: "UserOAuth");
        }
    }
}
