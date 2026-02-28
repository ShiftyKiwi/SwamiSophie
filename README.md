# Swami Sophie — Eorzean Mega Arcana

Swami Sophie is a Dalamud plugin for Final Fantasy XIV that delivers serious tarot-style readings using the Eorzean Mega Arcana deck. It is a text-focused, offline, data-driven reading tool built around spreads, interpretation modes, and doctrinal rules. It does not perform gameplay automation, combat interaction, or network-driven features.

## Features

- 3-card spread: `Aether Pulse`
- 9-card spread: `Convergence of the Star`
- Output modes: `Concise`, `Layered`, `Scholarly`
- Interpretation bias modes: `Auto`, `PreferCore`, `PreferShadow`, `StrictAuto`
- Pin Draw confirmation flow for replacing an existing spread
- Deck Browser with search, filters, random card selection, and copy helpers
- Reading History with reopen, copy, and export actions
- Copy, compact summary copy, and text export for readings

## Requirements

- Dalamud plugin environment
- Requires Dalamud API level `14`
- The repository is pinned to `.NET SDK 10.0.103` via [`global.json`](./global.json)
- The project currently targets `Dalamud.NET.Sdk/14.0.1`

## Install

### Manual install

Build the plugin in `Release` and load the output through your normal Dalamud dev-plugin workflow. The packaged plugin output is produced from [EorzeanMegaArcana.csproj](./src/EorzeanMegaArcana/EorzeanMegaArcana.csproj).

### Build from source

1. Clone the repository.
2. Open `SwamiSophie.sln` in Visual Studio or VS Code.
3. Build the solution:

```powershell
dotnet build SwamiSophie.sln -c Release -clp:ErrorsOnly
```

4. Use the generated plugin output with your local Dalamud development setup.

## Usage

Commands:

- `/swami`
- `/ema`

The main window contains the reading controls, spread selection, output mode selection, interpretation bias selection, draw actions, copy/export actions, and Pin Draw toggle.

Additional UI:

- `Open Deck Browser` opens the searchable deck browser
- `Open History` opens the in-memory reading history window

## Documentation

- [Usage Guide](./docs/USAGE.md)
- [Interpretation Guide](./docs/INTERPRETATION.md)

## Data-Driven Design

The reading system is driven by JSON data under [`/data`](./data):

- doctrine and spread rules in `/data/rules`
- card definitions in `/data/strata`

The plugin code loads and interprets this data at runtime rather than hardcoding the deck contents.

## Disclaimer

This plugin is provided for lore and reading purposes only. It is not affiliated with Square Enix, Final Fantasy XIV, or goatcorp. Use of Dalamud and third-party plugins carries risk; use at your own risk and follow the rules, policies, and expectations relevant to your environment.
