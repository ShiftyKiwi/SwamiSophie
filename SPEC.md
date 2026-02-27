# Eorzean Mega Arcana (Dalamud Plugin) - SPEC

## Goal
Build a Dalamud plugin that performs serious tarot-style readings using the "Eorzean Mega Arcana" 200-card system. The plugin must:
- Load deck + rules from JSON files in `/data`.
- Support the canonical spreads:
  - 3-card: "Aether Pulse"
  - 9-card: "Convergence of the Star"
- Produce interpretations in three output modes:
  - Concise (default)
  - Layered
  - Scholarly
- Follow the deck's doctrinal rules:
  - Hierarchy of influence (Calamity > Crystal > Divine > Primal > Shard > Persona > Court > Element)
  - No reversals; shadow meaning is contextual
  - Astral/Umbral era state computed from polarity weights
  - Scale computed as Minor/Major/Era based on rules in doctrine.json

## Non-Goals
- No gameplay automation, no interaction with combat/rotation.
- No network calls required to function.
- No reliance on web services.
- No need for card artwork in v1 (text-only tiles are fine).

## Tech Requirements
- Language: C#
- Framework: Dalamud + ImGui
- Data format: JSON (System.Text.Json)
- Plugin should be fully functional offline.

## Repository Layout (expected)
- `/data/rules/doctrine.json`
- `/data/rules/spreads.json`
- `/data/rules/output_modes.json`
- `/data/strata/*.json` (8 strata + 6 element files)
- `/src/EorzeanMegaArcana/` contains plugin code.

## Data Model Requirements

### Card model
Fields (all required unless noted):
- string `Id`
- string `Name`
- string `Stratum`
- string? `Element`
- string? `Rank`
- string `Polarity`
- int `PolarityWeight`
- string `Core`
- string `Shadow`
- string `AstralNote`
- string `UmbralNote`
- string[] `Flags`

### SpreadDefinition model
- string `Id`
- string `Name`
- int `CardCount`
- string `Layout`
- int `AxisIndex`
- Position[] `Positions`

Position:
- int `Index` (1-based)
- string `Name`
- string `Description`

### Doctrine model
- string[] `AuthorityOrder`
- Scale rules:
  - Definitions in doctrine.json as:
    - `scaleRules.minor.conditions`
    - `scaleRules.major.conditionsAny`
    - `scaleRules.era.conditionsAny`
- Polarity:
  - `astralThreshold`, `umbralThreshold`
  - `divineBalanceCardIds`
  - `stabilizeByOneTierIfBalancePresent`

## Core Services

### DataLoader
Responsibilities:
- Load all JSON files under `/data` at plugin start (or first UI open).
- Validate required fields; show errors in UI if loading fails.
- Produce:
  - `IReadOnlyList<Card>` AllCards
  - `IReadOnlyDictionary<string, SpreadDefinition>` SpreadsById
  - Doctrine instance
  - Output modes list

### DeckService
Responsibilities:
- Provide queries:
  - GetAllCards()
  - GetByStratum(stratum)
  - GetByElement(element)
  - GetById(id)
- Provide counts for diagnostics.

### DrawService
Responsibilities:
- Draw N cards from deck:
  - Seeded RNG optional (int?)
  - No duplicates by default
  - Allow duplicates if `AllowRepeats = true`
- Provide a `DrawResult` mapping spread positions to cards.

### InterpretationEngine
Input:
- SpreadDefinition
- DrawResult (position -> Card)
- Doctrine

Output:
- ReadingResult

ReadingResult must include:
- Header:
  - Scale: Minor/Major/Era
  - EraState: Astral/Umbral/Transitional
  - DominantElement: nullable string
  - Escalation: bool + list of reasons
  - Moderation: bool + list of reasons
- DrawnCards: list of (position, card)
- Narrative: string (filled by formatter)
- Breakdown: optional structured per-layer summaries
- Diagnostics: polarity sum, counts by stratum, counts by element

Engine Logic Requirements:
1) Determine Scale:
   - Evaluate doctrine scale rules with AxisIndex support:
     - `axisIsStratum`
     - `axisNotStratum`
   - If multiple scale conditions match, choose the highest (Era > Major > Minor).
