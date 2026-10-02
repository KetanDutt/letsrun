using System;
using System.IO;
using System.Text;

public enum SaveLoadResult
{
    NewProfile,
    Loaded,
    RecoveredBackup,
    Invalid,
    UnsupportedVersion
}

/// <summary>
/// Bounded JSON saves with replacement writes and a last-known-good backup.
/// Serialization is injected so persistence can be tested without the Unity engine.
/// </summary>
public sealed class SaveStore
{
    public const int MaxSaveBytes = 65536;
    public string Path { get; private set; }
    public string BackupPath { get { return Path + ".bak"; } }
    public bool ReadOnly { get; private set; }

    private readonly Func<GameData, string> serialize;
    private readonly Func<string, GameData> deserialize;

    public SaveStore(string path, Func<GameData, string> serializer, Func<string, GameData> deserializer)
    {
        if (string.IsNullOrEmpty(path)) throw new ArgumentException("A save path is required.", "path");
        Path = path;
        serialize = serializer ?? throw new ArgumentNullException("serializer");
        deserialize = deserializer ?? throw new ArgumentNullException("deserializer");
    }

    public SaveLoadResult Load(out GameData data, out string message)
    {
        data = null;
        message = null;
        ReadOnly = false;
        bool unsupported;
        if (TryRead(Path, out data, out message, out unsupported))
        {
            // Even a valid older primary must not rotate over a newer backup.
            GameData ignored; string backupError; bool futureBackup;
            TryRead(BackupPath, out ignored, out backupError, out futureBackup);
            if (futureBackup)
            {
                data = null; ReadOnly = true; message = backupError;
                return SaveLoadResult.UnsupportedVersion;
            }
            return SaveLoadResult.Loaded;
        }
        if (unsupported)
        {
            ReadOnly = true; // A downgraded app must not overwrite a newer profile.
            return SaveLoadResult.UnsupportedVersion;
        }

        string primaryError = message;
        if (TryRead(BackupPath, out data, out message, out unsupported))
        {
            message = "Recovered the last valid profile from backup.";
            return SaveLoadResult.RecoveredBackup;
        }
        if (unsupported)
        {
            ReadOnly = true;
            return SaveLoadResult.UnsupportedVersion;
        }

        message = primaryError ?? message;
        return File.Exists(Path) || File.Exists(BackupPath) ? SaveLoadResult.Invalid : SaveLoadResult.NewProfile;
    }

    public bool Save(GameData data, out string message)
    {
        message = null;
        if (ReadOnly)
        {
            message = "This profile was created by a newer version. Saving is disabled to protect it.";
            return false;
        }
        if (data == null || data.version != GameData.CurrentVersion)
        {
            message = "Refusing to write an unsupported profile.";
            return false;
        }

        string temporaryPath = Path + ".tmp";
        try
        {
            GameData ignoredBackup; string backupError; bool unsupportedBackup;
            TryRead(BackupPath, out ignoredBackup, out backupError, out unsupportedBackup);
            if (unsupportedBackup)
            {
                ReadOnly = true;
                throw new InvalidDataException("A newer backup profile is protected from being overwritten.");
            }
            data.Normalize();
            string json = serialize(data);
            if (string.IsNullOrEmpty(json) || Encoding.UTF8.GetByteCount(json) > MaxSaveBytes)
                throw new InvalidDataException("The profile is empty or exceeds the save size limit.");

            string directory = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));

            if (!File.Exists(Path))
            {
                File.Move(temporaryPath, Path);
                return true;
            }

            GameData previous;
            string ignored;
            bool unsupported;
            bool validPrimary = TryRead(Path, out previous, out ignored, out unsupported);
            if (unsupported)
            {
                ReadOnly = true;
                throw new InvalidDataException("A newer profile appeared on disk; it has not been overwritten.");
            }
            if (!validPrimary)
            {
                // Retain the damaged file for diagnosis. Never rotate it over a valid backup.
                string quarantine = Path + ".corrupt-" + DateTime.UtcNow.Ticks;
                File.Move(Path, quarantine);
                File.Move(temporaryPath, Path);
                return true;
            }

            try
            {
                File.Replace(temporaryPath, Path, BackupPath);
            }
            catch (PlatformNotSupportedException)
            {
                ReplaceWithBackup(temporaryPath);
            }
            catch (NotSupportedException)
            {
                ReplaceWithBackup(temporaryPath);
            }
            return true;
        }
        catch (Exception exception)
        {
            // The caller surfaces this to the player and retries; failures are never silent.
            message = "Could not save progress: " + exception.Message;
            return false;
        }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private void ReplaceWithBackup(string temporaryPath)
    {
        // Platforms without atomic replace still retain a complete recoverable old profile.
        File.Copy(Path, BackupPath, true);
        File.Copy(temporaryPath, Path, true);
        File.Delete(temporaryPath);
    }

    private bool TryRead(string path, out GameData data, out string message, out bool unsupported)
    {
        data = null;
        message = null;
        unsupported = false;
        if (!File.Exists(path)) return false;
        try
        {
            data = deserialize(ReadBoundedUtf8(path));
            if (data == null || data.version < 1 || data.unlockedHeroes == null)
                throw new InvalidDataException("The profile is missing required fields.");
            if (data.version != GameData.CurrentVersion)
            {
                unsupported = true;
                throw new InvalidDataException("Profile version " + data.version + " is not supported by this app.");
            }
            if (data.unlockedHeroes.Length > 64)
                throw new InvalidDataException("Invalid avatar list length.");
            data.Normalize();
            return true;
        }
        catch (Exception exception)
        {
            data = null;
            message = "Could not read profile: " + exception.Message;
            return false;
        }
    }

    private static string ReadBoundedUtf8(string path)
    {
        // Bound the actual read, not only a racy FileInfo check before ReadAllText.
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            long length = stream.Length;
            if (length <= 0 || length > MaxSaveBytes) throw new InvalidDataException("Invalid profile file size.");
            byte[] bytes = new byte[(int)length];
            int offset = 0;
            while (offset < bytes.Length)
            {
                int read = stream.Read(bytes, offset, bytes.Length - offset);
                if (read == 0) throw new InvalidDataException("Profile changed during reading.");
                offset += read;
            }
            if (stream.ReadByte() != -1) throw new InvalidDataException("Profile changed during reading.");
            string json = new UTF8Encoding(false, true).GetString(bytes);
            return json.Length > 0 && json[0] == '\ufeff' ? json.Substring(1) : json;
        }
    }
}
