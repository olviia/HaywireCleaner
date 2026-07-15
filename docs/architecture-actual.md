# HaywireCleaner — Architecture (as built)

*Living map of the actual code under `Assets/Scripts`. Companion to
`architecture-foundations.md` (the theory — why the boundaries are where they
are) and `quest-system-structure.md` (the reactive-narrative research this
project's fact system implements). This file is the **coarse map**: what the
systems are, where they live, how they depend on each other, and the gotchas
that aren't visible from any single file. It points into the code; it does not
duplicate signatures — read the file for details.*

**Last full sweep:** 2026-07-12 (all 79 scripts + 9 asmdefs).
**Maintenance rule:** keep it coarse and stable. Update a section when a system
changes shape or a stated invariant/status stops being true — not for every
edit. A map that lies is worse than none.

---

## 0. The map (read this first)

### The one idea everything narrative hangs on — the fact spine

**All persistent game/narrative state is flags and counters in one static store,
`WorldState`. Systems *write* facts and *react* to the `WorldState.FactChanged`
signal; they never call each other directly.** A "condition" is data
(`FactCondition`) evaluated against that store. Quests, cutscene gating, and the
tutorial trackers are all just writers and readers of facts. Keys are never
hand-typed — every key is minted in exactly one place, `FactKeys`, and surfaced
to authoring through an editor dropdown. This is the CD-Projekt/Osiris "facts +
generic conditions" shape; the reasoning is in `quest-system-structure.md`.

### Systems index

| # | System | One-line | Entry file(s) | Status |
|---|---|---|---|---|
| 1 | **Fact spine** | Persistent flag/counter store + `FactChanged`; *is* the save | `Core/SaveSystem/WorldState.cs`, `FactKeys.cs`, `SaveData.cs`, `Features/Quests/FactCondition.cs` | Live |
| 2 | **Fact-key tooling** (editor) | Dropdown of every valid key, gathered from code + assets | `Assets/Scripts/Editor/FactKeyRegistry/*` | Live (editor-only) |
| 3 | **Quests** | Conditions over facts; stage counter is the source of truth | `Features/Quests/*` (+ `Progression/DwellTracker.cs`) | Live |
| 4 | **Cutscenes** | Data-driven, event-triggered Timeline playback; writes finished-fact | `Features/Cutscenes/*` | Live |
| 5 | **Session intent / new-game** | Carries New-Game vs Continue across the scene load | `Core/SceneControls/GameSession.cs`, `App/GameplayBootstrap.cs` | Live (New-Game path) |
| 6 | **Scene flow** | Title↔Gameplay additive load, behind one vocabulary | `Core/SceneControls/SceneStateMachine.cs`, `SceneLoader.cs`, `App/Bootstrap.cs` | Live |
| 7 | **Possession & modules** | Which actor gets input; modules react to typed intents | `Core/Player/*`, `Features/Modules/*` | Live (one actor) |
| 8 | **Input routing** | Device→`Intent`; context stack; glyph display projection | `Features/Input/InputReader.cs`, `Core/Input/*` | Live (Player/Cutscene) |
| 9 | **Interaction & docking** | Focus sensor, interactables, charging dock | `Core/Interaction/*`, `Features/Modules/InteractionModule.cs`+`ChargingModule.cs`, `Features/Interactables/*` | Live |
| 10 | **UI prompt / mount** | SO event channels for prompt + prefab mounting | `Core/Events/UI*RequestSO.cs`, `Features/UI/*` | Live |
| 11 | **Quest journal (read-model + views)** | Read-only projection of quest facts → immutable snapshots; two-pane journal UI over the seam; UI knows no quest types | `Core/Quests/*`, `Features/Quests/QuestInfoPasser.cs`, `Features/UI/UIMenu/UIQuest*.cs` | Live (HUD pending) |

### Dependency direction (the only arrows allowed)

```
  App(Bootstrap)   Features.*(Input, Modules, Cutscenes, Interactables, UI, Quests, Title)
        \                 \        \        \         \        \       /
         \                 \        \        \         \        \     /
          └──────────────────────────── Core ─────────────────────┘
                    (SaveSystem · Player · Input · Interaction ·
                     Events · SceneControls)

  Features → Core only.  Feature → Feature: never.  Core → nothing above it.
  App(Bootstrap) → Core, and is the one place allowed to know Feature scene
  names / wire event assets (composition root).
```

Enforced mechanically: every Feature asmdef references **only `Core`** (plus
Unity packages). Verify: no Feature asmdef lists another Feature.

### Assemblies (asmdef → references)

| Assembly | References | Notes |
|---|---|---|
| `Core` | `Unity.Localization` | Was "references nothing"; now pulls Localization. Still references no Feature. |
| `App` | `Core` | `Bootstrap`, `GameplayBootstrap` (namespace `Bootstrap`) |
| `Features.Input` | `Core`, `Unity.InputSystem` | The Input System package is quarantined here |
| `Features.Modules` | `Core` | |
| `Cutscenes` | `Core`, `Unity.Timeline`, `Unity.TextMeshPro` | asmdef name is `Cutscenes`, not `Features.Cutscenes` |
| `Features.Interactables` | `Core`, `Unity.Localization` | |
| `Features.UI` | `Core`, `Unity.TextMeshPro` | |
| `Features.Quests` | `Core`, `Unity.Localization` | |
| `Features.Title` | `Core`, `Unity.Localization` | |
| *(none)* — `Editor/FactKeyRegistry/*`, `Editor/FontToSprite.cs` | predefined `Assembly-CSharp-Editor` | auto-references all asmdefs; namespace `Tools.FactKeyRegistry` / `Tools` |
| *(none)* — `Prototypes/*`, `FpvSlimPrototype/*`, `MyScript.cs` | predefined `Assembly-CSharp` | **scratch, not architecture** — see Appendix A |

### The narrative stack, end to end (the trace to hold onto)

This is the spine that ties fact + cutscene + quest + tracker, now wired end to
end: the QuestRuntime brain that reacts to facts is live (§3).

```
New Game (§5) → intro CutsceneDefinitionSO's eventTrigger raised
  → CutsceneDirector.Play: InputRouter.Enter(Cutscene); Timeline runs
  → on playable.stopped: if WritesFinishedFact → WorldState.SetFlag(
        FactKeys.CutsceneFinished("intro")); eventRaiseOnFinish?.Raise;
        InputRouter.Exit(Cutscene)
  → WorldState.FactChanged fires
  → QuestRuntime (§3): a quest whose startConditions test cutscene.intro.finished
        is now met → writes quest.{id}.stage = 1
  → entering stage 1 instantiates the stage's setupPrefabs, which include a
        DwellTracker (§3) for "move" and "rotate"
  → player moves/rotates → DwellTracker accumulates dwell time → writes
        FactKeys.TutorialPlayerMoved / TutorialPlayerRotated, destroys itself
  → FactChanged → QuestRuntime rechecks stage-1 objectives, all met → stage = 2 → …
```

---

## 1. Fact spine — `WorldState` + `FactKeys` + `FactCondition`

**Status:** Live and load-bearing. (This section supersedes the old "Save
System" write-up, which described the flag API as having *no caller* — that is
long obsolete; this is now the busiest seam in the game.)

### Components

| Component | Location | Responsibility |
|---|---|---|
| `WorldState` | `Core/SaveSystem/WorldState.cs` | Static mediator over the one in-memory `SaveData`. Typed `GetFlag/SetFlag`, `GetCounter/SetCounter/AddToCounter`. Every setter fires `FactChanged(key)`. Owns JSON file I/O (`Save`/`Load`/`NewSave`; `Load`/`NewSave` fire `FactChanged(null)` = "everything changed"). `SaveExists`. |
| `SaveData` | `Core/SaveSystem/SaveData.cs` | `internal` plain container. Dictionaries by value-shape: `flags:bool`, `counters:int`, `reactions:float`, `names:string`, `positions:Vector3`, `attributeValues:float`; lists `ownedModuleId`/`skillId`; character + version + `inGameTimeSeconds` + `savedAt`. Only `flags`/`counters` have accessors on `WorldState` today. |
| `FactKeys` | `Core/SaveSystem/FactKeys.cs` | **The single string authority.** Builder methods `CutsceneFinished(id)`→`cutscene.{id}.finished`, `QuestStage(id)`→`quest.{id}.stage`, `QuestCompleted(id)`→`quest.{id}.completed`; consts `TutorialPlayerMoved`="tutorial.moved", `TutorialPlayerRotated`="tutorial.rotated". Carries `[FactKeySource]` attribute (metadata for the editor registry, §2). |
| `FactCondition` | `Features/Quests/FactCondition.cs` | Serializable `{factKey, FactTest test, int value}` + `IsMet()`. `FactTest` = `FlagIsTrue`/`FlagIsFalse`/`CounterAtLeast`. The specification/predicate object; pulls from `WorldState`, never pushed to. |

### Who writes / reads facts today

- **Writers:** `CutsceneDirector` (`SetFlag(CutsceneFinished)`), `DwellTracker`
  (`SetFlag(Tutorial…)`), `GameFlow.StartNewGame`→`NewSave`, `TestButton`→`Save`,
  `QuestRuntime` (`SetCounter(QuestStage)` on start/advance, `SetFlag(QuestCompleted)`
  on finish).
- **Readers:** `FactCondition.IsMet` (`GetFlag`/`GetCounter`), `CutsceneDirector`
  (`GetFlag(CutsceneFinished)` for play-once gating).
- **`FactChanged` subscribers:** `QuestRuntime.OnFactChanged` (only one today).

### Rules & gotchas

1. **`SaveData` is `internal`** — nothing outside `Core/SaveSystem/` can compile
   a reference to it. Enforced by the compiler.
2. **Keys are never hand-typed.** Reader and writer both call the same `FactKeys`
   method. Key strings *are* the save schema — `FactKeys` is append-only; renaming
   a key breaks saves.
3. **`FactChanged` is synchronous.** A `SetFlag`/`SetCounter` inside a
   `FactChanged` handler re-enters all handlers before the setter returns. Fine
   and idempotent at this scale; mind it when QuestRuntime advances a stage from
   inside its own handler (it recurses into the next stage).
4. **`currentSaveData` starts null.** `GetFlag`/`GetCounter` before a
   `NewSave()`/`Load()` will NPE. `GameFlow.StartNewGame` calls `NewSave` first;
   entering Gameplay by any path that skips it (e.g. `TestButton.OnGoToGameplayButton`)
   leaves the store null. Something must seed it.
5. Add a new accessor pair on `WorldState` (mirroring `GetFlag`/`SetFlag`) the
   first time a feature needs `reactions`/`names`/`positions`/`attributeValues` —
   don't expose all of them up front.

---

## 2. Fact-key tooling (editor)

**Status:** Live, editor-only. Built before the first quest asset was authored
(tooling-before-authoring). Lets an authored `FactCondition` pick its key from a
hierarchical dropdown of *every* valid key in the project, instead of typing
strings.

### Components (all `namespace Tools.FactKeyRegistry`, no asmdef → `Assembly-CSharp-Editor`)

| Component | Location | Responsibility |
|---|---|---|
| `IFactKeySource` | `Editor/FactKeyRegistry/IFactKeySource.cs` | Contract: `IEnumerable<string> GetFactKeys()`. Implement to contribute keys. |
| `ConstKeySource` | `…/ConstKeySource.cs` | Reflects every `[FactKeySource]`-marked class's public-static-const strings (code-born keys, e.g. the tutorial consts). |
| `CutsceneKeySource` | `…/CutsceneKeySource.cs` | Enumerates `CutsceneDefinitionSO` assets that opt in, yields `FactKeys.CutsceneFinished(id)`. |
| `QuestKeySource` | `…/QuestKeySource.cs` | Enumerates `QuestDefinitionSO` assets, yields `FactKeys.QuestCompleted(id)` + `QuestStage(id)`. |
| `FactKeyRegistry` | `…/FactKeyRegistry.cs` | Static. `Collect()` concatenates all sources, de-dupes. The list backing the dropdown. |
| `FactKeyDropdown` / `FactKeyDropdownItem` | `…/FactKeyDropdownItem.cs` | `AdvancedDropdown` that splits keys on `.` into a tree; leaf carries the full key. |
| `FactConditionDrawer` | `…/FactConditionDrawer.cs` | `[CustomPropertyDrawer(typeof(FactCondition))]`. Renders key-dropdown + `test` + (`value` only when `CounterAtLeast`). |

**Invariant:** the *same* `FactKeys` method computes the key both at author time
(sources, in-editor) and at runtime (writers/readers). Adding a new derived key
family = one `FactKeys` method + (if asset-derived) one `IFactKeySource`, then
register it in `FactKeyRegistry.Sources`.

---

## 3. Quest system

**Status:** Live. The `QuestRuntime` brain reacts to `FactChanged` end to end
(start → per-stage setup → objective completion → advance → completed). The
progression model (see `quest-system-structure.md` §6): **quest progress is
itself a fact.** `quest.{id}.stage` (a counter) is the source of truth — `0`
inactive, `1..Length` active on that stage, `>Length` → write
`quest.{id}.completed`. On load the brain rebuilds from the stage counter; it
never remembers triggers.

### Components

| Component | Location | Responsibility |
|---|---|---|
| `QuestDefinitionSO` | `Features/Quests/QuestDefinitionSO.cs` | `id`, `title`, `Stage[] stages`, `FactCondition[] startConditions`. Nested: `Stage {journalEntry, Objective[] objective, GameObject[] setupPrefabs}`, `Objective {LocalizedString description, FactCondition condition}`. Menu `Cleanbot/Quests/Definition`. |
| `QuestCatalogSO` | `Features/Quests/QuestCatalogSO.cs` | `List<QuestDefinitionSO> quests`. Menu `Cleanbot/Quests/Catalog`. |
| `QuestRuntime` | `Features/Quests/QuestRuntime.cs` | `MonoBehaviour`. Subscribes `WorldState.FactChanged`; `OnEnable` does a catch-up scan. Holds `setupStage` (quest→physically-built stage) and `setupInstances` (quest→spawned `setupPrefabs`); `Evaluate` runs the 3-state machine + reconcile per quest, logging each transition. |
| `DwellTracker` | `Features/Quests/Progression/DwellTracker.cs` | Abstract `MonoBehaviour`: accumulates `Time.deltaTime` while `Intensity() > deadzone`, and after `requiredSeconds` does `SetFlag(FactKey, true)` + destroys itself. An *objective-completion writer* — it lives inside a stage's `setupPrefabs`. |
| `AxisInputDwell` | `Features/Quests/Progression/AxisInputDwell.cs` | Concrete `DwellTracker` subclass (its **own file**, not nested). Serialized `axis` (`Vertical`/`Horizontal`); subscribes `ModuleInput.OnIntent`, caches the latest `Move` value, and maps the chosen axis component → `FactKeys.TutorialPlayerMoved`/`TutorialPlayerRotated` through its `FactKey`/`Intensity` overrides. |

### The brain (implemented)

Per-quest, on every `FactChanged` (key ignored; re-scan, matching the
`null`="everything" convention): read `quest.{id}.stage`; if `0`, promote to `1`
when `startConditions` all met; if `>Length`, write the completed flag (guarded
against re-fire); else **reconcile** — if the built stage ≠ the counter, tear
down old `setupInstances` and instantiate the new stage's `setupPrefabs`; then if
all of the current stage's objectives `IsMet()`, advance the counter. The
counter, not derived state, is authoritative — so save/load is free and quest
chains are just conditions on other quests' stage facts.

### The read-model (journal / HUD seam)

**Status:** Live. Seam added 2026-07-13; the **journal menu views** landed
2026-07-15 (sub-section below). HUD tracker still pending. This is the **read
sibling of `QuestRuntime`**: same catalog, same `FactChanged` subscription,
opposite direction. `QuestRuntime` *writes* `quest.{id}.stage`; this *reads* it
and paints immutable snapshots for the UI. Because progress is already a Core
fact, the UI reaches quest state **through Core** and never references
`Features.Quests` — the RED Engine `CJournalManager` split (state flows through a
mediator; authored text stays owned by the quest module).

*(Naming: this seam was drafted as `QuestJournal` / `IQuestJournalSource` /
`QuestUIService`; the shipped names are `QuestInfo` / `IQuestInfoSource` /
`QuestInfoPasser` — same roles.)*

| Component | Location | Responsibility |
|---|---|---|
| `IQuestInfoSource` | `Core/Quests/IQuestInfoSource.cs` | The UI's entire contract: `Snapshots()`, `Get(id)`, `TrackedId`, `SetTracked(id)`, `Changed`. Pure C# — no `UnityEngine`, no quest types. |
| `QuestSnapshot` (+ `ObjectiveLine`, `QuestStatus`) | `Core/Quests/QuestSnapshot.cs` | Immutable value handed to the UI. Already-resolved `string`s only: `Id`, `Title`, `Status` (`Active`/`Completed`), `StageStory[]` (a journal line per reached stage), `Objectives[]` of `(Text, Completed)`. No `LocalizedString` crosses this line. |
| `QuestInfo` | `Core/Quests/QuestInfo.cs` | Static registry slot (the `GlyphInput` idiom). Holds the one `IQuestInfoSource`, re-exposes a stable `Changed`, forwards reads null-safe. Decouples widget subscription lifetime from service lifetime. |
| `QuestInfoPasser` | `Features/Quests/QuestInfoPasser.cs` | `MonoBehaviour, IQuestInfoSource`. Builds snapshots from `catalog` + `WorldState` facts; resolves `LocalizedString`→`string`; holds the tracked pin; fires `Changed`. `OnEnable` subscribes `WorldState.FactChanged` + `LocalizationSettings.SelectedLocaleChanged` and `Register`s into `QuestInfo`; `OnDisable` mirrors in reverse. |

**What `Build` projects** (a cross-file invariant the views rely on): `StageStory`
= the journal line of *every reached stage* `0..reached-1`, so its **last** entry
is the active stage's line and earlier entries are past stages. `Objectives` =
**only the current stage's** objectives, each `Completed` = `condition.IsMet()`
read live. A completed quest (`stage > Length`) carries all stage lines and an
**empty** `Objectives`; stage `0` returns `null` and is filtered from `Snapshots()`.

**Rules & gotchas**

1. **State through Core, text owned by the feature.** Quest *progress* reaches the
   UI as facts via `WorldState`; only authored *text* lives in `Features.Quests`,
   resolved to plain `string` inside `QuestInfoPasser`. So `Core/Quests/*` is
   **Localization-free** — the boundary speaks resolved text. The views
   (`Features.UI/UIMenu/*`) reference only `Core.Quests`, never the quest asmdef.
2. **Status is derived from the stage counter, identically to `QuestRuntime`**
   (`0` excluded from the journal, `1..Length` active, `>Length` completed). One
   source of truth — journal and runtime cannot disagree.
3. **The tracked pin is late-bound and transient.** `TrackedId` resolves on every
   read: explicit pin if still active, else first active quest, else `null` (HUD
   hides). Not persisted — persisting needs a `WorldState` string accessor (the
   `names` dict, §1 gotcha 5).
4. **`Changed` is coarse** — fired on every `FactChanged` and every locale change;
   the UI re-pulls. Registration order is irrelevant: `QuestInfo` is a facade
   with a stable event, so a widget may subscribe before the service exists.
5. **HUD tracker still plugs in as** (pending): always-mounted, reads
   `TrackedId`→`Get`, `CanvasGroup`-hidden when `TrackedId` is `null`. The journal
   menu views below are the shipped half of "views plug in as".

### The journal UI (two-pane views over the seam)

**Status:** Live (added 2026-07-15). Master/detail journal inside the menu, a
**pure projection of `QuestInfo`** — no view references `Features.Quests` or
touches `WorldState`. uGUI + TMP, under `Features/UI/UIMenu/`.

| Component | Location | Responsibility |
|---|---|---|
| `UIQuestListEntry` | `Features/UI/UIMenu/UIQuestListEntry.cs` | Dumb row. `Bind(snapshot)`: title + `description` = the active stage line (`StageStory[^1]`); if `Status == Completed`, recolors/re-styles the title (serialized `completedColor` + `FontStyles`). Holds `id`; a `Button` calls `Click()` → `event Action<string> Clicked`. Knows nothing of selection or the detail panel. |
| `UIQuestTab` | `Features/UI/UIMenu/UIQuestTab.cs` | List view **+ selection presenter, merged**. On `QuestInfo.Changed` (and `OnEnable`) → `Rebuild`: destroys old rows, spawns one `UIQuestListEntry` per snapshot into `activeGroup`/`completedGroup` by `Status`, forwards each row's `Clicked` to `Select`. Owns `currentId`; `Select(id)` → `detailPanel.Show(Get(id))` or `ShowEmpty()`. Re-defaults the pick each rebuild (`TrackedId` → first snapshot → none). |
| `UIQuestDetailPanel` | `Features/UI/UIMenu/UIQuestDetailPanel.cs` | Dumb detail. `Show(snapshot)`: title + one TMP rich-text block — active stage line, its objectives (done ones grayed + struck via the shared `completedColor`), then prior stages struck, newest-first; a completed quest strikes every line. `ShowEmpty()` for no selection. |

**Rules & gotchas**

1. **Both list *and* detail are pure functions of `Changed`.** `UIQuestTab.Rebuild`
   re-runs the detail's `Show` (via `Select(currentId)`) every rebuild, so an
   objective completing while the panel is open repaints the *detail*, not only the
   list. Refreshing the list alone is the bug that leaves a checked objective un-grayed.
2. **Selection lives in `UIQuestTab`, not the row.** Rows only shout `Clicked(id)`
   (event up); the tab decides what's selected and pushes to the detail (command
   down). This folds the presenter role into the list view — fine, but it means
   gotcha 1 has to be honored *here*.
