#!/usr/bin/env python3
"""Engine-free Unity asset/scene/package checks. Standard library only; does not modify files."""
from __future__ import annotations

import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets"
errors: list[str] = []
warnings: list[str] = []


def require(condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def field(text: str, key: str) -> str | None:
    match = re.search(r"^  " + re.escape(key) + r": (.*)$", text, re.MULTILINE)
    return match.group(1) if match else None


def main() -> int:
    guids: dict[str, Path] = {}
    for meta in ASSETS.rglob("*.meta"):
        asset = Path(str(meta)[:-5])
        require(asset.exists(), f"Orphan metadata: {meta.relative_to(ROOT)}")
        match = re.search(r"^guid: ([0-9a-f]{32})$", read(meta), re.MULTILINE)
        require(match is not None, f"Invalid GUID: {meta.relative_to(ROOT)}")
        if match:
            guid = match.group(1)
            require(guid not in guids, f"Duplicate GUID: {guid} ({asset.relative_to(ROOT)})")
            guids[guid] = asset

    for path in ASSETS.rglob("*"):
        if path.suffix == ".meta":
            continue
        if path.name.startswith("."):
            errors.append(f"OS/hidden artifact in Assets: {path.relative_to(ROOT)}")
            continue
        require(Path(str(path) + ".meta").exists(), f"Missing metadata: {path.relative_to(ROOT)}")

    external = json.loads(read(ROOT / "Tools/unity_package_guids.json"))
    references = 0
    serialized = list(ASSETS.rglob("*.unity")) + list(ASSETS.rglob("*.prefab")) + list(ASSETS.rglob("*.anim")) + list(ASSETS.rglob("*.controller"))
    for path in serialized:
        text = read(path)
        ids = re.findall(r"^--- !u!\d+ &(-?\d+)", text, re.MULTILINE)
        require(len(ids) == len(set(ids)), f"Duplicate local object ID: {path.relative_to(ROOT)}")
        for guid in re.findall(r"guid: ([0-9a-f]{32})", text):
            references += 1
            require(guid in guids or guid in external or guid in {
                "0000000000000000e000000000000000", "0000000000000000f000000000000000"
            },
                    f"Unresolved GUID {guid} in {path.relative_to(ROOT)}")
        for target in re.findall(r"m_MethodName: (\w+)", text):
            require(any(re.search(r"\b" + re.escape(target) + r"\s*\(", read(source)) for source in (ASSETS / "Scripts").rglob("*.cs")),
                    f"Missing button callback {target} in {path.relative_to(ROOT)}")

    scripts = list((ASSETS / "Scripts").rglob("*.cs"))
    for path in scripts:
        text = read(path)
        require(not re.search(r"using\s+System\.Runtime\.Serialization\.Formatters\.Binary|new\s+BinaryFormatter\s*\(", text),
                f"Unsafe binary deserialization in {path.relative_to(ROOT)}")
        require("Input.touches" not in re.sub(r"//[^\n]*", "", text), f"Allocating touch array polling in {path.relative_to(ROOT)}")

    manifest = json.loads(read(ROOT / "Packages/manifest.json"))["dependencies"]
    lock = json.loads(read(ROOT / "Packages/packages-lock.json"))["dependencies"]
    require(manifest.get("com.unity.ugui") == "1.0.0", "uGUI GUID contract needs com.unity.ugui 1.0.0")
    require("com.unity.test-framework" in manifest, "Unity Test Framework is required")
    for package, version in manifest.items():
        require(package in lock and lock[package]["version"] == version and lock[package]["depth"] == 0,
                f"Manifest/lock mismatch: {package}")
    needed = set(manifest)
    pending = list(needed)
    while pending:
        package = pending.pop()
        for dependency in lock.get(package, {}).get("dependencies", {}):
            require(dependency in lock, f"Missing transitive package: {dependency}")
            if dependency not in needed:
                needed.add(dependency)
                pending.append(dependency)
    require(set(lock) == needed, "Package lock contains unused entries")
    for unused in ("com.unity.ads", "com.unity.analytics", "com.unity.purchasing", "com.unity.collab-proxy"):
        require(unused not in manifest, f"Unused online service package remains: {unused}")

    build = read(ROOT / "ProjectSettings/EditorBuildSettings.asset")
    enabled = re.findall(r"- enabled: 1\s+path: (.+)", build)
    require(enabled == ["Assets/Scenes/MainMenu.unity", "Assets/Scenes/Gameplay.unity"], "Build scene order must be MainMenu then Gameplay")
    for scene in enabled:
        require((ROOT / scene).exists(), f"Missing build scene: {scene}")
    for index in range(9):
        require((ASSETS / f"Resources/Sprites/Player/hero{index}_big.png").exists(), f"Missing runner sprite {index}")
    for resource in ("Fonts/LuckiestGuy.ttf", "Sprites/UI/BG/Background.png", "Sprites/UI/BG/Menu Scene 1.png", "Sprites/Player/trex.png"):
        require((ASSETS / "Resources" / resource).exists(), f"Missing UI/game resource: {resource}")

    for name, fields in {
        "MainMenu": ("menuMusic", "uiClick", "purchaseSound", "hero_Menu"),
        "Gameplay": ("score_Text", "star_Score_Text", "ui_Click", "crash_Clip", "obstacles_Obj")
    }.items():
        text = read(ASSETS / f"Scenes/{name}.unity")
        for name_field in fields:
            require(re.search(r"^  " + name_field + r": \{fileID: (?!0\b)\d+", text, re.MULTILINE) is not None,
                    f"Missing scene reference {name}.{name_field}")

    settings = read(ROOT / "ProjectSettings/ProjectSettings.asset")
    require(field(settings, "defaultScreenOrientation") == "1", "Portrait presentation requires Portrait orientation")
    require(field(settings, "AndroidTargetArchitectures") == "3", "Android ARMv7 + ARM64 must be enabled")
    require(re.search(r"scriptingBackend:\s+Android: 1", settings) is not None, "Android must use IL2CPP")
    require(int(field(settings, "AndroidMinSdkVersion") or 0) >= 23, "Android minimum SDK must be at least 23")

    for path in ASSETS.rglob("*.asmdef"):
        try:
            json.loads(read(path))
        except json.JSONDecodeError as error:
            errors.append(f"Invalid assembly definition {path.relative_to(ROOT)}: {error}")

    readme = read(ROOT / "README.md")
    require("MIT License" not in readme, "README license must agree with the actual all-rights-reserved LICENSE")
    for document in [ROOT / "README.md", ROOT / "CHANGELOG.md", *(ROOT / "docs").glob("*.md")]:
        for target in re.findall(r"\]\(([^)]+)\)", read(document)):
            if "://" not in target and not target.startswith("#"):
                require((document.parent / target.split("#")[0]).exists(),
                        f"Broken documentation link in {document.relative_to(ROOT)}: {target}")

    try:
        tracked = subprocess.check_output(["git", "ls-files", "Library", "Logs", "UserSettings", ".vs", "obj"], cwd=ROOT, text=True).splitlines()
        if tracked:
            warnings.append(f"{len(tracked)} historical generated files still tracked; see docs/MAINTENANCE.md for the remaining cleanup.")
    except (OSError, subprocess.CalledProcessError):
        warnings.append("Git index unavailable; skipped generated-file inventory.")

    for warning in warnings:
        print("WARN", warning)
    for error in errors:
        print("ERROR", error)
    print(f"Checked {len(guids)} GUIDs, {references} serialized references, {len(scripts)} runtime scripts, {len(manifest)} packages.")
    print(f"{len(errors)} errors; {len(warnings)} warnings.")
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
