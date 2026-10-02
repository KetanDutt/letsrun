using System;
using System.IO;
using System.Text;

/// <summary>
/// Reads ONLY the original four-field GameData NRBF record, as data, without deserialization,
/// reflection, type activation, or BinaryFormatter. Unknown records are rejected.
/// The original file (including the historical missing-separator path) is never modified.
/// </summary>
public static class LegacySaveReader
{
    public static bool TryRead(string path, out GameData data, out string message)
    {
        data = null;
        message = null;
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists) return false;
            if (info.Length <= 0 || info.Length > SaveStore.MaxSaveBytes)
                throw new InvalidDataException("Invalid legacy save size.");
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
                data = ReadRecord(reader);
            data.Normalize();
            return true;
        }
        catch (Exception exception)
        {
            message = "Legacy profile was left untouched: " + exception.Message;
            return false;
        }
    }

    private static GameData ReadRecord(BinaryReader reader)
    {
        Require(reader.ReadByte() == 0, "Missing stream header.");
        int rootId = reader.ReadInt32();
        reader.ReadInt32(); // Header ID is not used by the original format.
        Require(rootId > 0 && reader.ReadInt32() == 1 && reader.ReadInt32() == 0, "Unknown stream version.");

        Require(reader.ReadByte() == 12, "Missing library record.");
        int libraryId = reader.ReadInt32();
        ReadString(reader); // Informational only: no library is ever loaded.
        Require(reader.ReadByte() == 5 && reader.ReadInt32() == rootId, "Unknown root record.");
        Require(ReadString(reader) == "GameData" && reader.ReadInt32() == 4, "Unknown legacy profile type.");

        var names = new string[4];
        for (int i = 0; i < names.Length; i++)
        {
            names[i] = ReadString(reader);
            Require(names[i] == "star_Score" || names[i] == "score_Count" || names[i] == "heroes" || names[i] == "selected_Index",
                "Unknown legacy field.");
            for (int j = 0; j < i; j++) Require(names[j] != names[i], "Duplicate field.");
        }
        var types = reader.ReadBytes(4);
        Require(types.Length == 4, "Incomplete field types.");
        for (int i = 0; i < names.Length; i++)
        {
            bool array = names[i] == "heroes";
            Require(types[i] == (array ? 7 : 0), "Unexpected field type.");
            Require(reader.ReadByte() == (array ? 1 : 8), "Unexpected primitive type.");
        }
        Require(reader.ReadInt32() == libraryId, "Unknown library reference.");

        var profile = new GameData();
        int arrayReference = 0;
        bool arrayRead = false;
        for (int i = 0; i < names.Length; i++)
        {
            switch (names[i])
            {
                case "star_Score": profile.stars = reader.ReadInt32(); break;
                case "score_Count": profile.bestScore = reader.ReadInt32(); break;
                case "selected_Index": profile.selectedHero = reader.ReadInt32(); break;
                case "heroes":
                    byte record = reader.ReadByte();
                    if (record == 9)
                    {
                        arrayReference = reader.ReadInt32();
                        Require(arrayReference > 0 && arrayReference != rootId, "Invalid array reference.");
                    }
                    else if (record == 15)
                    {
                        int id;
                        profile.unlockedHeroes = ReadBoolArray(reader, out id);
                        Require(id > 0 && id != rootId, "Invalid array ID.");
                        arrayRead = true;
                    }
                    else Require(record == 10, "Unknown array record."); // Null is repaired by Normalize.
                    break;
            }
        }

        byte next = reader.ReadByte();
        if (arrayReference != 0)
        {
            Require(next == 15 && !arrayRead, "Missing referenced array.");
            int id;
            profile.unlockedHeroes = ReadBoolArray(reader, out id);
            Require(id == arrayReference, "Mismatched array reference.");
            next = reader.ReadByte();
        }
        Require(next == 11 && reader.BaseStream.Position == reader.BaseStream.Length, "Unknown trailing record.");
        return profile;
    }

    private static bool[] ReadBoolArray(BinaryReader reader, out int id)
    {
        id = reader.ReadInt32();
        int count = reader.ReadInt32();
        Require(count >= 0 && count <= 64 && reader.ReadByte() == 1, "Invalid Boolean array.");
        var values = new bool[count];
        for (int i = 0; i < count; i++)
        {
            byte value = reader.ReadByte();
            Require(value == 0 || value == 1, "Invalid Boolean value.");
            values[i] = value == 1;
        }
        return values;
    }

    private static string ReadString(BinaryReader reader)
    {
        int length = 0;
        for (int shift = 0; shift < 35; shift += 7)
        {
            byte value = reader.ReadByte();
            Require(shift < 28 || (value & 0xf8) == 0, "Invalid string length.");
            length |= (value & 0x7f) << shift;
            if ((value & 0x80) != 0) continue;
            Require(length >= 0 && length <= 1024, "Legacy string is too large.");
            byte[] bytes = reader.ReadBytes(length);
            Require(bytes.Length == length, "Incomplete string.");
            return new UTF8Encoding(false, true).GetString(bytes);
        }
        throw new InvalidDataException("Invalid string prefix.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