2) Compute Polarity Sum:
   - Sum all `PolarityWeight`
   - EraState:
     - >= astralThreshold => Astral
     - <= umbralThreshold => Umbral
     - else => Transitional
   - If `divineBalanceCardIds` present and stabilizeByOneTierIfBalancePresent:
     - Pull eraState one step toward Transitional (Astral->Transitional, Umbral->Transitional).
3) Dominant Element:
   - Count element occurrences among Element + Court cards (ignore null element)
   - If tie or no majority, set null
4) Escalation:
   - True if:
     - count of Primal >= 2
     - any Calamity present
     - or special IDs (if later added)
   - Add reasons.
5) Moderation:
   - True if:
     - count of Divine >= 2 OR divine balance present OR specific moderation flags present
   - Add reasons.

Note: v1 does not need adjacency rules. Keep it simple and deterministic.

## Formatting / Output Modes

### Output Modes
- Concise:
  - Show header + 1 narrative block (3-6 short paragraphs max)
  - No per-tier breakdown
  - No raw counts displayed
- Layered:
  - Show header + sections:
    - Cosmic Authority (Calamity/Crystal/Divine)
    - Amplification & Distortion (Primal/Shard)
    - Personal Lens (Persona)
    - Manifestation (Courts/Elemental)
    - Recommendation (Action vs Reflection based on EraState)
- Scholarly:
  - Show everything in Layered, plus:
    - Explicit authority order used
    - Scale determination conditions that matched
    - Polarity sum and thresholds
    - Counts by stratum and element

### Formatter Requirements
Implement 3 formatters:
- `ConciseFormatter`
- `LayeredFormatter`
- `ScholarlyFormatter`

All formatters must:
- Never claim certainty ("will happen"); use "suggests/indicates/points toward"
- Avoid melodrama
- Preserve serious tone

Narrative generation approach (v1):
- Build a structured intermediate summary from:
  - highest authority cards present (Calamity, then Crystal, then Divine)
  - axis card emphasis
  - Persona lens if present
  - dominant element influence
  - eraState recommendation
- Use card fields:
  - Core / Shadow / AstralNote / UmbralNote
- Shadow selection:
  - If EraState is Umbral and card polarity is Astral => prefer Shadow
  - If EraState is Astral and card polarity is Umbral => prefer Shadow
  - Else prefer Core
  - If any Calamity present => increase chance of Shadow for non-Divine strata

## UI Requirements (ImGui)

### Main Window
Controls:
- Dropdown: Spread (Aether Pulse / Convergence of the Star)
- Text input: Question (optional)
- Dropdown: Output Mode (Concise/Layered/Scholarly)
- Checkbox: Allow Repeats (default false)
- Seed controls:
  - Checkbox: Use Seed
  - Int input: Seed value
- Button: Draw
- Button: Redraw (new seed if not using seed)
- Button: Reinterpret (same draw, different mode)

Display:
- Header panel: Scale, EraState, Dominant Element, Escalation, Moderation
- Spread display:
  - For 3-card: row of 3 tiles labeled by position name
  - For 9-card: 3x3 grid labeled by position name
  - Each tile shows: Stratum icon (text), Name, Element/Rank if applicable
- Output panel:
  - Formatted narrative text
  - Copy button
  - Export button (save to file in plugin config folder)

### Optional Windows (nice-to-have, but implementable in v1)
- Deck Browser:
  - Filter by Stratum, Element
  - Search by name
  - View card details (core/shadow/notes)
- Settings:
  - Default spread
  - Default output mode
  - Default allow repeats
  - Default use seed behavior

## Persistence
- Save settings to Dalamud plugin config.
- Save last reading state in memory (not required to persist across reload).
- Export reading to a timestamped `.txt` file if user clicks Export.

Export format:
- Question
- Date/time
- Spread
- Header
- List of positions + card names
- Output text
- If scholarly: include diagnostics.

## Testing (Engine)
Add a small test project or internal test harness.
At minimum validate:
- Scale determination when axis is Calamity => Era
- EraState calculation from polarity sum thresholds
- Dominant element logic returns null on ties
- Formatters do not use prohibited deterministic language ("will happen")

## Definition of Done (v1)
- Plugin loads all JSON and renders UI.
- User can draw 3 or 9 cards.
- Engine produces ReadingResult with header fields correct.
- Output displays in all three modes.
- Copy and Export work.
- No crashes on missing/invalid JSON; errors visible in UI.
