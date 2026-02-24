namespace SudokuArena.Infrastructure.Configuration;

public sealed class DataStoreOptions
{
    public const string SectionName = "DataStore";

    public DataStoreMode Mode { get; init; } = DataStoreMode.LocalSqlite;

    public string LocalSqliteConnectionString { get; init; } = "Data Source=sudokuarena.db";

    public string CloudPostgresConnectionString { get; init; } = string.Empty;
}

public enum DataStoreMode
{
    LocalSqlite = 0,
    CloudPostgres = 1
}
