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
| `WorldState` | `Core/SaveSystem/WorldState.cs` | Static mediator over the one in-memory `SaveData`. Typed `GetFlag/SetFlag`, `GetCounter/SetCounter/AddToCounter`. Every setter fires `FactChanged(key)`. Owns JSON file I/O (`Save`/`Load`/`NewSave`; `Load`/`NewSave` fire `FactChanged(null)` = "everything changed"). `SaveExists`. |
| `SaveData` | `Core/SaveSystem/SaveData.cs` | `internal` plain container. Dictionaries by value-shape: `flags:bool`, `counters:int`, `reactions:float`, `names:string`, `positions:Vector3`, `attributeValues:float`; lists `ownedModuleId`/`skillId`; character + version + `inGameTimeSeconds` + `savedAt`. Only `flags`/`counters` have accessors on `WorldState` today. |
| `FactKeys` | `Core/SaveSystem/FactKeys.cs` | **The single string authority.** Builders `CutsceneFinished(id)`→`cutscene.{id}.finished`, `QuestStage(id)`→`quest.{id}.stage`, `QuestCompleted(id)`→`quest.{id}.completed`; consts `TutorialPlayerMoved`="tutorial.moved", `TutorialPlayerRotated`="tutorial.rotated". Carries `[FactKeySource]` (metadata for the editor registry — [editor-tooling](editor-tooling.md)). |
| `FactCondition` | `Features/Quests/FactCondition.cs` | Serializable `{factKey, FactTest test, int value}` + `IsMet()`. `FactTest` = `FlagIsTrue`/`FlagIsFalse`/`CounterAtLeast`. The specification/predicate object; pulls from `WorldState`, never pushed to. *(Lives in `Features.Quests`, not Core — the one wrinkle in the spine's location.)* |

### Who writes / reads facts today

- **Writers:** `CutsceneDirector` (`SetFlag(CutsceneFinished)`), `DwellTracker`
  (`SetFlag(Tutorial…)`), `GameFlow.StartNewGame`→`NewSave`, `TestButton`→`Save`,
  `QuestRuntime` (`SetCounter(QuestStage)` on start/advance, `SetFlag(QuestCompleted)`
  on finish).
- **Readers:** `FactCondition.IsMet` (`GetFlag`/`GetCounter`), `CutsceneDirector`
  (`GetFlag(CutsceneFinished)` for play-once gating).
- **`FactChanged` subscribers:** `QuestRuntime.OnFactChanged`, `QuestInfoPasser`.

### Rules & gotchas

1. **`SaveData` is `internal`** — nothing outside `Core/SaveSystem/` can compile a
   reference to it. Enforced by the compiler.
2. **Keys are never hand-typed.** Reader and writer both call the same `FactKeys`
   method. Key strings *are* the save schema — `FactKeys` is append-only; renaming a
   key breaks saves.
3. **`FactChanged` is synchronous.** A `SetFlag`/`SetCounter` inside a `FactChanged`
   handler re-enters all handlers before the setter returns. Fine and idempotent at
   this scale; mind it when `QuestRuntime` advances a stage from inside its own
   handler (it recurses into the next stage).
4. **`currentSaveData` starts null.** `GetFlag`/`GetCounter` before a
   `NewSave()`/`Load()` will NPE. `GameFlow.StartNewGame` calls `NewSave` first;
   entering Gameplay by any path that skips it (e.g. `TestButton.OnGoToGameplayButton`)
   leaves the store null. Something must seed it.
5. Add a new accessor pair on `WorldState` (mirroring `GetFlag`/`SetFlag`) the first
   time a feature needs `reactions`/`names`/`positions`/`attributeValues` — don't
   expose all of them up front.

---

## 5. Session intent & new-game flow

**Status:** Live for New Game (drives the intro end to end). `Continue`/`None` are
stubs. `GameFlow.OnNewGameRequested`/`OnLoadGameRequested` fire but have no
subscribers (dead); `GameFlow.LoadGame` has no callers.

Entry intent is carried as **data, not a live object**. `GameSession` (SO) holds a
transient `EntryMode`; the Title button writes it, the Gameplay scene reads it once
and acts — so the "new game started" broadcast is raised only *after* gameplay
listeners exist.

| Component | Location | Responsibility |
|---|---|---|
| `GameSession` | `Core/SceneControls/GameSession.cs` | SO. `EntryMode {None,NewGame,Continue}`; `Request(mode)` (menu), `Consume()` (read-and-clear, gameplay), `OnEnable` resets to `None`. Menu `Cleanbot/App/GameSession`. |
| `GameplayBootstrap` | `App/GameplayBootstrap.cs` | `MonoBehaviour` in Gameplay. `Start`: `switch(session.Consume())` → NewGame: `InitializeNewGame()` (stub) + `newGameStarted.RaiseAction()`; Continue: `LoadSavedGame()` (stub); None: nothing. |
| `StartNewGameButton` | `Features/Title/StartNewGameButton.cs` | `Request(NewGame)` then `GameFlow.StartNewGame()`. |
| `GameFlow` | `Core/SceneControls/GameFlow.cs` | Static. `StartNewGame`: `WorldState.NewSave()` + (dead) `OnNewGameRequested` + `ChangeSceneTo(Gameplay)`. |
| `TestButton` | `Features/Title/TestButton.cs` | Dev harness: `StartNewGame` (no session intent → intro won't play), `OnGoToGameplayButton`, `OnSwitchLocale`, `OnSaveTestButton`→`WorldState.Save()`. |

**Timing invariant:** the raise happens in `GameplayBootstrap.Start`; listeners
(`CutsceneDirector`) subscribe in `OnEnable`. Unity runs all `OnEnable` before any
`Start`, so nothing is missed — **keep the raise in `Start`**. Two wires must point
at the *same* asset: the `GameSession` on button + bootstrap, and the `VoidEventSO`
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
