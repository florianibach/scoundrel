using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=/data/scoundrel.db";

builder.Services.AddSingleton(new Database(connectionString));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:4173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("frontend");

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Database>();
    await db.InitializeAsync();
    await db.SeedAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/profiles", async (Database db) => Results.Ok(await db.GetProfilesAsync()));
app.MapPost("/api/profiles", async (Database db, Profile profile) =>
{
    if (string.IsNullOrWhiteSpace(profile.Name))
    {
        return Results.BadRequest("Name is required.");
    }

    var created = await db.CreateProfileAsync(profile.Name, profile.AvatarUrl);
    return Results.Created($"/api/profiles/{created.Id}", created);
});

app.MapGet("/api/rulesets", async (Database db) => Results.Ok(await db.GetRulesetsAsync()));
app.MapPost("/api/rulesets", async (Database db, Ruleset ruleset) =>
{
    if (string.IsNullOrWhiteSpace(ruleset.Name))
    {
        return Results.BadRequest("Name is required.");
    }

    var created = await db.CreateRulesetAsync(ruleset.Name, ruleset.Description);
    return Results.Created($"/api/rulesets/{created.Id}", created);
});

app.MapGet("/api/sessions", async (Database db) => Results.Ok(await db.GetGameSessionsAsync()));
app.MapPost("/api/sessions", async (Database db, CreateGameSession request) =>
{
    if (request.ProfileId <= 0 || request.RulesetId <= 0)
    {
        return Results.BadRequest("Valid profileId and rulesetId are required.");
    }

    var created = await db.CreateGameSessionAsync(request.ProfileId, request.RulesetId, request.Score, request.DurationSeconds);
    return Results.Created($"/api/sessions/{created.Id}", created);
});

app.MapGet("/api/achievements", async (Database db) => Results.Ok(await db.GetAchievementsAsync()));
app.MapGet("/api/leaderboard", async (Database db) => Results.Ok(await db.GetLeaderboardAsync()));

app.Run();

record Profile(long Id, string Name, string? AvatarUrl, DateTime CreatedAt);
record Ruleset(long Id, string Name, string? Description, DateTime CreatedAt);
record GameSession(long Id, long ProfileId, long RulesetId, int Score, int DurationSeconds, DateTime CreatedAt);
record Achievement(long Id, long ProfileId, string Title, string Description, DateTime EarnedAt);
record LeaderboardEntry(long ProfileId, string ProfileName, int BestScore, int SessionsPlayed, int TotalScore);
record CreateGameSession(long ProfileId, long RulesetId, int Score, int DurationSeconds);

