using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ExamBuilderAI.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CurriculumUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Grade = table.Column<int>(type: "int", nullable: false),
                    UnitNumber = table.Column<int>(type: "int", nullable: false),
                    UnitTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GrammarPoints = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Vocabulary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Topics = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseSections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    CurriculumUnitId = table.Column<int>(type: "int", nullable: true),
                    GeneratedByUserId = table.Column<int>(type: "int", nullable: false),
                    Difficulty = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    QuestionCount = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exercises_CurriculumUnits_CurriculumUnitId",
                        column: x => x.CurriculumUnitId,
                        principalTable: "CurriculumUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Exercises_ExerciseSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "ExerciseSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Exercises_Users_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ExerciseId = table.Column<int>(type: "int", nullable: false),
                    UserAnswers = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false),
                    ScorePercent = table.Column<double>(type: "float", nullable: false),
                    TimeTakenSeconds = table.Column<int>(type: "int", nullable: false),
                    AiFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseResults_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseResults_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CurriculumUnits",
                columns: new[] { "Id", "Grade", "GrammarPoints", "Topics", "UnitNumber", "UnitTitle", "Vocabulary" },
                values: new object[,]
                {
                    { 1, 8, "[\"Gerunds as subjects and objects\", \"Verbs of liking + gerunds\", \"Adverbs of frequency\"]", "[\"free time activities\", \"hobbies\", \"leisure habits of teenagers\"]", 1, "Leisure Activities", "[\"leisure\", \"hang out\", \"surf the Internet\", \"window shopping\", \"board game\", \"DIY\", \"gardening\", \"crafts\", \"socialise\", \"stroll\"]" },
                    { 2, 8, "[\"Comparative forms of adjectives\", \"Articles: a/an/the\", \"Simple sentences with adjectives\"]", "[\"country life vs city life\", \"farming activities\", \"rural landscapes\"]", 2, "Life in the Countryside", "[\"countryside\", \"paddy field\", \"harvest\", \"nomadic\", \"peaceful\", \"vast\", \"pasture\", \"herd\", \"buffalo\", \"rural\"]" },
                    { 3, 8, "[\"Questions with 'Which/What'\", \"Exclamatory sentences with 'How' and 'What'\", \"Articles with proper nouns\"]", "[\"ethnic minorities in Vietnam\", \"cultural diversity\", \"traditional customs\"]", 3, "Peoples of Viet Nam", "[\"ethnic group\", \"minority\", \"majority\", \"custom\", \"tradition\", \"costume\", \"festival\", \"stilt house\", \"terraced field\", \"heritage\"]" },
                    { 4, 8, "[\"Should/Shouldn't for advice\", \"Have to for obligation\", \"Must for strong obligation\"]", "[\"Vietnamese customs\", \"family traditions\", \"social etiquette\"]", 4, "Our Customs and Traditions", "[\"custom\", \"tradition\", \"praying\", \"worship\", \"respect\", \"ancestor\", \"generation\", \"greeting\", \"polite\", \"palm\"]" },
                    { 5, 8, "[\"Compound and complex sentences\", \"Adverbial clauses of time\", \"Simple past tense review\"]", "[\"Vietnamese festivals\", \"cultural celebrations\", \"traditional ceremonies\"]", 5, "Festivals in Viet Nam", "[\"festival\", \"celebration\", \"worship\", \"offering\", \"lantern\", \"performance\", \"float\", \"parade\", \"incense\", \"ritual\"]" },
                    { 6, 8, "[\"Past continuous tense\", \"Past continuous with 'when' and 'while'\", \"Connectors: First, Then, Next, After that, Finally\"]", "[\"Vietnamese folk tales\", \"moral lessons\", \"storytelling\"]", 6, "Folk Tales", "[\"folk tale\", \"legend\", \"fairy\", \"knight\", \"giant\", \"cunning\", \"brave\", \"wicked\", \"generous\", \"greed\"]" },
                    { 7, 8, "[\"Conditional sentences type 1\", \"Conditional sentences type 2\", \"If-clauses\"]", "[\"types of pollution\", \"causes and effects\", \"environmental solutions\"]", 7, "Pollution", "[\"pollution\", \"contaminate\", \"poison\", \"toxic\", \"dump\", \"sewage\", \"emission\", \"radiation\", \"ecosystem\", \"biodiversity\"]" },
                    { 8, 8, "[\"Present simple for facts\", \"Passive voice (present simple)\", \"Relative clauses with 'which/that/who'\"]", "[\"English-speaking countries\", \"cultures around the world\", \"geography and landmarks\"]", 8, "English Speaking Countries", "[\"official language\", \"accent\", \"native speaker\", \"multicultural\", \"diverse\", \"landmark\", \"currency\", \"population\", \"territory\", \"monarchy\"]" },
                    { 9, 8, "[\"Past perfect tense\", \"Past perfect with 'before/after/when'\", \"Passive voice (past simple)\"]", "[\"types of natural disasters\", \"disaster preparedness\", \"emergency response\"]", 9, "Natural Disasters", "[\"earthquake\", \"flood\", \"drought\", \"tsunami\", \"tornado\", \"volcanic eruption\", \"typhoon\", \"landslide\", \"evacuate\", \"shelter\"]" },
                    { 10, 8, "[\"Future continuous tense\", \"Verbs of perception + bare infinitive/present participle\", \"Reported speech (statements)\"]", "[\"communication methods\", \"body language\", \"online communication etiquette\"]", 10, "Communication", "[\"communicate\", \"body language\", \"gesture\", \"facial expression\", \"verbal\", \"non-verbal\", \"signal\", \"message\", \"netiquette\", \"cyberbullying\"]" },
                    { 11, 8, "[\"Present perfect tense review\", \"Present perfect with 'for/since/already/yet'\", \"Future simple with 'will' for predictions\"]", "[\"inventions and technology\", \"future technology\", \"science in daily life\"]", 11, "Science and Technology", "[\"technology\", \"invention\", \"robot\", \"artificial intelligence\", \"device\", \"gadget\", \"software\", \"hardware\", \"innovation\", \"automatic\"]" },
                    { 12, 8, "[\"May/Might for possibility\", \"Reported speech (questions)\", \"Conditional type 2 review\"]", "[\"space exploration\", \"life on Mars\", \"the solar system\"]", 12, "Life on Other Planets", "[\"planet\", \"spacecraft\", \"astronaut\", \"galaxy\", \"orbit\", \"gravity\", \"alien\", \"atmosphere\", \"solar system\", \"space station\"]" }
                });

            migrationBuilder.InsertData(
                table: "ExerciseSections",
                columns: new[] { "Id", "Code", "Description", "DisplayOrder", "Icon", "Name" },
                values: new object[,]
                {
                    { 1, "pronunciation", "Choose the word whose underlined part is pronounced differently from the others", 1, "record_voice_over", "Pronunciation" },
                    { 2, "stress", "Choose the word having a different stress pattern from the others", 2, "graphic_eq", "Stress Pattern" },
                    { 3, "grammar_vocab", "Choose the correct answer to complete the sentences", 3, "spellcheck", "Grammar & Vocabulary" },
                    { 4, "synonym", "Choose the word(s) CLOSEST in meaning to the underlined word(s)", 4, "compare_arrows", "Synonym (Closest Meaning)" },
                    { 5, "antonym", "Choose the word(s) OPPOSITE in meaning to the underlined word(s)", 5, "swap_horiz", "Antonym (Opposite Meaning)" },
                    { 6, "cloze_test", "Read the passage and choose the correct word for each blank", 6, "article", "Cloze Test" },
                    { 7, "reading", "Read the passage. Decide True/False and choose the correct answer", 7, "menu_book", "Reading Comprehension" },
                    { 8, "sentence_completion", "Complete the sentence with the correct words", 8, "edit_note", "Sentence Completion" },
                    { 9, "word_form", "Use the correct form of the word given in each sentence", 9, "text_rotation_none", "Word Form" },
                    { 10, "paragraph_writing", "Write a paragraph on the given topic (80-100 words)", 10, "draw", "Paragraph Writing" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseResults_ExerciseId",
                table: "ExerciseResults",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseResults_UserId",
                table: "ExerciseResults",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_CurriculumUnitId",
                table: "Exercises",
                column: "CurriculumUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_GeneratedByUserId",
                table: "Exercises",
                column: "GeneratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_SectionId",
                table: "Exercises",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSections_Code",
                table: "ExerciseSections",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseResults");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "CurriculumUnits");

            migrationBuilder.DropTable(
                name: "ExerciseSections");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
