using MtgEngine.Shared.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MtgEngine.Application.CardEngine;

public sealed class CardLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public IReadOnlyList<CardDefinition> LoadFromDirectory(string directoryPath)
    {
        List<CardDefinition> cards = new List<CardDefinition>();

        if (!Directory.Exists(directoryPath))
            return cards;

        foreach (string file in Directory.GetFiles(directoryPath, "*.json"))
        {
            string json = File.ReadAllText(file);
            CardDefinition? card = JsonSerializer.Deserialize<CardDefinition>(json, JsonOptions);
            if (card != null)
                cards.Add(card);
        }

        return cards;
    }

    public IReadOnlyList<CardDefinition> LoadFromJsonArray(string json)
    {
        return JsonSerializer.Deserialize<List<CardDefinition>>(json, JsonOptions)
            ?? [];
    }
}
