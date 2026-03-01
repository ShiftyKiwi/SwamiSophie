# Swami Sophie — Usage Guide

## Opening the Plugin

Use either:

- `/swami`
- `/ema`

This opens the main reading window.

## Quick Start

If you want a stable first-use setup, start here:

- Spread: `Aether Pulse`
- Output Mode: `Concise`
- Interpretation Bias: `Auto`
- Allow Repeats: `Off`
- Use Seed: `Off`
- Pin Draw: `Off`

Then click `Draw`.

## Core Workflow

### 1. Choose a Spread

Swami Sophie currently supports:

- `Aether Pulse` — 3-card spread.
  Best for focused questions, immediate situations, and short-form insight.
- `Convergence of the Star` — 9-card spread.
  Best for layered situations, ongoing conflicts, and long-term dynamics.

Use `Aether Pulse` when you want clarity quickly. Use `Convergence of the Star` when you want to understand how multiple pressures interact.

### 2. Enter a Question (Optional)

The question field is optional but recommended.

Good examples:

- `What is shaping this conflict?`
- `What should I understand before I act?`
- `What is the true pressure beneath this situation?`

Avoid yes/no questions. This is a diagnostic system, not a fortune engine.

### 3. Choose Output Mode

Output Mode changes how much explanation you receive. It does not change the cards.

- `Concise`
  Core narrative plus position summaries.
  Best for quick clarity and everyday use.
- `Layered`
  Adds structured sections for Cosmic Authority, Amplification & Distortion, Personal Lens, Manifestation, and Recommendation.
  Best for reflection and understanding how layers interact.
- `Scholarly`
  Adds diagnostics such as polarity sum, scale determination logic, stratum counts, and element counts.
  Best for analytical users and understanding why the engine concluded what it did.

### 4. Interpretation Bias

Interpretation Bias changes emphasis only. It never changes the draw itself.

- `Auto`
  Lets the system choose between Core and Shadow emphasis contextually.
  Best for most users.
- `PreferCore`
  Leans toward the constructive or growth-oriented side of the cards.
  Best for coaching, self-reflection, and action planning.
- `PreferShadow`
  Leans toward tension, blind spots, distortion, or risk.
  Best for conflict analysis and stress-testing a situation.
- `StrictAuto`
  Uses Shadow only when the normal polarity mismatch rules call for it.
  Best for restrained, analytical reading.

### 5. Draw

Click `Draw` to generate a reading.

- If there is no current spread, the draw happens immediately.
- If `Pin Draw` is enabled and you already have a spread open, you will be asked whether to replace it.
- `Redraw` replaces the cards.
- `Reinterpret` keeps the same cards and reruns the text using the current output mode and question.

## Main Controls

### Allow Repeats

- `Off` (default)
  A card can appear only once in a spread.
  Best for a traditional tarot-like feel and cleaner layouts.
- `On`
  Cards may repeat within the same spread.
  Best when you want to allow heavy thematic reinforcement.

### Use Seed

This controls reproducibility.

- `Off` (default)
  Each draw is random.
- `On`
  The draw uses the integer in `Seed Value`.
  The same seed produces the same shuffle and the same draw.

Use a seed when you want to compare output modes, compare bias modes, or share a reproducible reading with someone else.

### Seed Value

This field is only used when `Use Seed` is enabled.

Enter any integer. That number determines the shuffle.

### Pin Draw

- `Off` (default)
  `Draw` and `Redraw` replace the current spread immediately.
- `On`
  `Draw` and `Redraw` ask for confirmation before replacing the current spread.

Use it when you do not want to overwrite a meaningful layout accidentally.

## Understanding the Header

Every reading begins with a header summary.

- `Scale`
  - `Minor` — personal or behavioral influence.
  - `Major` — structural or repeating influence.
  - `Era` — transformational or overriding force.
- `Era State`
  - `Astral` — outward movement or assertion.
  - `Umbral` — contraction or consolidation.
  - `Transitional` — instability or balancing pressure.
- `Dominant Element`
  - The most represented element across Element and Court cards.
  - Indicates how influence is likely to express itself.
- `Escalation`
  - Enabled when Primal or Calamity pressure is strong.
- `Moderation`
  - Enabled when Divine balancing influence stabilizes the field.

## Deck Browser

The Deck Browser is a study and reference tool.

It supports:

- search across `Name`, `Id`, `Core`, `Shadow`, `AstralNote`, and `UmbralNote`
- filtering by Stratum
- filtering by Element
- filtering by Flags (`Override`, `Amplify`, `Distort`, `Moderate`)
- random card selection from the visible result set
- detailed card inspection
- `Copy Card` and `Copy ID`

Use it when you want to learn the deck, inspect individual cards, or verify how specific cards are worded.

## History

The History window stores recent readings in a ring buffer.

You can:

- reopen a reading
- reinterpret it with a different output mode
- copy a prior reading
- export a prior reading

If history persistence is enabled in configuration, the lightweight history entries are reloaded on startup and the full narrative is recomputed when needed.

## Copy and Export Actions

- `Copy`
  Copies the full current reading as currently formatted.
- `Copy Summary`
  Copies a compact, deterministic summary intended for sharing or record-keeping.
- `Export`
  Saves the current reading as a timestamped text file in the plugin config directory.

## Recommended Setups

### Fast insight

- `Aether Pulse`
- `Concise`
- `Auto`
- `Allow Repeats` off
- `Use Seed` off

### Deep analysis

- `Convergence of the Star`
- `Layered` or `Scholarly`
- `StrictAuto`
- `Allow Repeats` off

### Reproducible comparison

- any spread
- `Use Seed` on
- fixed `Seed Value`
- compare output modes or interpretation bias without changing the cards

## FAQ

### What does Interpretation Bias actually change?

Only the emphasis of the text. It does not change the shuffle, draw order, scale, era state, or header calculations.

### Why did my reading text change when I clicked Reinterpret?

Because `Reinterpret` reruns the current draw through the selected output mode and current question. The cards stay the same; the presentation changes.

### Does Prefer Shadow mean the reading is worse?

No. It means the reading is framed more around tension, risk, distortion, or blind spots.

### When should I use a seed?

Use a seed when you want the same cards again on purpose, especially for testing, comparison, or discussion.