3. **Never hand `Show` a null.** `Get(id)` returns `null` for an unknown/absent id
   (`currentId` unset on first open, or a selected quest that vanished). `Select`
   guards → `ShowEmpty()`; `Rebuild` re-defaults `currentId` when it stops resolving.
   A blank pane on open reads as broken — hence the default-selection.
4. **Grey-out/strike is a data-driven visual *state*, one prefab.** Active vs
   Completed is a `QuestStatus` field the row/detail branch on — never a second
   prefab/variant (a quest moves Active→Completed at runtime).
5. **`testquest.cs`** (`Features/Quests/testquest.cs`) is a dev probe: logs the
   tracked quest's objectives `[x]`/`[ ]` on every `Changed` — the ground-truth
   check for what a snapshot actually carries.

---

## 4. Cutscene system

**Status:** Live, data-driven. Each cutscene declares its own trigger event;
`CutsceneDirector` subscribes to all of them and plays on raise. Adding a
cutscene needs no code change. **Play-once gating is back** (it writes/reads a
fact now) and the director **does** touch `WorldState` — reversing the older
"director no longer touches the save system" note.

### Components

| Component | Location | Responsibility |
|---|---|---|
| `CutsceneDefinitionSO` | `Features/Cutscenes/CutsceneDefinitionSO.cs` | `id`, `cutscenePrefab` (carries a `PlayableDirector`), `eventTrigger` (`VoidEventSO` — what fires it), `eventRaiseOnFinish` (`VoidEventSO` raised on stop — chains forward), `replayable`, `isTriggerForQuest`. Derived `WritesFinishedFact => !replayable || isTriggerForQuest`. |
| `CutsceneCatalogSO` | `Features/Cutscenes/CutsceneCatalogSO.cs` | `List<CutsceneDefinitionSO> cutscenes`, serialized onto the director. |
| `CutsceneDirector` | `Features/Cutscenes/CutsceneDirector.cs` | `MonoBehaviour`. `OnEnable`: for each def, skip if no trigger or if play-once-gated (`!replayable && GetFlag(CutsceneFinished(id))`), else bind a handler to `eventTrigger.Raised` (stored for `OnDisable`). `Play`: `InputRouter.Enter(Cutscene)` on first active cutscene; instantiate prefab; wire skip via `CutsceneInput.SkipCutscene`; on `playable.stopped` → write finished-fact if opted in, raise `eventRaiseOnFinish`, destroy instance, `InputRouter.Exit(Cutscene)` when the last one ends. |
| `CutsceneTextScrambleReveal` | `Features/Cutscenes/CutsceneTextScrambleReveal.cs` | `ITimeControl` on a Timeline clip: scrambles then settles TMP text, char-by-char, driven by clip time. |

