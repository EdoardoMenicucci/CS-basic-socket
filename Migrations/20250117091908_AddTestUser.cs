using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace chat_ia.Migrations
{
    /// <inheritdoc />
    public partial class AddTestUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Username", "Password" },
                values: new object[] { 1, "test", "password" }
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1
            );
        }

    }
}
