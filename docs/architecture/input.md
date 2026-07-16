# Input routing — contexts, execution & display projections

*`Core/Input/*` + `Features/Input/*` + `Features/UI/TextDisplay/*`. Map:
[`README.md`](README.md).*

**Status:** Live for Player + Cutscene + Menu contexts. One adapter, `InputReader`,
turns the action asset into a single `Intent→InputAction` map and fans it out three
ways.

## Three concerns, kept separate

- **Context (which map is active)** — `InputRouter` is a context *stack*
  (`Gameplay`/`Cutscene`/`Menu`); `Enter`/`Exit` push/pop and raise
  `ContextChangedTo`. `InputReader.Apply` enables the matching action map.
- **Execution (push)** — `ModuleInput.RaiseMove/RaiseInteract/RaiseStopCharging`
  broadcast `OnIntent(Intent,Vector2)`; the possessed `Actor` receives it
  ([core §7](core.md#7-possession--module-input)).
- **Display (pull)** — `InputGlyphProvider` wraps the same map and registers into
  `GlyphInput`; UI asks by **action-key string** (`"Map/Action"`) for a `Glyph`
  (label/sprite) to draw; actor-verb callers resolve their `Intent`→key via `KeyFor`.

## Components

| Component | Location | Responsibility |
|---|---|---|
| `InputReader` | `Features/Input/InputReader.cs` | The one adapter. Builds the `Intent→InputAction` map from the `Player` map; pumps `Move` each frame; `Interact` on `performed`; `Skip`→`CutsceneInput.RaiseSkip`; `ToggleMenu`→`MenuInput.RaiseToggleMenu`; `Confirm` on **`started`/`canceled`**→`MenuInput.RaiseConfirmDown/Up`; constructs+registers the glyph provider; enables/disables maps per context. |
| `InputContext` / `InputRouter` | `Core/Input/InputContext.cs`, `InputRouter.cs` | Enum + static context stack, `ContextChangedTo`, `ActiveContext` (`Gameplay` when empty). `Exit` is `List.Remove(value)`. |
| `ModuleInput` | `Core/Input/ModuleInput.cs` | Static execution transport (`OnIntent`) — the Player map's channel. |
| `CutsceneInput` | `Core/Input/CutsceneInput.cs` | The Cinematic map's channel: `SkipCutscene` (`RaiseSkip`). |
| `MenuInput` | `Core/Input/MenuInput.cs` | The **UI map's** channel: `ToggleMenu` (`RaiseToggleMenu`) + `ConfirmDown`/`ConfirmUp` (`RaiseConfirmDown/Up`). |
| `GlyphInput` | `Core/Input/GlyphInput.cs` | Static registry slot holding one `IInputGlyphProvider` (`Register`/`Glyphs`). |
| `IInputGlyphProvider` / `Glyph` | `Core/Input/*` | Display seam: `GetGlyph(string actionKey)` + `KeyFor(Intent)` + `DeviceChanged`; `Glyph {label, sprite}` (only `sprite` unwired). Glyphs are keyed by action, not `Intent` — `KeyFor` is the actor-verb→binding bridge. |
| `InputGlyphProvider` | `Features/Input/InputGlyphProvider.cs` | Plain C#. Resolves an action-key string→`Glyph` for the active control scheme; caches by key; tracks device via `InputSystem.onActionChange`, clears cache + raises `DeviceChanged` on a scheme switch. `IDisposable`. **`DisplayFor` is composite-aware** (gotcha 3). |

## Rules

- The `Intent→InputAction` map lives **only** in `InputReader`.
- **Core never references `UnityEngine.InputSystem`** — verify `InputSystem` has no
  matches under `Assets/Scripts/Core`. The package is quarantined in `Features.Input`
  (and editor-only tools, which are exempt — they compile into
  `Assembly-CSharp-Editor`, which auto-references every package).
- UI pulls glyphs only from `GlyphInput` (null until `InputReader.Awake` registers —
  null-guard and query at show time).
- Adding a verb = `Intent` + binding + one map line + `Raise…`; the glyph side needs
  no change.

## Gotchas

**1 — One channel per *map*, never per action.** `MenuInput` carries `ToggleMenu`
*and* `ConfirmDown`/`ConfirmUp` because both are UI-map actions. A `ConfirmInput`
class would be the first boundary in the project drawn around an *action* instead of
a map, and every new button would then cost a file. See
[core](core.md#the-core-idioms-three-shapes-reused-everywhere).

**2 — `Confirm` uses `started`/`canceled`, and carries *no* `Hold` interaction.**
This is deliberate and looks backwards. The Input System's `Hold` fires `started` and
`performed` with **nothing in between** — no progress callback. So a hold that must
*draw* a fill ring would need the interaction to own the duration for firing and the
UI to own a second copy for drawing: two clocks, one duration, guaranteed to drift.
Instead the action is a plain Button, `InputReader` reports both edges, and the
duration lives in exactly one place — the widget that draws it
([`UIHoldToConfirm`](ui.md#13-popups--modals)). The input layer can't emit a finished
"confirmed" verb without owning hold policy, and that policy belongs upstairs; hence
`ConfirmDown`/`ConfirmUp` sit at a lower altitude than their sibling channels. That's
forced, not a slip.

A free property falls out: `started` only fires on a **transition**, so a button
already held when a popup mounts produces nothing until release + re-press. Real
anti-misclick behaviour for zero code.

**3 — Composite bindings (why `DisplayFor` isn't a one-liner).** A simple action
(e.g. `Interact`=E) has one leaf binding the scheme mask matches directly. A
*composite* like WASD `Move` has an **empty-group header row** followed by the part
bindings — so scheme-filtered `GetBindingDisplayString` returns blank if you only
test the header. `DisplayFor` walks the bindings and accepts either a plain in-scheme
leaf **or** a composite whose *next* (part) binding is in-scheme, then renders the
composite by index. `PrimaryKeys` then strips alternate bindings (`W|Up/A|Left/…` →
`W/A/S/D`) by keeping the first token of each `/`-part. This is the hard-won shape;
don't "simplify" it back to a single `GetBindingDisplayString`.

**4 — The `UI` map is always on.** `InputReader.OnEnable` calls `menu.Enable()` and
`Apply` never disables it (it only disables `player`/`cinematic`). So `Confirm` and
`ToggleMenu` fire in *every* context, including Gameplay — harmless, because nothing
subscribes unless a consumer exists. Don't bind a UI action to a key the Player map
also uses.

**5 — Context is a stack, and `Exit` removes by value.** Two owners pushing `Menu`
gives `[Menu, Menu]`; removing "the first match" is indistinguishable from removing
"the right one" since the values are identical, so the list acts as a counter and
Enter/Exit stay balanced. Policy says that shouldn't happen anyway (see
[ui §12](ui.md#12-menu-shell--game-pause)), but the bookkeeping tolerates it.

## The `{0}` glyph-text view (localized text with inline key labels)

**Status:** Live. A reusable TMP text that shows *"Move {0}"* with the glyph label
substituted for the current device — the seam that lets tutorials/prompts teach
controls in localized copy.

| Component | Location | Responsibility |
|---|---|---|
| `InputGlyphText` | `Features/UI/TextDisplay/InputGlyphText.cs` | `MonoBehaviour` over a `LocalizeStringEvent`. Holds `string[] actionKeys`; on `OnEnable` + `GlyphInput.Glyphs.DeviceChanged`, resolves each key→`GetGlyph(key).label`, pushes them as the localized string's `Arguments` (`{0}`,`{1}`,…), `RefreshString()`. Numbered args are locale-order-independent. |
| `InputActionKeyAttribute` | `Features/UI/TextDisplay/InputActionKey.cs` | `PropertyAttribute` marker on `actionKeys`. UnityEngine-only (no InputSystem) — keeps the runtime view inside the quarantine; the drawer that reads it is editor-only ([editor-tooling](editor-tooling.md#input-action-key-drawer)). |

**Rules:** the substitution flows through Localization `Arguments`, **not** string
concatenation — so the sprite swap (`{0}`→`<sprite>`) is a later change to the glyph
*label*, not to this view. The view stays InputSystem-free.

**In use today:** the tutorial popup's two rows — `UI.popup.tutorial` = *"To move and
rotate use {0}"* keyed `Player/Move`, and `UI.button.close` = *"{0} Close"* keyed
`UI/Confirm`.
