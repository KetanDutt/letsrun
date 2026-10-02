using System;
using System.IO;
using System.Text.Json;

/// <summary>No NuGet packages or Unity installation are needed for this core contract harness.</summary>
internal static class Program
{
    private static int Main()
    {
        int passed = 0, failed = 0;
        foreach (CoreContractCase test in CoreContractCases.Create(Encode, Decode))
        {
            try { test.Test(); passed++; Console.WriteLine("PASS " + test.Name); }
            catch (Exception error) { failed++; Console.Error.WriteLine("FAIL " + test.Name + "\n" + error); }
        }
        Console.WriteLine("\n" + passed + " passed; " + failed + " failed.");
        return failed == 0 ? 0 : 1;
    }

    private static string Encode(GameData data)
    {
        // Anonymous properties keep this codec compatible with both older and current .NET hosts.
        return JsonSerializer.Serialize(new {
            data.version, data.stars, data.bestScore, data.selectedHero, data.unlockedHeroes,
            data.musicEnabled, data.sfxEnabled, data.reducedMotion, data.tutorialSeen,
            data.totalRuns, data.lifetimeStars
        });
    }

    private static GameData Decode(string json)
    {
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            JsonElement version, heroes;
            if (!root.TryGetProperty("version", out version) || version.GetInt32() < 1)
                throw new InvalidDataException("Missing profile version.");
            if (version.GetInt32() != GameData.CurrentVersion) return new GameData { version = version.GetInt32() };
            if (!root.TryGetProperty("unlockedHeroes", out heroes) || heroes.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("Missing profile avatar list.");
            var data = new GameData { version = version.GetInt32(), unlockedHeroes = new bool[heroes.GetArrayLength()] };
            int i = 0;
            foreach (JsonElement hero in heroes.EnumerateArray()) data.unlockedHeroes[i++] = hero.GetBoolean();
            data.stars = Number(root, "stars"); data.bestScore = Number(root, "bestScore");
            data.selectedHero = Number(root, "selectedHero"); data.totalRuns = Number(root, "totalRuns");
            data.lifetimeStars = Number(root, "lifetimeStars");
            data.musicEnabled = Boolean(root, "musicEnabled", true); data.sfxEnabled = Boolean(root, "sfxEnabled", true);
            data.reducedMotion = Boolean(root, "reducedMotion", false); data.tutorialSeen = Boolean(root, "tutorialSeen", false);
            return data;
        }
    }

    private static int Number(JsonElement root, string key)
    {
        JsonElement value; return root.TryGetProperty(key, out value) ? value.GetInt32() : 0;
    }
    private static bool Boolean(JsonElement root, string key, bool fallback)
    {
        JsonElement value; return root.TryGetProperty(key, out value) ? value.GetBoolean() : fallback;
    }
}
