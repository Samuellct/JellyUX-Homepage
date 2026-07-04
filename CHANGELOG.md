## [0.13.0](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.12.1...v0.13.0) (2026-07-04)

### Features

* enable JellyUX home rendering inside native shell clients ([9c0ac77](https://github.com/Samuellct/JellyUX-Homepage/commit/9c0ac7728e447bafa247fefa28ac0cac0072bbb3))

## [0.12.1](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.12.0...v0.12.1) (2026-07-04)

### Bug Fixes

* improve error handling and logging robustness ([4e2d3c2](https://github.com/Samuellct/JellyUX-Homepage/commit/4e2d3c220a6781e81821858a8fdc589e190ed3d6))

## [0.12.0](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.11.2...v0.12.0) (2026-07-03)

### Features

* add French and English language files ([3b114b2](https://github.com/Samuellct/JellyUX-Homepage/commit/3b114b20d3bc6b9889e7ce849add3a0ffaec2b24))
* add localization service with JSON language files ([c7494b2](https://github.com/Samuellct/JellyUX-Homepage/commit/c7494b226bdb94ce23d8f467beba1cb6aac7e234))
* apply i18n to all widgets and admin UI ([b4ffec1](https://github.com/Samuellct/JellyUX-Homepage/commit/b4ffec175a30a8a7970bcba87439fa503fb1670f))

## [0.11.2](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.11.1...v0.11.2) (2026-07-03)

### Bug Fixes

* deduplicate TMDb list items by id before caching ([28ce003](https://github.com/Samuellct/JellyUX-Homepage/commit/28ce00367ca00002765357f74ff8380d51a72b19))
* remove Upcoming Movies widget (unusable in the Connected model, never matches local library) ([a492557](https://github.com/Samuellct/JellyUX-Homepage/commit/a492557f9c2813bccf5af9f9373d764024608b9a))

## [0.11.1](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.11.0...v0.11.1) (2026-07-02)

### Bug Fixes

* broaden On TV widget results using tv/on_the_air instead of tv/airing_today ([74543a3](https://github.com/Samuellct/JellyUX-Homepage/commit/74543a34a3746fb6c89c8b9474db27bca7962945))
* stop background TMDb status poll from wiping unsaved Pages/Region edits ([5dd8c0a](https://github.com/Samuellct/JellyUX-Homepage/commit/5dd8c0a83e67903790e4a27d2fcebb7f2d5b997b))

## [0.11.0](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.10.1...v0.11.0) (2026-07-02)

### Features

* add configurable pagination to TMDb list fetches ([f1868e1](https://github.com/Samuellct/JellyUX-Homepage/commit/f1868e14532babaaa6d8b0070a2d12911441548a))
* add customizable DiscoverMoviesWidget with TMDb autocomplete ([24d9db7](https://github.com/Samuellct/JellyUX-Homepage/commit/24d9db75f3ce50f712bfdf0daed425d7c048d64a))
* add TopRatedMoviesWidget, TopRatedShowsWidget, and NowPlayingMoviesWidget ([3b86684](https://github.com/Samuellct/JellyUX-Homepage/commit/3b866845efc1bc514c3978e0d129683f8e280e8d))

### Bug Fixes

* match widget config row by instance value in on-demand section fetch ([bb9261d](https://github.com/Samuellct/JellyUX-Homepage/commit/bb9261dd38d0bd5bbe6552299dc9b98e90dfcf62))

## [0.10.1](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.10.0...v0.10.1) (2026-07-02)

### Bug Fixes

* preserve existing TMDb cache instead of overwriting with an empty result ([7d148fb](https://github.com/Samuellct/JellyUX-Homepage/commit/7d148fb30146675d023ec018846e318b59a83854))

## [0.10.0](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.9.1...v0.10.0) (2026-07-02)

### Features

* add TMDb refresh status and manual trigger in admin UI ([557a5ff](https://github.com/Samuellct/JellyUX-Homepage/commit/557a5ffe008085b8304c8c0dd657705b9651337d))
* add TrendingMoviesWidget ([3386e63](https://github.com/Samuellct/JellyUX-Homepage/commit/3386e635ed0f09806147f9ccab6be0b999740ddf))
* add TrendingShowsWidget, AiringTodayShowsWidget, UpcomingMoviesWidget ([4da93dc](https://github.com/Samuellct/JellyUX-Homepage/commit/4da93dc089022727631e8b3d6dfccda25929aaa2))

## [0.9.1](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.9.0...v0.9.1) (2026-07-02)

### Bug Fixes

* log explicit per-type item and library-match counts on TMDb refresh ([02faa10](https://github.com/Samuellct/JellyUX-Homepage/commit/02faa1011c92a5838e69d21a13a0c822922c40c0))

## [0.9.0](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.8.0...v0.9.0) (2026-07-02)

### Features

* add TMDb cache service with library cross-referencing ([0bea103](https://github.com/Samuellct/JellyUX-Homepage/commit/0bea103046ce0269333f34566c4a5e2fb6eccfa3))
* add TMDb daily refresh scheduled task ([a35b023](https://github.com/Samuellct/JellyUX-Homepage/commit/a35b0234bc59a16164989ae95691c54e0ae44e49))
* add TMDb HTTP client with error handling ([a8e5496](https://github.com/Samuellct/JellyUX-Homepage/commit/a8e54962271cc167abde34103436fc6210363d93))

## [0.8.0](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.7.1...v0.8.0) (2026-07-01)

### Features

* add BecauseYouWatchedWidget ([65843dc](https://github.com/Samuellct/JellyUX-Homepage/commit/65843dcb430fec9ec8c1cfa6cef728f70fd34561))
* add FavoriteActorWidget and FavoriteDirectorWidget ([caa693f](https://github.com/Samuellct/JellyUX-Homepage/commit/caa693fbbeac2b6ab4876ea7a88f0d5176798102))
* add PersonalizedWidget base class and FavoriteGenreWidget ([2744341](https://github.com/Samuellct/JellyUX-Homepage/commit/27443413cfb9482e06107f0c4cf762968977d821))
* add ScoringService for user preference analysis ([a5a85f0](https://github.com/Samuellct/JellyUX-Homepage/commit/a5a85f051296bc0152e6cda1b5523ed810a55c43))
* emit one descriptor per widget instance from CreateInstances fan-out ([4b0bac6](https://github.com/Samuellct/JellyUX-Homepage/commit/4b0bac69cc4d1a6251a2e3287555218eb25a2134))
* register personalized widgets and add management UI ([a73bdcd](https://github.com/Samuellct/JellyUX-Homepage/commit/a73bdcd2a5f833726ee41cede2ebd96f9e9ff44e))

## [0.7.1](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.7.0...v0.7.1) (2026-07-01)

### Bug Fixes

* add search-as-you-type autocomplete and restore collection label on reload ([57a9a74](https://github.com/Samuellct/JellyUX-Homepage/commit/57a9a740cdd08a8b752587e6c3b7c0791364c96c))
* resolve collection items via BoxSet linked children instead of AncestorIds ([43a2d28](https://github.com/Samuellct/JellyUX-Homepage/commit/43a2d28142cc9a70ccc08209f7d5488f3af02b7a))

## [0.7.0](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.6.2...v0.7.0) (2026-07-01)

### Features

* add ActorWidget and DirectorWidget ([430fdcf](https://github.com/Samuellct/JellyUX-Homepage/commit/430fdcf32af9e611fd9d66e23b81325bb211dd8d))
* add admin widget management UI with value autocomplete ([276c952](https://github.com/Samuellct/JellyUX-Homepage/commit/276c952357e6f456abad41fc69a57438edecb098))
* add AdminWidget base class with library value provider ([b77fa9e](https://github.com/Samuellct/JellyUX-Homepage/commit/b77fa9ea18ee80e80c9a2aa5fc7d6e19311c62d9))
* add GenreWidget ([8a0b896](https://github.com/Samuellct/JellyUX-Homepage/commit/8a0b896855a4c4ac72b69e842b3d49dbc92d5efc))
* add StudioWidget, CollectionWidget, TagWidget, YearWidget ([8b9ac85](https://github.com/Samuellct/JellyUX-Homepage/commit/8b9ac85ff3300baa4531494a93cc12f217d3888e))
* add widget values endpoint for admin autocomplete ([d85b2a2](https://github.com/Samuellct/JellyUX-Homepage/commit/d85b2a2a066e7c97e32b7a35481ab830f292cefe))
* propagate per-instance AdditionalData through widget layout ([62a76dc](https://github.com/Samuellct/JellyUX-Homepage/commit/62a76dc98dd6eaeb7fcb6af3a4bbb684c96e2a05))

## [0.6.2](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.6.1...v0.6.2) (2026-06-30)

### Bug Fixes

* strip remaining is=emby-* attributes to silence webcomponents polyfill errors ([76c18d3](https://github.com/Samuellct/JellyUX-Homepage/commit/76c18d39bf9b7e642a6df4ac5fddfbd38d24291b))

## [0.6.1](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.6.0...v0.6.1) (2026-06-30)

### Bug Fixes

* remove is=emby-checkbox to prevent htmlFor error in webcomponents polyfill ([199844a](https://github.com/Samuellct/JellyUX-Homepage/commit/199844a9bc584d7ecd4624acfa7ca3e2b01a69c0))

## [0.6.0](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.5.1...v0.6.0) (2026-06-30)

### Features

* add admin configuration page with widget management UI ([754b793](https://github.com/Samuellct/JellyUX-Homepage/commit/754b793708611b236a27a38a578b2ecaecdca9da))
* invalidate session cache when configuration changes ([46df34e](https://github.com/Samuellct/JellyUX-Homepage/commit/46df34e31dc5dd46468dd287f3b4900278d2d7ba))

## [0.5.1](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.5.0...v0.5.1) (2026-06-30)

### Bug Fixes

* resolve broken see-all links for recently-added sections ([5e6ec17](https://github.com/Samuellct/JellyUX-Homepage/commit/5e6ec173d26edc4ea3c60dad92f33b1b21da7a11))

## [0.5.0](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.4.0...v0.5.0) (2026-06-30)

### Features

* add client detection and JellyfinAPI global attachment ([cad02fc](https://github.com/Samuellct/JellyUX-Homepage/commit/cad02fcf622af8f27582f0d9db194093c7a37894))
* implement loadSections override with API-driven widget rendering ([961fda5](https://github.com/Samuellct/JellyUX-Homepage/commit/961fda53431e5d3e2bfcce416b5c5c0e87e2bbb4))

## [0.4.0](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.3.1...v0.4.0) (2026-06-29)

### Features

* add ContinueWatchingWidget ([fd73acd](https://github.com/Samuellct/JellyUX-Homepage/commit/fd73acd6a8591dce3fb7f6e5c957efbd68833bc5))
* add MyMediaWidget ([04626ed](https://github.com/Samuellct/JellyUX-Homepage/commit/04626ed3e80580057a0107c5a58b7d7ffa170a0b))
* add NativeWidgetBase shared base class for native widgets ([68a41e4](https://github.com/Samuellct/JellyUX-Homepage/commit/68a41e4a4c8d45a19bd5db3c10f4944ebe9b51b4))
* add NextUpWidget ([3db6221](https://github.com/Samuellct/JellyUX-Homepage/commit/3db6221af8dc2fdab4a81ba4c84da364adb19576))
* add RecentlyAddedMoviesWidget and RecentlyAddedShowsWidget ([2f3bd63](https://github.com/Samuellct/JellyUX-Homepage/commit/2f3bd63833a2f9cf854cb158dfdcc12c105e3140))
* register native widgets with default configuration ([594df9f](https://github.com/Samuellct/JellyUX-Homepage/commit/594df9fecb6a81bb7a8ce53cf13490569b561996))

## [0.3.1](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.3.0...v0.3.1) (2026-06-29)

### Bug Fixes

* replace Dictionary ExtraParams with WidgetExtraParam array for XmlSerializer compatibility ([ed67721](https://github.com/Samuellct/JellyUX-Homepage/commit/ed677215aa319d8b0f37624b5914ea9b44c63eb1))

## [0.3.0](https://github.com/Samuellct/JellyUX-Homepage/compare/v0.2.0...v0.3.0) (2026-06-29)

### Features

* add API endpoints for sections, items and configuration ([8222a75](https://github.com/Samuellct/JellyUX-Homepage/commit/8222a75443d19c2ef380ca0895b8deee772f6f41))
* add complete plugin and user configuration models ([891de7c](https://github.com/Samuellct/JellyUX-Homepage/commit/891de7c7c73d4de6724939ac253464656a893e25))
* add widget engine core interfaces and models ([f42bece](https://github.com/Samuellct/JellyUX-Homepage/commit/f42bece0dba534cfdde3ca932ef610d49345b9c7))
* add widget registry with external DLL discovery ([4703498](https://github.com/Samuellct/JellyUX-Homepage/commit/47034986986fcdf06ac9952bd6714c52049c25b8))
* add widget service with session cache and MinItems filtering ([27d226a](https://github.com/Samuellct/JellyUX-Homepage/commit/27d226a85648d85d3cfe5478d0107fdaa8ab74d1))

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
