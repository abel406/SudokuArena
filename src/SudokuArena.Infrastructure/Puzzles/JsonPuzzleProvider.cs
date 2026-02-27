using System.Text.Json;
using SudokuArena.Application.Puzzles;
using SudokuArena.Domain.Models;

namespace SudokuArena.Infrastructure.Puzzles;

public sealed class JsonPuzzleProvider : IPuzzleProvider
{
    private readonly IReadOnlyDictionary<DifficultyTier, IReadOnlyList<PuzzleDefinition>> _byTier;
    private readonly Dictionary<DifficultyTier, int> _nextIndexByTier;
    private readonly Dictionary<DifficultyTier, string?> _lastPuzzleIdByTier;
    private readonly object _sync = new();

    public JsonPuzzleProvider(string datasetPath)
    {
        if (string.IsNullOrWhiteSpace(datasetPath))
        {
            throw new ArgumentException("Dataset path cannot be empty.", nameof(datasetPath));
        }

        if (!File.Exists(datasetPath))
        {
            throw new FileNotFoundException("Puzzle dataset file was not found.", datasetPath);
        }

        var json = File.ReadAllText(datasetPath);
        var document = JsonSerializer.Deserialize<PuzzleDatasetDocument>(json)
            ?? throw new FormatException("Puzzle dataset JSON could not be deserialized.");

        _byTier = BuildValidatedIndex(document);
        _nextIndexByTier = Enum.GetValues<DifficultyTier>()
            .ToDictionary(tier => tier, _ => 0);
        _lastPuzzleIdByTier = Enum.GetValues<DifficultyTier>()
            .ToDictionary(tier => tier, _ => (string?)null);
    }

    public PuzzleDefinition? GetNext(DifficultyTier difficultyTier)
    {
        lock (_sync)
        {
            foreach (var tier in BuildTierResolutionOrder(difficultyTier))
            {
                if (!_byTier.TryGetValue(tier, out var definitions) || definitions.Count == 0)
                {
                    continue;
                }

                return SelectNextForTier(tier, definitions);
            }

            return null;
        }
    }

    private PuzzleDefinition SelectNextForTier(DifficultyTier tier, IReadOnlyList<PuzzleDefinition> definitions)
    {
        var currentIndex = _nextIndexByTier[tier];
        var selected = definitions[currentIndex % definitions.Count];
        if (definitions.Count > 1 &&
            _lastPuzzleIdByTier.TryGetValue(tier, out var lastPuzzleId) &&
            string.Equals(selected.PuzzleId, lastPuzzleId, StringComparison.Ordinal))
        {
            currentIndex++;
            selected = definitions[currentIndex % definitions.Count];
        }

        _nextIndexByTier[tier] = (currentIndex + 1) % definitions.Count;
        _lastPuzzleIdByTier[tier] = selected.PuzzleId;
        return selected;
    }

    private static IReadOnlyList<DifficultyTier> BuildTierResolutionOrder(DifficultyTier requestedTier)
    {
        var tiers = Enum.GetValues<DifficultyTier>();
        var requestedIndex = Array.IndexOf(tiers, requestedTier);
        var resolutionOrder = new List<DifficultyTier>(tiers.Length);
        for (var offset = 0; offset < tiers.Length; offset++)
        {
            var lowerIndex = requestedIndex - offset;
            if (lowerIndex >= 0)
            {
                resolutionOrder.Add(tiers[lowerIndex]);
            }

            var upperIndex = requestedIndex + offset;
            if (offset == 0 || upperIndex >= tiers.Length)
            {
                continue;
            }

            resolutionOrder.Add(tiers[upperIndex]);
        }

        return resolutionOrder;
    }

