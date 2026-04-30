# 5e Character Wizard

A data-driven, client-only Blazor WebAssembly wizard for building D&D 5e characters using SRD mechanics only. Published as a static site on GitHub Pages.

> See [requirements.md](./requirements.md) for the full specification.

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

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Build Instructions

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Check code formatting (must pass in CI)
dotnet format --verify-no-changes
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

The [deploy.yml](.github/workflows/deploy.yml) workflow does this automatically on every push to `main`.

## Data & Schemas

- JSON schemas live in `/schemas/` and define the structure for character exports and canonical game data.
- SRD seed data lives in `/data/` (mechanical fields only; no PHB flavor text).
- CI validates all `/data` files against their corresponding schemas.

## License

Code is MIT licensed — see [LICENSE](./LICENSE).  
SRD content in `/data` is used under the D&D 5e SRD Creative Commons Attribution 4.0 International license.

