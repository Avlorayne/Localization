# Changelog

All notable changes to this package will be documented in this file.

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
