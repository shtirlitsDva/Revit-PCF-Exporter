# Design Brief — "Norsyn Commands" palette for Autodesk Revit

<paste-note>
This is a design brief to hand to a UI-design AI. It asks for a few **self-contained HTML mockups** that emulate how a WPF panel will look **docked inside Autodesk Revit**. The final thing is not a web page — it is a native WPF `UserControl` hosted in a Revit *dockable pane* — but HTML mockups are the fastest way to compare visual directions before we build the real WPF. Design for the **look and interaction**, not for production HTML.
</paste-note>

<the-problem>
Revit has ~40+ small in-house tools (and growing). Today they live on the **ribbon**. The ribbon is a bad home for tools you use constantly, because:

- **Selecting any element auto-switches the ribbon to the "Modify" tab.** So to run a frequent command you must: re-click your custom tab → find the button → click it. Every single time. Dozens of times a day.
- Ribbon buttons are small, buried in flyouts, and far from where the mouse is working.

The fix: a **dockable pane** — the same kind of panel Revit's own *Properties* and *Project Browser* are. A dockable pane **stays put regardless of which ribbon tab is active**, can be docked to a side or floated anywhere, is resizable, and hosts arbitrary WPF content. That is where the commands should live.
</the-problem>

<what-to-design>
Two related surfaces:

1. **The main palette** — a dockable pane listing **every** command, organized so any command is findable and reachable in one click.
2. **The favorites mini-palette** — a small, low-chrome, draggable/floating panel showing only the user's favorited commands, meant to sit right next to where they are working for instant mouse access.

The main palette has a button/toggle that opens (or reveals) the favorites mini-palette.
</what-to-design>

<the-data-each-command-carries>
Every command exposes this metadata (already defined in code — do not invent new fields, but you may choose which to surface and how):

- **Label** — short display text, e.g. "Create all insulation", "Count welds", "(Re-)Number".
- **Tooltip / long description** — a sentence explaining what it does.
- **Category** — one of ~10 groups (see the palette below). Drives color.
- **Source** — which in-house add-in the command came from (e.g. "MEPUtils", "PCF Exporter", "NTR Exporter"). Some users will want to group/sort by this.
- **Icon** — a 16px and 32px PNG. The existing icon style (match it): a **rounded square filled with the category color**, a subtle lighter gradient on the top half, a thin lighter outline, and a **bold white 2-letter monogram** centered (e.g. "IC" for *Insulation → Create all*). Clean, flat, high-contrast.
- **Favorite** — a per-command on/off flag the user sets; persisted between sessions.

There are ~42 commands today across the categories below; the list grows over time, so the design must scale to, say, 80+ without becoming unusable.
</the-data-each-command-carries>

<categories-and-their-colors>
Use these category colors for the icons and any category accents (RGB):

| Category               | Color            |
|------------------------|------------------|
| Insulation             | teal   `#2E8B8B` |
| Pipe & Geometry        | blue   `#3A6EA5` |
| Instrumentation        | purple `#7A4FA3` |
| Piping Systems         | green  `#3C8C4A` |
| Parameters & Tagging   | orange `#C67A2E` |
| Rooms, Levels & Docs   | rust   `#A8503C` |
| Family                 | magenta`#A03A7A` |
| Analysis & QA          | slate  `#52667A` |
| Connectors             | brown  `#B06028` |
| Supports               | grey   `#5A6470` |

A typical category holds 2–8 commands.
</categories-and-their-colors>

<must-haves-for-the-main-palette>
- **Search / filter box** at the top — type to filter commands live by label (and ideally tooltip). This is the single most important feature: for a power user, type-to-filter beats hunting.
- **Grouping / view toggle** — the user can switch how the list is organized:
  - **By category** (default) — commands under collapsible category headers.
  - **By source add-in** — commands grouped by which product they came from.
  - **Flat A–Z** — one ungrouped alphabetical list.
- **Collapsible category sections** with **expand-all / collapse-all**. BUT — an explicit design tension to solve: the user is wary of folds that *hide* things and make him click twice. So also offer, or make easy to reach, a **"show everything expanded"** state. Show both a collapsed and an expanded state in your mockup.
- **Compact rows** — icon + label per command, minimal vertical space, so many commands fit without endless scrolling. Consider an optional denser 2-column layout for narrow docks.
- **Per-command favorite toggle** — a star (or similar) on each row to pin/unpin from favorites, visible on hover or always-on.
- **Category color** used as a subtle accent (left edge stripe, header tint, or the icon alone) — enough to scan by color, not so much it's noisy.
</must-haves-for-the-main-palette>

