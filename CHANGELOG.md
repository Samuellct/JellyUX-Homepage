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
