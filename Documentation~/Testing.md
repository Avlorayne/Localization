# Testing

**English** | [简体中文](Testing.zh-CN.md)

## Automated tests

EditMode tests live in `Tests/Editor`, assembly `Dotline.Localization.Editor.Tests`.

How to run:

1. Put the package into a host project (embedded under `Packages/` or referenced via Git URL).
2. Open the Unity Test Runner (Window → General → Test Runner).
3. Select EditMode and run `Dotline.Localization.Editor.Tests`.

## Covered cases

| Suite | Target |
| --- | --- |
| `LocalizationKeyUtilityTests` | Key naming rules, bracket-note trimming, display-key detection. |
| `LocalizationTemplateParserTests` | Basic placeholders, nested arguments, TMP tag skipping, nested-key detection. |
| `LocalizationDataTests` | Language text hits and empty-text failures. |
| `LanguageConfigSOTests` | Requested language lookup, one-level fallback and default-language fallback. |
| `LocalizationSourceHeadersTests` | Comment column header aliases. |

## Manual acceptance cases

| # | Scenario | Steps | Expected |
| --- | --- | --- | --- |
| M01 | First-time setup | Run `Tools/Localization/Open Language Config` | Creates and selects `Assets/Resources/Localization/LanguageConfig.asset`. |
| M02 | Incremental CSV import | Modify a CSV in the source folder, run `Convert Changed Source Files` | Only the changed source's `LanguageDataSO` is updated; Console logs the processed count. |
| M03 | XLSX import | Prepare an XLSX with a `Key` header and run conversion | Language columns import into `entries.texts`. |
| M04 | Headerless import | First column of the first sheet uses valid keys, no `Key` header | The first sheet imports with the standard header; sheets without a `Key` header are skipped. |
| M05 | Duplicate keys | Create the same key in two SOs of the same namespace, run validation | Console logs a fatal error and lists the conflicting files. |
| M06 | Invalid key | Enter a lowercase or space-containing key in the Inspector | The Inspector highlights it and logs an invalid-key error. |
| M07 | Key in content | Enter `<UI|START_GAME>` as language text | The Inspector highlights the content field and logs a fatal error. |
| M08 | CSV export | Click export in the `LanguageDataSO` Inspector and choose `.csv` | File is written with UTF-8 BOM and the standard header. |
| M09 | XLSX export | Click export in the `LanguageDataSO` Inspector and choose an existing `.xlsx` | Workbook structure is preserved; matching keys are updated and missing keys appended. |
| M10 | Automatic Addressables registration | Import or fix a DataSO in the Inspector until no key, duplicate-key or content-placeholder errors remain | The `Localization` group is created/reused; the DataSO joins it addressed by `NamespaceId`. |
| M11 | Runtime lookup | Call `<UI|START_GAME>` | Returns the current language's text; missing languages fall back. |
| M12 | Nested template | Call `<UI|WELCOME(<UI|PLAYER_NAME>)>` | Arguments resolve first, then the child template's `{0}` is replaced. |
| M13 | WebGL sync limitation | First synchronous lookup of an uncached namespace in a WebGL player | The limitation is documented; the current public API does not provide an async/preload recovery path. |
| M14 | Default fallback | Configure `fr -> en` with `zh-Hans` as default, omit the key from `fr` and `en`, and query in `fr` | The `zh-Hans` text is returned; `en`'s fallback is not followed recursively. |

## Pre-release checklist

- All EditMode tests pass.
- The Package Manager correctly displays the README, Samples and dependencies.
- `Basic Localization Example` imports without script compile errors.
- `Third Party Notices.md` covers the embedded Superpower and ExcelDataReader.
- The `package.json` version matches `CHANGELOG.md`.
