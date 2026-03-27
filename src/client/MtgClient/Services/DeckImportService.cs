using System.Text.RegularExpressions;
using MtgClient.Models;

namespace MtgClient.Services;

public sealed class DeckImportService
{
    private static readonly Regex CardLineRegex = new(
        @"^\s*(\d+)x?\s+(.+?)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly HashSet<string> _knownCardNames;

    public DeckImportService(IEnumerable<string> knownCardNames)
    {
        _knownCardNames = new HashSet<string>(knownCardNames, StringComparer.OrdinalIgnoreCase);
    }

    public DeckList Parse(string deckText)
    {
        var result = new DeckList();
        var lines = deckText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var section = "deck";

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Skip comments
            if (line.StartsWith("//") || string.IsNullOrWhiteSpace(line))
                continue;

            // Section markers
            if (line.Equals("COMMANDER", StringComparison.OrdinalIgnoreCase))
            {
                section = "commander";
                continue;
            }
            if (line.Equals("DECK", StringComparison.OrdinalIgnoreCase))
            {
                section = "deck";
                continue;
            }

            var match = CardLineRegex.Match(line);
            if (!match.Success)
            {
                result.Errors.Add($"Format invalide : \"{line}\"");
                continue;
            }

            var quantity = int.Parse(match.Groups[1].Value);
            var cardName = match.Groups[2].Value;
            var cardId = ToCardId(cardName);

            if (!_knownCardNames.Contains(cardName) && !_knownCardNames.Contains(cardId))
            {
                result.Errors.Add($"Carte inconnue : \"{cardName}\"");
                continue;
            }

            var entry = new DeckEntry
            {
                CardId = cardId,
                CardName = cardName,
                Quantity = quantity
            };

            result.Entries.Add(entry);

            if (section == "commander")
            {
                result.CommanderId = cardId;
            }
        }

        // Validate
        if (result.CommanderId == null)
            result.Errors.Add("Aucun commandant spécifié (ajouter section COMMANDER)");

        if (result.TotalCards != 100)
            result.Errors.Add($"Le deck doit contenir exactement 100 cartes ({result.TotalCards} trouvées)");

        // Check for duplicates (basic lands exempted)
        var basicLands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "mountain", "forest", "plains", "swamp", "island"
        };

        var nonBasicEntries = result.Entries.Where(e => !basicLands.Contains(e.CardId));
        foreach (var entry in nonBasicEntries)
        {
            if (entry.Quantity > 1)
                result.Errors.Add($"Doublon interdit en Commander : \"{entry.CardName}\" x{entry.Quantity}");
        }

        return result;
    }

    private static string ToCardId(string cardName)
    {
        return cardName
            .ToLowerInvariant()
            .Replace(' ', '_')
            .Replace(",", "")
            .Replace("'", "")
            .Replace("'", "");
    }
}
