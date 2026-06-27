# JellyUX Homepage

A modular home screen engine for Jellyfin that replaces the default home page with a fully configurable widget system.

![Build](https://github.com/Samuellct/JellyUX-Homepage/actions/workflows/build.yml/badge.svg)
![Version](https://img.shields.io/badge/version-0.1.0-blue)
![Jellyfin](https://img.shields.io/badge/Jellyfin-10.11.10%2B-orange)
![License](https://img.shields.io/badge/license-GPL--3.0-green)

---

## Prerequisites

- **Jellyfin** 10.11.10 or later
- **[File Transformation plugin](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation)** (required - JellyUX uses it to inject its scripts into the Jellyfin web client)
- **.NET 9 SDK** (contributors only, not required for end users)

---

## Installation

1. In your Jellyfin dashboard, go to **Administration > Plugins > Repositories**
2. Click **Add** and paste the following URL:
   ```
   https://raw.githubusercontent.com/Samuellct/JellyUX-Homepage/main/manifest.json
   ```
3. Go to **Administration > Plugins > Catalog**, find **JellyUX Homepage** and install it
4. Restart Jellyfin
5. Go to **Administration > Plugins > JellyUX Homepage** to configure your widgets

---

## Configuration

After installation, the plugin configuration page lets you:
- Enable or disable individual widgets
- Reorder widgets via drag and drop
- Configure per-widget parameters (minimum items, source library, etc.)

---

## Compatibility

| Component | Status |
|---|---|
| Jellyfin Web client | Supported (all major browsers) |
| Media Bar plugin | Compatible |
| ZestyTheme CSS | Compatible |
| Native TV clients (Android TV, Apple TV, Roku) | Auto-disabled - native home screen is preserved |
| Jellyfin mobile apps | Not supported (native clients) |

---

## Widgets

| Widget | Category | Status |
|---|---|---|
| Continue Watching | Native | Planned |
| Next Up | Native | Planned |
| Recently Added Movies | Native | Planned |
| Recently Added Shows | Native | Planned |
| My Media | Native | Planned |
| By Genre | Administrative | Planned |
| By Actor | Administrative | Planned |
| By Director | Administrative | Planned |
| By Studio | Administrative | Planned |
| By Collection | Administrative | Planned |
| By Tag | Administrative | Planned |
| By Year | Administrative | Planned |
| Because You Watched | Personalized | Planned |
| Favorite Genre Picks | Personalized | Planned |
| Favorite Actor Picks | Personalized | Planned |
| Favorite Director Picks | Personalized | Planned |
| Trending Movies (TMDb) | Connected | Planned |
| Trending Shows (TMDb) | Connected | Planned |
| Airing Today (TMDb) | Connected | Planned |
| Upcoming Movies (TMDb) | Connected | Planned |

---

## Roadmap

See [TODO_V1.md](TODO_V1.md) for the full development roadmap (local planning document, not tracked in the repository).

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup, conventions, and guidelines.

---

## License

This project is licensed under the GNU General Public License v3.0. See the [LICENSE](LICENSE) file for details.
