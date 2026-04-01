using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSPMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSubmissionAnswerCascadeOnDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmissionAnswers_Questions_QuestionId",
                table: "SubmissionAnswers");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmissionAnswers_Questions_QuestionId",
                table: "SubmissionAnswers",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmissionAnswers_Questions_QuestionId",
                table: "SubmissionAnswers");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmissionAnswers_Questions_QuestionId",
                table: "SubmissionAnswers",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
