# Quests

*`Features/Quests/*` + the read-model contracts in `Core/Quests/*`. Map:
[`README.md`](README.md). The three **views** over this seam (journal list, detail
panel, HUD tracker) live in [`ui.md`](ui.md#the-quest-views) — they're `Features.UI`
and reference only `Core.Quests`.*

**Status:** Live. The `QuestRuntime` brain reacts to `FactChanged` end to end
(start → per-stage setup → objective completion → advance → completed).

## The progression model

*(See `../quest-system-structure.md` §6.)* **Quest progress is itself a fact.**
`quest.{id}.stage` (a counter) is the source of truth — `0` inactive, `1..Length`
active on that stage, `>Length` → write `quest.{id}.completed`. On load the brain
rebuilds from the stage counter; **it never remembers triggers.**

That's what makes save/load free and quest chains trivial: a chain is just a
condition on another quest's stage fact. Nothing derived is authoritative.

## Components

| Component | Location | Responsibility |
|---|---|---|
| `QuestDefinitionSO` | `Features/Quests/QuestDefinitionSO.cs` | `id`, `title`, `Stage[] stages`, `FactCondition[] startConditions`. Nested: `Stage {journalEntry, Objective[] objective, GameObject[] setupPrefabs}`, `Objective {LocalizedString description, FactCondition condition}`. Menu `Cleanbot/Quests/Definition`. |
| `QuestCatalogSO` | `Features/Quests/QuestCatalogSO.cs` | `List<QuestDefinitionSO> quests`. Menu `Cleanbot/Quests/Catalog`. |
| `QuestRuntime` | `Features/Quests/QuestRuntime.cs` | `MonoBehaviour`. Subscribes `WorldState.FactChanged`; `OnEnable` does a catch-up scan. Holds `setupStage` (quest→physically-built stage) and `setupInstances` (quest→spawned `setupPrefabs`); `Evaluate` runs the 3-state machine + reconcile per quest, logging each transition. Non-re-entrant — see the pass loop below. |
| `FactSetterSO` | `Features/Quests/Progression/FactSetterSO.cs` | SO wrapping one `FactCondition` used as a *write*: `Write()` applies it (`FlagIsTrue`→`SetFlag(true)`, etc.). Has no C# callers by design — it is dropped into `UnityEvent`s in the inspector (`SlidingDoors.onOpened`, `UIHoldToConfirm.onCompleted`). Menu `Cleanbot/Quests/Fact Setter`. **Writes silently** and `default:` is a no-op — see README Appendix B. |
| `DwellTracker` | `Features/Quests/Progression/DwellTracker.cs` | Abstract `MonoBehaviour`: accumulates `Time.deltaTime` while `Intensity() > deadzone`, and after `requiredSeconds` does `SetFlag(FactKey, true)` + destroys itself. An *objective-completion writer* — it lives inside a stage's `setupPrefabs`. |
| `AxisInputDwell` | `Features/Quests/Progression/AxisInputDwell.cs` | Concrete `DwellTracker` subclass (its **own file**, not nested). Serialized `axis` (`Vertical`/`Horizontal`); subscribes `ModuleInput.OnIntent`, caches the latest `Move` value, and maps the chosen axis component → `FactKeys.TutorialPlayerMoved`/`TutorialPlayerRotated` through its `FactKey`/`Intensity` overrides. |
| `FactCondition` | `Features/Quests/FactCondition.cs` | The predicate object — documented with the spine in [core](core.md#1-fact-spine). |
| `testquest.cs` | `Features/Quests/testquest.cs` | Dev probe: logs the tracked quest's objectives `[x]`/`[ ]` on every `Changed` — ground truth for what a snapshot actually carries. |

## The brain (implemented)

Per-quest, on every `FactChanged` (key ignored; re-scan, matching the
`null`="everything" convention): read `quest.{id}.stage`; if `0`, promote to `1`
when `startConditions` all met; if `>Length`, write the completed flag (guarded
against re-fire); else **reconcile** — if the built stage ≠ the counter, tear down
old `setupInstances` and instantiate the new stage's `setupPrefabs`; then if all of
the current stage's objectives `IsMet()`, advance the counter.

### The pass loop — why the brain is not re-entrant

`Evaluate` writes facts (`SetCounter(QuestStage)`), and `FactChanged` is synchronous
([core](core.md#rules--gotchas) gotcha 3). Naïvely that means a stage advance opens a
**nested** full sweep of the catalog from inside the outer `foreach`, which can nest
again — stack depth scaling with cascade length, and the outer pass continuing over
quests whose state changed underneath it.

`OnFactChanged` therefore flattens recursion into iteration:

```
needsAnotherPass = true
if (reconciling) return          // a write *during* reconciliation is a request
                                 // for another pass, not a nested run
reconciling = true
try   while (needsAnotherPass) { needsAnotherPass = false; sweep catalog
                                 if (++passes >= 32) { LogError; break } }
finally reconciling = false
```

Three properties worth preserving:

1. **Re-entry is a request, never a run.** Constant stack depth; every pass reads a
   world that is not being mutated by a frame beneath it.
2. **Termination is a fixpoint** — a full sweep that writes nothing. That *is* the
   definition of "the world is consistent" for a rule system.
3. **Non-convergence is detected, not survived.** Two quests toggling each other's
   conditions would otherwise hang Unity inside an event handler with no stack trace
   and no way to break in. The cap exists to *detect*. The current response is
   `LogError` + `break`, which leaves the game running on half-reconciled state;
   throwing instead would stop execution and surface the `SetFlag` that started the
   cascade — the intended direction, not yet applied (README Appendix B).
   `finally` guarantees `reconciling` resets either way, so a throw anywhere in
   `Evaluate` cannot leave the brain permanently deaf to future fact changes.

Same discipline as ordered, phase-based system updates in shipped ECS gameplay
architectures (Overwatch): work discovered mid-frame is queued for a defined later
pass rather than interleaved unpredictably.

### `setupPrefabs` — what a stage is allowed to spawn

`ReconcileSetup` does `Instantiate(prefab)` with **no parent** → the prefab lands at
the **active scene's root** (see the additive-load gotcha in
[core](core.md#6-scene-flow)). Two consequences:

1. **World things are fine** — `AxisInputDwell` trackers are logic-only (position
   irrelevant); a spawned interactable carries its position in its own transform.
2. **UI prefabs are not.** A canvas-less `RectTransform` at world root renders
   nothing. So a stage never spawns a widget directly — it spawns a **requester**
   (`UIPopupRequest`) that asks the mount channel to build the widget inside the
   canvas. `QuestRuntime` stays ignorant of canvases; the stage prefab expresses a
   *wish*. See [ui §13](ui.md#13-popups--modals).
3. **Permanent things are not.** `Teardown` destroys these instances when the stage
   advances, so anything that must outlive the quest — a module grant, an unlocked
   area — belongs in a fact, not here ([modules](modules.md)).

---

## The read-model (journal / HUD seam)

**Status:** Live. This is the **read sibling of `QuestRuntime`**: same catalog, same
`FactChanged` subscription, opposite direction. `QuestRuntime` *writes*
`quest.{id}.stage`; this *reads* it and paints immutable snapshots for the UI.
Because progress is already a Core fact, the UI reaches quest state **through Core**
and never references `Features.Quests` — the RED Engine `CJournalManager` split
(state flows through a mediator; authored text stays owned by the quest module).

*(Naming: drafted as `QuestJournal` / `IQuestJournalSource` / `QuestUIService`; the
shipped names are `QuestInfo` / `IQuestInfoSource` / `QuestInfoPasser` — same roles.
`menu-ui-handoff.md` still uses the old names in places.)*

| Component | Location | Responsibility |
|---|---|---|
| `IQuestInfoSource` | `Core/Quests/IQuestInfoSource.cs` | The UI's entire contract: `Snapshots()`, `Get(id)`, `TrackedId`, `SetTracked(id)`, `Changed`. Pure C# — no `UnityEngine`, no quest types. |
| `QuestSnapshot` (+ `ObjectiveLine`, `QuestStatus`) | `Core/Quests/QuestSnapshot.cs` | Immutable value handed to the UI. Already-resolved `string`s only: `Id`, `Title`, `Status` (`Active`/`Completed`), `StageStory[]` (a journal line per reached stage), `Objectives[]` of `(Text, Completed)`. No `LocalizedString` crosses this line. |
| `QuestInfo` | `Core/Quests/QuestInfo.cs` | Static registry slot (the `GlyphInput` idiom — [core](core.md#the-core-idioms-three-shapes-reused-everywhere)). Holds the one `IQuestInfoSource`, re-exposes a stable `Changed`, forwards reads null-safe. |
| `QuestInfoPasser` | `Features/Quests/QuestInfoPasser.cs` | `MonoBehaviour, IQuestInfoSource`. Builds snapshots from `catalog` + `WorldState` facts; resolves `LocalizedString`→`string`; holds the tracked pin; fires `Changed`. `OnEnable` subscribes `WorldState.FactChanged` + `LocalizationSettings.SelectedLocaleChanged` and `Register`s into `QuestInfo`; `OnDisable` mirrors in reverse. |

**What `Build` projects** (a cross-file invariant the views rely on): `StageStory` =
the journal line of *every reached stage* `0..reached-1`, so its **last** entry is
the active stage's line and earlier entries are past stages. `Objectives` = **only
the current stage's** objectives, each `Completed` = `condition.IsMet()` read live.
A completed quest (`stage > Length`) carries all stage lines and an **empty**
`Objectives`; stage `0` returns `null` and is filtered from `Snapshots()`.

### Rules & gotchas

1. **State through Core, text owned by the feature.** Quest *progress* reaches the UI
   as facts via `WorldState`; only authored *text* lives in `Features.Quests`,
   resolved to plain `string` inside `QuestInfoPasser`. So `Core/Quests/*` is
   **Localization-free** — the boundary speaks resolved text.
2. **Status is derived from the stage counter, identically to `QuestRuntime`** (`0`
   excluded from the journal, `1..Length` active, `>Length` completed). One source of
   truth — journal and runtime cannot disagree.
3. **The tracked pin is late-bound and transient.** `TrackedId` resolves on every
   read: explicit pin if still active, else first active quest, else `null` (HUD
   hides). Not persisted — persisting needs a `WorldState` string accessor (the
   `names` dict, [core](core.md#1-fact-spine) gotcha 5).
4. **`Changed` is coarse** — fired on every `FactChanged` and every locale change; the
   UI re-pulls. Registration order is irrelevant: `QuestInfo` is a facade with a
   stable event, so a widget may subscribe before the service exists.
5. **All three views are pure subscribers** of the same `QuestInfo.Changed` —
   always-mounted, they re-pull on every fire and never talk to each other.
