using System.Text.Json;
using System.Text.Json.Serialization;
using MtgEngine.Domain.Interfaces;
using MtgEngine.Shared.Models;

namespace MtgEngine.Infrastructure.Mods;

public sealed class ModManifest
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string Author { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string CompatibleEngineVersion { get; init; } = ">=0.1.0";
}

public sealed class ModInfo
{
    public required ModManifest Manifest { get; init; }
    public required IReadOnlyList<CardDefinition> Cards { get; init; }
}

public sealed class ModLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly ICardRepository _cardRepository;
    private readonly List<ModInfo> _loadedMods = [];

    public IReadOnlyList<ModInfo> LoadedMods => _loadedMods;

    public ModLoader(ICardRepository cardRepository)
    {
        _cardRepository = cardRepository;
    }

    public void LoadModsFromDirectory(string modsDirectory)
    {
        if (!Directory.Exists(modsDirectory))
            return;

        foreach (var modDir in Directory.GetDirectories(modsDirectory))
        {
            try
            {
                var mod = LoadMod(modDir);
                if (mod != null)
                    _loadedMods.Add(mod);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load mod from {modDir}: {ex.Message}");
            }
        }
    }

    private ModInfo? LoadMod(string modDirectory)
    {
        var manifestPath = Path.Combine(modDirectory, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            Console.Error.WriteLine($"Mod at {modDirectory} missing manifest.json — skipped");
            return null;
        }

        var manifestJson = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<ModManifest>(manifestJson, JsonOptions);
        if (manifest == null)
        {
            Console.Error.WriteLine($"Invalid manifest at {manifestPath} — skipped");
            return null;
        }

        var cards = new List<CardDefinition>();
        var cardsPath = Path.Combine(modDirectory, "cards.json");
        if (File.Exists(cardsPath))
        {
            var cardsJson = File.ReadAllText(cardsPath);
            var modCards = JsonSerializer.Deserialize<List<CardDefinition>>(cardsJson, JsonOptions);
            if (modCards != null)
            {
                // Validate: no conflicts with core cards
                foreach (var card in modCards)
                {
                    if (_cardRepository.Exists(card.Id))
                    {
                        Console.Error.WriteLine($"Mod {manifest.Id}: card {card.Id} conflicts with core — skipped");
                        continue;
                    }
                    cards.Add(card);
                    _cardRepository.Register(card);
                }
            }
        }

        Console.WriteLine($"Loaded mod: {manifest.Name} v{manifest.Version} ({cards.Count} cards)");

        return new ModInfo
        {
            Manifest = manifest,
            Cards = cards
        };
    }
}
