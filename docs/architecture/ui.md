# UI

*`Features/UI/*` + the `Core/Events` channels that drive it. Map:
[`README.md`](README.md). Skin/art direction and the open design forks live in
`../menu-ui-handoff.md`.*

Covers: prompt & mount channels · popups & modals · menu shell & pause · the quest
views.

**The rule that holds the whole assembly together:** no feature references UI. UI
subscribes to channels and pulls from Core registry slots. `Features.UI` references
only `Core` (+ TMP/Localization) — never `Features.Quests`, never `WorldState`.

---

## Which seam when

Three different wires do three different jobs here, and picking the wrong one is the
source of the nastiest bug this system has produced. The deciding question is
**does the reference survive serialization?**

| Seam | Use when | Why |
|---|---|---|
| **SO event channel** (`UIElementDisplayRequestSO`) | Crossing a boundary — a runtime-spawned object talking to a scene object, or two prefabs that never meet at author time | Both ends point at an **asset**. A prefab can serialize a reference to an asset; it **cannot** serialize a reference to a scene object (there's nothing on disk to point at, and the object doesn't exist yet). |
| **`UnityEvent`** (`Button.onClick`, `UIHoldToConfirm.onCompleted`) | *Inside* one prefab, where both ends exist on disk together | Author-time wiring with zero code — the component references nothing, which is what makes it reusable. Costs: reflection dispatch, and the binding is stored by **method-name string** (rename → silent dead wire). |
| **Core registry slot** (`QuestInfo`, `GlyphInput`) | A view needs to *pull* state, subscription order unknown | Stable facade event; a widget may subscribe before the service exists. |