<must-haves-for-the-favorites-mini-palette>
- **Small footprint**, low chrome — it floats over the Revit model near the mouse.
- Shows **only favorited commands**, as **large, easy click targets** (icon + short label, or big icon with label under).
- **Draggable** anywhere on screen; feels like a floating toolbar / launcher.
- Reorderable would be nice (user arranges his top tools).
- Empty state: a friendly prompt telling the user to star commands in the main palette.
</must-haves-for-the-favorites-mini-palette>

<visual-and-technical-constraints>
- **Dark theme is primary** (it must sit comfortably next to Revit's dark UI). Also provide a **light theme**. The real app has a shared theme with named brushes, so keep colors token-like/consistent.
- **Narrow widths matter.** A docked pane is often only **~260–360px wide**. The layout must look good and stay usable when narrow (this is the common case), and also when the user floats it wider. Show at least the narrow docked width in your mockup.
- **Native-Revit feel**, not web-flashy: think Visual Studio / Revit tool windows — restrained, dense, professional. No rounded card shadows everywhere, no marketing gradients. The command **icons** are the main splash of color.
- Font: a clean system UI font (Segoe UI). Small, legible.
- No external assets — inline everything (mock the icons as CSS rounded squares with a 2-letter monogram; you do not need the real PNGs).
</visual-and-technical-constraints>

<what-to-deliver>
Produce **2–3 distinct visual/interaction directions** as separate self-contained HTML mockups. For each direction, show:

1. The **main palette** at a narrow docked width (~300px), in **dark theme**, with:
   - the search box (show it once with a query typed in, filtering the list),
   - category grouping with **some sections collapsed and some expanded**,
   - visible favorite stars,
   - a realistic number of commands (use the sample list below).
2. The **favorites mini-palette** (dark theme), floating, with ~5–6 favorited commands.
3. One shot of the **light theme** for either surface.

For each direction, add a 2–3 sentence note on its tradeoffs (density vs clarity, how it handles growth to 80+ commands, how it resolves the "don't hide things behind folds" tension).

Distinct directions worth exploring (pick/mix, don't do all):
- **A. Dense list** — tight rows, category headers, closest to a Revit tool window.
- **B. Icon-forward grid** — commands as small icon tiles in a wrapping grid under category headers; more visual, fewer words.
- **C. Search-first / command-palette style** — minimal chrome, big search, results below; categories secondary. Optimized for "type 3 letters, hit enter."
</what-to-deliver>

<sample-commands-to-populate-the-mockup>
Insulation: Create all insulation (IC), Delete all insulation (ID), Insulation settings (IS)
Pipe & Geometry: Create flanges (FL), Pipe from connector (PP), Move to distance (MV), Element from DirectShape (DS)
Instrumentation: Create instrument (IN), Create instrument NN (IZ)
Piping Systems: Add PS view-filters (PF), Isolate selected PS (PI), Hide selected PS (PH), Select by GUID (SG), Create PS legend (LG), Update abbreviation (UA), Read link workset (RW)
Parameters & Tagging: (Re-)Number (NM), Split parameter value (SP), Copy flow data (FC), Write all par GUIDs (WG), Copy PST params (CP), Set & increment (SI), Set from ME (SM)
Rooms, Levels & Docs: Assign correct levels (AL), Copy to another doc (CD), Write room numbers (WR), Rooms from link (RL), Room nums generic (RG)
Family: Family add parameters (AP), Create family types (FT)
Analysis & QA: Total length (TL), Count welds (CW), Pressure loss calc (PL), Spindle orientation QA (SQ)
Connectors: Connect connectors, Tilkoblet check (TK)
Supports: Place supports, Support tools (SU)
</sample-commands-to-populate-the-mockup>

<favorites-sample>
Star these in the mockup's favorites mini-palette: (Re-)Number (NM), Count welds (CW), Create flanges (FL), Isolate selected PS (PI), Move to distance (MV), Copy flow data (FC).
</favorites-sample>
