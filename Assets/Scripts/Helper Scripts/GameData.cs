using System;

/// <summary>The versioned, JSON-friendly player profile. Contains no Unity dependencies.</summary>
[Serializable]
public sealed class GameData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public int stars;
    public int bestScore;
    public int selectedHero;
    public bool[] unlockedHeroes = new bool[RunnerRules.HeroCount];
    public bool musicEnabled = true;
    public bool sfxEnabled = true;
    public bool reducedMotion;
    public bool tutorialSeen;
    public int totalRuns;
    public int lifetimeStars;

    public GameData()
    {
        unlockedHeroes[0] = true;
    }

    /// <summary>Repair invalid values without granting currency or locking the starter hero.</summary>
    public void Normalize()
    {
        stars = Math.Max(0, stars);
        bestScore = Math.Max(0, bestScore);
        totalRuns = Math.Max(0, totalRuns);
        lifetimeStars = Math.Max(0, lifetimeStars);

        if (unlockedHeroes == null || unlockedHeroes.Length != RunnerRules.HeroCount)
        {
            var repaired = new bool[RunnerRules.HeroCount];
            if (unlockedHeroes != null)
                Array.Copy(unlockedHeroes, repaired, Math.Min(unlockedHeroes.Length, repaired.Length));
            unlockedHeroes = repaired;
        }

        unlockedHeroes[0] = true;
        if (selectedHero < 0 || selectedHero >= unlockedHeroes.Length || !unlockedHeroes[selectedHero])
            selectedHero = 0;
    }
}
