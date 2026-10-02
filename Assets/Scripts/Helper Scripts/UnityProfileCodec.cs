using System;
using System.IO;
using UnityEngine;

/// <summary>Unity's field-based JSON adapter for the engine-independent SaveStore.</summary>
public static class UnityProfileCodec
{
    [Serializable]
    private sealed class VersionHeader { public int version = 0; }
    [Serializable]
    private sealed class AvatarHeader { public bool[] unlockedHeroes = null; }

    public static string Serialize(GameData data) { return JsonUtility.ToJson(data, true); }

    public static GameData Deserialize(string json)
    {
        // Read the stable version header before interpreting version-specific fields.
        VersionHeader header = JsonUtility.FromJson<VersionHeader>(json);
        if (header == null || header.version < 1)
            throw new InvalidDataException("Missing profile version.");
        if (header.version != GameData.CurrentVersion)
            return new GameData { version = header.version }; // Let SaveStore protect the unknown schema.

        AvatarHeader avatars = JsonUtility.FromJson<AvatarHeader>(json);
        if (avatars == null || avatars.unlockedHeroes == null)
            throw new InvalidDataException("Missing profile avatar list.");
        return JsonUtility.FromJson<GameData>(json);
    }
}
