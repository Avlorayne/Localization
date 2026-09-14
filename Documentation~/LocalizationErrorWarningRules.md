# Localization Error and Warning Rules

This document centralizes the error and warning rules for the `com.dotline.localization`
system. It is intended for frontend Editor tooling, QA, planning, and runtime debugging.

## Severity

| Severity | Meaning | Expected action |
| --- | --- | --- |
| Fatal Error | Data is invalid or an operation failed. The user must fix data/configuration before trusting generated localization output. | Block save/publish/registration where possible. Show the issue in the Editor UI. |
| Error | Runtime or Editor operation failed. Existing data may remain unchanged, but the requested action did not complete. | Show a visible failure state and provide a direct fix. |
| Warning | The system recovered, skipped optional work, or detected risky configuration. | Allow the workflow to continue, but surface the warning. |
| Dialog | Blocking Editor dialog shown to the user. | Treat as an operation result in frontend tooling. |

## General Data Rules

| ID | Severity | Rule | Trigger | User-facing guidance | Source |
| --- | --- | --- | --- | --- | --- |
| LOC-DATA-001 | Fatal Error | Keys inside a `LanguageDataSO` must be valid literals. | A key is empty or contains characters outside `A-Z`, `0-9`, `_`. | Rename the key. Use upper snake case, for example `START_GAME`. | `LanguageDataSOEditor`, `LanguageDataSOAddressableRegistrar` |
| LOC-DATA-002 | Fatal Error | Keys must be unique within the same namespace. | Two or more `LanguageDataSO` assets share the same effective namespace and key. | Rename or remove the duplicate entry. One namespace may contain a key only once. | `LanguageDataSODuplicateKeyValidator`, `LocalizationLookup` |
| LOC-DATA-003 | Fatal Error | Language text must not store localization placeholders. | A language text field contains a parseable placeholder such as `<UI\|START_GAME>`. | Move composition into the calling template instead of storing key references in translated text. | `LocalizationSourceSchema`, `LanguageDataSOEditor` |
| LOC-DATA-004 | Warning | Empty-key entries are ignored at runtime. | A loaded `LanguageDataSO` contains an entry with an empty key. | Fix or delete the entry in the Inspector/source file. | `LocalizationLookup` |
| LOC-DATA-005 | Warning | A `LanguageDataSO` namespace should be unique. | A `LanguageDataSO` with empty explicit namespace falls back to an asset name that is already cached by another SO. | Set a unique `namespaceId` or rename the asset. | `LanguageDataSO` |

## Language Config Rules

| ID | Severity | Rule | Trigger | User-facing guidance | Source |
| --- | --- | --- | --- | --- | --- |
| LOC-CONFIG-001 | Error | Source and SO folders must be configured before conversion. | `sourceFolderPath` or `soFolderPath` is empty. | Open `Tools/Localization/Open Language Config` and fill both paths. | `LocalizationSourceConverter` |
| LOC-CONFIG-002 | Error | Source folder must exist before conversion. | `sourceFolderPath` points to a missing folder. | Create the folder or correct the path. | `LocalizationSourceConverter` |
| LOC-CONFIG-003 | Warning | Generated SO folder should be under a `Resources` folder. | `soFolderPath` does not contain `/Resources/` and does not start with `Resources/`. | Prefer `Assets/Resources/Localization` unless the project has a custom loading path. | `LanguageConfigSOEditor` |
| LOC-CONFIG-004 | Warning | Unknown language codes fall back to default language. | `LanguageConfigSO.GetDefinition(languageCode)` cannot find the requested code. | Add the language in **Project Settings → Localization** or request a supported language. | `LanguageConfigSO` |
| LOC-CONFIG-005 | Warning | Setting current language to an unknown code is rejected. | `LocalizationSystem.CurrentLanguageCode` is assigned a code missing from the configured languages. | Use a configured language code. | `LocalizationSystem` |
| LOC-CONFIG-006 | Warning | Missing language config falls back to built-in language columns in Editor parsing/export. | Project Settings language definitions are unavailable. | Open **Project Settings → Localization** before importing or exporting. | `LocalizationSourceSchema` |
| LOC-CONFIG-007 | Warning | Duplicate language codes in config are ignored by Editor schema loading. | Two `LanguageDefinition` entries normalize to the same code. | Keep only one entry per language code. | `LocalizationSourceSchema` |
| LOC-CONFIG-008 | Warning | Runtime language config may be stale. | Project Settings changed but the baked `Resources/Localization/LanguageConfig` copy was not refreshed. | Run `Tools/Localization/Bake Runtime Language Config`. | `LanguageConfigBaker` |