**The trap, concretely:** dragging a prefab's own root into a `UnityEvent` argument
from the hierarchy serializes an *internal* reference — and `Instantiate` **remaps
internal references to the copies**. So the argument arrives at runtime as the
**instance**, not the prefab asset. (Dragging the same prefab from the *Project*
window instead serializes an asset reference, which is **not** remapped. Two drags
that look identical, opposite runtime behaviour. Don't build on that distinction.)

---

## 10. UI prompt & mount channels

**Status:** Live.

| Component | Location | Responsibility |
|---|---|---|
| `UIPromptDisplayRequestSO` | `Core/Events/UIPromptDisplayRequestSO.cs` | `Show(string text, Intent)` / `Hide()`. Raised by interactables on focus. |
| `UIPromptPositionRequestSO` | `Core/Events/UIPromptPositionRequestSO.cs` | `SetPosition(Vector3)`. |
| `UIElementDisplayRequestSO` | `Core/Events/UIElementDisplayRequestSO.cs` | Generic widget channel `Show/Hide(GameObject)` — the doc-comment calls it the "Unreal WidgetChannel", which is exactly right. |
| `UIPrompt` | `Features/UI/UIPrompt.cs` | Listens to a prompt channel; sets label; resolves the glyph for the `Intent` from `GlyphInput` ([input](input.md)) and refreshes on `DeviceChanged`; fades a `CanvasGroup`. |
| `UIPromptPosition` | `Features/UI/UIPromptPosition.cs` | Places the prompt at `WorldToScreenPoint(hitPoint + offset)` each `LateUpdate`. |
| `UIMountPoint` | `Features/UI/UIMountPoint.cs` | Sits on the canvas. On `Show(prefab)` → `Instantiate(prefab, container)`, keyed into `active`. On `Hide(key)` → destroy. **The only thing that destroys mounted instances.** |

### The canvas rule

**A mounted popup prefab has no `Canvas` of its own.** Its root is a plain
`RectTransform`; `UIMountPoint.container` parents it into the canvas already in the
scene. Reasons, in order of how much they bite:

1. **`CanvasScaler` is per *root* canvas.** A popup with its own root canvas needs its
   own scaler set identically (1920×1080, match 0.5) or it's the wrong size — two
   truths that drift.
2. **A Canvas is a batching/draw boundary, not an organizational unit.** Each root
   Screen-Space-Overlay canvas is its own rebuild + draw batch, and inter-canvas
   ordering becomes `sortingOrder` magic numbers.

The shape at scale is a **small fixed set of root canvases as layers** (HUD / Modal /
Tooltip), each with its own mount point, established once in the scene. The one
legitimate Canvas *inside* a popup is a **child** Canvas component (inherits render
mode + scaler — no second scaler) to isolate a fast-changing element from dirtying
the parent. That's a profiler decision, not a structural one.

The parallel: UMG is `CreateWidget` + `AddToViewport(ZOrder)` — the widget doesn't
carry a viewport, it gets added to one.

### `Unmount` is dual-addressed (and why)

`active` maps **prefab → instance**. `Unmount(key)` first tries `active.Remove(key)`
(addressed by prefab), then falls back to a reverse scan over `active.Values`
(addressed by instance).

Two callers with genuinely different knowledge force this:

- **`UIPopupRequest`** only knows the *prefab* — it asked for the popup and got
  nothing back, because `RaiseShow` returns void.
- **The popup's own close doors** only know *themselves* — they have no idea which
  prefab they came from.

Neither can learn the other's address without extra plumbing, so the mount point —
which knows both, it built the mapping — translates. A grown-up UI stack hands back a
**handle** on show (`CreateWidget` returns the widget; you later `RemoveFromParent`
on *it*); this reverse lookup is what we pay instead of having one.

**Gotchas**

1. **Find first, mutate after.** The reverse scan breaks out of the `foreach` before
   removing. Removing *inside* the loop appears to work only because `return` fires
   before the enumerator's next `MoveNext()` — a landmine for whoever adds a line
   after it.
2. **Nothing else may `Destroy` a mounted instance.** A popup that destroys itself
   leaves a stale `active` entry, and `Mount`'s `ContainsKey` guard then refuses to
   ever show that prefab again. Silent, permanent. Close = ask the channel.
3. **Double-hide is safe.** Both lookups miss, nothing happens. That's what makes
   "player closed it, then the quest stage tore down the requester" a no-op rather
   than a double-destroy.

---

## 13. Popups & modals

**Status:** Live — the tutorial popup for quest stage 1 is the first consumer of the
mount channel (which existed unused since it was written).

```
QuestRuntime spawns at world root ──► TutorialPopupRequest
                                        [UIPopupRequest]  prefab + channel
                                            │ OnEnable → RaiseShow(prefab)
                                            ▼
                                  UIMountPoint (on canvas, same channel asset)
                                            │ Instantiate(prefab, container)
                                            ▼
                                    TutorialPopup instance
                                      ├ UIPopup          Enter/Exit(Menu)
                                      ├ UIHoldToConfirm  onCompleted ─┐
                                      ├ btn_close        onClick ─────┤
                                      └ InputGlyphText ×2             │
                                                                      ▼
                                              UIElementDisplayRequestSO.RaiseHide(root)
                                                          → mount point destroys instance
                                                          → UIPopup.OnDisable → Exit(Menu)
```

| Component | Location | Responsibility |
|---|---|---|
| `UIPopupRequest` | `Features/UI/UIPopupRequest.cs` | Lives on a **world-root** prefab (a quest `setupPrefab`). `OnEnable`→`Show()`→`RaiseShow(prefab)`; `OnDisable`→`Hide()`→`RaiseHide(prefab)`. Pure mount plumbing — knows nothing about contexts or tutorials, so a non-freezing popup (toast, hint) reuses it as-is. |
| `UIPopup` | `Features/UI/UIPopup.cs` | On the **popup prefab root**. `OnEnable`→`InputRouter.Enter(Menu)`, `OnDisable`→`Exit(Menu)`. That's all of it. |
| `UIHoldToConfirm` | `Features/UI/UIHoldToConfirm.cs` | Reusable hold-to-act. Subscribes `MenuInput.ConfirmDown/Up`; accumulates `Time.unscaledDeltaTime` into `elapsed`, writes `fill.fillAmount = elapsed/holdSeconds`, fires `UnityEvent onCompleted` at the threshold. `ResetHold` on release **and** on enable. |

Both close doors point at `UIElementDisplayRequestSO.RaiseHide` with the popup root
as the argument — which arrives as the **instance**, which is what `Unmount`'s
reverse lookup exists for.

### Rules & gotchas

**1 — A context push must be owned by an object whose lifetime *is* the context's
duration.** This is the load-bearing rule of the whole system, and it was learned the
hard way: `Enter/Exit(Menu)` originally lived on `UIPopupRequest`, which **outlives
the popup** (it sits at world root until the quest stage advances). Closing the popup
destroyed the instance but never disabled the requester, so `Exit(Menu)` never ran and
the game stayed frozen forever. On `UIPopup` it cannot fail — there's no way to destroy
an object without disabling it first, so *any* close path resumes time.

The same move killed a second bug: a requester teardown *after* a player-close would
have fired `Exit(Menu)` with no matching `Enter` — a phantom Exit that pops whatever
context someone else owns.

**2 — `unscaledDeltaTime` is mandatory in anything a modal draws.** `Enter(Menu)`
means `GamePauseInMenu` is holding `timeScale = 0`, so `deltaTime` is flatly zero: a
`deltaTime` fill ring sits at empty forever while the player holds the button, and it
reads as broken input rather than a stopped clock. `Update` and the EventSystem still
tick at zero timescale, so both button clicks and the hold work.

**3 — The widget owns the hold clock.** `holdSeconds` lives on `UIHoldToConfirm`, not
on a `Hold` interaction in the action asset — see [input gotcha 2](input.md#gotchas)
for why the interaction was deliberately removed.

**4 — The `fired` guard is not optional.** Once `elapsed` passes the threshold it
stays past it until release, so without the flag `onCompleted` fires *every frame*.
The tutorial hides this (the popup is destroyed on the first one) — which is exactly
why it must be there for the next consumer.

**5 — `ResetHold`, not `Reset`.** `Reset()` is a Unity magic method the editor invokes
when the component is added or reset from the context menu; it would run with `fill`
unassigned and throw. Editor-only, so it hides from play-mode testing.

---

## 12. Menu shell & game-pause

**Status:** Live shell. The Quest-tab *content* is the journal (below); the skin/art
pass is still open (`../menu-ui-handoff.md`).

| Component | Location | Responsibility |
|---|---|---|
| `UIMenuController` | `Features/UI/UIMenu/UIMenuController.cs` | `MonoBehaviour` on a **persistent** object (never on the panel it toggles). Subscribes `MenuInput.ToggleMenu`; `OnToggle` gates: *close* reads own `state`; *open* only when `ActiveContext == Gameplay`. `Open`/`Close` flip `state`, `SetActive` the `panel`, and `InputRouter.Enter/Exit(Menu)`. `Start` forces the closed baseline. |
| `GamePauseInMenu` | `Features/UI/UIMenu/GamePauseInMenu.cs` | The single `Time.timeScale` authority. On `InputRouter.ContextChangedTo`: `Menu → 0`, else `1`. `OnDisable` restores `1`. |

**Rules & gotchas**

1. **`state` is the one source of truth, not `panel.activeSelf`.** A view property is
   not logical state — reading `activeSelf` back would be two truths that can drift.
2. **Only emit a router transition when one actually happened.** `Start` sets the
   closed baseline with **no** `Exit(Menu)` (we were never in Menu — a phantom Exit
   lies to the context stack). *(`OnDisable` still does this unconditionally —
   README Appendix B.)*
3. **Pause is context-driven, one owner.** Nothing else writes `Time.timeScale`. Do
   **not** put a second `GamePauseInMenu` on a popup: it restores `timeScale = 1` in
   `OnDisable`, so the copy would force time back on at teardown regardless of what
   the scene's copy believes. The popup pushes the *context* instead and gets pause
   for free.
4. **Policy: only one thing holds `Menu` at a time.** Enforced by exactly one line —
   `UIMenuController`'s `ActiveContext == Gameplay` open-gate. A tutorial popup
   therefore makes the journal unopenable for free, no new logic. Sub-panels that want
   to layer (a "Reset to defaults?" confirm) are **panels inside the owner**, not
   context pushers — the cost being that whoever holds the context owns Back/Esc
   routing for its own children. The day a second thing genuinely needs to sit over a
   menu, `Menu` splits into `Menu` + `Modal` and the gate checks both.
5. **Tabs are deferred (YAGNI)** until a 2nd tab exists — `lastTabIndex` is present but
   unused; the multi-tab restore returns with tab #2 (watch the for-loop
   closure-capture trap when it does).

---

## The quest views

**Status:** Live. Three **pure projections of `QuestInfo`** — no view references
`Features.Quests` or touches `WorldState`. The seam itself, and the `Build`
projection invariant these rely on, is in [`quests.md`](quests.md#the-read-model-journal--hud-seam).

| Component | Location | Responsibility |
|---|---|---|
| `UIQuestListEntry` | `Features/UI/UIMenu/UIQuestListEntry.cs` | Dumb row. `Bind(snapshot)`: title + `description` = the active stage line (`StageStory[^1]`); if `Status == Completed`, recolors/re-styles the title (serialized `completedColor` + `FontStyles`). Holds `id`; a `Button` calls `Click()` → `event Action<string> Clicked`. Knows nothing of selection or the detail panel. |
| `UIQuestTab` | `Features/UI/UIMenu/UIQuestTab.cs` | List view **+ selection presenter, merged**. On `QuestInfo.Changed` (and `OnEnable`) → `Rebuild`: destroys old rows, spawns one `UIQuestListEntry` per snapshot into `activeGroup`/`completedGroup` by `Status`, forwards each row's `Clicked` to `Select`. Owns `currentId`; `Select(id)` → `detailPanel.Show(Get(id))` or `ShowEmpty()`. Re-defaults the pick each rebuild (`TrackedId` → first snapshot → none). |
| `UIQuestDetailPanel` | `Features/UI/UIMenu/UIQuestDetailPanel.cs` | Dumb detail. `Show(snapshot)`: title + one TMP rich-text block — active stage line, its objectives (done ones grayed + struck via the shared `completedColor`), then prior stages struck, newest-first; a completed quest strikes every line. `ShowEmpty()` for no selection. |
| `HUDQuest` | `Features/UI/HUDQuest.cs` | Always-on gameplay widget. On `QuestInfo.Changed` (+ `OnEnable`) → `Refresh`: `Get(TrackedId)`; if `null`, `CanvasGroup.alpha = 0` and bail; else paint `title` + a single `objectives` TMP block, `<s>`-struck + `completedColor`-tinted when `Completed`. `alpha = 1`. |

**Rules & gotchas**

1. **Both list *and* detail are pure functions of `Changed`.** `UIQuestTab.Rebuild`
   re-runs the detail's `Show` (via `Select(currentId)`) every rebuild, so an objective
   completing while the panel is open repaints the *detail*, not only the list.
   Refreshing the list alone is the bug that leaves a checked objective un-grayed.
2. **Selection lives in `UIQuestTab`, not the row.** Rows only shout `Clicked(id)`
   (event up); the tab decides what's selected and pushes to the detail (command down).
   This folds the presenter role into the list view — fine, but it means gotcha 1 has
   to be honored *here*.
3. **Never hand `Show` a null.** `Get(id)` returns `null` for an unknown/absent id.
   `Select` guards → `ShowEmpty()`; `Rebuild` re-defaults `currentId` when it stops
   resolving. A blank pane on open reads as broken — hence the default-selection.
4. **Grey-out/strike is a data-driven visual *state*, one prefab.** Active vs Completed
   is a `QuestStatus` field the row/detail branch on — never a second prefab/variant (a
   quest moves Active→Completed at runtime).
5. **HUD hide is alpha, not `SetActive`.** `alpha = 0` keeps the subscription alive so
   the widget reappears the instant a quest is tracked — a disabled object would miss
   its own `Changed`.
6. **HUD and detail share the strike contract** (`completedColor` + `<s>`), so they read
   identically; both build TMP rich text with a `StringBuilder`, not per-line children.