    private static IReadOnlyDictionary<DifficultyTier, IReadOnlyList<PuzzleDefinition>> BuildValidatedIndex(
        PuzzleDatasetDocument document)
    {
        if (!string.Equals(document.SchemaVersion, PuzzleDatasetSchema.Version1, StringComparison.Ordinal))
        {
            throw new FormatException(
                $"Unsupported puzzle dataset schema_version '{document.SchemaVersion}'. Expected '{PuzzleDatasetSchema.Version1}'.");
        }

        if (document.QuestionBank.Count == 0)
        {
            throw new FormatException("Puzzle dataset must contain at least one question in question_bank.");
        }

        var seenPuzzleIds = new HashSet<string>(StringComparer.Ordinal);
        var definitions = new List<PuzzleDefinition>(document.QuestionBank.Count);
        foreach (var entry in document.QuestionBank)
        {
            if (entry.BoardKind is not PuzzleBoardKind.Classic9x9)
            {
                // Current desktop runtime supports classic 9x9 only.
                continue;
            }

            ValidateQuestionEntry(entry, seenPuzzleIds);

            if (!document.SolverDetails.TryGetValue(entry.PuzzleId, out var solverDetail))
            {
                throw new FormatException($"Missing solver_details entry for puzzle_id '{entry.PuzzleId}'.");
            }

            if (!document.TimeMap.TryGetValue(entry.PuzzleId, out var timeBuckets))
            {
                throw new FormatException($"Missing time_map entry for puzzle_id '{entry.PuzzleId}'.");
            }

            if (timeBuckets.Length != 4 || timeBuckets.Any(bucket => bucket <= 0))
            {
                throw new FormatException($"Invalid time_map for puzzle_id '{entry.PuzzleId}'. Expected 4 positive values.");
            }

            var resolvedMode = PuzzleModeResolver.Resolve(entry.BoardKind, entry.DifficultyTier, entry.Mode);
            definitions.Add(new PuzzleDefinition(
                entry.PuzzleId,
                entry.Puzzle,
                entry.Solution,
                entry.DifficultyTier,
                entry.GivenCount,
                solverDetail.WeightedScoreEstimate,
                solverDetail.MaxTechniqueRate,
                solverDetail.AdvancedHits,
                entry.BoardKind,
                resolvedMode,
                timeBuckets.ToArray()));
        }

        if (definitions.Count == 0)
        {
            throw new FormatException("Puzzle dataset does not contain supported Classic9x9 entries.");
        }

        return definitions
            .GroupBy(definition => definition.DifficultyTier)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<PuzzleDefinition>)group.ToList());
    }

    private static void ValidateQuestionEntry(PuzzleQuestionEntry entry, ISet<string> seenPuzzleIds)
    {
        if (string.IsNullOrWhiteSpace(entry.PuzzleId))
        {
            throw new FormatException("Puzzle id cannot be empty.");
        }

        if (!seenPuzzleIds.Add(entry.PuzzleId))
        {
            throw new FormatException($"Duplicate puzzle_id detected: '{entry.PuzzleId}'.");
        }

        if (entry.Puzzle.Length != 81)
        {
            throw new FormatException($"Puzzle '{entry.PuzzleId}' must contain exactly 81 chars.");
        }

        if (entry.Solution.Length != 81 || entry.Solution.Any(character => character is < '1' or > '9'))
        {
            throw new FormatException($"Solution '{entry.PuzzleId}' must contain exactly 81 digits from 1 to 9.");
        }

        if (entry.GivenCount is < 0 or > 81)
        {
            throw new FormatException($"Puzzle '{entry.PuzzleId}' has invalid given_count '{entry.GivenCount}'.");
        }

        _ = SudokuBoard.CreateFromString(entry.Puzzle);

        var computedGivenCount = entry.Puzzle.Count(character => character is >= '1' and <= '9');
        if (computedGivenCount != entry.GivenCount)
        {
            throw new FormatException(
                $"Puzzle '{entry.PuzzleId}' has mismatched given_count. Expected {computedGivenCount}, got {entry.GivenCount}.");
        }

        for (var i = 0; i < entry.Puzzle.Length; i++)
        {
            var puzzleChar = entry.Puzzle[i];
            if (puzzleChar is '.' or '0')
            {
                continue;
            }

            if (puzzleChar != entry.Solution[i])
            {
                throw new FormatException(
                    $"Puzzle '{entry.PuzzleId}' contains a given that does not match its solution at index {i}.");
            }
        }
    }
}
