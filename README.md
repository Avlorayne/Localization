# Localization for Unity

[![Version](https://img.shields.io/badge/version-1.0.0-blue)](CHANGELOG.md)
[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black?logo=unity&logoColor=white)](https://unity.com/releases/editor/archive)
[![Addressables](https://img.shields.io/badge/Addressables-1.22.3-orange)](https://docs.unity3d.com/Packages/com.unity.addressables@1.22/manual/index.html)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE.md)

**English** | [简体中文](README.zh-CN.md)

A lightweight localization package for Unity projects. It turns `.csv` / `.xlsx` source sheets into `LanguageDataSO` assets, validates keys, resolves nested text templates at runtime, and loads language data through Addressables.

## Features

| Area | What you get |
| --- | --- |
| Configuration | A single `LanguageConfigSO` holds the source folder, the generated asset folder, the default language, and the fallback language chain. |
| Source files | `.csv` and `.xlsx` are both supported; sheet headers are driven by the language configuration. |
| Editor workflows | `Tools/Localization` menu converts changed (or all) source files in batch. |
| Asset inspector | The `LanguageDataSO` inspector supports import, export, search, add, delete, plus duplicate-key and invalid-content validation. |
| Templates | `<Namespace\|KEY>` and `<Namespace\|KEY(arg0,arg1)>` syntax with nested resolution at runtime. |
| Runtime loading | Language data is loaded through Addressables and cached after first use. |

## Installation

Requires **Unity 2022.3** or newer. `com.unity.addressables` 1.22.3 is resolved automatically.

### Option 1 — Git URL (recommended)

In the Package Manager, click **Add package from git URL** (`+` button) and paste:

```
https://github.com/Avlorayne/Localization.git
```

Or add it to `Packages/manifest.json` directly:

```json
{
  "dependencies": {
    "com.dotline.localization": "https://github.com/Avlorayne/Localization.git"
  }
}
```

### Option 2 — Embedded package

Copy this folder into your project's `Packages/com.dotline.localization`; Unity picks it up without any manifest entry. To pin it explicitly:

```json
{
  "dependencies": {
    "com.dotline.localization": "file:com.dotline.localization"
  }
}
```

## Quick start

1. Open **Tools → Localization → Open Language Config**, then create or select `Assets/Settings/LanguageConfig.asset`.
2. Set `sourceFolderPath` (default: `Assets/Editor/Text Files/Localization`).
3. Set `soFolderPath` (default: `Assets/Resources/Localization`). Runtime loading accepts Resources-style addresses, but the generated assets must be marked Addressable to be resolved by the current loader.
4. Create a `.csv` or `.xlsx` file in the source folder. A good first header row: `Key,zh-Hans,zh-Hant,en,ja,ko,Comment`.
5. Run **Tools → Localization → Convert Changed Source Files**.
6. Add the generated `LanguageDataSO` assets to Addressables and make sure their address resolves by namespace or asset path.
7. Create a `LocalizationSystem` in code:

```csharp
using Localization;
using UnityEngine;

public sealed class LocalizedLabelExample : MonoBehaviour
{
    [SerializeField] private LanguageConfigSO languageConfig;

    private LocalizationSystem localization;

    private void Awake()
    {
        localization = new LocalizationSystem(languageConfig);
        localization.SetLanguage("en");
        Debug.Log(localization.GetLocalizedText("<UI|START_GAME>"));
    }
}
```

## Template syntax

| Syntax | Meaning |
| --- | --- |
| `<UI\|START_GAME>` | Basic reference. |
| `<UI\|ITEM_COUNT(3)>` | Reference with arguments. |
| `<UI\|WELCOME(<UI\|PLAYER_NAME>)>` | Nested templates as arguments. |
| `<UI\|WELCOME(<UI\|PLAYER_NAME>, "Captain")>` | Text arguments must be wrapped in double quotes; the resolved value keeps the quotes stripped. |

- `{0}`, `{1}`, … inside the referenced entry are replaced by the arguments, e.g. `Welcome, {0} {1}!` → `Welcome, Captain Captain!`.
- Injecting a `string` variable also works, but still needs double quotes:

  ```csharp
  string name = "William";
  localization.GetLocalizedText("<UI|WELCOME(<UI|PLAYER_NAME>, \"{William}\")>");
  ```

- Ordinary TextMesh Pro rich text tags such as `<color=red>` are never treated as localization keys.

## Documentation

| Document | Contents |
| --- | --- |
| [Configuration.md](Documentation~/Configuration.md) | Configuration fields, source file format, path rules. |
| [Architecture.md](Documentation~/Architecture.md) | Runtime/editor architecture and data flow. |
| [Testing.md](Documentation~/Testing.md) | Automated tests and manual acceptance cases. |
| [FAQ.md](Documentation~/FAQ.md) | Frequently asked questions. |

## Sample

Import **Basic Localization Example** from the Samples section of the package in the Package Manager. You get a minimal CSV source file and the `BasicLocalizationExample` scripts, demonstrating configuration, conversion, and runtime lookup.

## Known limitations

- WebGL players cannot block on Addressables loads; use the asynchronous loading path.
- `LocalizationSystem.GetLocalizedText` relies on synchronous lookups and is not suitable for first-time loads of uncached assets on WebGL.
- When a source file is deleted, the converter only cleans up its hash record and keeps the corresponding `LanguageDataSO`, protecting manually maintained data.
- The package embeds third-party binaries; keep `Third Party Notices.md` together with the package when redistributing.

## License

MIT — see [LICENSE.md](LICENSE.md). Third-party libraries are covered by [Third Party Notices.md](Third%20Party%20Notices.md).
