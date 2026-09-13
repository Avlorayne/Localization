# FAQ

**English** | [简体中文](FAQ.zh-CN.md)

## Why does the output folder default to `Assets/Resources/Localization`?

It is a historical default, and the address candidates stay compatible with Resources-style paths. Runtime loading actually goes through Addressables: once a `LanguageDataSO` passes key, duplicate-key and content-placeholder validation, the tooling automatically adds it to the `Localization` Addressables group with its `NamespaceId` as the address.

## Why does runtime report "Addressable language data not found"?

Common causes: the data did not pass validation, or the Addressables address does not match the namespace. Auto-registration uses `NamespaceId` as the address; it is still recommended to keep the `LanguageDataSO` asset name and `NamespaceId` identical, e.g. both `UI`.

## What about garbled CSV text?

The importer detects the BOM first; without a BOM it strictly tries UTF-8, then falls back to GB18030 for Chinese Excel ANSI files. Teams should still standardize on UTF-8.

## Can keys contain Chinese or lowercase letters?

Not recommended. The safest runtime key format is `A-Z`, `0-9`, `_`, e.g. `MAIN_MENU_START`. Headered source files may import display keys, but the Inspector marks invalid keys as errors.

## Can I write `<UI|SOME_KEY>` inside text content?

Never store localization keys as language text content. The tooling treats localization placeholders in content as errors, preventing translation data from forming implicit dependency chains. To compose text, pass nested arguments in the calling template.

## When do template arguments need quotes?

Bare (unquoted) arguments support numbers only, e.g. `<UI|ITEM_COUNT(3)>`. Text arguments must be wrapped in double quotes, e.g. `<UI|WELCOME(<UI|PLAYER_NAME>, "Captain")>`; the outer quotes are stripped after parsing. Quoted content is passed through as-is without variable substitution — interpolate dynamic values into the template in code before passing them.

## How do I add a new language?

Add a `LanguageDefinition` to `LanguageConfigSO.languages`, e.g. `fr`. Then re-import or re-export the source files; standard headers and Inspector fields will automatically include the new language column.

## Why does the SO remain after deleting a source file?

The converter only cleans up the hash record and never deletes `LanguageDataSO` automatically. This protects hand-maintained or already versioned language data from accidental removal.

## Why can't WebGL load synchronously?

The current public API exposes synchronous lookup only. WebGL does not support synchronous waits for Addressables, so this release does not support the first lookup of an uncached namespace on WebGL. A public asynchronous/preload API is required for that scenario.

## Can it coexist with the official Unity Localization package?

They can coexist, but their responsibilities differ. This package uses its own `LanguageDataSO`, source converter and template syntax; do not expect it to read Unity's official String Tables.
