# MTG Commander Engine

A moddable, multiplayer MTG Commander game engine with automated rule enforcement.

## Vision

An **MTG Arena-like** experience for the **Commander** format:

- **2 to 6 players**, turn-based
- **Automated effects** — the engine enforces card rules on the battlefield
- **Moddable** — host your own server with custom cards (like Minecraft)
- **Community-driven** — contributors add cards via GitHub PRs or mods

## Project Structure

```
mtg-project/
├── src/
│   ├── server/        # Game server (C# / ASP.NET Core)
│   ├── client/        # Desktop client (C# / WPF or Avalonia)
│   └── shared/        # Shared models and contracts
├── cards/             # Core card definitions (JSON)
├── mods/              # Mod packages (cards + effects)
├── docs/              # Documentation
│   ├── user-stories/  # User stories
│   ├── architecture/  # Architecture decision records
│   └── api/           # API / protocol documentation
└── tools/             # Scripts and utilities
```

## Tech Stack

| Component   | Technology              |
|-------------|-------------------------|
| Server      | C# / ASP.NET Core       |
| Client      | C# / WPF or Avalonia    |
| Realtime    | WebSocket               |
| Database    | SQL                     |
| Card format | JSON (V1) → DSL (V2+)  |

## Getting Started

> Project in early setup phase. Instructions coming soon.

## Contributing

Cards and effects can be contributed in two ways:

1. **Core cards** — Submit a PR adding JSON card definitions to `cards/`
2. **Mods** — Create a mod package in `mods/` format and share it

See [CONTRIBUTING.md](CONTRIBUTING.md) for details (coming soon).

## License

TBD