sealed class Database(string connectionString)
{
    public async Task InitializeAsync()
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Profiles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                AvatarUrl TEXT NULL,
                CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS Rulesets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT NULL,
                CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS GameSessions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProfileId INTEGER NOT NULL,
                RulesetId INTEGER NOT NULL,
                Score INTEGER NOT NULL,
                DurationSeconds INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY(ProfileId) REFERENCES Profiles(Id),
                FOREIGN KEY(RulesetId) REFERENCES Rulesets(Id)
            );

            CREATE TABLE IF NOT EXISTS Achievements (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProfileId INTEGER NOT NULL,
                Title TEXT NOT NULL,
                Description TEXT NOT NULL,
                EarnedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY(ProfileId) REFERENCES Profiles(Id)
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    public async Task SeedAsync()
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM Profiles;";
        var profileCount = (long)(await countCommand.ExecuteScalarAsync() ?? 0L);
        if (profileCount > 0)
        {
            return;
        }

        await CreateProfileAsync("Ava", "https://i.pravatar.cc/80?img=5");
        await CreateProfileAsync("Kai", "https://i.pravatar.cc/80?img=11");

        await CreateRulesetAsync("Classic", "Standard scoring and deck.");
        await CreateRulesetAsync("Speedrun", "Short rounds, high intensity.");

        await CreateGameSessionAsync(1, 1, 1200, 420);
        await CreateGameSessionAsync(1, 2, 890, 210);
        await CreateGameSessionAsync(2, 1, 1430, 530);

        await CreateAchievementAsync(1, "First Blood", "Completed the first session.");
        await CreateAchievementAsync(2, "High Roller", "Scored above 1400 points.");
    }

    public async Task<List<Profile>> GetProfilesAsync()
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, AvatarUrl, CreatedAt FROM Profiles ORDER BY CreatedAt DESC;";

        var result = new List<Profile>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new Profile(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                DateTime.Parse(reader.GetString(3))));
        }

        return result;
    }

    public async Task<Profile> CreateProfileAsync(string name, string? avatarUrl)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Profiles (Name, AvatarUrl)
            VALUES ($name, $avatarUrl);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$avatarUrl", (object?)avatarUrl ?? DBNull.Value);

        var id = (long)(await command.ExecuteScalarAsync() ?? 0L);
        return new Profile(id, name, avatarUrl, DateTime.UtcNow);
    }

    public async Task<List<Ruleset>> GetRulesetsAsync()
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Description, CreatedAt FROM Rulesets ORDER BY CreatedAt DESC;";

        var result = new List<Ruleset>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new Ruleset(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                DateTime.Parse(reader.GetString(3))));
        }

        return result;
    }

    public async Task<Ruleset> CreateRulesetAsync(string name, string? description)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Rulesets (Name, Description)
            VALUES ($name, $description);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$description", (object?)description ?? DBNull.Value);

        var id = (long)(await command.ExecuteScalarAsync() ?? 0L);
        return new Ruleset(id, name, description, DateTime.UtcNow);
    }

    public async Task<List<GameSession>> GetGameSessionsAsync()
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, ProfileId, RulesetId, Score, DurationSeconds, CreatedAt FROM GameSessions ORDER BY CreatedAt DESC;";

        var result = new List<GameSession>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new GameSession(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetInt64(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                DateTime.Parse(reader.GetString(5))));
        }

        return result;
    }

    public async Task<GameSession> CreateGameSessionAsync(long profileId, long rulesetId, int score, int durationSeconds)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO GameSessions (ProfileId, RulesetId, Score, DurationSeconds)
            VALUES ($profileId, $rulesetId, $score, $durationSeconds);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$profileId", profileId);
        command.Parameters.AddWithValue("$rulesetId", rulesetId);
        command.Parameters.AddWithValue("$score", score);
        command.Parameters.AddWithValue("$durationSeconds", durationSeconds);

        var id = (long)(await command.ExecuteScalarAsync() ?? 0L);
        return new GameSession(id, profileId, rulesetId, score, durationSeconds, DateTime.UtcNow);
    }

    public async Task<List<Achievement>> GetAchievementsAsync()
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, ProfileId, Title, Description, EarnedAt FROM Achievements ORDER BY EarnedAt DESC;";

        var result = new List<Achievement>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new Achievement(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetString(2),
                reader.GetString(3),
                DateTime.Parse(reader.GetString(4))));
        }

        return result;
    }

    private async Task<Achievement> CreateAchievementAsync(long profileId, string title, string description)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Achievements (ProfileId, Title, Description)
            VALUES ($profileId, $title, $description);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$profileId", profileId);
        command.Parameters.AddWithValue("$title", title);
        command.Parameters.AddWithValue("$description", description);

        var id = (long)(await command.ExecuteScalarAsync() ?? 0L);
        return new Achievement(id, profileId, title, description, DateTime.UtcNow);
    }

    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync()
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                p.Id,
                p.Name,
                MAX(gs.Score) AS BestScore,
                COUNT(gs.Id) AS SessionsPlayed,
                COALESCE(SUM(gs.Score), 0) AS TotalScore
            FROM Profiles p
            LEFT JOIN GameSessions gs ON gs.ProfileId = p.Id
            GROUP BY p.Id, p.Name
            ORDER BY BestScore DESC, TotalScore DESC;
            """;

        var result = new List<LeaderboardEntry>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new LeaderboardEntry(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                reader.GetInt32(3),
                reader.GetInt32(4)));
        }

        return result;
    }
}
