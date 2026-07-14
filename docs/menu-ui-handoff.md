# Menu + Quest UI — handoff

Building a tabbed in-game menu (Quest tab only, for now) + a left-side quest HUD
tracker, both as **views over `QuestUI`** (`Core.Quests`) — the UI never references
`Features.Quests`. Working cadence: thin tickets, the dev writes all `.cs`, review after.

## Decisions locked
- **Glyph seam** re-keyed by action-key string `"Map/Action"` (not `Intent`);
  `KeyFor(Intent)` bridges gameplay prompts. Runtime UI stays InputSystem-free.
  (§8 in `architecture-actual.md` reflects this.)
- **Menu-open** rides a `MenuInput.ToggleMenu` static channel (sibling of `CutsceneInput`),
  raised by `InputReader` from a `ToggleMenu` action on the always-on `UI` map
  (Tab + Gamepad/`start` = the ≡). The future corner mouse-button raises the SAME channel.
- **`UIMenuController`** owns open/close and drives `InputRouter.Enter/Exit(Menu)`.
  Gate: *close* reads own `state`; *open* reads `ActiveContext == Gameplay`; ignore in
  Cutscene / when a tutorial modal already holds Menu. Controller lives on a persistent
  object; `MenuPanel` is a `SetActive` child (never co-locate the controller on the panel).
- **Time-freeze** is context-driven: whenever `ActiveContext == Menu`, `Time.timeScale = 0`
  (a separate GamePause authority), so tutorials freeze too. Menu anims must use `unscaledDeltaTime`.
- **Journal** = two-pane (list grouped Active/Completed + detail: story, objectives,
  Track→`SetTracked`). **HUD** = compact (title + current objectives), hides via `CanvasGroup`
  when `TrackedId` is null.
- Crimson Desert refs: menu opens on the **last tab** (remember `lastTabIndex`); ≡ = Gamepad `start`.

## Done
- **#1** Glyph seam refactor (action-key strings + `KeyFor`). Verified via interact prompt.
- **#3** `MenuInput` channel + `InputReader` hook. Verified via Debug.Log.
- **#4** `UIMenuController` — open/close + context push. Fixes landed:
  - Dropped `isOpen => panel.activeSelf`: a *view* property is not *logical* state — that was
    two sources of truth. `state` is the single authority.
  - Split **reconcile** from **transition**: `Start()` forces the baseline (`panel` off,
    `state = Closed`) with **no** `Exit` (we were never in Menu — a phantom Exit lies to the
    router); `Close()` is the real transition (state, hide, `Exit(Menu)`); `OnDisable` guards
    `if (state == Opened)`. Rule: only emit a router transition when one actually happened —
    a stack router trusts honest Enter/Exit pairs.
  - **Tabs deferred (YAGNI)** until a 2nd tab exists; `tabPanels`/`lastTabIndex` pulled out.
    They return the day tab #2 lands: `Button[] tabButtons`, `SelectTab(int)`, `lastTabIndex`
    restored in `Open()` (watch the for-loop closure-capture trap — copy `i` to a local).

