using ExamBuilderAI.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamBuilderAI.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<CurriculumUnit> CurriculumUnits => Set<CurriculumUnit>();
    public DbSet<ExerciseSection> ExerciseSections => Set<ExerciseSection>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseResult> ExerciseResults => Set<ExerciseResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
        });

        // ExerciseSection
        modelBuilder.Entity<ExerciseSection>(e =>
        {
            e.HasIndex(s => s.Code).IsUnique();
        });

        // Exercise
        modelBuilder.Entity<Exercise>(e =>
        {
            e.HasOne(x => x.Section)
                .WithMany(s => s.Exercises)
                .HasForeignKey(x => x.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CurriculumUnit)
                .WithMany(c => c.Exercises)
                .HasForeignKey(x => x.CurriculumUnitId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.GeneratedByUser)
                .WithMany(u => u.GeneratedExercises)
                .HasForeignKey(x => x.GeneratedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ExerciseResult
        modelBuilder.Entity<ExerciseResult>(e =>
        {
            e.HasOne(r => r.User)
                .WithMany(u => u.ExerciseResults)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Exercise)
                .WithMany(ex => ex.Results)
                .HasForeignKey(r => r.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // === SEED DATA ===
        SeedExerciseSections(modelBuilder);
        SeedCurriculumUnits(modelBuilder);
    }

    private static void SeedExerciseSections(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExerciseSection>().HasData(
            new ExerciseSection { Id = 1, Code = "pronunciation", Name = "Pronunciation", Description = "Choose the word whose underlined part is pronounced differently from the others", Icon = "record_voice_over", DisplayOrder = 1 },
            new ExerciseSection { Id = 2, Code = "stress", Name = "Stress Pattern", Description = "Choose the word having a different stress pattern from the others", Icon = "graphic_eq", DisplayOrder = 2 },
            new ExerciseSection { Id = 3, Code = "grammar_vocab", Name = "Grammar & Vocabulary", Description = "Choose the correct answer to complete the sentences", Icon = "spellcheck", DisplayOrder = 3 },
            new ExerciseSection { Id = 4, Code = "synonym", Name = "Synonym (Closest Meaning)", Description = "Choose the word(s) CLOSEST in meaning to the underlined word(s)", Icon = "compare_arrows", DisplayOrder = 4 },
            new ExerciseSection { Id = 5, Code = "antonym", Name = "Antonym (Opposite Meaning)", Description = "Choose the word(s) OPPOSITE in meaning to the underlined word(s)", Icon = "swap_horiz", DisplayOrder = 5 },
            new ExerciseSection { Id = 6, Code = "cloze_test", Name = "Cloze Test", Description = "Read the passage and choose the correct word for each blank", Icon = "article", DisplayOrder = 6 },
            new ExerciseSection { Id = 7, Code = "reading", Name = "Reading Comprehension", Description = "Read the passage. Decide True/False and choose the correct answer", Icon = "menu_book", DisplayOrder = 7 },
            new ExerciseSection { Id = 8, Code = "sentence_completion", Name = "Sentence Completion", Description = "Complete the sentence with the correct words", Icon = "edit_note", DisplayOrder = 8 },
            new ExerciseSection { Id = 9, Code = "word_form", Name = "Word Form", Description = "Use the correct form of the word given in each sentence", Icon = "text_rotation_none", DisplayOrder = 9 },
            new ExerciseSection { Id = 10, Code = "paragraph_writing", Name = "Paragraph Writing", Description = "Write a paragraph on the given topic (80-100 words)", Icon = "draw", DisplayOrder = 10 }
        );
    }

    private static void SeedCurriculumUnits(ModelBuilder modelBuilder)
    {
        // Tiếng Anh 8 Global Success — 12 Units
        modelBuilder.Entity<CurriculumUnit>().HasData(
            new CurriculumUnit
            {
                Id = 1, Grade = 8, UnitNumber = 1, UnitTitle = "Leisure Activities",
                GrammarPoints = "[\"Gerunds as subjects and objects\", \"Verbs of liking + gerunds\", \"Adverbs of frequency\"]",
                Vocabulary = "[\"leisure\", \"hang out\", \"surf the Internet\", \"window shopping\", \"board game\", \"DIY\", \"gardening\", \"crafts\", \"socialise\", \"stroll\"]",
                Topics = "[\"free time activities\", \"hobbies\", \"leisure habits of teenagers\"]"
            },
            new CurriculumUnit
            {
                Id = 2, Grade = 8, UnitNumber = 2, UnitTitle = "Life in the Countryside",
                GrammarPoints = "[\"Comparative forms of adjectives\", \"Articles: a/an/the\", \"Simple sentences with adjectives\"]",
                Vocabulary = "[\"countryside\", \"paddy field\", \"harvest\", \"nomadic\", \"peaceful\", \"vast\", \"pasture\", \"herd\", \"buffalo\", \"rural\"]",
                Topics = "[\"country life vs city life\", \"farming activities\", \"rural landscapes\"]"
            },
            new CurriculumUnit
            {
                Id = 3, Grade = 8, UnitNumber = 3, UnitTitle = "Peoples of Viet Nam",
                GrammarPoints = "[\"Questions with 'Which/What'\", \"Exclamatory sentences with 'How' and 'What'\", \"Articles with proper nouns\"]",
                Vocabulary = "[\"ethnic group\", \"minority\", \"majority\", \"custom\", \"tradition\", \"costume\", \"festival\", \"stilt house\", \"terraced field\", \"heritage\"]",
                Topics = "[\"ethnic minorities in Vietnam\", \"cultural diversity\", \"traditional customs\"]"
            },
            new CurriculumUnit
            {
                Id = 4, Grade = 8, UnitNumber = 4, UnitTitle = "Our Customs and Traditions",
                GrammarPoints = "[\"Should/Shouldn't for advice\", \"Have to for obligation\", \"Must for strong obligation\"]",
                Vocabulary = "[\"custom\", \"tradition\", \"praying\", \"worship\", \"respect\", \"ancestor\", \"generation\", \"greeting\", \"polite\", \"palm\"]",
                Topics = "[\"Vietnamese customs\", \"family traditions\", \"social etiquette\"]"
            },
            new CurriculumUnit
            {
                Id = 5, Grade = 8, UnitNumber = 5, UnitTitle = "Festivals in Viet Nam",
                GrammarPoints = "[\"Compound and complex sentences\", \"Adverbial clauses of time\", \"Simple past tense review\"]",
                Vocabulary = "[\"festival\", \"celebration\", \"worship\", \"offering\", \"lantern\", \"performance\", \"float\", \"parade\", \"incense\", \"ritual\"]",
                Topics = "[\"Vietnamese festivals\", \"cultural celebrations\", \"traditional ceremonies\"]"
            },
            new CurriculumUnit
            {
                Id = 6, Grade = 8, UnitNumber = 6, UnitTitle = "Folk Tales",
                GrammarPoints = "[\"Past continuous tense\", \"Past continuous with 'when' and 'while'\", \"Connectors: First, Then, Next, After that, Finally\"]",
                Vocabulary = "[\"folk tale\", \"legend\", \"fairy\", \"knight\", \"giant\", \"cunning\", \"brave\", \"wicked\", \"generous\", \"greed\"]",
                Topics = "[\"Vietnamese folk tales\", \"moral lessons\", \"storytelling\"]"
            },
            new CurriculumUnit
            {
                Id = 7, Grade = 8, UnitNumber = 7, UnitTitle = "Pollution",
                GrammarPoints = "[\"Conditional sentences type 1\", \"Conditional sentences type 2\", \"If-clauses\"]",
                Vocabulary = "[\"pollution\", \"contaminate\", \"poison\", \"toxic\", \"dump\", \"sewage\", \"emission\", \"radiation\", \"ecosystem\", \"biodiversity\"]",
                Topics = "[\"types of pollution\", \"causes and effects\", \"environmental solutions\"]"
            },
            new CurriculumUnit
            {
                Id = 8, Grade = 8, UnitNumber = 8, UnitTitle = "English Speaking Countries",
                GrammarPoints = "[\"Present simple for facts\", \"Passive voice (present simple)\", \"Relative clauses with 'which/that/who'\"]",
                Vocabulary = "[\"official language\", \"accent\", \"native speaker\", \"multicultural\", \"diverse\", \"landmark\", \"currency\", \"population\", \"territory\", \"monarchy\"]",
                Topics = "[\"English-speaking countries\", \"cultures around the world\", \"geography and landmarks\"]"
            },
            new CurriculumUnit
            {
                Id = 9, Grade = 8, UnitNumber = 9, UnitTitle = "Natural Disasters",
                GrammarPoints = "[\"Past perfect tense\", \"Past perfect with 'before/after/when'\", \"Passive voice (past simple)\"]",
                Vocabulary = "[\"earthquake\", \"flood\", \"drought\", \"tsunami\", \"tornado\", \"volcanic eruption\", \"typhoon\", \"landslide\", \"evacuate\", \"shelter\"]",
                Topics = "[\"types of natural disasters\", \"disaster preparedness\", \"emergency response\"]"
            },
            new CurriculumUnit
            {
                Id = 10, Grade = 8, UnitNumber = 10, UnitTitle = "Communication",
                GrammarPoints = "[\"Future continuous tense\", \"Verbs of perception + bare infinitive/present participle\", \"Reported speech (statements)\"]",
                Vocabulary = "[\"communicate\", \"body language\", \"gesture\", \"facial expression\", \"verbal\", \"non-verbal\", \"signal\", \"message\", \"netiquette\", \"cyberbullying\"]",
                Topics = "[\"communication methods\", \"body language\", \"online communication etiquette\"]"
            },
            new CurriculumUnit
            {
                Id = 11, Grade = 8, UnitNumber = 11, UnitTitle = "Science and Technology",
                GrammarPoints = "[\"Present perfect tense review\", \"Present perfect with 'for/since/already/yet'\", \"Future simple with 'will' for predictions\"]",
                Vocabulary = "[\"technology\", \"invention\", \"robot\", \"artificial intelligence\", \"device\", \"gadget\", \"software\", \"hardware\", \"innovation\", \"automatic\"]",
                Topics = "[\"inventions and technology\", \"future technology\", \"science in daily life\"]"
            },
            new CurriculumUnit
            {
                Id = 12, Grade = 8, UnitNumber = 12, UnitTitle = "Life on Other Planets",
                GrammarPoints = "[\"May/Might for possibility\", \"Reported speech (questions)\", \"Conditional type 2 review\"]",
                Vocabulary = "[\"planet\", \"spacecraft\", \"astronaut\", \"galaxy\", \"orbit\", \"gravity\", \"alien\", \"atmosphere\", \"solar system\", \"space station\"]",
                Topics = "[\"space exploration\", \"life on Mars\", \"the solar system\"]"
            }
        );
    }
}
