# Data Files

This directory contains JSON data files used by the 5e Wizard character builder.

## names.json

Fantasy-flavoured name dataset used for random name generation in the character wizard.

### Shape

```json
{
  "full":    [ "Full Name 1", "Full Name 2", ... ],
  "given":   [ "Given1", "Given2", ... ],
  "surname": [ "Surname1", "Surname2", ... ]
}
```

- `full` — 500 pre-composed full names (given + surname pairs) ready for use as character names.
- `given` — 500 individual given/first names.
- `surname` — 500 individual surnames/family names.

### Source

Names were procedurally generated using fantasy naming conventions (original, not derived from any
copyrighted work). Released under the same MIT licence as the rest of this repository.

### How to update

1. Edit or replace the arrays in `data/names.json` directly, maintaining the same JSON shape.
2. Ensure each array contains at least one entry; the client falls back to a small built-in list
   if the file fails to load or any array is empty.
3. Run `dotnet test src/CharacterWizard.slnx` to confirm the deserialization tests still pass.
