# 5e Character Wizard

[![CI](https://github.com/csMACnz/5e-wizard/actions/workflows/ci.yml/badge.svg)](https://github.com/csMACnz/5e-wizard/actions/workflows/ci.yml)
[![Deploy](https://github.com/csMACnz/5e-wizard/actions/workflows/deploy.yml/badge.svg)](https://github.com/csMACnz/5e-wizard/actions/workflows/deploy.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)
[![Live App](https://img.shields.io/badge/Live%20App-GitHub%20Pages-blue)](https://csmacnz.github.io/5e-wizard/)

A data-driven, client-only Blazor WebAssembly wizard for building D&D 5e characters using SRD mechanics only. Published as a static site on GitHub Pages.

> See [requirements.md](./requirements.md) for the full specification.

## Features

- 🎲 **8-step guided wizard** — Walk through character name/meta, ability scores, race, class, background, spells, equipment, and a full review with validation report
- ⚔️ **Three ability-score methods** — Standard Array, Point Buy, and Manual Roll, each with live validation
- 🧝 **Full SRD race/subrace support** — All SRD races with subrace selection (e.g. High Elf, Mountain Dwarf)
- 🛡️ **Multiclass support** — Add multiple class/level combinations with prerequisite checks
- 📜 **Background proficiencies** — Skill selections validated against class and background rules
- ✨ **Spell selection** — Per-class spell and cantrip selection for caster classes
- 🎒 **Equipment selection** — Starter package equipment picker
- ✅ **Full validation** — Final character review with detailed error and warning messages
- 💾 **JSON export** — Download your character as a standards-compliant JSON file
- 🖨️ **Print sheet** — Printer-friendly character sheet view
- ♿ **Accessible** — ARIA labels, skip-navigation, and keyboard-navigable throughout

## Repository Structure

```
/
├── .github/workflows/     # CI and deploy workflows
├── data/                  # SRD seed JSON data files
├── schemas/               # JSON Schemas for character and canonical data
├── src/
│   ├── CharacterWizard.Client/   # Blazor WASM project
│   ├── CharacterWizard.Shared/   # Models and validation engine
│   └── CharacterWizard.Tests/    # xUnit test project
├── LICENSE                # MIT
├── README.md
└── requirements.md        # Authoritative project spec
```

## Usage

1. **Visit the live app** at [https://csmacnz.github.io/5e-wizard/](https://csmacnz.github.io/5e-wizard/)
2. Click **Start New Character** on the home page
3. Step through the 8-step wizard:
   - **Step 1 — Meta**: Enter character name, player name, campaign, and choose an ability-score generation method
   - **Step 2 — Abilities**: Assign ability scores using Standard Array, Point Buy, or Manual Roll
   - **Step 3 — Race**: Choose race (and subrace where applicable)
   - **Step 4 — Class**: Select one or more classes and their levels
   - **Step 5 — Background**: Pick a background and assign skill proficiencies
   - **Step 6 — Spells**: Choose spells/cantrips for caster classes (skipped for non-casters)
   - **Step 7 — Equipment**: Select starting gear
   - **Step 8 — Review**: Full validation report; export as JSON or view the print sheet
4. Use the **Export JSON** button to save your character
5. Use the **Print Sheet** button to open a printer-friendly view

> All navigation is keyboard-accessible. Use **Tab** to move between controls and **Enter/Space** to activate buttons.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Build Instructions

```bash
# Restore dependencies
dotnet restore src/CharacterWizard.slnx

# Build solution
dotnet build src/CharacterWizard.slnx -c Release

# Run tests (includes schema and data validation)
dotnet test src/CharacterWizard.slnx -c Release

# Check code formatting (must pass in CI)
dotnet format src/CharacterWizard.slnx --verify-no-changes
```

## Publishing / Deployment

```bash
# Publish Blazor WASM client for GitHub Pages
dotnet publish src/CharacterWizard.Client/CharacterWizard.Client.csproj \
  -c Release \
  -o out \
  -p:GHPagesBaseHref=/5e-wizard/
```

The `GHPagesBaseHref` property replaces the `<base href>` placeholder in `wwwroot/index.html` at publish time so the app routes correctly under the `/5e-wizard/` subpath on GitHub Pages.

The [deploy.yml](.github/workflows/deploy.yml) workflow runs automatically on every push to `main`, injecting the current commit SHA into `wwwroot/build-info.json` so the deployed app can display the exact build version.

## Data & Schemas

- JSON schemas live in `/schemas/` and define the structure for character exports and canonical game data.
- SRD seed data lives in `/data/` (mechanical fields only; no PHB flavor text).
- CI validates all `/data` files against their corresponding schemas.

## License

Code is MIT licensed — see [LICENSE](./LICENSE).  
SRD content in `/data` is used under the D&D 5e SRD Creative Commons Attribution 4.0 International license.

