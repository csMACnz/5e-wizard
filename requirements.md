# requirements.md — 5e-wizard (csMACnz/5e-wizard)

Version: 0.1
Date: 2026-04-30
Status: Draft

Purpose
- Provide a data-driven, client-only Blazor WebAssembly (dotnet 10) wizard to build D&D 5e characters using SRD mechanics only.
- Ensure only valid characters (per SRD mechanics and PHB-equivalent rules present in SRD) can be produced — deterministic validation at each step and a final full validation pass.
- Allow customization of display names and flavor descriptions without changing mechanical effects.
- Publish as a static site on GitHub Pages under csMACnz/5e-wizard subpath; CI validates and publishes automatically.

Scope
- Rules source: 5e SRD (no PHB verbatim copyrighted text).
- Content: all mechanics available via SRD — races, subraces, classes, subclasses, backgrounds, feats (SRD-feasible), spells, equipment, multiclassing, ASIs, point-buy/standard array/rolling.
- UI: Wizard to create characters up to level 20, with multiclass support.
- Hosting: static GitHub Pages (repo subpath). No server-side components except CI.

Legal / Licensing Note
- Only SRD mechanics will be included in seeded data. Do not include PHB flavor or verbatim text.
- Code will be MIT (or other chosen OSS license) but SRD content licensing must be respected (SRD is open; confirm exact SRD license at integration time).
- Contributors must not add copyrighted PHB text into /data; CI will include a check for disallowed text patterns (optional).

User personas
- Player: guided creation of a legal SRD character.
- DM: validate uploaded/imported characters for rule compliance.
- Contributor: update/extend canonical SRD data via PRs.
- Developer: maintain validator and static site CI/CD.

High-level goals / success criteria
- Users can complete wizard and obtain a validated SRD-compliant character JSON and printable sheet.
- The validator prevents invalid builds (e.g., illegal multiclass entry, incorrect point-buy costs, too many skill proficiencies, improper spell selections).
- Repository contains schema definitions and seeded SRD data to start.
- CI runs build, unit tests, data-schema validation, and deploys successful builds to GitHub Pages.

Functional Requirements (FR)
FR1 — Wizard flows
- FR1.0: Home / Landing Page.
  - FR1.0.1: The landing page shall provide a "Start New Character" button that navigates to the first step of the guided wizard flow.
  - FR1.0.2: The landing page shall provide a "Roll Random Character" button that generates a fully-random SRD-legal character — selecting a random race (and subrace where applicable), class, background, ability scores via 4d6-drop-lowest, starting equipment, and spells where applicable — and navigates the user directly to the Review step (Step 8) of the wizard.
- FR1.1: Start: meta (name, player, campaign), choose ability generation method (Standard Array, Point Buy [PHB rules], Roll with configurable reroll policy).
  - FR1.1.1: The character name and campaign name fields shall have randomizer dice buttons to generate a suggested value. The player name field is a plain text input and does not have a randomizer button.
- FR1.2: Ability score assignment and modifiers (apply racial bonuses after assignment; allow custom name/flavor but preserve mechanic IDs).
  - FR1.2.1: Rolled method — include a "Roll All" button to roll 4d6-drop-lowest for all abilities, and per-ability "Reroll" buttons to regenerate individual scores.
  - FR1.2.2: Standard Array method — include a "Random Assign" button to randomly shuffle the standard array values across the six abilities.
- FR1.3: Race/Subrace selection and mechanical feature application.
  - FR1.3.1: Add a "Random Race" dice button adjacent to the Race dropdown. Clicking it picks a uniformly random race from the available list and updates the selection.
  - FR1.3.2: When a race with subrace options is selected (either manually or via the random button), add a "Random Subrace" dice button adjacent to the Subrace dropdown. Clicking it picks a uniformly random subrace from those available for the current race.
