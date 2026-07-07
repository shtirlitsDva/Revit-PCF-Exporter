# Handoff: Norsyn Commands — Revit dockable pane

## Overview
"Norsyn Commands" is a **dockable pane** for Autodesk Revit that replaces the ribbon as the home for ~40+ (growing to 80+) in-house commands. A dockable pane stays put regardless of which ribbon tab is active (unlike the ribbon, which auto-switches to *Modify* whenever an element is selected), can be docked/floated/resized like Revit's own *Properties* and *Project Browser*, and hosts arbitrary WPF content.

Two surfaces:
1. **Main palette** — lists every command; search + grouping + collapse so any command is findable and one-click reachable.
2. **Favorites mini-palette** — a small, low-chrome, draggable floating launcher showing only starred commands, meant to sit next to the cursor.

The final product is a **native WPF `UserControl`** hosted in a Revit dockable pane (`IDockablePaneProvider`). The bundled HTML is a **design reference only** — see below.

## About the Design Files
The file in this bundle (`Norsyn Commands.dc.html`) is a **design reference created in HTML** — a live, interactive prototype showing intended look and behavior. It is **not production code to copy directly**.

The target implementation is **WPF (XAML + C#)** hosted in a Revit dockable pane. Recreate the visual design and interactions in WPF using the app's established patterns: a shared theme with named brushes (`DynamicResource`), `UserControl`s, `ItemsControl`/`ListBox` with `DataTemplate`s, `CollectionViewSource` for grouping/filtering, and MVVM (`INotifyPropertyChanged` / `ICommand`). Do **not** ship HTML in the add-in.

> The HTML uses a small custom component runtime (`support.js`, `.dc.html`). Ignore that machinery — it is only the prototyping harness. What matters is the rendered UI, the measurements, and the behavior documented here. Open the file in a browser to interact with it.

## Fidelity
**High-fidelity (hifi).** Final colors, typography, spacing, density, and interactions are all intentional and specified below. Recreate the UI faithfully in WPF. The category icon colors in particular are a fixed spec (see Design Tokens).

## The design presents THREE directions (pick one to build)
All three are shown side-by-side on one canvas, tagged `1a`, `1b`, `1c`. They share the exact same data, theme, search, grouping, favorites, and mini-palette — they differ only in how the command list is laid out. **Choose one** for implementation (or a hybrid — see end).

- **1a — Dense list** *(recommended default)*: tight single-column rows (icon + label), collapsible category headers, Expand-all / Collapse-all. Closest to a native Revit / Visual Studio tool window. Manages volume by *collapsing*.
- **1b — Icon-forward grid**: commands as 2-column color tiles (big icon + wrapped label) under collapsible headers. More visual, fewer words, good in a narrow dock. Manages volume by *recognition + collapse*.
- **1c — Search-first command palette**: one large search field, results below, **nothing ever folds** (categories are quiet uppercase dividers), each row also shows its source add-in. Optimized for "type three letters, run." Manages volume by *filtering*.

The core difference between 1a and 1c: 1a collapses to manage 80+ commands; 1c never hides anything and relies on the search field. This resolves the stated tension of "don't hide things behind folds."

---

## Shared shell (all three directions)

### Dockable pane
- **Docked width in the mockup: 300px.** This is the common case — the layout must stay usable at ~260–360px and also when floated wider. Height fills the pane.
- Column layout, top to bottom: **Title bar → Toolbar → Scrolling command list**.
- Left border `1px solid #1E1E1E` where it meets the model view.

### Title bar (height 32px)
- Background `#333337`, bottom border `1px solid #26262A`, horizontal padding 10px.
- Left: text **"Norsyn Commands"**, 12px, weight 600, color `#E8E8E8`.
- Right (margin-left auto): button **"★ Favorites"** — background `#0E639C`, white text 11px, padding 3px 9px, radius 3px. Toggles the favorites mini-palette open/closed.

### Toolbar (below title bar, background `#2D2D30`, bottom border `#26262A`, padding 8px, vertical gap 7px)
1. **Search box**: row, background `#1E1E1E`, border `1px solid #4A4A4F`, radius 3px, padding 5px 8px. Leading 13px magnifier SVG (stroke `#8A8A8A`, stroke-width 2.4). Input: transparent, no border, text `#EAEAEA` 12px, placeholder "Search commands…". Filters live on **label AND tooltip** (case-insensitive substring).
2. **View segmented control**: 3 equal buttons in a track (background `#232326`, radius 4px, padding 2px, gap 3px). Buttons "Category" / "Source" / "A–Z". Each: 11px, radius 3px, padding 4px 0. **Active**: background `#0E639C`, white, weight 600. **Idle**: transparent, `#BDBDBD`, weight 500.
3. **Utility row** (font 10.5px, color `#8A8A8A`): left "`{n}` commands" (live count of visible commands). Right (margin-left auto): text buttons "Expand all" · "Collapse all", color `#7FB0D6` (link blue). *(1c omits this row and instead shows "`{n}` results · nothing folded".)*

### Command list (scrolling, background `#2D2D30`)
Grouped per the active view:
- **Category** (default): groups are the 10 categories, in the fixed order listed in Design Tokens. Header color = category color.
- **Source**: groups by originating add-in (`MEPUtils`, `NTR Exporter`, `PCF Exporter`), alphabetical. Header dot uses a neutral slate `#52667A`; each command's icon keeps its own category color.
- **A–Z**: single flat list, no headers, sorted by label.

**Group header** (1a / 1b, height ~28px, background `#313135`, hover `#3A3A3F`, bottom accents via border `#26262A`, padding 6px 9px, gap 7px, clickable to toggle collapse):
- Chevron (9px, `#9A9A9A`): `▾` expanded / `▸` collapsed.
- Category color dot: 8×8px, radius 2px, `background: <categoryColor>`.
- Label: 11px, weight 600, `#D8D8D8`, flex 1.
- Count: 10px, `#7A7A7A`.

**Collapse behavior**: category is collapsed when its key is in the collapsed set AND there is no active search. **Typing any query force-expands every group** (search overrides collapse). "Collapse all" sets all group keys collapsed; "Expand all" clears the set. In the mockup, 1a starts with Insulation / Pipe & Geometry / Instrumentation / Piping Systems expanded and the rest collapsed; 1b starts with Piping Systems / Parameters & Tagging / Rooms, Levels & Docs collapsed.

**Command row — 1a Dense list** (padding 4px 9px 4px 12px, gap 8px, hover background `#3A3A3F`):
- Icon 22px (see Icon spec).
- Label: 12px, `#E2E2E2`, single line, ellipsis on overflow.
- Star button (margin-left auto): `★` when favorited (color `#E2B93B` gold), `☆` when not (color `#6B6B70`); 13px; hover `filter: brightness(1.4)`. Toggles favorite.
- `title` attribute = tooltip (long description).

**Command tile — 1b Icon grid** (2-col grid, gap 6px, padding 8px; tile: column, center, gap 5px, padding 10px 6px 8px, background `#333338`, border `1px solid #3C3C42`, radius 5px, hover background `#3A3A41` / border `#4A4A52`):
- Icon 40px.
- Label: 10px, centered, `#D5D5D5`, up to 2 lines (fixed height ~25px, overflow hidden).
- Star: absolutely positioned top:3px right:4px, same gold/grey as above, 12px.

**Command row — 1c Command palette**:
- Search box is larger/emphasized: padding 9px 11px, radius 5px, border `1px solid #4A90C2`, outer glow `0 0 0 2px rgba(74,144,194,.15)`, 15px magnifier, input 14px `#F2F2F2`, trailing pill "↵ run" (10px, `#8A8A8A`, border `1px solid #4A4A4F`, radius 3px, padding 1px 6px).
- Group divider (no collapse, no chevron): padding 10px 12px 3px, gap 6px — 7px color dot + label 9.5px weight 700, letter-spacing .7px, UPPERCASE, `#8A8A8A`.
- Result row (padding 6px 12px, gap 9px, hover background `#0E639C` full-row highlight): 22px icon + label 12.5px `#EAEAEA` (ellipsis, flex 1) + **source** 10px `#7A7A7A` + star (same as 1a).

### Icon spec (the main splash of color — match this exactly)
A rounded square filled with the **category color**, a lighter gradient on the top half, a thin lighter outline, and a **bold white 2-letter monogram** centered.
- Sizes used: **22px** (list rows, 1a/1c), **28px** (favorites icon-only mode), **40px** (1b tiles + favorites label mode). Radius 5px at ≤22px, else 7px.
- Background: `linear-gradient(180deg, color-mix(in srgb, <color> 50%, #ffffff), <color>)`.
- Border: `1px solid color-mix(in srgb, <color> 40%, #ffffff)`.
- Inner highlight: `box-shadow: inset 0 1px 0 rgba(255,255,255,.28)`.
- Monogram: white, weight 700, letter-spacing .3px. Font size 9.5px @22px, 11px @28px, 14px @40px. Centered.
- In WPF, precompute the two lighter shades per category (or blend in code) since `color-mix` is CSS-only; the real add-in ships 16px + 32px PNGs matching this style, so you may simply render the PNGs instead of drawing the square.

---

## Favorites mini-palette (all three directions)
A small floating launcher over the Revit model, near the cursor.

- **Container**: width ~216px, background `#2B2B2E`, border `1px solid #45454B`, radius 6px, shadow `0 12px 34px rgba(0,0,0,.55)`. Absolutely positioned; opens near the model.
- **Header** (drag handle — `cursor: move`, background `#333337`, bottom border `#26262A`, padding 6px 9px, gap 7px): gold `★` 12px + "Favorites" 11px weight 600 `#E8E8E8` (flex 1) + **"Aa" labels-toggle button** + 6-dot drag glyph SVG (`#6A6A6A`).
- **Aa button**: toggles labels on/off. Active (labels on): background `#0E639C`, white. Idle: transparent, `#9A9A9A`, border `1px solid #45454B`. 10px, weight 600, radius 3px, padding 1px 6px.
- **Body — labels ON (dense list, default)**: column, padding 4px. Each item is a button (padding 4px 6px, gap 8px, hover background `#3A3A41`, left-aligned): 22px icon + label 11.5px `#E2E2E2` (ellipsis). This is the high-density-near-the-cursor layout.
- **Body — labels OFF (icon-only strip)**: wrapping flex row, gap 5px, padding 8px, of 28px icons only (tooltip = label). Maximum density.
- **Drag**: the whole panel moves when the header is dragged (mouse down on header → follow mouse until mouse up). Position persists during the session.
- **Empty state** (no favorites): centered text 11px `#8A8A8A`: "No favorites yet. / Star commands in the palette to pin them here."
- **Reorder** (nice-to-have, not built in the mock): let the user drag favorites to arrange their top tools. Order is currently `favOrder` (see State).

---

## Interactions & Behavior
- **Search**: on every keystroke, filter commands where `label` OR `tooltip` contains the query (case-insensitive). Empty groups disappear. Any non-empty query force-expands all groups.
- **View toggle**: Category / Source / A–Z re-groups the same command set instantly.
- **Collapse**: click a group header to toggle; chevron flips. Expand-all / Collapse-all operate on all current groups. Collapse state is ignored while searching.
- **Favorite toggle**: clicking a star adds/removes the command from favorites; the mini-palette updates live. New favorites append to the end of the order.
- **Favorites button** in the title bar opens/closes the mini-palette.
- **Aa toggle** switches the mini-palette between labelled list and icon-only strip.
- **Mini-palette drag**: header is a drag handle; panel follows the cursor.
- **Run a command**: clicking a command row/tile, or a favorites item, invokes it (in the mock these are no-ops). In 1c, `Enter` in the search field runs the top result.

## State Management
Persisted between sessions (Revit add-in settings / user config):
- `favorites` — set of command ids the user has starred. **Must persist.**
- `favOrder` — ordered list of favorite ids (for the mini-palette + future reorder).
- `favLabelsOn` — bool, mini-palette labels on/off.

Session / view state (need not persist, but may):
- `query` — current search text (per palette).
- `view` — `category | source | az`.
- `collapsedGroups` — set of collapsed group keys.
- `miniOpen` — mini-palette visibility.
- `miniPosition` — floating x/y.

Data (from existing code — do not invent fields): each command exposes **Label**, **Tooltip/long description**, **Category**, **Source** (add-in), **Icon** (16px + 32px PNG), **Favorite** flag.

## Design Tokens

### Category colors (fixed spec — used for icons and category accents)
| Category | Hex |
|---|---|
| Insulation | `#2E8B8B` (teal) |
| Pipe & Geometry | `#3A6EA5` (blue) |
| Instrumentation | `#7A4FA3` (purple) |
| Piping Systems | `#3C8C4A` (green) |
| Parameters & Tagging | `#C67A2E` (orange) |
| Rooms, Levels & Docs | `#A8503C` (rust) |
| Family | `#A03A7A` (magenta) |
| Analysis & QA | `#52667A` (slate) |
| Connectors | `#B06028` (brown) |
| Supports | `#5A6470` (grey) |

Category display order = the order above.

### Theme brushes (dark; define as named resources)
- App/model backdrop: `#383838` (with subtle grid + faint blue model lines in the mock — not part of the pane).
- Pane background: `#2D2D30`
- Toolbar/header/title background: `#333337`, group header `#313135`
- Hover: `#3A3A3F` (rows/headers), `#3A3A41` (tiles), `#3F3F47` (mini items)
- Row highlight (1c): `#0E639C`
- Search field background: `#1E1E1E`, border `#4A4A4F` (1c focus border `#4A90C2`)
- Divider / hairline: `#26262A`, pane left border `#1E1E1E`
- Text primary `#EAEAEA` / `#E2E2E2`, secondary `#D8D8D8`, muted `#8A8A8A` / `#7A7A7A`
- Accent (buttons, active seg, primary): `#0E639C`; link blue `#7FB0D6`
- Favorite star gold: `#E2B93B`; inactive star `#6B6B70`
- Tile surface `#333338`, tile border `#3C3C42`; mini surface `#2B2B2E`, mini border `#45454B`

### Typography
- Font family: **Segoe UI** (system UI). Fallback: `'Segoe UI', -apple-system, Roboto, 'Helvetica Neue', sans-serif`.
- Sizes: command label 12px (1c 12.5px), group label 11px, monograms 9.5/11/14px per icon size, meta/utility 10–10.5px, mini label 11.5px, 1c search input 14px.
- `-webkit-font-smoothing: antialiased` in the mock; use ClearType defaults in WPF.

### Spacing / radius / other
- Radii: rows/buttons 3–4px, icons 5px (≤22) / 7px (larger), tiles/mini panel 5–6px.
- Row padding 4px 9–12px; toolbar padding 8px; tile grid gap 6px; mini list padding 4px.
- Shadows: mini-palette `0 12px 34px rgba(0,0,0,.55)`.
- Icon gradient/border/inset-highlight per Icon spec.

## Assets
- **Command icons**: the real add-in ships **16px + 32px PNGs** per command (rounded square, category color, top gradient, lighter outline, bold white 2-letter monogram). Use those PNGs, or render the equivalent per the Icon spec.
- **SVGs in the mock** (magnifier, 6-dot drag handle) are trivial — reproduce with WPF `Path`/vector or Segoe MDL2 glyphs (`Search` `\uE721`, grip `\uE76F`).
- No external fonts or images beyond the above; everything else is drawn with theme brushes.

## Sample data used in the mock (38 commands)
Format: `Monogram — Label — Category — Source`. Favorites in the mock: NM, CW, FL, PI, MV, FC.

- IC — Create all insulation — Insulation — MEPUtils
- ID — Delete all insulation — Insulation — MEPUtils
- IS — Insulation settings — Insulation — MEPUtils
- FL — Create flanges — Pipe & Geometry — MEPUtils
- PP — Pipe from connector — Pipe & Geometry — MEPUtils
- MV — Move to distance — Pipe & Geometry — MEPUtils
- DS — Element from DirectShape — Pipe & Geometry — MEPUtils
- IN — Create instrument — Instrumentation — MEPUtils
- IZ — Create instrument NN — Instrumentation — MEPUtils
- PF — Add PS view-filters — Piping Systems — MEPUtils
- PI — Isolate selected PS — Piping Systems — MEPUtils
- PH — Hide selected PS — Piping Systems — MEPUtils
- SG — Select by GUID — Piping Systems — MEPUtils
- LG — Create PS legend — Piping Systems — NTR Exporter
- UA — Update abbreviation — Piping Systems — NTR Exporter
- RW — Read link workset — Piping Systems — NTR Exporter
- NM — (Re-)Number — Parameters & Tagging — MEPUtils
- SP — Split parameter value — Parameters & Tagging — MEPUtils
- FC — Copy flow data — Parameters & Tagging — MEPUtils
- WG — Write all par GUIDs — Parameters & Tagging — MEPUtils
- CP — Copy PST params — Parameters & Tagging — MEPUtils
- SI — Set & increment — Parameters & Tagging — MEPUtils
- SM — Set from ME — Parameters & Tagging — MEPUtils
- AL — Assign correct levels — Rooms, Levels & Docs — MEPUtils
- CD — Copy to another doc — Rooms, Levels & Docs — MEPUtils
- WR — Write room numbers — Rooms, Levels & Docs — MEPUtils
- RL — Rooms from link — Rooms, Levels & Docs — MEPUtils
- RG — Room nums generic — Rooms, Levels & Docs — MEPUtils
- AP — Family add parameters — Family — MEPUtils
- FT — Create family types — Family — MEPUtils
- TL — Total length — Analysis & QA — PCF Exporter
- CW — Count welds — Analysis & QA — PCF Exporter
- PL — Pressure loss calc — Analysis & QA — MEPUtils
- SQ — Spindle orientation QA — Analysis & QA — MEPUtils
- CC — Connect connectors — Connectors — PCF Exporter
- TK — Tilkoblet check — Connectors — PCF Exporter
- PS — Place supports — Supports — MEPUtils
- SU — Support tools — Supports — MEPUtils

*(Tooltips/long descriptions for each are in the HTML `title` attributes — one sentence per command.)*

## Optional hybrid
If neither 1a nor 1c is a clear winner: a strong hybrid is 1c's large search field + source column on top of 1a's collapsible categories and Expand/Collapse-all — "type to filter, or browse and fold." Ask before building; the mock currently keeps them distinct on purpose.

## Files
- `Norsyn Commands.dc.html` — the interactive prototype containing all three directions (1a, 1b, 1c) + the favorites mini-palette. Open in a browser; pan/zoom the canvas. Search, view toggle, collapse, star, Aa labels toggle, and mini-palette drag are all live.
