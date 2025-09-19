using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobHunter.Migrations
{
    /// <inheritdoc />
    public partial class updateinjob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Jobs",
                newName: "years_of_experience");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "years_of_experience",
                table: "Jobs",
                newName: "Name");
        }
    }
}