## Source Import and Conversion Rules

| ID | Severity | Rule | Trigger | User-facing guidance | Source |
| --- | --- | --- | --- | --- | --- |
| LOC-SOURCE-001 | Warning | Unreadable source/SO files are skipped. | Hashing throws `IOException` or `UnauthorizedAccessException`. | Close Excel or any process locking the file, then convert again. | `LocalizationSourceConverter` |
| LOC-SOURCE-002 | Warning | Empty source files do not update existing SO data. | Parsed source produces zero localization entries. | Check the source has a `Key` column or valid headerless rows. | `LocalizationSourceConverter` |
| LOC-SOURCE-003 | Warning | Deleted source files only remove hash records. | A previously hashed source file no longer exists. | Manually review the corresponding SO; it is intentionally kept. | `LocalizationSourceConverter` |
| LOC-SOURCE-004 | Warning | Corrupt hash cache is ignored. | Loading `Assets/Settings/LocalizationSourceHashes.json` fails. | Convert again; the cache will be regenerated when conversion succeeds. | `LocalizationSourceConverter` |
| LOC-SOURCE-005 | Error | Unsupported source format cannot be imported. | Source extension is not `.csv` or `.xlsx`. | Use `.csv` or `.xlsx`. | `LocalizationSourceParser`, `LanguageDataSOEditor` |
| LOC-SOURCE-006 | Warning | Unknown source columns are skipped. | Import/export schema sees a header that is not `Key`, a configured language, or a comment alias. | Fix the header spelling or add the language to config. | `LocalizationSourceSchema` |
| LOC-SOURCE-007 | Dialog | Import requires a source path. | User clicks `Import from Source` on a SO with empty `sourceFilePath`. | Assign a source file first. | `LanguageDataSOEditor` |
| LOC-SOURCE-008 | Dialog | Import source must exist. | Stored source path resolves to a missing file. | Restore the file or update `sourceFilePath`. | `LanguageDataSOEditor` |
| LOC-SOURCE-009 | Dialog | Empty import result does not overwrite the SO. | Import from source returns zero entries. | Fix the source structure and retry. | `LanguageDataSOEditor` |
| LOC-SOURCE-010 | Dialog | Import replaces all existing entries. | User imports a supported source into an existing SO. | Confirm intentionally; frontend should show this as destructive replacement. | `LanguageDataSOEditor` |

## CSV Rules

| ID | Severity | Rule | Trigger | User-facing guidance | Source |
| --- | --- | --- | --- | --- | --- |
| LOC-CSV-001 | Error | CSV file must exist. | `CsvParser.Import(csvPath)` receives a missing path. | Restore the CSV or correct `sourceFilePath`. | `CsvParser` |
| LOC-CSV-002 | Warning | Non-UTF-8 CSV fallback requires GB18030 support. | CSV has no BOM, strict UTF-8 decoding fails, and `Encoding.GetEncoding("GB18030")` is unavailable. | Save the CSV as UTF-8 with BOM if characters look broken. | `CsvParser` |
| LOC-CSV-003 | Warning | Invalid CSV rows are skipped silently. | Row key cannot be normalized by `LocalizationKeyUtility`. | Check rows whose key is empty or invalid. | `CsvParser` |

## XLSX Rules

| ID | Severity | Rule | Trigger | User-facing guidance | Source |
| --- | --- | --- | --- | --- | --- |
| LOC-XLSX-001 | Error | XLSX file must exist. | `XlsxLocalizationParser.Import(xlsxPath)` receives a missing path. | Restore the XLSX or correct `sourceFilePath`. | `XlsxLocalizationParser` |
| LOC-XLSX-002 | Error | XLSX import failure is reported with exception text. | ExcelDataReader cannot open/read the workbook. | Close Excel, verify the file is a valid `.xlsx`, and retry. | `XlsxLocalizationParser` |
| LOC-XLSX-003 | Warning | Worksheets without a `Key` header are skipped, except first-sheet headerless import. | A non-first sheet lacks `Key`, or first sheet does not look like localization data. | Add a `Key` header to each sheet intended for localization. | `XlsxLocalizationParser` |
| LOC-XLSX-004 | Error | Existing workbook export requires at least one worksheet with `Key`. | Export patches an existing workbook but no sheet has a `Key` header. | Add a `Key` header or export to a new workbook. | `XlsxLocalizationExporter` |
| LOC-XLSX-005 | Error | XLSX export failure is reported with exception text. | Creating/patching the workbook throws. | Check file permissions, workbook structure, and whether the file is locked. | `XlsxLocalizationExporter` |

