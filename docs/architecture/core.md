# Core

*The `Core` assembly (+ `App`, the composition root). Everything here is
referenced by Features and references nothing above itself. Map: [`README.md`](README.md).*

Covers: the fact spine · session intent & new-game · scene flow · possession &
module input · the Core idioms every feature leans on.

Input contexts live in `Core/Input` but only make sense with their adapter —
they're documented in [`input.md`](input.md). The quest read-model contracts live
in `Core/Quests` but are quest-shaped — [`quests.md`](quests.md).

---

## The Core idioms (three shapes, reused everywhere)

Worth naming up front, because the same three shapes account for nearly every
cross-system connection in the game:

1. **Static channel** — `ModuleInput`, `CutsceneInput`, `MenuInput`. A static class
   holding `event`s plus `RaiseX()` methods. One class **per input map**, not per
   action (`ModuleInput` carries Move *and* Interact *and* StopCharging; `MenuInput`
   carries ToggleMenu *and* ConfirmDown/Up). Drawing them per-action would fan the
   uses-graph out with every new button.
2. **Static registry slot** — `GlyphInput`, `QuestInfo`. A static facade holding
   *one* implementation, registered at runtime, re-exposing a stable event and
   forwarding reads null-safe. Decouples subscriber lifetime from service lifetime:
   a widget may subscribe before the service exists.