- FR1.4: Class selection and subclass picker; allow multiclass entries (class + level), enforce total level ≤ 20.
  - FR1.4.1: Subclass picker is enabled/visible when the character's class level reaches the class's required subclass level; hidden or disabled otherwise.
  - FR1.4.2: Add a "Random Class" dice button adjacent to each Class dropdown (primary and each multiclass entry). Clicking it picks a uniformly random class from the available list and updates only that entry; the current level and subclass selection are reset.
  - FR1.4.3: When a subclass picker is visible and enabled (i.e., the character's class level has reached the required subclass level), add a "Random Subclass" dice button adjacent to the subclass dropdown. Clicking it picks a uniformly random subclass from those offered by the current class.
- FR1.5: Background selection; grant proficiencies and feature references.
- FR1.6: Skill and proficiency assignment: enforce class/background/tool restrictions and counts.
  - FR1.6.1: Add a "Random Skills" dice button at the top of the class skill proficiency selection section. Clicking it randomly selects exactly the required number of skills (as defined by the class's `skillChoices.count`) from the allowed options, skipping any skills already granted by the background. Any previously checked class skill choices are replaced.
- FR1.7: Equipment: starting choices guided by standard PHB-ruleset choices from the character's class.
  - FR1.7.1: A "Strict starting equipment" toggle (default: on) limits visible/selectable items to only the standard choices for the character's class.
  - FR1.7.2: When strict mode is enabled, any equipment selections outside the allowed list are automatically deselected and validation errors are shown.
  - FR1.7.3: The toggle can be turned off by the user to allow free selection of any valid item; in non-strict mode a warning is displayed.
- FR1.8: Spells: enforce known/prepared rules per class and spell slot table.
  - FR1.8.1: For each spellcasting class card, add a "Random Cantrips" dice button in the cantrips section (visible only when `maxCantrips > 0`). Clicking it randomly selects exactly `maxCantrips` cantrips from the available cantrip list for that class, replacing any current cantrip selections for that class.
  - FR1.8.2: For each spellcasting class card, add a "Random Spells" dice button in the known/leveled spells section. For known-spell casters (non-prepare), it randomly selects exactly `maxKnown` spells from the full leveled spell list for that class. For prepare casters, it randomly selects a reasonable default count (equal to the spellcasting modifier + class level, capped to the total available spells) and marks them as prepared. Any previous leveled spell selections for that class are replaced.
- FR1.9: Level progression: show features unlocked per class level, including ASIs and feature choices.
  - FR1.9.1: A dedicated "Level Features" wizard step (Step 5) shall be inserted between the Class step and the Background step.
  - FR1.9.2: The Level Features step shall display a read-only racial traits panel at the top, listing all trait IDs from the selected race's `traitIds` (and the selected subrace's `traitIds` if applicable), with display names resolved via `feats.json`.
  - FR1.9.3: For each class entry, the Level Features step shall display a read-only "Automatic Features" panel listing all class features granted from level 1 up to and including the character's current class level, excluding the `feat:asi` sentinel. Each feature shall show its display name (resolved from `feats.json`) and level number.
  - FR1.9.4: For each class level that contains the `feat:asi` sentinel (at or below the character's current class level), an interactive "ASI choice" block shall be rendered with the header "Level N — Ability Score Improvement or Feat" and three mutually-exclusive radio options:
    1. `+2 to one ability` — exposes a single ability dropdown for the +2 target.
    2. `+1 / +1 to two abilities` — exposes two ability dropdowns for the two +1 targets.
    3. `Take a feat` — exposes a select populated with all feats where `type = "general"`.
  - FR1.9.5: ASI/feat choices are bound to in-memory shadow state (`_allAsiChoicesByKey`, keyed as `"classId|classLevel"`). This dictionary retains every choice made in the current browser session regardless of whether the class level has subsequently been lowered, so that choices rehydrate automatically if the level is raised again.
  - FR1.9.6: Step validation shall issue a non-blocking warning (`WARN_ASI_INCOMPLETE`) for each `feat:asi` opportunity that has no choice saved. These warnings shall not prevent the user from advancing.
  - FR1.9.7: `Character.Features` shall be populated during `CommitAllToCharacter()` in the following order: race trait IDs (`SourceId = raceId`), subrace trait IDs (`SourceId = subraceId`), background feature (`SourceId = backgroundId`), class features per level excluding `feat:asi` (`SourceId = classId`), and general feats taken via ASI choices (`SourceId = classId`).
  - FR1.9.8: For exportable ASI choices where an ability bump (not a feat) was selected, the corresponding `Character.AbilityScores.<ability>.OtherBonus` value shall be incremented (+2 for a single-ability choice, +1/+1 for a split choice). These bonuses are accumulated only from exportable choices (level-eligible) and recalculated on every `CommitAllToCharacter()` call.
  - FR1.9.9: Exportable ASI choices are defined as entries in `_allAsiChoicesByKey` where the corresponding class entry still exists and `classEntry.Level >= choice.ClassLevel`. Non-exportable choices (level lowered since choice was made) are excluded from session saves, JSON exports, and FC5e XML exports, while remaining in memory.
  - FR1.9.10: The Level Features step shall include a "Hit Points" panel for assigning hit points for each class level, appearing after the ASI/feat choices section.
  - FR1.9.11: For level 1, the HP die-roll value shall be fixed at the class's maximum hit die value (equal to the class's `hitDie`, e.g. 10 for a Fighter with d10) and displayed as a read-only entry. No user input is presented for level 1.
  - FR1.9.12: For each level above 1, the user shall be presented with two mutually-exclusive options for that level's HP die-roll value:
    1. "Average" — automatically uses the fixed average value for the class's hit die: `floor(hitDie / 2) + 1` (e.g. 5 for d8, 6 for d10, 7 for d12). This is the default selection.
    2. "Manual" — exposes a numeric input field pre-filled with the average, and a "Roll" button that uses a client-side PRNG to simulate the class's hit die and overwrites the input with the result. The entered value must be within the valid range [1, hitDie] and is validated immediately on change.
  - FR1.9.13: The "Roll" button for manual HP entry uses a client-side PRNG seeded at click time (consistent with all other randomisation buttons per the general randomisation constraints). After rolling, the same step-level validation that runs on manual change is triggered immediately.
  - FR1.9.14: For multiclass characters, each level's HP entry is associated with the class that owns that level and uses that class's `hitDie` for the average calculation, roll range, and validation. The HP panel presents levels in ascending order across all class entries.
- FR1.10: Review & final validation with explicit errors/warnings.

General constraints applying to all randomisation buttons (FR1.1.1, FR1.2.1, FR1.2.2, FR1.3.1, FR1.3.2, FR1.4.2, FR1.4.3, FR1.6.1, FR1.8.1, FR1.8.2, FR1.9.13):
- All random selections use a client-side PRNG seeded at click time (no server call needed; consistent with NFR1 client-only operation).
- After any random selection, the same step-level validation that runs on manual change must be triggered immediately.
- Randomisation buttons use the Casino dice icon and secondary colour styling to match FR1.1.1's established dice-button pattern.
- Randomisation buttons are disabled when no valid options exist (e.g., "Random Subrace" is disabled if no race is selected, "Random Subclass" is disabled if no class or subclass-level not reached).

FR2 — Validation
- FR2.1: Step-level validation: run relevant validators for immediate feedback.
  - FR2.1.1 (BUG-FIX): Step validation errors shall be visible on screen after any field change/edit within a step, not only on "Next" click. This applies to all input field wizard steps.
- FR2.2: Full validation engine producing structured report: errors (fatal) and warnings.
- FR2.3: Provide machine-readable error codes and human-friendly messages (e.g., ERR_MULTICLASS_PREREQ — "Strength 13 is required to multiclass into Barbarian"). Level-feature validation codes:
  - `WARN_ASI_INCOMPLETE` — an ASI/feat opportunity at a given class level has no choice saved (non-blocking warning).
  - `ERR_ASI_INVALID_FEAT` — a general feat was chosen for an ASI slot but the feat ID is unknown or its `type` is not `"general"`.
  - `ERR_HP_ROLL_OUT_OF_RANGE` — a manually-entered hit-point die-roll value for a given level is outside the valid range [1, hitDie] for that level's class (e.g. a value of 0 or 11 for a d10 class).
- FR2.4: CI validates canonical /data JSON files against JSON schemas and custom rules.

FR3 — Data & Customization
- FR3.1: All canonical mechanics are stored in versioned JSON files under /data (races.json, classes.json, backgrounds.json, spells.json, equipment.json, feats.json).
- FR3.2: Each data object separates mechanic fields (ids, numeric bonuses, rules references) from display fields (displayName, description).
- FR3.3: Contributors change flavor by editing displayName/description only for UI flair without changing mechanical fields.
- FR3.4: Seed repository with SRD canonical mechanics (mechanics & minimal flavor).
- FR3.5: Every entry in `feats.json` shall carry a `"type"` field with one of four values: `"class"` (auto-granted class feature), `"background"` (auto-granted background feature), `"general"` (player-selectable feat; valid as an ASI alternative), or `"asi"` (the `feat:asi` sentinel entry only). The `feat.schema.json` schema shall make `"type"` a required enum field.
- FR3.6: Data integrity rules (enforced by tests):
  - Every feat ID listed in `ClassDefinition.featuresByLevel` shall resolve to a known entry in `feats.json`.
  - Every `BackgroundDefinition.featureId` (where non-empty) shall resolve to a feat in `feats.json` with `type = "background"`.
  - Every feat with `type = "class"` shall appear in at least one class's `featuresByLevel`.
  - Every feat with `type = "general"` shall not appear in any class's `featuresByLevel` or as a background `featureId`.
- FR3.7: `IDataService` shall expose `GetFeatsAsync()` returning `IReadOnlyList<FeatDefinition>`, loading from `data/feats.json` with single-load caching.

FR4 — Export / Import / Share
- FR4.1: Export validated character JSON (schema-backed). The downloaded filename must include the character name and total level (e.g. `thorin-level5.json`). The exported `hitPoints` object shall contain only `maximum` (the calculated total max HP); `current` and `temporary` fields are not tracked by this application and shall not be present in the export.
- FR4.2: Import to resume or validate existing characters.
- FR4.3: Printable view (HTML print stylesheet and optional PDF generation client-side).
  - FR4.3.1: The printable character sheet shall include a "Features & Traits" section that lists all `Character.Features` entries with display names resolved from `feats.json` (falling back to the raw feature ID if not found). The source of each feature (race, subrace, background, class) shall be displayed alongside the feature name.
- FR4.4: Optional URL state encoding for shareable links (client-side: base64 or compressed state in URL query).
- FR4.5: On the Review step of the wizard, provide an "Export for FightClub 5e" button that generates and downloads a `.xml` file in the FightClub 5e character XML format. The export is offered regardless of validation state (warnings/errors are shown but do not block download). Only a best-effort mapping of available SRD data is produced; fields without a mapping (e.g. alignment, personality traits) are emitted as empty or default values. The downloaded filename must include the character name and total level (e.g. `thorin-level5.xml`). Since current HP is not tracked, the `<hpCurrent>` element shall be emitted with the same value as `<hpMax>` as a neutral default.
  - FR4.5.1: When emitting `<feat>` elements from `Character.Features`, the exporter shall resolve the `FeatureId` to its `DisplayName` via `feats.json` for the `<name>` child element. The `SourceId` (or source description) shall be placed in the `<text>` child.

FR5 — Hosting & CI/CD
- FR5.1: Deploy static site to GitHub Pages (repo subpath /5e-wizard).
- FR5.2: GitHub Actions workflows:
  - CI: build, tests, data-schema validation on PRs and pushes.
  - CD: publish to GitHub Pages on merges to main.
- FR5.3: Build should set <base href> to "/5e-wizard/" automatically at build time to support pages subpath.

FR6 — UX and Accessibility
- FR6.1: Responsive design, keyboard-navigable with ARIA support.
- FR6.2: Clear step progress and validation messages.
- FR6.3: Theme: D&D-appropriate look (parchment/dark-red/gold), cross-platform friendly.
- FR6.4: Internationalization-ready (strings in resource files).

FR7 — Session Management and Local Storage
- FR7.1: Session identifier per character creation.
  - FR7.1.1: Each new character creation session shall be assigned a unique session identifier (UUID v4) at the moment the wizard is started.
  - FR7.1.2: The active session identifier shall be reflected in the browser URL as a query parameter (`?session=<id>`), allowing the URL to be bookmarked or shared to resume that session later.
  - FR7.1.3: Navigating to `/wizard?session=<id>` shall load the saved session from local storage and restore the character state and active step exactly as it was last saved.
  - FR7.1.4: If a `session` query parameter is present but no matching session exists in local storage, the wizard shall start a fresh character creation session under that session ID (graceful fallback).
- FR7.2: Automatic persistence to browser local storage.
  - FR7.2.1: The wizard shall automatically save the current character state and active step to browser local storage whenever the user advances, goes back, or commits changes in any step.
  - FR7.2.2: Each session shall be stored as a JSON object under the key `5ew_session_<sessionId>` in localStorage, containing a `schemaVersion` integer, the session ID, character name (for display in the list), creation timestamp, last-modified timestamp, active step, and the full `Character` object.
  - FR7.2.3: A session index (an ordered list of session IDs) shall be maintained in localStorage under the key `5ew_sessions` to allow enumeration without scanning all localStorage keys.
  - FR7.2.4: Local storage operations shall be non-blocking and shall not affect wizard step performance (NFR2 still applies).
  - FR7.2.5: The `schemaVersion` field shall start at `1` and shall be incremented whenever the persisted session format changes in a backward-incompatible way. When loading a session, the application shall reject (return null / skip) any session whose `schemaVersion` does not match the current supported version, ensuring stale or future-format data is never silently mis-read. Current supported version: `2` (bumped from `1` when `Character.AsiChoices` was added).
- FR7.3: Character session list screen.
  - FR7.3.1: A dedicated "My Characters" page shall be accessible at `/characters`, linked from the landing page.
  - FR7.3.2: The page shall list all character sessions stored in local storage, displaying: character name (or "Unnamed Character" if blank), creation date, last-modified date, and current wizard step reached.
  - FR7.3.3: Each session entry shall provide a "Resume" button/link that navigates to `/wizard?session=<id>` to continue that session.
  - FR7.3.4: Each session entry shall provide a "Delete" button to remove the session from local storage, with a confirmation prompt before deletion.
  - FR7.3.5: The list shall be ordered by last-modified date, most recent first.
  - FR7.3.6: When no sessions exist, the page shall display a friendly empty-state message with a "Start New Character" button.
- FR7.4: Landing page integration.
  - FR7.4.1: The landing page shall display a "My Characters" button/link when one or more saved sessions exist in local storage, navigating to `/characters`.
  - FR7.4.2: The "Start New Character" button on the landing page shall always create a new session (a fresh UUID), ensuring an existing in-progress session is not overwritten.

Non-functional Requirements (NFR)
- NFR1: Client-only operation (works offline after initial page load).
- NFR2: Fast: typical step response < 300 ms on common desktops.
- NFR3: Secure: treat imported JSON as untrusted; validate before using any values.
- NFR4: Maintainable: data-driven architecture to allow data-only updates.
- NFR5: Testable: unit tests for validation engine with target >= 80% coverage.

Constraints & Assumptions
- Runtime: .NET 10, Blazor WebAssembly.
- Data editing via PRs — no admin UI.
- SRD is used for mechanics; no PHB text included.
- DNS / Pages config is pre-existing in account; site will be served at https://csMACnz.github.io/5e-wizard/ (or configured domain pointing to that).

Core data model (high-level)
- Character
  - id, name, playerName, campaign, totalLevel, levels: array of {classId, subclassId?, level}
  - abilityScores: {STR, DEX, CON, INT, WIS, CHA} (base, bonuses, final)
  - generationMethod: enum {standardArray, pointBuy, roll}
  - raceId, subraceId
  - backgroundId
  - proficiencies: weapons/armor/tools/languages
  - skills: {skillId: proficiencyLevel} (none/proficient/expert)
  - hitPoints: maximum hit points only; current hit points and temporary hit points are gameplay mechanics outside the scope of this application and are not tracked; stored as a list of per-level entries each recording the level, classId, method (average or manual), and the die-roll value chosen (excluding CON modifier); total max HP = sum of all per-level die-roll values + (CON modifier × total character level), with each level's contribution floored at a minimum of 1 after CON is applied.
  - features: list of resolved features (with source references and optional display overrides)
  - asiChoices: list of {classId, classLevel, mode ("plus2"|"split"|"feat"|null), featId?, abilityOne?, abilityTwo?} — exportable subset only (level-eligible choices at session save/export time)
  - spells: known/prepared/spellSlots per class feature
  - equipment: items w/ quantity
  - validationReport: list of {severity, code, message, detail, path}

- Canonical items (example class object)
  - id (mechanic id), mechanicalName, displayName, description (flavor), mechanics:
    - hitDie, proficienciesGranted, savingThrows, skillChoices, multiclassPrereqs, featuresByLevel (featureId references), spellcasting (castingType, slotTableRef, cantripsKnown, spellsKnownRules), subclassOptions.

Validation engine design
- Layers:
  - JSON Schema validation (structural): ensures files conform to expected schema shapes.
  - Rule validators (semantic): modular validator functions for:
    - ability generation (point-buy costs, caps)
    - race application & validations
    - class selection & multiclass prerequisites
    - proficiency & skill selection cardinality
    - spellcasting constraints (slots, known/prepared)
    - equipment starting-choice constraints
    - level features & ASI choices (`LevelFeatureValidator`)
  - Composition: step validators (contextual) and final full-run.
- Output:
  - Structured ValidationResult {isValid: bool, errors: [], warnings: []}
  - Error code patterns: ERR_*, WARN_*
- Testability:
  - Unit tests verifying each validator against positive and negative examples.

User flows & UI components
- Landing page (start wizard, roll random character, import)
- Wizard container with progress steps:
  1. Character meta & generation method
  2. Ability scores UI (Standard Array, Point Buy, Roll)
  3. Race/Subrace selector + apply features
  4. Class selection (including multiclass flow), subclass picker
  5. **Level Features** — racial traits, auto-granted class features (read-only), and interactive ASI/feat choices per qualifying level
  6. Background & proficiencies (combined with skill selection)
  7. Spells (if caster)
  8. Equipment starter package selection
  9. Review & final validation
- Character sheet viewer (print friendly)
- Components:
  - Reusable validators UI, selectable lists, pickers, live preview panel
- UI tech:
  - Recommended: MudBlazor (MIT) for cross-platform friendliness, themable, and rich components
  - Alternative: TailwindCSS + DaisyUI for lightweight utility-first approach
  - Theme: provide custom MudBlazor theme with parchment texture cues and D&D colors; keep high contrast for accessibility.

Tech stack & patterns
- .NET 10, Blazor WebAssembly (Client project)
- Shared library project for models and validation (CharacterWizard.Shared)
- Unit tests: xUnit (CharacterWizard.Tests)
- Data: JSON files in /data and JSON Schemas in /schemas
- Lint/format: dotnet format in CI
- CI/CD: GitHub Actions; use actions/upload-pages-artifact + actions/deploy-pages or peaceiris/actions-gh-pages

Repository layout (suggested)
- /.github/workflows
  - ci.yml
  - deploy.yml
- /data
  - races.json
  - classes.json
  - backgrounds.json
  - spells.json
  - equipment.json
  - feats.json
  - data-version.json
- /schemas
  - character.schema.json
  - class.schema.json
  - race.schema.json
  - spell.schema.json
- /src
  - /CharacterWizard.Client (Blazor WASM)
  - /CharacterWizard.Shared (models, validation engine)
  - /CharacterWizard.Tests (xUnit)
- /docs (design notes, rules interpretations)
- README.md
- requirements.md (this file)

Seed data
- Repository will be seeded with SRD mechanics content (mechanical fields + minimal display strings).
- Seed priorities:
  1. Ability generation rules (point-buy costs table, standard array)
  2. Race and subrace mechanics (SRD races and their ability bonuses and features)
  3. Classes and core class features & spellcasting tables
  4. Backgrounds and proficiencies
  5. Spells (SRD spells)
  6. Equipment and starting packages
- Seeds will avoid flavor text beyond minimal labels and will include source metadata and version tags.

CI/CD summary (high-level)
- CI (ci.yml)
  - Trigger: pull_request, push (non-deploy branches)
  - Steps:
    1. checkout
    2. setup-dotnet 10.x
    3. dotnet restore
    4. dotnet build -c Release
    5. dotnet test --no-build
    6. dotnet format check
    7. JSON Schema validation for /data files (run a provided validator console tool or dotnet test that validates files)
    8. Linting for data (disallowed phrases or PHB text patterns)
- Deploy (deploy.yml)
  - Trigger: push to main (or manual dispatch)
  - Steps:
    1. checkout
    2. setup-dotnet
    3. dotnet publish CharacterWizard.Client -c Release -o out
    4. ensure base href set to "/5e-wizard/" (build-time token replacement in index.html or using <base href="." /> + use relative paths)
    5. create pages artifact and deploy using actions/deploy-pages (preferred) or peaceiris/actions-gh-pages
  - Note: Use GITHUB_TOKEN; ensure Pages deployment permissions are set in repo settings (or use personal access token for additional privileges if needed).

Testing & QA
- Unit tests for:
  - All validator functions (point-buy, multiclass prereqs, spell rules)
  - JSON schema conformance tests against seeded /data files
  - Example character golden files (valid and invalid cases)
- Integration tests:
  - bUnit for Blazor components and key wizard flows
  - Optional Playwright for end-to-end flows in CI (if you want browser tests)
- Manual QA:
  - Create representative characters: single-class, multiclass, caster, non-caster, feats vs ASI, variant human.

Acceptance criteria (examples)
- AC1: Creating a Level 1 Fighter with standard array and a Human (SRD) race completes with zero errors and exports a valid character.json conforming to character.schema.json.
- AC2: Attempting to multiclass into Paladin without STR 13 (or required prereq) produces ERR_MULTICLASS_PREREQ and prevents final validation pass.
- AC3: Point-buy cost calculator prevents exceeding allowed 27-point budget per SRD and flags errors for illegal increases beyond 20 cap.
- AC4: Imported character JSON is validated and rejected if schema or rule checks fail.
- AC5: CI fails PR checks if /data files do not validate against schemas.

Roadmap & milestones (suggested)
- Sprint 0 (planning & legal): finalize SRD set and confirm licensing and scope (1-2 days).
- Sprint 1 (scaffold): repo + Blazor scaffold + shared models + JSON schema + seed minimal data (3-5 days).
- Sprint 2 (validation core): implement ability, race, class validators + unit tests (1-2 weeks).
- Sprint 3 (wizard UI): implement stepper components, ability UI, race/class pickers (1-2 weeks).
- Sprint 4 (spells & equipment): implement spell rules, equipment, and printable sheet (1-2 weeks).
- Sprint 5 (polish & deploy): theme, accessibility, tests, CI/CD polish and deploy (1 week).

Deliverables to produce next (I can generate any of these)
- repository README.md (initial)
- fully-detailed requirements.md (this file) — done
- JSON Schemas for Character and canonical data
- Seed SRD JSON files (starting set)
- Blazor solution scaffold (Client + Shared + Tests)
- Validation engine skeleton
- GitHub Actions workflows (ci.yml and deploy.yml)
- Theme and UI guidelines (MudBlazor theme)
