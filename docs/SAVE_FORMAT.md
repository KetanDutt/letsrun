# Profile format, migration, and recovery

## Files

The profile lives under `Application.persistentDataPath`, keeping the original company/product identifiers so an existing installation's data location is not intentionally changed.

| File | Purpose |
| --- | --- |
| `profile.json` | Current versioned profile |
| `profile.json.bak` | Previous known-valid profile |
| `profile.json.tmp` | Temporary write; never loaded as a completed profile |
| `profile.json.corrupt-<UTC ticks>` | Damaged primary retained when repairing/replacing it |
| `GameData.dat` | Original legacy file; never modified by migration |

The original code also wrote to `persistentDataPath + "GameData.dat"` **without a separator**. The importer checks that historical sibling location as well as `Path.Combine(persistentDataPath, "GameData.dat")` when no JSON profile/backup exists.

## Version 1 schema

```json
{
  "version": 1,
  "stars": 20,
  "bestScore": 12,
  "selectedHero": 0,
  "unlockedHeroes": [true, false, false, false, false, false, false, false, false],
  "musicEnabled": true,
  "sfxEnabled": true,
  "reducedMotion": false,
  "tutorialSeen": true,
  "totalRuns": 3,
  "lifetimeStars": 20
}
```

`GameData` uses public fields because Unity JsonUtility does not serialize ordinary properties. `UnityProfileCodec` first checks a default-free header so `{}`, a missing version, or a missing/null avatar list cannot become a valid default profile.

Normalization clamps negative counters, resizes ownership to nine while preserving known entries, unlocks only the starter if repair is needed, and falls back to the starter when a selected index is invalid/locked. It never grants new currency.

Files must be 1–65,536 valid UTF-8 bytes. The actual read is bounded, not just a prior file-size observation. Invalid UTF-8, oversized files, and unreasonable avatar arrays are rejected. A UTF-8 BOM is accepted.

The stable `version` header is examined **before** interpreting version-specific avatar fields. Unknown/newer versions become **read-only** rather than being mistaken for corruption when their schema changes. A newer backup is protected even when the primary is a valid older profile. Writes also recheck the on-disk primary and backup.

## Write and recovery behavior

1. Normalize and serialize the current supported profile.
2. Write a complete temporary file in the same directory.
3. For an existing valid primary, use `File.Replace` to retain the previous primary as backup.
4. Where atomic replacement is unsupported, copy the valid primary to backup before copying in the new complete file. This fallback is recoverable but not fully atomic.
5. If the existing primary is invalid, quarantine it instead of rotating it over a good backup.
6. Remove temporary files after success/failure when possible.

Loading tries the primary, then a valid backup. A valid backup recovery is surfaced to the player and restores the primary on the next save. Unsupported versions are protected before fallback to an older profile. Damaged files remain available for diagnosis.

IO/parse errors return actionable messages to GameManager, which logs a warning and shows a toast/banner. Failed writes keep progress dirty and retry after ten seconds; a successful write clears the warning.

### Durability limits

Stars are banked in memory on pickup; ordinary disk saves are throttled to at most once per two seconds. Purchases, settings, pause/backgrounding, results, scene exits, and normal quit flush immediately. Abrupt process termination/power loss can still lose the most recent unflushed progress. A backup protects against a torn/corrupt primary, not against every filesystem or device failure. There is no cross-device/cloud sync or simultaneous multi-process writer guarantee.

## Safe legacy import

`LegacySaveReader` parses **only** the original NRBF representation:

- Stream header and one informational library record.
- A root class named `GameData` with the four known fields: `star_Score`, `score_Count`, `heroes`, `selected_Index`.
- Int32 scalar fields and one bounded Boolean array, inline or referenced.
- A final message-end record with no trailing payload.

It does not use `BinaryFormatter`, reflection, assembly loading, or object activation. Unknown types, field kinds, records, oversized strings/arrays, truncation, and trailing data are rejected. This is deliberately **not** a general-purpose binary deserializer.

A supported old file preserves balance, best score, ownership, and selection and is then saved as JSON. Settings/statistics not present in the old format take new-profile defaults. The source binary stays untouched. Unsupported/corrupt legacy files produce a notice and a clean JSON profile rather than executing unknown data.

The importer was tested against bounded synthetic fixtures, truncated/random inputs, and an actual locally generated original-format record. Actual Unity profile IO/JsonUtility behavior still needs the EditMode suite and device lifecycle tests.

## Manual recovery

1. Stop the game before editing or moving saves.
2. Copy all profile, backup, quarantine, and legacy files somewhere safe first.
3. Inspect primary and backup version/JSON validity. Do not overwrite a newer-version profile with an older one.
4. If the primary is corrupt and the backup is valid, starting the app should recover it automatically.
5. To test a genuinely new profile, temporarily move **both** JSON files and both possible legacy locations aside; otherwise the importer may restore old progress.
6. Verify the wallet, owned/selected runner, best score, and settings before deleting any archived copy.

There is no in-game destructive reset button or save export/import UI in this revision. Those are sensible future support features, with explicit confirmation and validation.
