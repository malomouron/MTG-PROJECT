using System.Text.Json;
using System.Text.Json.Serialization;
using MtgEngine.Domain.Interfaces;
using MtgEngine.Shared.Models;

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
        var cards = new List<CardDefinition>();

        if (!Directory.Exists(directoryPath))
            return cards;

        foreach (var file in Directory.GetFiles(directoryPath, "*.json"))
        {
            var json = File.ReadAllText(file);
            var card = JsonSerializer.Deserialize<CardDefinition>(json, JsonOptions);
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
