using System;

public enum HeroSelectionResult
{
    Selected,
    Purchased,
    AlreadySelected,
    NotEnoughStars,
    InvalidHero
}

/// <summary>Shared balance and economy rules, also exercised outside Unity.</summary>
public static class RunnerRules
{
    public const int HeroCount = 9;
    public const int HeroPrice = 1000;
    public const float PowerUpSeconds = 7f;
    public const float FirstPaceThreshold = 30f;
    public const float SecondPaceThreshold = 60f;

    public static int SaturatingAdd(int value, int amount)
    {
        return (int)Math.Min(int.MaxValue, (long)Math.Max(0, value) + Math.Max(0, amount));
    }

    public static HeroSelectionResult SelectHero(GameData data, int index)
    {
        if (data == null || index < 0 || index >= HeroCount)
            return HeroSelectionResult.InvalidHero;

        data.Normalize();
        if (data.selectedHero == index)
            return HeroSelectionResult.AlreadySelected;

        bool purchased = !data.unlockedHeroes[index];
        if (purchased)
        {
            if (data.stars < HeroPrice)
                return HeroSelectionResult.NotEnoughStars;
            data.stars -= HeroPrice;
            data.unlockedHeroes[index] = true;
        }

        data.selectedHero = index;
        return purchased ? HeroSelectionResult.Purchased : HeroSelectionResult.Selected;
    }

    public static float TargetSpeed(float score)
    {
        if (score >= SecondPaceThreshold) return 16f;
        if (score >= FirstPaceThreshold) return 14f;
        return 12f;
    }

    public static float EaseOutCubic(float t)
    {
        t = Math.Max(0f, Math.Min(1f, t));
        float remaining = 1f - t;
        return 1f - remaining * remaining * remaining;
    }

    /// <summary>A bounded pool search: an empty or exhausted pool returns -1, never spins.</summary>
    public static int FindAvailableIndex(int count, int start, Func<int, bool> isAvailable)
    {
        if (count <= 0 || isAvailable == null) return -1;
        start = ((start % count) + count) % count;
        for (int i = 0; i < count; i++)
        {
            int index = (start + i) % count;
            if (isAvailable(index)) return index;
        }
        return -1;
    }
}