3. **SO event channel** — `Core/Events/*RequestSO.cs`. A `ScriptableObject` asset
   carrying `event` + `Raise`. Because it's an *asset*, both a prefab on disk and a
   runtime-spawned object can point at the same one — which is exactly what a plain
   C# event or a scene reference cannot do. See the seam-choice rule in
   [`ui.md`](ui.md#which-seam-when).

`InputRouter` is a fourth shape and worth its own note: **a broadcast bus, not a
pipe.** Anyone pushes a context; anyone reacts. That's why the tutorial popup gets
time-freeze *and* menu-suppression for free without knowing either exists.

### `Core/Events` channels

| Channel | Payload | Raised by |
|---|---|---|
| `VoidEventSO` | — (`RaiseAction()`) | Cutscene triggers/chains, `GameplayBootstrap.newGameStarted`. `RaiseAction` is public/void/no-args, so it can be dropped **straight into a `Button.onClick`**. |
| `UIPromptDisplayRequestSO` | `Show(string, Intent)` / `Hide()` | Interactables on focus |
| `UIPromptPositionRequestSO` | `SetPosition(Vector3)` | Interactables |
| `UIElementDisplayRequestSO` | `Show/Hide(GameObject)` | `UIPopupRequest`, popup close doors |

---

## 1. Fact spine

**Status:** Live and load-bearing — the busiest seam in the game.

### Components

| Component | Location | Responsibility |
|---|---|---|
| `WorldState` | `Core/SaveSystem/WorldState.cs` | Static mediator over the one in-memory `SaveData`. Typed `GetFlag/SetFlag`, `GetCounter/SetCounter/AddToCounter`, `TryGetPosition/SetPosition`. Flag/counter setters fire `FactChanged(key)`; **position setters deliberately do not** (see gotcha 6). Owns JSON file I/O (`Save`/`Load`/`NewSave`; `Load`/`NewSave` fire `FactChanged(null)` = "everything changed"). `Load()` returns `bool` and swallows a missing/corrupt file. `SaveExists`. |
| `SaveData` | `Core/SaveSystem/SaveData.cs` | `internal` plain container, **engine-type-free by rule** — it is the on-disk wire format. Dictionaries by value-shape: `flags:bool`, `counters:int`, `reactions:float`, `names:string`, `positions:SaveVec3`, `attributeValues:float`; list `skillId`; character + version + `inGameTimeSeconds` + `savedAt`. All collections initialised at declaration so a file written by an older build deserialises missing fields to empty, not `null`. |
| `SaveVec3` | `Core/SaveSystem/SaveData.cs` | `internal struct {float x,y,z}` + `ToVector3()`. Exists because Newtonsoft serialises `Vector3`'s `normalized`/`magnitude` **properties** — bloat at best, self-referencing loop at worst. `Vector3` stops at the `WorldState` API boundary. |
| `FactKeys` | `Core/SaveSystem/FactKeys.cs` | **The single string authority.** Builders `CutsceneFinished(id)`→`cutscene.{id}.finished`, `QuestStage(id)`→`quest.{id}.stage`, `QuestCompleted(id)`→`quest.{id}.completed`, `ModuleOwned(id)`→`module.{id}.owned`; consts `TutorialPlayerMoved`="tutorial.moved", `TutorialPlayerRotated`="tutorial.rotated". Carries `[FactKeySource]` (metadata for the editor registry — [editor-tooling](editor-tooling.md)). |
| `FactCondition` | `Features/Quests/FactCondition.cs` | Serializable `{factKey, FactTest test, int value}` + `IsMet()`. `FactTest` = `FlagIsTrue`/`FlagIsFalse`/`CounterAtLeast`. The specification/predicate object; pulls from `WorldState`, never pushed to. *(Lives in `Features.Quests`, not Core — the one wrinkle in the spine's location.)* |

### Who writes / reads facts today

- **Writers:** `CutsceneDirector` (`SetFlag(CutsceneFinished)`), `DwellTracker`
  (`SetFlag(Tutorial…)`), `GameFlow.Begin`→`NewSave`/`Load`, `TestButton`→`Save`,
  `QuestRuntime` (`SetCounter(QuestStage)` on start/advance, `SetFlag(QuestCompleted)`
  on finish), `FactSetterSO.Write` (**inspector-wired** — invoked from `UnityEvent`s
  such as `SlidingDoors.onOpened` and `UIHoldToConfirm.onCompleted`, never from C#).
- **Readers:** `FactCondition.IsMet` (`GetFlag`/`GetCounter`), `CutsceneDirector`
  (`GetFlag(CutsceneFinished)` for play-once gating), `ModuleLoadout`
  (`GetFlag(ModuleOwned)` — [modules](modules.md)).
- **`FactChanged` subscribers:** `QuestRuntime.OnFactChanged`, `QuestInfoPasser`,
  `ModuleLoadout.OnFactChanged`.

**Grep will not find the `FactSetterSO` writers.** They are persistent `UnityEvent`
calls living in prefab/scene YAML, so the data→behaviour bridge is invisible to
search — and renaming or duplicating a prefab can silently detach one. Components
that bridge from inspector data into behaviour are exactly the ones that should
announce themselves in the log; `FactSetterSO` currently does not (Appendix B).

### Rules & gotchas

1. **`SaveData` is `internal`** — nothing outside `Core/SaveSystem/` can compile a
   reference to it. Enforced by the compiler.
2. **Keys are never hand-typed.** Reader and writer both call the same `FactKeys`
   method. Key strings *are* the save schema — `FactKeys` is append-only; renaming a
   key breaks saves.
3. **`FactChanged` is synchronous.** A `SetFlag`/`SetCounter` inside a `FactChanged`
   handler re-enters all handlers before the setter returns. Consumers that can
   *cause* fact writes must therefore not be re-entrant — `QuestRuntime` flattens this
   into a pass loop ([quests](quests.md#the-brain-implemented)). A consumer that only
   reads (`ModuleLoadout`, `QuestInfoPasser`) needs no such guard.
4. **The store is never null.** `WorldState.Data` lazily creates an empty `SaveData`
   on first access, so a consumer that wakes before `Load()`/`NewSave()` reads
   defaults instead of throwing. This is a safety net, not a licence: a late load
   still fires `FactChanged(null)` and reconciling consumers self-correct, but
   **one-shot side effects cannot be un-fired** (a cutscene that already started
   playing). State must still be prepared before the scene loads — see §5.
5. **Ids inside keys are a save-file contract.** `module.{id}.owned`,
   `quest.{id}.stage` and friends embed asset ids in the on-disk schema. Rename the
   asset freely; freeze the `id` field once a build has been played. `FactKeys` is
   append-only.
6. **Positions are state, not facts.** `SetPosition` does not raise `FactChanged`:
   nothing branches on a position, and routing continuous values through the fact bus
   would re-run every quest evaluation and every loadout reconcile per write. Keep
   high-frequency simulation values off the bus — the same rule will apply to dust
   coverage.
7. Add a new accessor pair on `WorldState` (mirroring `GetFlag`/`SetFlag`) the first
   time a feature needs `reactions`/`names`/`attributeValues` — don't expose all of
   them up front.
8. **All file I/O is confined to `Save()`/`Load()`.** Nothing else in `Core` touches
   `File`/`Path`. Keep it that way — it is what would make a second storage backend
   a local change.

---

## 5. Session intent & new-game flow

**Status:** Live for New Game *and* Continue. `None` (entering Gameplay directly
from the editor) is deliberately inert — see below.

Entry intent is carried as **data, not a live object**. `GameSession` (SO) holds a
transient `EntryMode`; the Title button writes it, the Gameplay scene reads it once
and acts — so the "new game started" broadcast is raised only *after* gameplay
listeners exist. This is the Unreal `GameInstance` shape: the data outlives the
scene load, so producer and consumer never have to be alive simultaneously.

### Three responsibilities, deliberately separated

| Concern | Where | Why there |
|---|---|---|
| **Intent** — "the player chose Continue" | `GameSession` | must survive the scene load |
| **State preparation** — "therefore the save must exist" | `GameFlow.Begin` | must happen **before** the gameplay scene's objects wake |
| **Execution** — "therefore play the awakening" | `GameplayBootstrap` | needs in-scene listeners to exist |

Collapsing any two of these is the bug that was here before: `GameFlow` called
`NewSave()` while `GameplayBootstrap.InitializeNewGame()` sat empty, and
`StartNewGameButton` called both `Request()` and `StartNewGame()`.

| Component | Location | Responsibility |
|---|---|---|
| `GameSession` | `Core/SceneControls/GameSession.cs` | SO. `EntryMode {None,NewGame,Continue}`; `Request(mode)` (menu), `Consume()` (read-and-clear, gameplay), `OnEnable` resets to `None`. Menu `Cleanbot/App/GameSession`. |
| `GameFlow` | `Core/SceneControls/GameFlow.cs` | Static. **One transaction:** `Begin(session, mode)` → prepare `WorldState` (`Load()` for Continue, `NewSave()` for NewGame) → `session.Request(mode)` → `ChangeSceneTo(Gameplay)`, in that order. If `Load()` fails it **rewrites `mode` to `NewGame`** before recording intent, so the downstream scene plays the awakening rather than dropping into a blank world. |
| `GameplayBootstrap` | `App/GameplayBootstrap.cs` | `MonoBehaviour` in Gameplay. `Start`: `switch(session.Consume())` → NewGame: `newGameStarted.RaiseAction()`; Continue: nothing (state already prepared by `GameFlow`); None: nothing. |
| `StartNewGameButton` | `Features/Title/StartNewGameButton.cs` | `GameFlow.Begin(session, NewGame)`. One call. |
| `LoadGameButton` | `Features/Title/LoadGameButton.cs` | `GameFlow.Begin(session, Continue)`. `OnEnable` sets `button.interactable = WorldState.SaveExists` (re-checked on enable, so returning to Title after a save makes it live). |
| `TestButton` | `Features/Title/TestButton.cs` | Dev harness: `OnGoToGameplayButton`, `OnSwitchLocale`, `OnSaveTestButton`→`WorldState.Save()`. |

**Why state prep must precede the transition.** `SceneLoader` loads additively and
asynchronously: every object in the new scene runs `Awake`/`OnEnable` before
`loadOperation.completed` fires, and Unity guarantees no ordering *between* objects.
Preparing `WorldState` while still in Title sidesteps the race entirely — there is no
window in which `QuestRuntime` or `CutsceneDirector` can read an empty world and
start a cutscene that a later load cannot un-play.

**`EntryMode.None` is not wired.** Pressing Play directly on the Gameplay scene has
no prelude, so it would need its own load in `Awake` plus a Script Execution Order
entry to beat every other `OnEnable`. Deliberately skipped: testing goes through the
Title scene. If the editor shortcut is ever wanted, that is the shape it must take —
`Start` is too late.

**Timing invariant:** the raise happens in `GameplayBootstrap.Start`; listeners
(`CutsceneDirector`) subscribe in `OnEnable`. Unity runs all `OnEnable` before any
`Start`, so nothing is missed — **keep the raise in `Start`**. Two wires must point
at the *same* asset: the `GameSession` on buttons + bootstrap, and the `VoidEventSO`
on bootstrap's `newGameStarted` + the intro def's `eventTrigger`.

---

## 6. Scene flow

**Status:** Live. Title↔Gameplay, additive load/unload, behind one vocabulary; no
feature touches `SceneManager` directly.

| Component | Location | Responsibility |
|---|---|---|
| `SceneStateMachine` | `Core/SceneControls/SceneStateMachine.cs` | Static vocabulary. `GameScene {Title,Gameplay}`, `CurrentGameScene`, `ChangeSceneTo(next)`, `event OnGameSceneChanged(from,to)` (fires the instant a transition is *requested*). |
| `SceneLoader` | `Core/SceneControls/SceneLoader.cs` | Static interpreter. `Initialize(map)` (by Bootstrap), `LoadScene(from,to)` loads `to` additively + unloads `from` on completion, then fires `OnSceneLoaded(to)` (after the new scene's `OnEnable`s). |
| `Bootstrap` | `App/Bootstrap.cs` | Composition root. `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`: sets the `GameScene→name` map, wires `SceneStateMachine.OnGameSceneChanged += SceneLoader.LoadScene`. |

**Rules:** features request transitions only via `ChangeSceneTo`; only `SceneLoader`
calls `SceneManager`; anything needing a just-loaded scene's objects subscribes to
`SceneLoader.OnSceneLoaded`, **not** `OnGameSceneChanged` (the latter fires before
the async load starts). Verify: `SceneManager.` appears only in `SceneLoader.cs`
(and the scratch `Prototypes/SceneSwitcher.cs` — README Appendix A).

**Gotcha:** parentless `Instantiate` lands in the **active** scene. Since Gameplay
is loaded additively, that matters for anything spawned at world root — e.g. quest
`setupPrefabs` ([quests](quests.md)).

---

## 7. Possession & module input

**Status:** Live for one actor. `ActorHost.Awake` auto-possesses (a `TestPossess`
call marked TODO-remove), so a fresh Gameplay scene *does* have a controlled actor.
No second possessable character yet; `Posession.OnPosessionChanged` is declared but
never raised.

`Actor` (plain C#, deliberately **not** a `MonoBehaviour`) is the thing possessed:
it owns a `TagSet`, an `InteractionFocus`, and a list of `IModule`s. Only the
possessed actor is subscribed to input; a non-possessed actor's modules are never
called.

| Component | Location | Responsibility |
|---|---|---|
| `IPosessable` | `Core/Player/IPosessable.cs` | `OnPosessed`/`OnUnposessed`. |
| `Posession` | `Core/Player/Posession.cs` | Static. `Register`/`Unregister` (by `ActorHost`), `Posess(next)` unpossesses current then possesses next, `Available`. |
| `Actor` | `Core/Player/Actor.cs` | Plain C#, `IPosessable`. `Tags`, `Focus`, module list. `OnPosessed` subscribes `Send` to `ModuleInput.OnIntent`. `Send`(input) / `Dispatch`(world, carries a `Transform`) build a `Command` and forward to modules whose `ReactsTo` contains the intent. `GetModule<T>()`. |
| `ActorHost` | `Core/Player/ActorHost.cs` | `MonoBehaviour`, `namespace Core.Player`. Registers its `Actor` on enable; `[ContextMenu]`+`Awake` `TestPossess`. |
| `IModule` | `Core/Player/IModule.cs` | `ReactsTo` (declared intents), `Tag BlockedBy => Tag.None`, `Handle(owner, cmd)`. |
| `Command` | `Core/Player/Command.cs` | `readonly struct`: `Intent WhatToDo`, `Vector2 ExtraInfo`, `Transform Position` (two ctors — input payload vs world payload). |
| `Intent` | `Core/Player/Intent.cs` | `Move`, `Interact`, `Charge`, `StopCharge`. |
| `TagSet` / `Tag` | `Core/Player/TagSet.cs` | `[Flags] Tag {None,Interacting,Charging,Busy}`; `TagSet` is a ref-counted set with `Add`/`Remove`/`HasAny`/`HasAll` + `Added`/`Removed` events. Modules gate on it. |
| `WalkModule` | `Features/Modules/WalkModule.cs` | `MonoBehaviour,IModule`. Finds `ActorHost` via `GetComponentInParent` in `Awake`; self-registers on enable. Reacts to `Move`: rigidbody rotate (`ExtraInfo.x`) + forward/reverse drive (`ExtraInfo.y`) in `FixedUpdate`; blocked while `Interacting`/`Charging`. |

**Rules:** modules never read `ModuleInput` directly — only the possessed `Actor`
does. A module finds its owner via `GetComponentInParent<ActorHost>()`, never an
Inspector reference (drop-in under any character). Verify: `ModuleInput.RaiseX`
invoked only in `InputReader.cs`; `OnIntent` subscribed in `Actor.cs` and
`DwellTracker.cs`.
