# HaywireCleaner — Architecture (as built)

*Living map of the actual code under `Assets/Scripts`. Companion to
`../architecture-foundations.md` (the theory — why the boundaries are where they
are) and `../quest-system-structure.md` (the reactive-narrative research this
project's fact system implements).*

**This file is the map only** — what the systems are, how they depend on each
other, and the trace that ties them together. The detail lives one file per
system, so a task can load only what it needs:

| File | Covers |
|---|---|
| [`core.md`](core.md) | Fact spine · scene flow & session intent · possession & modules · Core idioms & event channels |
| [`quests.md`](quests.md) | `QuestRuntime` brain · progression model · the read-model seam (`QuestInfo`) |
| [`cutscenes.md`](cutscenes.md) | Data-driven Timeline playback · finished-facts · play-once gating |
| [`input.md`](input.md) | Context stack · execution transport · glyph display projection · `{0}` glyph-text view |
| [`interaction.md`](interaction.md) | Focus sensor · interactables · charging dock |
| [`ui.md`](ui.md) | Prompt/mount channels · popups & modals · menu shell & pause · quest views |
| [`editor-tooling.md`](editor-tooling.md) | Fact-key registry & dropdown · input action-key drawer |

**Last full sweep:** 2026-07-12 (all 79 scripts + 9 asmdefs).
**Partial updates since:** 2026-07-15 — quest journal + HUD tracker; composite-aware
glyph display; menu shell & context-driven pause. 2026-07-16 — split into this
folder; popup/modal system (`UIPopupRequest`/`UIPopup`/`UIHoldToConfirm`) and the
dual-addressed mount point; `MenuInput.ConfirmDown/Up`. Not a full re-sweep — the
sections touched reflect current code.

**Maintenance rule:** keep it coarse and stable. Update a section when a system
changes shape or a stated invariant/status stops being true — not for every edit.
A map that lies is worse than none. *(It has lied before: `menu-ui-handoff.md`
sent a session chasing a file that was already written.)*

---

## The one idea everything narrative hangs on — the fact spine

**All persistent game/narrative state is flags and counters in one static store,
`WorldState`. Systems *write* facts and *react* to the `WorldState.FactChanged`
signal; they never call each other directly.** A "condition" is data
(`FactCondition`) evaluated against that store. Quests, cutscene gating, and the
tutorial trackers are all just writers and readers of facts. Keys are never
hand-typed — every key is minted in exactly one place, `FactKeys`, and surfaced
to authoring through an editor dropdown. This is the CD-Projekt/Osiris "facts +
generic conditions" shape; the reasoning is in `../quest-system-structure.md`.

## Systems index

| # | System | One-line | Detail | Status |
|---|---|---|---|---|
| 1 | **Fact spine** | Persistent flag/counter store + `FactChanged`; *is* the save | [core](core.md#1-fact-spine) | Live |
| 2 | **Fact-key tooling** (editor) | Dropdown of every valid key, gathered from code + assets | [editor-tooling](editor-tooling.md#fact-key-registry) | Live (editor-only) |
| 3 | **Quests** | Conditions over facts; stage counter is the source of truth | [quests](quests.md) | Live |
| 4 | **Cutscenes** | Data-driven, event-triggered Timeline playback; writes finished-fact | [cutscenes](cutscenes.md) | Live |
| 5 | **Session intent / new-game** | Carries New-Game vs Continue across the scene load | [core](core.md#5-session-intent--new-game-flow) | Live (New-Game path) |
| 6 | **Scene flow** | Title↔Gameplay additive load, behind one vocabulary | [core](core.md#6-scene-flow) | Live |
| 7 | **Possession & modules** | Which actor gets input; modules react to typed intents | [core](core.md#7-possession--module-input) | Live (one actor) |
| 8 | **Input routing** | Device→`Intent`; context stack; glyph projection + `{0}` glyph-text view | [input](input.md) | Live (Player/Cutscene/Menu) |
| 9 | **Interaction & docking** | Focus sensor, interactables, charging dock | [interaction](interaction.md) | Live |
| 10 | **UI prompt / mount** | SO event channels for prompt + prefab mounting | [ui](ui.md#10-ui-prompt--mount-channels) | Live |
| 11 | **Quest read-model + views** | Quest facts → immutable snapshots; journal + HUD over the seam | [quests](quests.md#the-read-model-journal--hud-seam) · [ui](ui.md#the-quest-views) | Live |
| 12 | **Menu shell & game-pause** | Menu open/close pushes `Menu` context; context drives `timeScale` | [ui](ui.md#12-menu-shell--game-pause) | Live (shell; skin pending) |
| 13 | **Popups & modals** | Quest-spawned requester → mount channel → canvas-parented modal + hold-to-confirm | [ui](ui.md#13-popups--modals) | Live (tutorial popup) |

## Dependency direction (the only arrows allowed)

```
  App(Bootstrap)   Features.*(Input, Modules, Cutscenes, Interactables, UI, Quests, Title)
        \                 \        \        \         \        \       /
         \                 \        \        \         \        \     /
          └──────────────────────────── Core ─────────────────────┘
                    (SaveSystem · Player · Input · Interaction ·
                     Events · SceneControls · Quests)

  Features → Core only.  Feature → Feature: never.  Core → nothing above it.
  App(Bootstrap) → Core, and is the one place allowed to know Feature scene
  names / wire event assets (composition root).
```

Enforced mechanically: every Feature asmdef references **only `Core`** (plus Unity
packages). Verify: no Feature asmdef lists another Feature.

## Assemblies (asmdef → references)

| Assembly | References | Notes |
|---|---|---|
| `Core` | `Unity.Localization` | Was "references nothing"; now pulls Localization. Still references no Feature. |
| `App` | `Core` | `Bootstrap`, `GameplayBootstrap` (namespace `Bootstrap`) |
| `Features.Input` | `Core`, `Unity.InputSystem`, `Unity.TextMeshPro` | The Input System package is quarantined here. (TMP ref is probably strippable — Appendix B.) |
| `Features.Modules` | `Core` | |
| `Cutscenes` | `Core`, `Unity.Timeline`, `Unity.TextMeshPro` | asmdef name is `Cutscenes`, not `Features.Cutscenes` |
| `Features.Interactables` | `Core`, `Unity.Localization` | |
| `Features.UI` | `Core`, `Unity.TextMeshPro`, `Unity.Localization` | **`UnityEngine.UI` is not listed** but `Image`/`Button` resolve anyway — `Unity.TextMeshPro` drags it in (TMP's own types derive from `MaskableGraphic`). Implicit; add it explicitly if TMP is ever dropped. |
| `Features.Quests` | `Core`, `Unity.Localization` | |
| `Features.Title` | `Core`, `Unity.Localization` | |
| *(none)* — `Editor/**` | predefined `Assembly-CSharp-Editor` | auto-references all asmdefs **and all packages** (so editor tools may use `Unity.InputSystem` freely without breaking the runtime quarantine); namespaces `Tools.FactKeyRegistry` / `Tools` |
| *(none)* — `Prototypes/*`, `FpvSlimPrototype/*`, `MyScript.cs` | predefined `Assembly-CSharp` | **scratch, not architecture** — see Appendix A |

## The narrative stack, end to end (the trace to hold onto)

The spine that ties fact + cutscene + quest + tutorial together, wired end to end:

```
New Game (core §5) → intro CutsceneDefinitionSO's eventTrigger raised
  → CutsceneDirector.Play: InputRouter.Enter(Cutscene); Timeline runs
  → on playable.stopped: if WritesFinishedFact → WorldState.SetFlag(
        FactKeys.CutsceneFinished("intro")); eventRaiseOnFinish?.Raise;
        InputRouter.Exit(Cutscene)
  → WorldState.FactChanged fires
  → QuestRuntime: a quest whose startConditions test cutscene.intro.finished
        is now met → writes quest.{id}.stage = 1
  → entering stage 1 instantiates the stage's setupPrefabs, which include
        AxisInputDwell trackers for "move"/"rotate" AND a UIPopupRequest
  → UIPopupRequest.OnEnable → RaiseShow(popupPrefab) → UIMountPoint mounts it
        into the canvas → UIPopup.OnEnable → InputRouter.Enter(Menu)
        → GamePauseInMenu freezes time; the journal refuses to open
  → player holds Confirm (UIHoldToConfirm, unscaled clock) or clicks Close
        → RaiseHide → mount point destroys the instance → UIPopup.OnDisable
        → Exit(Menu) → time resumes
  → player moves/rotates → DwellTracker accumulates → writes
        FactKeys.TutorialPlayerMoved / TutorialPlayerRotated, destroys itself
  → FactChanged → QuestRuntime rechecks stage-1 objectives, all met → stage = 2 → …
```

Read that trace once and the whole game's control flow is in your head: **nothing
in it is a direct call between systems.** Every arrow is a fact write, an event
raise, or a context push.

---

## Appendix A — Prototype / scratch code (NOT architecture)

Two self-contained spike folders in their own namespaces, referencing neither
`Core` nor `Features` (they fall into `Assembly-CSharp`). They are the
pre-architecture gameplay experiments — ignore them when reasoning about systems,
and don't wire production code to them.

- **`Prototypes/`** (namespace `Prototypes`, 9 files) — the original cleaning
  loop spike: `PrototypeRobotMove`/`PrototypeRobotClean`/`PrototypeDirtPatch`/
  `PrototypeBeads`/`PrototypeSliderDirtCollected`/`PrototypeCamera`/
  `PrototypeFlashLight`/`PrototypeInteractPrompt`, plus `SceneSwitcher`
  (a raw `SceneManager.LoadScene` — the one legitimate non-`SceneLoader`
  `SceneManager` call, and it's scratch).
- **`FpvSlimPrototype/`** (namespace `FpvSlimPrototype`, 7 files) — the
  first-person "slim mode squeeze" spike: `FpvSlimProtBotMove` (space toggles a
  y-scale squash), `FpvSlimProtClean`/`FpvSlimProtDirt`/`FpvSlimProtBeads`
  (forked copies of the Prototype cleaning loop), `FpvSlimProtCamera`/
  `FpvSlimCameraSwitch`/`FpvSlimProtEdgeDetector`.
- **`MyScript.cs`** (root, no namespace) — empty class stub. Delete.

These are the concrete referents behind `../architecture-foundations.md`'s slim/
suction/clean module discussion — kept as design memory, superseded in code by the
Core/Feature stack above.

## Appendix B — Known stale / cleanup TODOs

- `ActorHost.Awake → TestPossess()` and `[ContextMenu]` — dev-only auto-possess,
  marked for removal.
- `GameFlow.OnNewGameRequested`/`OnLoadGameRequested` + `LoadGame()` — dead
  (no subscribers/callers). `Posession.OnPosessionChanged` — declared, never raised.
- `ModuleInput.RaiseStopCharging` / `Intent.StopCharge` — raised but unhandled
  ([interaction](interaction.md)).
- `MyScript.cs` — empty stub, delete.
- `Core` asmdef references `Unity.Localization` — confirm something in Core
  actually needs it, or drop the reference to keep Core lean.
- `Features.Input` asmdef references `Unity.TextMeshPro` — nothing in that
  assembly should need TMP (it's the InputSystem quarantine); likely strip back to
  `Core`, `Unity.InputSystem`.
- `UIPopupRequest` still has an unused `using Core.Input;` (left over from when it
  owned the context push — see [ui](ui.md#13-popups--modals)).
- `UIMenuController.OnDisable` calls `Exit(Menu)` **unconditionally**, even when the
  menu was never open — a phantom Exit that can pop a context another owner pushed.
  Only reachable on scene unload today. Same bug class `Start` was written to avoid.
- `InputGlyphDrawer` ignores `BeginProperty`'s **return value**; feeding it back into
  the `Popup` is what makes prefab-override state and mixed-value display work.
- `InputSystem_Actions`: the `Back` action (UI map, `buttonEast`/`escape`) has no
  reader. `Cancel`/`Confirm`/`Back` all have `expectedControlType: ""` — `Cancel`
  was `"Button"` before and looks accidentally wiped.