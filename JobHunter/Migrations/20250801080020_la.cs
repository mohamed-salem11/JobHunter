using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobHunter.Migrations
{
    /// <inheritdoc />
    public partial class la : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_AspNetUsers_ApplicantId",
                table: "Applications");

            migrationBuilder.RenameColumn(
                name: "ApplicantId",
                table: "Applications",
                newName: "ApplicationUserId");

            migrationBuilder.RenameColumn(
                name: "JobApplicationId",
                table: "Applications",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_Applications_ApplicantId",
                table: "Applications",
                newName: "IX_Applications_ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_AspNetUsers_ApplicationUserId",
                table: "Applications",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_AspNetUsers_ApplicationUserId",
                table: "Applications");

            migrationBuilder.RenameColumn(
                name: "ApplicationUserId",
                table: "Applications",
                newName: "ApplicantId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Applications",
                newName: "JobApplicationId");

            migrationBuilder.RenameIndex(
                name: "IX_Applications_ApplicationUserId",
                table: "Applications",
                newName: "IX_Applications_ApplicantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_AspNetUsers_ApplicantId",
                table: "Applications",
                column: "ApplicantId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
