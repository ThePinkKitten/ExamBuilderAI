using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamBuilderAI.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEdPronunciationSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ExerciseSections",
                keyColumn: "Id",
                keyValue: 10,
                column: "DisplayOrder",
                value: 11);

            migrationBuilder.InsertData(
                table: "ExerciseSections",
                columns: new[] { "Id", "Code", "Description", "DisplayOrder", "Icon", "Name" },
                values: new object[] { 11, "ed_pronunciation", "Choose the word whose -ed part is pronounced differently from the others", 10, "history_edu", "Pronunciation of -ed" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ExerciseSections",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.UpdateData(
                table: "ExerciseSections",
                keyColumn: "Id",
                keyValue: 10,
                column: "DisplayOrder",
                value: 10);
        }
    }
}
