# Changelog

All notable changes to this package will be documented in this file.

## [1.0.4] - 2026-09-14

### Added

- Declare `com.unity.textmeshpro` `3.0.7` as a package dependency so TMP components and Essentials are available to the package and its samples.

## [1.0.3] - 2026-09-14

### Changed

- Restrict Editor-only implementation types to `internal` while preserving EditMode test access through a friend assembly.
- Store the runtime `LanguageConfigSO` at `Assets/Resources/Localization/LanguageConfig.asset` by default; keep the Editor-only source hash cache under `Assets/Settings`.
- Update the Basic Localization sample and runtime API documentation to expose a reusable singleton component with automatic Resources configuration loading, current language code lookup, and documented usage.
- Update package metadata and installation references to `1.0.3`.

## [1.0.2] - 2026-09-14

### Fixed

- Ignore the `Key` source column after CSV key normalization so valid CSV imports do not emit a false unknown-column warning.
- Pin the README Git installation examples to the matching `1.0.2` package tag.

## [1.0.0] - 2026-09-13

### Added

- Runtime localization data model, template parsing, language lookup, and Addressables-backed loading.
- Editor CSV/XLSX import and export workflows.
- `LanguageConfigSO` and `LanguageDataSO` custom editor tooling.
- Duplicate key, invalid key, and embedded localization placeholder validation.
- Package README, documentation, tests, and a Package Manager sample.
