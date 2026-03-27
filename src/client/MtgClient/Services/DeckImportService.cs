using MtgClient.Models;
using System.Text.RegularExpressions;

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
        DeckList result = new DeckList();
        string[] lines = deckText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string section = "deck";

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

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

            Match match = CardLineRegex.Match(line);
            if (!match.Success)
            {
                result.Errors.Add($"Format invalide : \"{line}\"");
                continue;
            }

            int quantity = int.Parse(match.Groups[1].Value);
            string cardName = match.Groups[2].Value;
            string cardId = ToCardId(cardName);

            if (!_knownCardNames.Contains(cardName) && !_knownCardNames.Contains(cardId))
            {
                result.Errors.Add($"Carte inconnue : \"{cardName}\"");
                continue;
            }

            DeckEntry entry = new DeckEntry
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
        HashSet<string> basicLands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "mountain", "forest", "plains", "swamp", "island"
        };

        IEnumerable<DeckEntry> nonBasicEntries = result.Entries.Where(e => !basicLands.Contains(e.CardId));
        foreach (DeckEntry? entry in nonBasicEntries)
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