### Rules

1. **Adding a cutscene is code-free** — make the def, assign prefab + `eventTrigger`,
   add to catalog, ensure something raises the trigger.
2. **Playback is prefab-instantiation** — each cutscene is a self-contained prefab
   with its own `PlayableDirector` and Timeline bindings. Unbound `ActivationTrack`s
   fail silently.
3. **Replay is prevented by the finished-fact** (`WritesFinishedFact`), re-checked
   at subscription time in `OnEnable`. A `replayable` cutscene that also
   `isTriggerForQuest` still writes its fact (so a quest can gate on it) but is not
   play-once-blocked.
4. **Input context is the director's responsibility** — it enters/exits the
   `Cutscene` context around playback (§8); skip is a `Cinematic`-map action.

---

## 5. Session intent & new-game flow

**Status:** Live for New Game (drives the intro end to end). `Continue`/`None`
are stubs. `GameFlow.OnNewGameRequested`/`OnLoadGameRequested` fire but have no
subscribers (dead); `GameFlow.LoadGame` has no callers.

Entry intent is carried as **data, not a live object**. `GameSession` (SO) holds
a transient `EntryMode`; the Title button writes it, the Gameplay scene reads it
once and acts — so the "new game started" broadcast is raised only *after*
gameplay listeners exist.

