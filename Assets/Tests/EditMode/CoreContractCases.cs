using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>Shared contracts: run in Unity/NUnit with JsonUtility, or in the standalone CI harness.</summary>
public sealed class CoreContractCase
{
    public string Name { get; private set; }
    public Action Test { get; private set; }
    public CoreContractCase(string name, Action test) { Name = name; Test = test; }
    public override string ToString() { return Name; }
}

public static class CoreContractCases
{
    public static IEnumerable<CoreContractCase> Create(Func<GameData, string> encode, Func<string, GameData> decode)
    {
        yield return Case("New profiles grant no test currency and unlock only the starter", () =>
        {
            var data = new GameData();
            Check(data.stars == 0 && data.bestScore == 0 && data.selectedHero == 0, "Unsafe defaults");
            Check(data.musicEnabled && data.sfxEnabled && !data.reducedMotion, "Invalid settings defaults");
            Check(data.unlockedHeroes.Length == 9 && data.unlockedHeroes[0], "Missing starter");
            for (int i = 1; i < 9; i++) Check(!data.unlockedHeroes[i], "Free paid hero");
        });
        yield return Case("Profile normalization repairs corrupt fields without granting stars", () =>
        {
            var data = new GameData { stars = -4, bestScore = -2, totalRuns = -1, lifetimeStars = -3, selectedHero = 8, unlockedHeroes = new[] { false, true } };
            data.Normalize();
            Check(data.stars == 0 && data.bestScore == 0 && data.totalRuns == 0 && data.lifetimeStars == 0, "Negative data");
            Check(data.unlockedHeroes.Length == 9 && data.unlockedHeroes[0] && data.unlockedHeroes[1] && data.selectedHero == 0, "Repair lost ownership");
            data.unlockedHeroes = null; data.selectedHero = -1; data.Normalize();
            Check(data.unlockedHeroes[0] && data.selectedHero == 0, "Null ownership repair");
        });
        yield return Case("Currency addition saturates rather than overflowing", () =>
        {
            Check(RunnerRules.SaturatingAdd(int.MaxValue - 1, 5) == int.MaxValue, "Overflow");
            Check(RunnerRules.SaturatingAdd(-5, -3) == 0, "Negative addition");
        });
        yield return Case("An exact-price purchase selects a hero and charges only once", () =>
        {
            var data = new GameData { stars = RunnerRules.HeroPrice };
            Check(RunnerRules.SelectHero(data, 2) == HeroSelectionResult.Purchased, "Purchase failed");
            Check(data.stars == 0 && data.selectedHero == 2 && data.unlockedHeroes[2], "Invalid transaction");
            Check(RunnerRules.SelectHero(data, 2) == HeroSelectionResult.AlreadySelected && data.stars == 0, "Double charge");
            Check(RunnerRules.SelectHero(data, 0) == HeroSelectionResult.Selected, "Starter unavailable");
            Check(RunnerRules.SelectHero(data, 2) == HeroSelectionResult.Selected && data.stars == 0, "Owned hero charged");
        });
        yield return Case("Insufficient balance leaves the wallet and selection unchanged", () =>
        {
            var data = new GameData { stars = RunnerRules.HeroPrice - 1 };
            Check(RunnerRules.SelectHero(data, 3) == HeroSelectionResult.NotEnoughStars, "Insufficient balance accepted");
            Check(data.stars == RunnerRules.HeroPrice - 1 && data.selectedHero == 0 && !data.unlockedHeroes[3], "Partial purchase");
        });
        yield return Case("Invalid hero indices and null profiles are rejected", () =>
        {
            var data = new GameData { stars = 2000 };
            Check(RunnerRules.SelectHero(data, -1) == HeroSelectionResult.InvalidHero, "Negative index");
            Check(RunnerRules.SelectHero(data, 9) == HeroSelectionResult.InvalidHero, "Out-of-range index");
            Check(RunnerRules.SelectHero(null, 0) == HeroSelectionResult.InvalidHero && data.stars == 2000, "Null profile");
        });
        yield return Case("Difficulty boundaries retain the authored 12-14-16 pace", () =>
        {
            Check(RunnerRules.TargetSpeed(29.99f) == 12f && RunnerRules.TargetSpeed(30f) == 14f, "First threshold");
            Check(RunnerRules.TargetSpeed(59.99f) == 14f && RunnerRules.TargetSpeed(60f) == 16f, "Second threshold");
        });
        yield return Case("Tween easing is clamped monotonic and has exact endpoints", () =>
        {
            Check(RunnerRules.EaseOutCubic(-1f) == 0f && RunnerRules.EaseOutCubic(2f) == 1f, "Unclamped ease");
            float previous = 0f;
            for (int i = 0; i <= 100; i++) { float value = RunnerRules.EaseOutCubic(i / 100f); Check(value >= previous && value <= 1f, "Non-monotonic ease"); previous = value; }
        });
        yield return Case("Empty and exhausted obstacle pools never spin", () =>
        {
            Check(RunnerRules.FindAvailableIndex(0, 0, i => true) == -1, "Empty pool");
            int calls = 0;
            Check(RunnerRules.FindAvailableIndex(7, 3, i => { calls++; return false; }) == -1 && calls == 7, "Unbounded search");
        });
        yield return Case("Obstacle pool searches wrap safely from any starting index", () =>
        {
            Check(RunnerRules.FindAvailableIndex(4, -1, i => i == 1) == 1, "Negative start");
            Check(RunnerRules.FindAvailableIndex(4, 10, i => i == 3) == 3, "Large start");
            Check(RunnerRules.FindAvailableIndex(3, 0, null) == -1, "Null predicate");
        });
        yield return Case("Run state gates ticking pickups and repeated game-over calls", () =>
        {
            var run = new RunSession();
            run.Advance(100f);
            Check(!run.Begin() && !run.CollectStar() && run.Score == 0, "Ready accepted gameplay");
            Check(run.StartCountdown() && !run.StartCountdown() && run.Begin(), "Invalid countdown transition");
            run.Advance(1.2f); Check(run.Score == 1 && run.CollectStar() && run.Stars == 1, "Running did not advance");
            Check(run.Pause() && !run.Pause(), "Double pause");
            run.Advance(100f); Check(run.Score == 1 && !run.CollectStar(), "Pause accepted gameplay");
            Check(run.StartCountdown() && run.Begin(), "Resume failed");
            Check(run.Finish() && !run.Finish(), "Repeated game over");
            run.Advance(100f); Check(run.Score == 1 && !run.CollectStar() && !run.StartCountdown(), "Game-over accepted gameplay");
        });
        yield return Case("Countdown can be paused and safely restarted", () =>
        {
            var run = new RunSession();
            Check(run.StartCountdown() && run.Pause() && !run.Begin(), "Paused countdown began");
            Check(run.StartCountdown() && run.Begin(), "Countdown resume failed");
        });
        yield return Case("Scores floor elapsed time and ignore invalid deltas", () =>
        {
            var run = new RunSession(); run.StartCountdown(); run.Begin();
            run.Advance(0.5f); Check(run.Score == 0, "Score rounded too early");
            run.Advance(0.5f); Check(run.Score == 1, "Score did not tick");
            run.Advance(-1f); run.Advance(float.NaN); run.Advance(float.PositiveInfinity);
            Check(run.Score == 1, "Invalid delta changed score");
        });
        yield return Case("Long runs retain sub-second score precision and saturate safely", () =>
        {
            var run = new RunSession(); run.StartCountdown(); run.Begin();
            run.Advance(16777216f);
            for (int i = 0; i < 4; i++) run.Advance(0.25f);
            Check(run.Score == 16777217, "Long-run clock lost small deltas");
            run.Advance(float.MaxValue); run.Advance(1f);
            Check(run.Score == int.MaxValue, "Maximum score overflowed");
        });
        yield return Case("A changed future schema is protected before interpreting old fields", () => WithDirectory(folder =>
        {
            var store = Store(folder, encode, decode);
            string future = "{\"version\":2,\"unlockedHeroes\":{\"runnerA\":true}}";
            File.WriteAllText(store.Path, future); GameData loaded; string message;
            Check(store.Load(out loaded, out message) == SaveLoadResult.UnsupportedVersion && store.ReadOnly, "Future schema treated as corruption");
            Check(!store.Save(new GameData(), out message) && File.ReadAllText(store.Path) == future, "Future schema overwritten");
        }));
        yield return Case("A valid old primary cannot overwrite a changed future backup", () => WithDirectory(folder =>
        {
            var store = Store(folder, encode, decode);
            string primary = encode(new GameData { stars = 5 });
            string future = "{\"version\":2,\"newCatalog\":[\"runnerA\"]}";
            File.WriteAllText(store.Path, primary); File.WriteAllText(store.BackupPath, future);
            GameData loaded; string message;
            Check(store.Load(out loaded, out message) == SaveLoadResult.UnsupportedVersion && store.ReadOnly, "Newer backup lost to a valid older primary");
            var writer = Store(folder, encode, decode);
            Check(!writer.Save(new GameData(), out message) && writer.ReadOnly, "Unloaded writer replaced future backup");
            Check(File.ReadAllText(store.Path) == primary && File.ReadAllText(store.BackupPath) == future, "Protected files changed");
        }));
        yield return Case("Invalid UTF-8 is rejected rather than silently replaced", () => WithDirectory(folder =>
        {
            var store = Store(folder, encode, decode);
            string json = encode(new GameData());
            byte[] prefix = Encoding.UTF8.GetBytes(json.Substring(0, json.Length - 1) + ",\"note\":\"");
            byte[] bytes = new byte[prefix.Length + 3]; Array.Copy(prefix, bytes, prefix.Length);
            bytes[prefix.Length] = 0xff; bytes[prefix.Length + 1] = (byte)'"'; bytes[prefix.Length + 2] = (byte)'}';
            File.WriteAllBytes(store.Path, bytes); GameData loaded; string message;
            Check(store.Load(out loaded, out message) == SaveLoadResult.Invalid && loaded == null, "Invalid UTF-8 accepted");
        }));
        yield return Case("A missing save starts a clean profile", () => WithDirectory(folder =>
        {
            var store = Store(folder, encode, decode); GameData loaded; string message;
            Check(store.Load(out loaded, out message) == SaveLoadResult.NewProfile && loaded == null && !store.ReadOnly, "Missing save treated as corrupt");
        }));
        yield return Case("Profiles and all settings survive a save/load round trip", () => WithDirectory(folder =>
        {
            var store = Store(folder, encode, decode);
            var data = new GameData { stars = 2345, bestScore = 70, selectedHero = 2, musicEnabled = false, sfxEnabled = false, reducedMotion = true, tutorialSeen = true, totalRuns = 4, lifetimeStars = 3456 };
            data.unlockedHeroes[2] = true;
            string message; Check(store.Save(data, out message), message);
            GameData loaded; Check(store.Load(out loaded, out message) == SaveLoadResult.Loaded, message);
            Check(loaded.stars == 2345 && loaded.bestScore == 70 && loaded.selectedHero == 2 && loaded.unlockedHeroes[2], "Lost progress");
            Check(!loaded.musicEnabled && !loaded.sfxEnabled && loaded.reducedMotion && loaded.tutorialSeen && loaded.totalRuns == 4 && loaded.lifetimeStars == 3456, "Lost settings/statistics");
            Check(!File.Exists(store.Path + ".tmp"), "Temporary file retained");
        }));
        yield return Case("A corrupt primary restores the last-known-good backup", () => WithDirectory(folder =>
        {
            var store = Store(folder, encode, decode); string message;
            Check(store.Save(new GameData { stars = 3 }, out message), message);
            Check(store.Save(new GameData { stars = 7 }, out message), message);
            File.WriteAllText(store.Path, "{broken");
            GameData loaded; Check(store.Load(out loaded, out message) == SaveLoadResult.RecoveredBackup && loaded.stars == 3, "Backup not recovered");
            Check(store.Save(loaded, out message), message);
            Check(Directory.GetFiles(folder, "*.corrupt-*").Length == 1, "Damaged primary was not retained");
            Check(decode(File.ReadAllText(store.BackupPath)).stars == 3, "Corruption rotated over backup");
        }));
        yield return Case("Future-version saves are read-only and never overwritten", () => WithDirectory(folder =>
        {
            var store = Store(folder, encode, decode); string future = encode(new GameData { version = GameData.CurrentVersion + 1, stars = 999 });
            File.WriteAllText(store.Path, future);
            GameData loaded; string message;
            Check(store.Load(out loaded, out message) == SaveLoadResult.UnsupportedVersion && store.ReadOnly, "Downgrade not protected");
            Check(!store.Save(new GameData(), out message) && File.ReadAllText(store.Path) == future, "Future profile overwritten");
        }));
        yield return Case("A newer backup is also protected from a downgraded app", () => WithDirectory(folder =>
        {
            var store = Store(folder, encode, decode);
            File.WriteAllText(store.BackupPath, encode(new GameData { version = 2 }));
            GameData loaded; string message;
            Check(store.Load(out loaded, out message) == SaveLoadResult.UnsupportedVersion && store.ReadOnly, "Newer backup ignored");
        }));
        yield return Case("A future profile appearing during a write is not replaced", () => WithDirectory(folder =>
        {
            var store = Store(folder, encode, decode); string message;
            Check(store.Save(new GameData(), out message), message);
            string future = encode(new GameData { version = 2, stars = 88 }); File.WriteAllText(store.Path, future);
            Check(!store.Save(new GameData { stars = 1 }, out message) && store.ReadOnly && File.ReadAllText(store.Path) == future, "Concurrent newer profile overwritten");
            Check(!File.Exists(store.Path + ".tmp"), "Failed write left temporary file");
        }));
        yield return Case("Malformed JSON or missing required fields is rejected", () => WithDirectory(folder =>
        {
            var store = Store(folder, encode, decode); GameData loaded; string message;
            foreach (string json in new[] { "{}", "{", "{\"version\":1}", "{\"version\":1,\"unlockedHeroes\":null}" })
            {
                File.WriteAllText(store.Path, json);
                Check(store.Load(out loaded, out message) == SaveLoadResult.Invalid && loaded == null && !string.IsNullOrEmpty(message), "Invalid profile accepted: " + json);
            }
        }));
        yield return Case("Oversized files and oversized output are rejected", () => WithDirectory(folder =>
        {
            var store = Store(folder, encode, decode); File.WriteAllText(store.Path, new string('x', SaveStore.MaxSaveBytes + 1));
            GameData loaded; string message;
            Check(store.Load(out loaded, out message) == SaveLoadResult.Invalid, "Oversized file accepted");
            var writer = Store(folder, data => new string('x', SaveStore.MaxSaveBytes + 1), decode);
            Check(!writer.Save(new GameData(), out message) && !File.Exists(writer.Path + ".tmp"), "Oversized write accepted");
        }));
        yield return Case("A failed write reports an error and preserves existing directories", () => WithDirectory(folder =>
        {
            string blocked = Path.Combine(folder, "blocked"); Directory.CreateDirectory(blocked);
            var store = new SaveStore(blocked, encode, decode); string message;
            Check(!store.Save(new GameData(), out message) && !string.IsNullOrEmpty(message), "Write error hidden");
            Check(Directory.Exists(blocked) && !File.Exists(blocked + ".tmp"), "Failed write damaged target");
        }));
        yield return Case("A damaged primary and backup can be replaced without losing diagnostic files", () => WithDirectory(folder =>
        {
            var store = Store(folder, encode, decode); File.WriteAllText(store.Path, "bad"); File.WriteAllText(store.BackupPath, "old bad");
            GameData loaded; string message;
            Check(store.Load(out loaded, out message) == SaveLoadResult.Invalid, "Invalid pair accepted");
            Check(store.Save(new GameData(), out message), message);
            Check(File.ReadAllText(store.BackupPath) == "old bad" && Directory.GetFiles(folder, "*.corrupt-*").Length == 1, "Recovery discarded diagnostics");
        }));
        yield return Case("A valid profile's negative fields and short avatar list are repaired", () => WithDirectory(folder =>
        {
            var store = Store(folder, encode, decode);
            File.WriteAllText(store.Path, encode(new GameData { stars = -1, bestScore = -2, selectedHero = 99, unlockedHeroes = new[] { false } }));
            GameData loaded; string message;
            Check(store.Load(out loaded, out message) == SaveLoadResult.Loaded && loaded.stars == 0 && loaded.bestScore == 0 && loaded.selectedHero == 0 && loaded.unlockedHeroes.Length == 9 && loaded.unlockedHeroes[0], "Profile was not repaired");
        }));
        yield return Case("Stale interrupted-write temporary files are ignored and replaced", () => WithDirectory(folder =>
        {
            var store = Store(folder, encode, decode); File.WriteAllText(store.Path + ".tmp", "partial");
            GameData loaded; string message;
            Check(store.Load(out loaded, out message) == SaveLoadResult.NewProfile, "Partial temporary file loaded");
            Check(store.Save(new GameData { stars = 5 }, out message) && !File.Exists(store.Path + ".tmp"), message);
        }));
        foreach (bool inline in new[] { false, true })
        {
            bool inlineValue = inline;
            yield return Case("Legacy NRBF " + (inline ? "inline" : "referenced") + " Boolean arrays migrate safely", () => WithDirectory(folder =>
            {
                string path = Path.Combine(folder, "legacy.dat"); byte[] original = LegacyFixture(inlineValue);
                File.WriteAllBytes(path, original); GameData data; string message;
                Check(LegacySaveReader.TryRead(path, out data, out message), message);
                Check(data.stars == 1750 && data.bestScore == 42 && data.selectedHero == 2 && data.unlockedHeroes[0] && data.unlockedHeroes[2], "Legacy data lost");
                Check(File.ReadAllBytes(path).Length == original.Length, "Original legacy file changed");
            }));
        }
        yield return Case("Legacy unexpected types records and trailing payloads are rejected", () => WithDirectory(folder =>
        {
            string path = Path.Combine(folder, "legacy.dat"); GameData data; string message;
            File.WriteAllBytes(path, LegacyFixture(false, "EvilType"));
            Check(!LegacySaveReader.TryRead(path, out data, out message), "Unknown type activated");
            byte[] valid = LegacyFixture(false); Array.Resize(ref valid, valid.Length + 1); File.WriteAllBytes(path, valid);
            Check(!LegacySaveReader.TryRead(path, out data, out message), "Trailing record accepted");
        }));
        yield return Case("Truncated and random legacy streams fail safely", () => WithDirectory(folder =>
        {
            var random = new Random(42); string path = Path.Combine(folder, "legacy.dat"); GameData data; string message;
            byte[] valid = LegacyFixture(false);
            for (int size = 0; size < valid.Length; size += 7)
            {
                var bytes = new byte[size]; Array.Copy(valid, bytes, size); File.WriteAllBytes(path, bytes);
                Check(!LegacySaveReader.TryRead(path, out data, out message), "Truncated legacy stream accepted");
            }
            for (int i = 0; i < 100; i++)
            {
                var bytes = new byte[random.Next(1, 256)]; random.NextBytes(bytes); File.WriteAllBytes(path, bytes);
                Check(!LegacySaveReader.TryRead(path, out data, out message), "Random legacy stream accepted");
            }
        }));
    }

