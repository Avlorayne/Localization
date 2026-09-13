# Architecture

**English** | [简体中文](Architecture.zh-CN.md)

## Overview

The package is split into a Runtime layer and an Editor layer:

- Runtime provides data structures, template parsing, language switching and Addressables loading.
- Editor provides source import/export, the Inspector editing experience, batch conversion and data validation.

Core data flow:

```text
CSV/XLSX source
  -> LocalizationSourceParser
  -> LocalizationData[]
  -> LanguageDataSO
  -> Addressables
  -> LocalizationSystem.GetLocalizedText(template)
```

## Assembly boundaries

| Assembly | Path | Responsibility | Dependencies |
| --- | --- | --- | --- |
| `Dotline.Localization` | `Runtime/Dotline.Localization.asmdef` | Runtime API, template parsing, language data, Addressables loading. | `Unity.Addressables`, `Unity.ResourceManager` |
| `Dotline.Localization.Editor` | `Editor/Dotline.Localization.Editor.asmdef` | Menus, Inspectors, CSV/XLSX import/export, validation. | `Dotline.Localization`, UnityEditor |
| `Dotline.Localization.Editor.Tests` | `Tests/Editor/Dotline.Localization.Editor.Tests.asmdef` | EditMode automated tests. | `Dotline.Localization`, `Dotline.Localization.Editor`, Unity Test Framework |

## Runtime modules

| Module | Description |
| --- | --- |
| `LanguageConfigSO` | Project language configuration and fallback definitions. |
| `LanguageDataSO` | Namespaced language table holding a list of `LocalizationData`. |
| `LocalizationData` | Multilingual texts and comment for a single key. |
| `LocalizationTemplateParser` | Superpower-based parser for `<Namespace|KEY(args)>` placeholders. |
| `LocalizationTemplateResolver` | Recursively resolves placeholders and applies arguments to `{0}`, `{1}`. |
| `LocalizationLookup` | Lazily loads and caches `LanguageDataSO` per namespace; case-insensitive lookup. |
| `LanguageDataLoader` | Addressables synchronous/async loading with handle caching. |
| `LocalizationKeyUtility` | Key naming-rule validation and normalization (uppercase snake case, bracket-note trimming, display-key detection). |

## Editor modules

Grouped by subfolder:

| Module | Path | Description |
| --- | --- | --- |
| `LocalizationCSVConverter` | `Editor/Core` | Scans the source folder and converts CSV/XLSX to SOs incrementally based on source-file + asset hashes. |
| `LanguageConfigSOEditor` | `Editor/Core` | Custom Inspector for `LanguageConfigSO`. |
| `LanguageDataSOAddressableRegistrar` | `Editor/Core` | Auto-registers validated `LanguageDataSO` assets into the `Localization` Addressables group, addressed by `NamespaceId`. |
| `LocalizationEditorLanguage` | `Editor/Core` | Detects the Unity editor UI language and maps it to a localization language code (cached). |
| `LocalizationEditorText` | `Editor/Core` | Multilingual text table for the editor UI (en/zh-Hans/zh-Hant/ja/ko). |
| `LanguageDataSOEditor` | `Editor/Inspector` | UI Toolkit Inspector: editing, import, export, filtering and error highlighting. |
| `LanguageDataSODuplicateKeyValidator` | `Editor/Inspector` | Scans for duplicate keys within the same namespace. |
| `LanguageDataSOPathService` | `Editor/Inspector` | Source path resolution and normalization: in-project paths stored as `Assets/...`, external paths as absolute paths. |
| `LocalizationSourceParser` | `Editor/Source` | Parses CSV/XLSX source files into `LocalizationData[]`. |
| `LocalizationSourceSchema` | `Editor/Source` | Standard headers, language column mapping, field read/write, content validation. |
| `LocalizationSourceHeaders` | `Editor/Source` | Semantic detection of source headers (Key/language/comment column aliases). |
| `LocalizationSourceExporter` | `Editor/Source` | Exports `LocalizationData` lists back to CSV/XLSX source files. |
| `CsvParser` | `Editor/Source` | Character-level CSV parsing and export: quotes, commas, multiline fields, UTF-8 BOM and GB18030 fallback. |
| `XlsxLocalizationParser` | `Editor/Source` | Reads XLSX via ExcelDataReader with multi-sheet import. |
| `XlsxLocalizationExporter` | `Editor/Source` | Updates existing workbooks or creates new ones via OpenXML. |
| `OpenXmlWorkbookBuilder` | `Editor/Source` | Builds the OpenXML ZIP structure used by XLSX export. |
| `LocalizationSourceFileAccess` | `Editor/Source` | Reads source files with shared read access to avoid file-lock conflicts with Excel etc. |

## Template parsing

Template placeholders must declare a namespace explicitly:

```text
<UI|START_GAME>
<UI|WELCOME(<UI|PLAYER_NAME>)>
<UI|WELCOME(<UI|PLAYER_NAME>, "Playing")>
```

The parser supports balanced matching of parentheses and angle brackets, so arguments can nest localization templates. Text arguments must use double quotes; the outer quotes are stripped after parsing. Bare arguments support numbers only. Fragments that fail to parse are kept as plain text, so TMP rich text and user content are never broken.

## Data consistency policy

- After import, language columns are normalized so every entry contains all language codes from the current configuration.
- Duplicate keys within the same namespace raise errors; later-loaded data overwrites earlier data.
- `<Namespace|KEY>` placeholders inside text content are treated as content errors, preventing translations from referencing keys indirectly.
- Deleting a source file only cleans up its hash record; the SO is never removed automatically.

## Addresses & caching

At runtime, `LanguageDataSO` is loaded through Addressables, maintaining:

- `ResolvedAddresses`: cache of requested name → actual Addressables address.
- `CachedHandles`: cache of Addressables handles.
- `CachedResources`: cache of loaded SOs.

The cache release method is internal and is not part of the public package API. If callers need explicit cache lifecycle control in the future, expose it on `LocalizationSystem` or a dedicated runtime facade.

## Development environment

- Unity: `2022.3.62f3c1`
- Test Framework: `com.unity.test-framework` `1.1.33`