| Component | Location | Responsibility |
|---|---|---|
| `GameSession` | `Core/SceneControls/GameSession.cs` | SO. `EntryMode {None,NewGame,Continue}`; `Request(mode)` (menu), `Consume()` (read-and-clear, gameplay), `OnEnable` resets to `None`. Menu `Cleanbot/App/GameSession`. |
| `GameplayBootstrap` | `App/GameplayBootstrap.cs` | `MonoBehaviour` in Gameplay. `Start`: `switch(session.Consume())` → NewGame: `InitializeNewGame()` (stub) + `newGameStarted.RaiseAction()`; Continue: `LoadSavedGame()` (stub); None: nothing. |
| `StartNewGameButton` | `Features/Title/StartNewGameButton.cs` | `Request(NewGame)` then `GameFlow.StartNewGame()`. |
| `GameFlow` | `Core/SceneControls/GameFlow.cs` | Static. `StartNewGame`: `WorldState.NewSave()` + (dead) `OnNewGameRequested` + `ChangeSceneTo(Gameplay)`. |
| `TestButton` | `Features/Title/TestButton.cs` | Dev harness: `StartNewGame` (no session intent → intro won't play), `OnGoToGameplayButton`, `OnSwitchLocale`, `OnSaveTestButton`→`WorldState.Save()`. |

**Timing invariant:** the raise happens in `GameplayBootstrap.Start`; listeners
(`CutsceneDirector`) subscribe in `OnEnable`. Unity runs all `OnEnable` before any
`Start`, so nothing is missed — keep the raise in `Start`. Two wires must point at
the *same* asset: the `GameSession` on button + bootstrap, and the `VoidEventSO`
on bootstrap's `newGameStarted` + the intro def's `eventTrigger`.

---

## 6. Scene flow

**Status:** Live. Title↔Gameplay, additive load/unload, behind one vocabulary;
no feature touches `SceneManager` directly.

| Component | Location | Responsibility |
|---|---|---|
| `SceneStateMachine` | `Core/SceneControls/SceneStateMachine.cs` | Static vocabulary. `GameScene {Title,Gameplay}`, `CurrentGameScene`, `ChangeSceneTo(next)`, `event OnGameSceneChanged(from,to)` (fires the instant a transition is *requested*). |
| `SceneLoader` | `Core/SceneControls/SceneLoader.cs` | Static interpreter. `Initialize(map)` (by Bootstrap), `LoadScene(from,to)` loads `to` additively + unloads `from` on completion, then fires `OnSceneLoaded(to)` (after the new scene's `OnEnable`s). |
| `Bootstrap` | `App/Bootstrap.cs` | Composition root. `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`: sets the `GameScene→name` map, wires `SceneStateMachine.OnGameSceneChanged += SceneLoader.LoadScene`. |

**Rules:** features request transitions only via `ChangeSceneTo`; only
`SceneLoader` calls `SceneManager`; anything needing a just-loaded scene's objects
subscribes to `SceneLoader.OnSceneLoaded`, not `OnGameSceneChanged` (the latter
fires before the async load starts). Verify: `SceneManager.` appears only in
`SceneLoader.cs` (and the `Prototypes/SceneSwitcher.cs` scratch file — Appendix A).

---

## 7. Possession & module input

**Status:** Live for one actor. `ActorHost.Awake` now auto-possesses (a `TestPossess`
call marked TODO-remove), so a fresh Gameplay scene *does* have a controlled actor —
reversing the old "nothing is possessed automatically" note. No second possessable
character yet; `Posession.OnPosessionChanged` is declared but never raised.

`Actor` (plain C#, deliberately not a `MonoBehaviour`) is the thing possessed: it
owns a `TagSet`, an `InteractionFocus`, and a list of `IModule`s. Only the
possessed actor is subscribed to input; a non-possessed actor's modules are never
called.

| Component | Location | Responsibility |
|---|---|---|
| `IPosessable` | `Core/Player/IPosessable.cs` | `OnPosessed`/`OnUnposessed`. |
| `Posession` | `Core/Player/Posession.cs` | Static. `Register`/`Unregister` (by `ActorHost`), `Posess(next)` unpossesses current then possesses next, `Available`. |
| `Actor` | `Core/Player/Actor.cs` | Plain C#, `IPosessable`. `Tags`, `Focus`, module list. `OnPosessed` subscribes `Send` to `ModuleInput.OnIntent`. `Send`(input) / `Dispatch`(world, carries a `Transform`) build a `Command` and forward to modules whose `ReactsTo` contains the intent. `GetModule<T>()`. |
| `ActorHost` | `Core/Player/ActorHost.cs` | `MonoBehaviour`, `namespace Core.Player` (the old namespace/asmdef inconsistency is **resolved** — it compiles into `Core` and is named to match). Registers its `Actor` on enable; `[ContextMenu]`+`Awake` `TestPossess`. |
| `IModule` | `Core/Player/IModule.cs` | `ReactsTo` (declared intents), `Tag BlockedBy => Tag.None`, `Handle(owner, cmd)`. |
| `Command` | `Core/Player/Command.cs` | `readonly struct`: `Intent WhatToDo`, `Vector2 ExtraInfo`, `Transform Position` (two ctors — input payload vs world payload). |
| `Intent` | `Core/Player/Intent.cs` | `Move`, `Interact`, `Charge`, `StopCharge`. |
| `TagSet` / `Tag` | `Core/Player/TagSet.cs` | **Now fully implemented** (was empty). `[Flags] Tag {None,Interacting,Charging,Busy}`; `TagSet` is a ref-counted set with `Add`/`Remove`/`HasAny`/`HasAll` + `Added`/`Removed` events. Modules gate on it (`WalkModule`/`InteractionModule` `BlockedBy Interacting|Charging`). |
| `WalkModule` | `Features/Modules/WalkModule.cs` | `MonoBehaviour,IModule`. Finds `ActorHost` via `GetComponentInParent` in `Awake`; self-registers on enable. Reacts to `Move`: rigidbody rotate (`ExtraInfo.x`) + forward/reverse drive (`ExtraInfo.y`) in `FixedUpdate`; blocked while `Interacting`/`Charging`. |

**Rules:** modules never read `ModuleInput` directly — only the possessed `Actor`
does. A module finds its owner via `GetComponentInParent<ActorHost>()`, never an
Inspector reference (drop-in under any character). Verify: `ModuleInput.RaiseX`
invoked only in `InputReader.cs`; `OnIntent` subscribed in `Actor.cs` and
`DwellTracker.cs`.

---

## 8. Input routing — contexts, execution & display projections

**Status:** Live for Player + Cutscene contexts; Menu map exists (always enabled
for UI clicks). One adapter, `InputReader`, turns the action asset into a single
`Intent→InputAction` map and fans it out three ways.

**Three concerns, kept separate:**

- **Context (which map is active)** — `InputRouter` is a context *stack*
  (`Gameplay`/`Cutscene`/`Menu`); `Enter`/`Exit` push/pop and raise
  `ContextChangedTo`. `InputReader.Apply` enables the matching action map. The
  cutscene director drives this (§4).
- **Execution (push)** — `ModuleInput.RaiseMove/RaiseInteract/RaiseStopCharging`
  broadcast `OnIntent(Intent,Vector2)`; the possessed `Actor` receives it (§7).
- **Display (pull)** — `InputGlyphProvider` wraps the same map and registers into
  `GlyphInput`; UI asks by **action-key string** (`"Map/Action"`) for a `Glyph`
  (label/sprite) to draw; actor-verb callers resolve their `Intent`→key via `KeyFor` first.

| Component | Location | Responsibility |
|---|---|---|
| `InputReader` | `Features/Input/InputReader.cs` | The one adapter. Builds the `Intent→InputAction` map from the `Player` map; pumps `Move` each frame, `Interact` on `performed`, `Skip`→`CutsceneInput.RaiseSkip`; constructs+registers the glyph provider; enables/disables maps per context (`Player`/`Cinematic`/`UI`). |
| `ModuleInput` | `Core/Input/ModuleInput.cs` | Static execution transport (`OnIntent`). |
| `InputContext` / `InputRouter` | `Core/Input/InputContext.cs`, `InputRouter.cs` | Enum + static context stack, `ContextChangedTo`, `ActiveContext`. |
| `CutsceneInput` | `Core/Input/CutsceneInput.cs` | Static `SkipCutscene` event (`RaiseSkip`). |
| `GlyphInput` | `Core/Input/GlyphInput.cs` | Static registry holding one `IInputGlyphProvider`. |
| `IInputGlyphProvider` / `Glyph` | `Core/Input/*` | Display seam: `GetGlyph(string actionKey)` + `KeyFor(Intent)` + `DeviceChanged`; `Glyph {label, sprite}` (only `sprite` unwired). Glyphs are keyed by action, not `Intent` — `KeyFor` is the actor-verb→binding bridge. |
| `InputGlyphProvider` | `Features/Input/InputGlyphProvider.cs` | Plain C#. Resolves an action-key string→`Glyph` via `FindAction`+`GetBindingDisplayString` for the active control scheme; caches by key; `KeyFor` bridges `Intent`→key via the intent map; tracks device via `InputSystem.onActionChange`, clears cache + raises `DeviceChanged` on a scheme switch. `IDisposable`. |

**Rules:** the `Intent→InputAction` map lives only in `InputReader`. **Core never
references `UnityEngine.InputSystem`** — verify `InputSystem` has no matches under
`Assets/Scripts/Core`. UI pulls glyphs only from `GlyphInput` (null until
`InputReader.Awake` registers — null-guard and query at show time). Add a verb =
`Intent` + binding + one map line + `Raise…`; the glyph side needs no change.

---

## 9. Interaction & docking

**Status:** Live. Two interactables shipped (`SlidingDoors`, `ChargingStation`).
Interaction is split orthogonally: a **sensor** decides *what* is focused; the
**`Interact` intent** decides *when* to act on it.

| Component | Location | Responsibility |
|---|---|---|
| `IInteractable` | `Core/Interaction/IInteractable.cs` | `CanInteract(actor)`, `OnFocus(hitPoint)`, `OnUnfocus()`, `Interact(actor)`. |
| `InteractionFocus` | `Core/Player/InteractionFocus.cs` | Held by `Actor`. `Current`, `Set`/`Clear` with focus/unfocus callbacks. |
| `InteractionModule` | `Features/Modules/InteractionModule.cs` | `MonoBehaviour,IModule` = the sensor **and** the executor. `Update` does a `SphereCast` from the camera, sets `Actor.Focus`; `Handle(Interact)` calls `Focus.Current.Interact(owner)`. Blocked while `Interacting`/`Charging`. |
| `IDock` / `IChargeable` | `Core/Interaction/IDock.cs`, `Core/Player/IChargeable.cs` | `Dock/UnDock/Docked` ; `StartDocking(dock)`. |
| `ChargingModule` | `Features/Modules/ChargingModule.cs` | `IModule,IChargeable`. `StartDocking` docks the rigidbody + subscribes `Docked`→swap `Interacting`→`Charging` tag; pressing interact while charging → `StopCharge` (undock, clear tag). |
| `ChargingStation` | `Features/Interactables/ChargingStation.cs` | `IInteractable,IDock`. `Interact` → `actor.GetModule<IChargeable>().StartDocking(this)` + adds `Interacting`; `Dock` shows a stop-prompt, raises a static dock camera's depth, coroutine-lerps the body to `dockAnchor`, then fires `Docked`. |
| `SlidingDoors` | `Features/Interactables/SlidingDoors.cs` | `IInteractable`. Toggles an `Animator` bool; shows/hides a prompt (§10); `OnMotionFinished` (animation event) clears `isBusy`. |

**Notes:** the world commands the actor via the same module dispatch — this is
the world-side sibling of input (`Actor.Dispatch` exists for it, though today
`ChargingStation.Interact` calls `StartDocking` directly). Tag mutual-exclusion
(`Interacting`/`Charging`) is what stops walking while docked.
`ModuleInput.RaiseStopCharging` currently has no module reacting to
`Intent.StopCharge` (ChargingModule reacts to `Interact`) — effectively dead;
clean up or wire.

---

## 10. UI prompt & mount channels

**Status:** Live. All UI is driven by SO event channels — no feature references
UI; UI subscribes and queries. (Corrects the old doc's `UIInteractPromptDisplayRequestSO`
name and 3-arg payload — the real channels are below.)

| Component | Location | Responsibility |
|---|---|---|
| `UIPromptDisplayRequestSO` | `Core/Events/UIPromptDisplayRequestSO.cs` | `Show(string text, Intent)` / `Hide()`. Raised by interactables on focus. |
| `UIPromptPositionRequestSO` | `Core/Events/UIPromptPositionRequestSO.cs` | `SetPosition(Vector3)`. |
| `UIElementDisplayRequestSO` | `Core/Events/UIElementDisplayRequestSO.cs` | Generic widget channel `Show/Hide(GameObject prefab)` ("Unreal WidgetChannel"). |
| `UIPrompt` | `Features/UI/UIPrompt.cs` | Listens to a prompt channel; sets label; resolves the glyph for the `Intent` from `GlyphInput` (§8) and refreshes on `DeviceChanged`; fades a `CanvasGroup`. |
| `UIPromptPosition` | `Features/UI/UIPromptPosition.cs` | Places the prompt at `WorldToScreenPoint(hitPoint + offset)` each `LateUpdate`. |
| `UIMountPoint` | `Features/UI/UIMountPoint.cs` | Instantiates/destroys a prefab under a container in response to a `UIElementDisplayRequestSO`. |

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

These are the concrete referents behind `architecture-foundations.md`'s slim/
suction/clean module discussion — kept as design memory, superseded in code by
the Core/Feature stack above.

## Appendix B — Known stale / cleanup TODOs (surfaced by this sweep)

- `ActorHost.Awake → TestPossess()` and `[ContextMenu]` — dev-only auto-possess,
  marked for removal.
- `GameFlow.OnNewGameRequested`/`OnLoadGameRequested` + `LoadGame()` — dead
  (no subscribers/callers). `Posession.OnPosessionChanged` — declared, never raised.
- `ModuleInput.RaiseStopCharging` / `Intent.StopCharge` — raised but unhandled (§9).
- `MyScript.cs` — empty stub, delete.
- `Core` asmdef now references `Unity.Localization` — confirm something in Core
  actually needs it, or drop the reference to keep Core lean.