## Export Rules

| ID | Severity | Rule | Trigger | User-facing guidance | Source |
| --- | --- | --- | --- | --- | --- |
| LOC-EXPORT-001 | Error | Unsupported export format cannot be written. | Output extension is not `.csv` or `.xlsx`. | Export to `.csv` or `.xlsx`. | `LocalizationSourceExporter`, `LanguageDataSOEditor` |
| LOC-EXPORT-002 | Dialog | Export overwrites the target source file. | User exports to an existing or selected source path. | Confirm intentionally; frontend should show overwrite clearly. | `LanguageDataSOEditor` |
| LOC-EXPORT-003 | Dialog | Export failure is shown to the user. | Export returns `false`. | Surface the path and underlying console error. | `LanguageDataSOEditor` |

## Addressables Rules

| ID | Severity | Rule | Trigger | User-facing guidance | Source |
| --- | --- | --- | --- | --- | --- |
| LOC-ADDR-001 | Warning | Addressables settings must be available for automatic registration. | `AddressableAssetSettingsDefaultObject.GetSettings(true)` returns null. | Check the Addressables package, project asset permissions, and settings integrity. | `LanguageDataSOAddressableRegistrar` |
| LOC-ADDR-002 | Error | Localization Addressables group must be creatable. | Creating the `Localization` group returns null. | Check Addressables package/settings integrity. | `LanguageDataSOAddressableRegistrar` |
| LOC-ADDR-003 | Error | Asset entry must be creatable or movable into the Localization group. | `CreateOrMoveEntry` returns null. | Check the asset GUID, Addressables settings, and package state. | `LanguageDataSOAddressableRegistrar` |
| LOC-ADDR-004 | Rule | Invalid localization data is not registered. | Any key, duplicate-key, or embedded-placeholder validation fails. | Fix validation errors first; registration happens after clean validation. | `LanguageDataSOAddressableRegistrar`, `LanguageDataSOEditor` |

## Runtime Loading and Lookup Rules

| ID | Severity | Rule | Trigger | User-facing guidance | Source |
| --- | --- | --- | --- | --- | --- |
| LOC-RUNTIME-001 | Error | Addressable language data address cannot be empty. | Runtime load receives null, empty, or whitespace address. | Check template namespace and loader input. | `LanguageDataLoader` |
| LOC-RUNTIME-002 | Error | Synchronous Addressables loading is not supported on WebGL Player. | `TryLoadDataResource` is called on WebGL outside the Editor. | Preload asynchronously or expose a public async API before WebGL use. | `LanguageDataLoader` |
| LOC-RUNTIME-003 | Error | Addressables address resolution failed. | No candidate address resolves to a `LanguageDataSO`. | Ensure SO is registered as Addressable with address equal to `NamespaceId`. | `LanguageDataLoader` |
| LOC-RUNTIME-004 | Error | Addressables asset load failed. | Load operation fails or throws. | Inspect the operation exception; rebuild Addressables if necessary. | `LanguageDataLoader` |
| LOC-RUNTIME-005 | Error | Template namespace cannot be empty. | Resolver asks lookup for an empty namespace. | Use templates in the form `<Namespace\|KEY>`. | `LocalizationLookup` |
| LOC-RUNTIME-006 | Error | Namespace could not be loaded. | Lookup misses the namespace and loader cannot load it. | Check namespace spelling, asset name, `NamespaceId`, and Addressables address. | `LocalizationLookup` |
| LOC-RUNTIME-007 | Warning | Key or language text was not found. | Namespace loads but key/language lookup fails after fallback attempts. | Check key spelling and language/fallback/default language configuration. | `LocalizationLookup` |
| LOC-RUNTIME-008 | Error | Duplicate runtime keys overwrite previous entries. | Loaded data contains duplicate keys in the same namespace. | Fix data; runtime keeps the later-loaded value. | `LocalizationLookup` |

## Template Parsing Rules

| ID | Severity | Rule | Trigger | User-facing guidance | Source |
| --- | --- | --- | --- | --- | --- |
| LOC-TEMPLATE-001 | Rule | Valid placeholder form is `<Namespace\|KEY>` or `<Namespace\|KEY(args)>`. | Parser sees a balanced placeholder matching grammar. | Use explicit namespace for every placeholder. | `LocalizationTemplateParser` |
| LOC-TEMPLATE-002 | Rule | Invalid placeholder-like text is treated as normal text. | Parser sees invalid angle-bracket content, TMP rich text, or malformed args. | Do not rely on malformed placeholders producing errors. Use validation in Editor tooling. | `LocalizationTemplateParser` |
| LOC-TEMPLATE-003 | Rule | Arguments may be quoted strings, numbers, or nested placeholders. | Argument parsing validates each argument. | Quote literal strings with double quotes. Missing `{index}` args remain unchanged. | `LocalizationTemplateParser`, `LocalizationTemplateResolver` |