## Active task — Quest Journal (View B, was #8) + the menu shell that hosts it
The Quest tab's real content. **Plain-text first** — structure & data flow are the hard part;
the cute skin is paint on a working machine. Lives inside `MenuPanel` (shell not built yet:
`Canvas` → persistent object w/ `UIMenuController` → `MenuPanel` child *starting inactive* →
scrim + header + this journal + bottom glyph bar). CanvasScaler = Scale-With-Screen-Size,
1920×1080, match 0.5 (or it's the wrong size on half the web-demo machines).

### Art direction — moodboard `docs/ChatGPT Image Jul 14, 2026, 10_23_39 AM.png`
A **brief to build from, not sliceable art** (AI concept won't stay style-consistent).
Cute-robotic, **not hackery** — roundness + a companion **face** carry the cute; neon-green-on-
black is the hackery trap (the terminal variant on the board is the "don't").
- **Adopt:** rounded humanist font (same across all UI incl. `UIPrompt`); one soft mint accent
  used *only* on selection/current; rounded cards; bottom **glyph action bar** (fed by the glyph
  seam); the **OS-has-a-face** companion (Happy/Thinking/Excited/Oops) — the soul of the menu.
  It's "Clippy redeemed": it reacts to *your* success (quest done → Excited, collector full → Oops).
- **Reject:** the 3D metal bezel (raster, won't scale on web, real-estate hog, photoreal-vs-flat
  clash), the glitch title, the neon glow.
- **Refs:** structure = BG3 / KCD2 / Crimson Desert; feel = Chibi-Robo / Wall-E / Astro Bot /
  Animal Crossing. One-liner: *"a Roomba's companion app, designed by the Animal Crossing team."*
- **OPEN fork (decide before skinning):** cozy-dark vs warm-cream. Cream is more on-brief for
  "cute, not hackery"; dark sits better over the dimmed frozen game.
- **Scope guard:** the board implies systems you don't have — Daily Tasks, Claim Reward, coins/
  Power-Cells economy, 4 animated moods. Build NONE of them. Demo = Active/Completed only, real
  quests, one static "happy" companion sprite, glyph bar. No fake features behind the paint.

### Layout — two-pane master/detail (list **left**, detail **right**)
```
┌───────────────────────────────────────────────────────────┐
│  ●  QUEST LOG                                   (bot face) │  header
│───────────────────────────────────────────────────────────│
│  ACTIVE                    │  Learn what happened          │
│  ▸ Learn what happened  ◀  │  MAIN                         │  ← detail pane
│    Clean the room          │                               │
│                            │  "Something's wrong with my   │
│  COMPLETED                 │   software. I woke up..."      │  story log
│    Go outside              │                               │
│   (list, grouped)          │  Objectives                   │
│                            │  ○ Find the box               │  ← not done
│                            │  ✓ Open the door              │  ← done
│───────────────────────────────────────────────────────────│
│  ≡ Close          ✜ Navigate          ★ Track             │  glyph bar
└───────────────────────────────────────────────────────────┘
```

### Data map — the journal is a pure projection of `Core.Quests.QuestUI` (the seam you built)
- list ← `Snapshots`, grouped by `snapshot.Status` (Active/Completed)
- detail ← `Get(selectedId)` · story ← `snapshot.StageStory` · objectives ← `snapshot.Objectives`
- track ← `SetTracked(id)` · current star ← `TrackedId` · auto-refresh ← subscribe `Changed`

### Decisions locked this session
- **Kind vs state** — Active/Completed is *state*, not two types. Proof: the model is *one*
  `QuestSnapshot` with a `Status` **field**. Test: a quest moves Active→Completed at runtime, so
  it MUST be one type with a field — two prefabs/scripts would force destroy+respawn on completion.
  → **one** row widget, **one** list view. (Grey-out is a colour toggle, not a reason to duplicate.)
- **Selection ≠ Tracked** — selection = ephemeral *view* state (owned by the presenter); tracked =
  *game* state (in QuestUI, drives the HUD). You can read a Completed quest without tracking it.
- **Track lives in the detail pane** — one button, `interactable = selected.Status == Active`.
  NOT per-row (that dissolves "show only for active", and is controller-sane).
- **Objectives binary** ✓/○ — `ObjectiveLine` only has `Completed`. The wireframe's ◉ "current"
  needs a `QuestSnapshot` change (index/enum) — **deferred**, decide later.
- **No MAIN badge** — no quest-kind in the snapshot; drop for the demo.
- **Empty state mandatory** — "No quests yet"; a blank pane reads as broken (the exact thing we
  guard against).
- **UI = pure function of state** — Track writes model → `Changed` → Rebuild → star moves. Never
  hand-sync two copies of truth (React / Overwatch-UI flow).

### Tickets — build BOTTOM-UP, verify each before the next
- **J1 · `QuestRowWidget`** *(dumb view — the smallest contract, start here)*. One prefab.
  `Bind(QuestSnapshot)`: set title label; if `Status == Completed`, grey it. Holds its `id`;
  exposes `event Action<string> Clicked` raised by its `Button`. **Hides:** how one quest looks.
  **Trap:** must NOT touch `QuestUI` or know about selection — it only displays and shouts "clicked".
- **J2 · `QuestListView`**. Serialized: `activeContainer`, `completedContainer` (the two
  VerticalLayoutGroups, each + `ContentSizeFitter`), row prefab. `Rebuild(snapshots)`: clear both,
  partition by `Status`, spawn a `QuestRowWidget` per quest into its container, forward each row's
  `Clicked` upward. **Hides:** how the list is built & grouped. **Trap:** spawn logic is identical
  for both groups — one `Fill(container, subset)` called twice, NOT two spawner scripts.
- **J3 · `QuestDetailView`** *(dumb view)*. `Show(QuestSnapshot)`: fill title, story (join
  `StageStory` lines), spawn objective rows (✓/○ from `Completed`). Owns the Track button:
  `interactable = snap.Status == Active`; onClick raises `event Action<string> TrackRequested` —
  it does NOT call `QuestUI` itself. `ShowEmpty()` for no selection. **Hides:** detail layout.
  **Trap:** must NOT own selection.
- **J4 · `QuestJournalView`** *(presenter — the policy)*. Owns `selectedId`. `OnEnable`:
  `QuestUI.Changed += Rebuild`. `Rebuild()`: `list.Rebuild(Snapshots)`; if `selectedId` is null/
  gone → default to `TrackedId`, else first active, else first, else none; `detail.Show(Get(id))`.
  Wire `list.Clicked → Select(id)`; `detail.TrackRequested → QuestUI.SetTracked(id)` (Changed then
  loops back to Rebuild — don't manually repaint the star). **Hides:** selection + the rules tying
  list ↔ detail ↔ track.

### Testing note
Needs a **live quest** to render anything — a `testquest` forcing `quest.{id}.stage` active.
Same dependency as #6 HUD; build it once, reuse for both.

## Also remaining (original backlog, after / parallel to the journal)
- **#5 GamePause** — MonoBehaviour on `InputRouter.ContextChangedTo`: `Time.timeScale =
  (ctx == Menu) ? 0 : 1`; restore `1` in `OnDisable`. (Menu anims must use `unscaledDeltaTime`.)
- **#6 QuestTrackerHUD** (View A) — left side; `CanvasGroup`-hidden when `TrackedId` null, else
  `Get(id)` → title + objective rows. Needs the same live quest as the journal.
- **#2 action-key dropdown** (parked; do *right before* #7) — serialized string key + editor
  property-drawer picker (clone the FactKey dropdown). Avoids hand-typing `"UI/ToggleMenu"`.
- **#7 Corner menu button + `ActionGlyphLabel`** — persistent button, `onClick →
  MenuInput.RaiseToggleMenu()`; label pulls `GetGlyph(key)` in `Start`, refreshes on
  `DeviceChanged`. Reused later by the tutorial panel.