    public static byte[] LegacyFixture(bool inline, string className = "GameData")
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, Encoding.UTF8))
        {
            writer.Write((byte)0); writer.Write(1); writer.Write(-1); writer.Write(1); writer.Write(0);
            writer.Write((byte)12); writer.Write(2); writer.Write("Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            writer.Write((byte)5); writer.Write(1); writer.Write(className); writer.Write(4);
            writer.Write("star_Score"); writer.Write("score_Count"); writer.Write("heroes"); writer.Write("selected_Index");
            writer.Write(new byte[] { 0, 0, 7, 0 }); writer.Write(new byte[] { 8, 8, 1, 8 }); writer.Write(2);
            writer.Write(1750); writer.Write(42);
            if (inline) WriteArray(writer);
            else { writer.Write((byte)9); writer.Write(3); }
            writer.Write(2);
            if (!inline) WriteArray(writer);
            writer.Write((byte)11); writer.Flush(); return stream.ToArray();
        }
    }

    private static void WriteArray(BinaryWriter writer)
    {
        writer.Write((byte)15); writer.Write(3); writer.Write(9); writer.Write((byte)1);
        for (int i = 0; i < 9; i++) writer.Write(i == 0 || i == 2);
    }

    private static SaveStore Store(string folder, Func<GameData, string> encode, Func<string, GameData> decode)
    { return new SaveStore(Path.Combine(folder, "profile.json"), encode, decode); }
    private static CoreContractCase Case(string name, Action action) { return new CoreContractCase(name, action); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message ?? "Contract failed"); }
    private static void WithDirectory(Action<string> test)
    {
        string folder = Path.Combine(Path.GetTempPath(), "LetsRunTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try { test(folder); }
        finally { Directory.Delete(folder, true); }
    }
}