## Inspector UI Rules

| ID | Severity | Rule | Trigger | User-facing guidance | Source |
| --- | --- | --- | --- | --- | --- |
| LOC-UI-001 | Warning | Missing USS only affects styling. | `LanguageDataSO.uss` cannot be loaded. | Restore the USS file if the Inspector looks broken. | `LanguageDataSOEditor` |
| LOC-UI-002 | Fatal Error | Inspector error box aggregates duplicate keys, invalid keys, and embedded placeholders. | Any of the three validation sets is non-empty. | Fix all displayed issues before expecting Addressables registration. | `LanguageDataSOEditor` |
| LOC-UI-003 | Dialog | Deleting an entry requires confirmation. | User clicks delete on an entry. | Confirm only if the entry should be removed from the SO. | `LanguageDataSOEditor` |

## Sample Rules

| ID | Severity | Rule | Trigger | User-facing guidance | Source |
| --- | --- | --- | --- | --- | --- |
| LOC-RUNTIME-008 | Error | Runtime language config is missing. | `LocalizationSystem` cannot load `Resources/Localization/LanguageConfig`. | Bake Project Settings to `Assets/Resources/Localization/LanguageConfig.asset`. | `LocalizationSystem` |

## Frontend Editor Handling Policy

1. Treat all `Fatal Error` items as save/publish blockers.
2. Treat all `Error` items as failed operations; keep previous data unchanged unless the called Unity API already changed it.
3. Treat all `Warning` items as non-blocking but visible.
4. Always display the asset path, namespace, key, language code, and source file path when available.
5. For batch conversion, return both processed files and skipped files.
6. For import, make replacement explicit because importing source data overwrites all entries in the target SO.
7. For export, make overwrite explicit because the target source file may be replaced or patched.
8. After any successful data edit/import, run the same validation categories as the Inspector:
   invalid keys, duplicate keys, embedded localization placeholders.
9. Only register `LanguageDataSO` assets to Addressables after validation passes.
10. For WebGL, do not call the synchronous runtime lookup path for namespaces that may not already be cached.

## Recommended Stable Error Codes for Frontend APIs

Use these stable codes even if the localized Console text changes:

```text
INVALID_KEY
DUPLICATE_KEY_IN_NAMESPACE
EMBEDDED_LOCALIZATION_PLACEHOLDER
EMPTY_KEY_SKIPPED
NON_UNIQUE_NAMESPACE
MISSING_CONFIG_PATH
SOURCE_FOLDER_MISSING
SO_FOLDER_NOT_RESOURCES
UNKNOWN_LANGUAGE_CODE
MISSING_LANGUAGE_CONFIG
DUPLICATE_LANGUAGE_CODE
MULTIPLE_LANGUAGE_CONFIGS
FILE_READ_FAILED
SOURCE_EMPTY
SOURCE_DELETED_HASH_CLEANED
HASH_CACHE_LOAD_FAILED
UNSUPPORTED_SOURCE_FORMAT
UNKNOWN_SOURCE_COLUMN
SOURCE_FILE_REQUIRED
SOURCE_FILE_NOT_FOUND
CSV_NOT_FOUND
CSV_ENCODING_FALLBACK_UNAVAILABLE
XLSX_NOT_FOUND
XLSX_IMPORT_FAILED
XLSX_SHEET_SKIPPED
XLSX_NO_KEY_SHEET
XLSX_EXPORT_FAILED
UNSUPPORTED_EXPORT_FORMAT
EXPORT_FAILED
ADDRESSABLE_SETTINGS_MISSING
ADDRESSABLE_GROUP_CREATE_FAILED
ADDRESSABLE_ENTRY_CREATE_FAILED
ADDRESSABLE_ADDRESS_EMPTY
WEBGL_SYNC_LOAD_UNSUPPORTED
ADDRESSABLE_ADDRESS_NOT_FOUND
ADDRESSABLE_LOAD_FAILED
EMPTY_NAMESPACE
NAMESPACE_LOAD_FAILED
KEY_OR_LANGUAGE_NOT_FOUND
INSPECTOR_STYLE_MISSING
SAMPLE_CONFIG_MISSING
```
