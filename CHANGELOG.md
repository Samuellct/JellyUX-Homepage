## [0.2.0](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.1.1...v0.2.0) (2026-06-28)

### Features

* add controller endpoints for web resources and meta ([47df8a9](https://github.com/Samuellct/JellyUX-Homepage/commit/47df8a910bf7feac8e5679c8570f4f643f63fb94))
* add embedded web resource placeholders ([8721595](https://github.com/Samuellct/JellyUX-Homepage/commit/8721595000deb0aba5f20bc9e245010bdeec0bd4))
* add FileTransformation detector with explicit error on missing dependency ([55afd38](https://github.com/Samuellct/JellyUX-Homepage/commit/55afd3876d45d250edc3f38e0a2e63e52f76e361))
* inject CSS and JS into index.html via FileTransformation ([6ca0f74](https://github.com/Samuellct/JellyUX-Homepage/commit/6ca0f7406af0829d4e95ac5689678710b73d7dd5))

## [0.1.1](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.1.0...v0.1.1) (2026-06-28)

### Bug Fixes

* correct sourceUrl in manifest and plugin-url in release workflow ([9a3769a](https://github.com/Samuellct/JellyUX-Homepage/commit/9a3769af4d9969023e254ac67a892c752d65c5ad))
* remove stale task reference comment from PluginServiceRegistrator ([0442ed2](https://github.com/Samuellct/JellyUX-Homepage/commit/0442ed24f1fc02bba2945f3e091026830ffa4936))

# Changelog

<!-- Entries above this line are generated automatically by semantic-release. -->

## [0.1.0] - 2026-06-28

### Added

- .NET 9 solution (`JellyUX-Homepage.sln`) and `Jellyfin.Plugin.JuxHomepage` project (net9.0, Jellyfin 10.11 NuGet packages)
- `Plugin.cs` - plugin entry point inheriting `BasePlugin<PluginConfiguration>` (GUID, Name, Description)
- `PluginConfiguration.cs` - empty configuration class, to be populated in Phase 3
- `PluginServiceRegistrator.cs` - DI service registrator stub implementing `IPluginServiceRegistrator`
- `.gitignore` exceptions for `src/` (source files tracked, `bin/` and `obj/` ignored)
- GitHub Actions CI workflow (`build.yml`) with conditional guard (no-op until Phase 1 adds a .sln)
- GitHub Actions release workflow (`release.yml`) using JPRM for plugin packaging and MD5 checksum
- `build.yaml` - JPRM plugin metadata (GUID: 3adf1f1f-1541-4e47-b9e3-34d2d2968af6, targetAbi 10.11.0.0, net9.0)
- `manifest.json` - initial Jellyfin plugin repository manifest (empty versions, filled by release workflow)
- `README.md` - project overview, installation instructions, widget table, compatibility notes
- `CONTRIBUTING.md` - development setup, code conventions, No AI Slope design rules, widget contribution guide
- `LICENSE` - GNU General Public License v3.0
