# Scene Flow Architecture

**Status:** Implemented
**Scope:** Title ↔ Gameplay scene transitions

## 1. Overview

Scene transitions are split into three components, each with a single
responsibility. Features request transitions through a shared vocabulary and
never interact with Unity's scene system directly.

## 2. Components

| Component | Location | Responsibility | Used by |
|---|---|---|---|
| `SceneStateMachine` | `Core/SceneStateMachine.cs` | Defines the transition vocabulary: current scene state, transition request method, transition event | Features, `SceneLoader` |
| `SceneLoader` | `Core/SceneLoader.cs` | Loads/unloads Unity scenes via `SceneManager` in response to transitions | `Bootstrap` only |
| `Bootstrap` | `App/Bootstrap.cs` (asmdef `App`, namespace `Bootstrap`) | Composition root. `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` configures the scene name map and connects `SceneStateMachine` to `SceneLoader` at startup | — |

## 3. API

| Method / Event | Owner | Purpose |
|---|---|---|
| `SceneStateMachine.ChangeSceneTo(GameScene next)` | `SceneStateMachine` | Request a transition to `next` |
| `SceneStateMachine.OnGameSceneChanged(from, to)` | `SceneStateMachine` | Raised when a transition is requested |
| `SceneLoader.Initialize(map)` | `SceneLoader` | Set the `GameScene → scene name` map (called once, by Bootstrap) |
| `SceneLoader.LoadScene(from, to)` | `SceneLoader` | Load `to` additively, unload `from` on completion |
| `SceneLoader.OnSceneLoaded(GameScene to)` | `SceneLoader` | Raised once `to` has actually finished loading (from inside `loadOperation.completed`) — fires after the new scene's `MonoBehaviour`s have already run `OnEnable` |

## 4. Rules

1. Features request transitions only via `SceneStateMachine.ChangeSceneTo`.
2. No code outside `Core/SceneLoader.cs` may call `UnityEngine.SceneManagement.SceneManager`.
3. `Bootstrap` is the only component permitted to call `SceneLoader` directly.
4. Anything that needs to act only once a target scene's objects exist (e.g. finding a `MonoBehaviour` that just got loaded) must subscribe to `SceneLoader.OnSceneLoaded`, not `SceneStateMachine.OnGameSceneChanged`. The latter fires synchronously the instant a transition is *requested*, before the async load has even started — *historically*, `CutsceneDirector` firing into a scene whose `CutscenePlayer` didn't exist yet came from exactly this confusion. Both have since been refactored away; the current gameplay-side trigger (`GameplayBootstrap`) instead relies on the all-`OnEnable`-before-all-`Start` ordering *within* the loaded scene — see *Session Intent & New-Game Flow* §4.

## 5. Verification

Run before committing changes that touch scene flow:

```
grep -rn "SceneManager\." Assets/Scripts --include=*.cs
```

Expected result: matches only in `Core/SceneLoader.cs`.

## 6. Usage example

```csharp
// Feature code requesting a transition
SceneStateMachine.ChangeSceneTo(GameScene.Gameplay);
```

## 7. Assembly dependencies

| Assembly | References |
|---|---|
| `Core` | none |
| `App` (namespace `Bootstrap`) | `Core` |
| `Features.*` | `Core` |

---

# Save System Architecture

**Status:** Implemented — `flags` only (other dictionaries exist but have no accessors yet). Note: the typed flag Get/Set API currently has **no caller** — the cutscene director was its only user and stopped using it when play-once gating was removed. `NewSave`/`Save` are the only live entry points today.
**Scope:** typed save-game persistence (JSON to disk)

## 1. Overview

World/save state lives in one mediator, `WorldState`. It owns the only
instance of `SaveData` in memory and is the only thing allowed to read or
write it. Features never see `SaveData` itself — they call `WorldState`'s
narrow, typed Get/Set API.

## 2. Components

| Component | Location | Responsibility | Used by |
|---|---|---|---|
| `WorldState` | `Core/SaveSystem/WorldState.cs` | Mediator: owns the current `SaveData` in memory, exposes typed Get/Set API, owns JSON file I/O | `GameFlow` (`NewSave`), `TestButton` (`Save`); typed flag Get/Set API currently has **no caller** |
| `SaveData` | `Core/SaveSystem/SaveData.cs` | `internal` plain data container — value-shape-typed dictionaries (`flags: bool`, `counters: int`, `reactions: float`, `names: string`, `positions: Vector3`, `attributeValues: float`) plus `ownedModuleId`/`skillId` lists and character/version/timestamp fields | `WorldState` only |

## 3. API

| Method | Owner | Purpose |
|---|---|---|
| `WorldState.GetFlag(string key)` / `SetFlag(string key, bool value)` | `WorldState` | Typed bool flag read/write |
| `WorldState.Save()` | `WorldState` | Serializes the current `SaveData` to `Application.persistentDataPath/save.json` (Newtonsoft Json) |
| `WorldState.Load()` | `WorldState` | Deserializes `SaveData` back from that file |
| `WorldState.NewSave()` | `WorldState` | Replaces the current `SaveData` with a fresh, empty one — called by `GameFlow.StartNewGame` |

## 4. Rules

1. `SaveData` is `internal` — code outside `Core/SaveSystem/` cannot even compile a reference to it. Enforced by the compiler, not just convention.
2. Features build their own save keys via small per-feature static classes shaped `"category.id.field"` (e.g. `Features/Cutscenes/CutsceneSaveKeys.Played(id)` → `"cutscene.{id}.played"`) — `Core` never knows feature category names. (That `CutsceneSaveKeys` example still exists but currently has **no caller** — see *Cutscene System* §2.)
3. Only `flags` has Get/Set methods on `WorldState` today. `counters`/`reactions`/`names`/`positions`/`attributeValues` exist in `SaveData` but are unused — add a same-shaped accessor pair on `WorldState` (mirroring `GetFlag`/`SetFlag`) the first time a feature actually needs one, rather than exposing all of them up front.

## 5. Verification

```
grep -rn "SaveData" Assets/Scripts --include=*.cs
```

Expected result: matches only in `Core/SaveSystem/WorldState.cs` and `Core/SaveSystem/SaveData.cs` itself.

## 6. Usage example

```csharp
// The typed flag API — shape only; no feature calls it today. The cutscene
// director used this before play-once gating was removed:
if (WorldState.GetFlag(someKey)) return;
WorldState.SetFlag(someKey, true);

// Live callers today:
WorldState.NewSave();   // GameFlow.StartNewGame
WorldState.Save();      // TestButton (dev harness)
```

## 7. Assembly dependencies

`Core/SaveSystem` is a subfolder of `Core`, not a separate assembly — it
compiles under `Core.asmdef` (no references) like the rest of Core.

---

# Session Intent & New-Game Flow Architecture

**Status:** Implemented — a **New Game** press drives the intro cutscene end
to end. `Continue` and `None` branches are stubs (`LoadSavedGame` /
`InitializeNewGame` are empty). `GameFlow`'s `OnNewGameRequested` /
`OnLoadGameRequested` events still fire but have **no subscribers** (dead), and
`GameFlow.LoadGame()` has no callers.
**Scope:** how "which way did we enter gameplay" (New Game vs Continue) crosses
the Title→Gameplay scene boundary and fans out to gameplay systems (today: the
intro cutscene).

## 1. Overview

Entry intent is carried as **data, not a live object**. `GameSession` (a
ScriptableObject) holds a transient `EntryMode`; the Title menu writes it, the
Gameplay scene reads it once and acts. Because the reader lives on the gameplay
side, the "new game started" broadcast (`newGameStarted`, a `VoidEventSO`) is
raised only *after* the Gameplay scene and its listeners exist — the fix for the
listener-lifetime bug that firing an event straight from the menu button would
have caused.

`GameFlow` (pre-existing, static) still owns the mechanical transition —
`WorldState.NewSave()` + `SceneStateMachine.ChangeSceneTo`. The button calls
*both*: `GameSession.Request(NewGame)` for intent, `GameFlow.StartNewGame()` for
the save-reset + scene change. Intent and transition are two concerns on two
objects.

## 2. Components

| Component | Location | Responsibility | Used by |
|---|---|---|---|
| `GameSession` | `Core/SceneControls/GameSession.cs` | SO carrying `EntryMode {None,NewGame,Continue}`. `Request(mode)` writes it (menu side); `Consume()` reads-and-resets it (gameplay side, consume-once); `OnEnable` resets to `None` for editor hygiene. `PendingEntry` is a read-only getter for inspector visibility. Menu: `Cleanbot/App/GameSession` | `StartNewGameButton`, `GameplayBootstrap` |
| `VoidEventSO` | `Core/Events/VoidEventSO.cs` | Payload-less SO event channel: `event Action Raised` + `RaiseAction()`. The reusable "something happened" broadcast primitive. Menu: `Cleanbot/Events/VoidEventSO` | `GameplayBootstrap` (raises `newGameStarted`), `CutsceneDirector` (subscribes via each definition's `trigger`) |
| `GameplayBootstrap` | `App/GameplayBootstrap.cs` (namespace `Bootstrap`) | `MonoBehaviour` in the Gameplay scene. On `Start`, switches on `session.Consume()` — `NewGame`: `InitializeNewGame()` (stub) + `newGameStarted.RaiseAction()`; `Continue`: `LoadSavedGame()` (stub); `None`: nothing | — |
| `StartNewGameButton` | `Features/Title/StartNewGameButton.cs` | `MonoBehaviour` on the New Game button. `StartNewGame()` (wired via the Button's inspector `onClick`, not `AddListener`): `gameSession.Request(NewGame)` then `GameFlow.StartNewGame()` | — |
| `GameFlow` | `Core/SceneControls/GameFlow.cs` | Static. `StartNewGame()`: `WorldState.NewSave()` + fires (dead) `OnNewGameRequested` + `ChangeSceneTo(Gameplay)`. `LoadGame()`: fires (dead) `OnLoadGameRequested` + `ChangeSceneTo(Gameplay)` — no callers | `StartNewGameButton`, `TestButton` |
| `TestButton` | `Features/Title/TestButton.cs` | Dev-only harness (locale switch, `WorldState.Save()` test, scene nav). Its `StartNewGame()` calls `GameFlow.StartNewGame()` **without** setting `GameSession` intent — so entering gameplay via `TestButton` leaves intent `None` and the intro does **not** play | — |

## 3. The flow

```
StartNewGameButton.StartNewGame()          [Title scene, Button.onClick]
  ├─ gameSession.Request(EntryMode.NewGame)            // leave the note
  └─ GameFlow.StartNewGame()
       ├─ WorldState.NewSave()
       ├─ OnNewGameRequested?.Invoke()                 // dead — no subscribers
       └─ SceneStateMachine.ChangeSceneTo(Gameplay)    // → SceneLoader loads Gameplay

Gameplay scene finishes loading
  ├─ CutsceneDirector.OnEnable()   subscribes to newGameStarted.Raised (via NewGameIntro.trigger)
  └─ GameplayBootstrap.Start()     session.Consume() == NewGame
       ├─ InitializeNewGame()                          // stub
       └─ newGameStarted.RaiseAction() ─────────────►  CutsceneDirector plays NewGameIntro
```

## 4. Rules

1. **`GameSession` is consume-once.** `Consume()` reads and clears in one call; `OnEnable` also resets to `None`. A second read yields `None`. This — not a saved flag — is what stops the intro replaying on a later scene reload.
2. **Timing is safe by lifecycle order.** The raise happens in `GameplayBootstrap.Start`; listeners subscribe in `OnEnable`. Unity runs *all* `OnEnable` before *any* `Start` within a loaded scene, so the director is subscribed before the intro fires. Keep the raise in `Start` (not `Awake`/`OnEnable`) and no sticky/replay channel is needed.
3. **Two wires must point at the *same* asset** (silent failure otherwise): the same `GameSession` asset in `StartNewGameButton.gameSession` and `GameplayBootstrap.session`; the same `VoidEventSO` asset in `GameplayBootstrap.newGameStarted` and the `NewGameIntro` definition's `trigger`.
4. **`GameFlow` coexists with `GameSession` deliberately, but its events are dead.** `GameFlow` provides `NewSave` + the scene change; `GameSession` carries the gameplay-side intent. `OnNewGameRequested` / `OnLoadGameRequested` currently have no subscribers — the gameplay-side signal is `newGameStarted`, not these. Either wire them or delete them; right now they are noise.
5. **`GameplayBootstrap` Continue/None branches are stubs** — the load-game path isn't implemented.

## 5. Verification

```
grep -rn "RaiseAction\|\.Raised" Assets/Scripts --include=*.cs
```

Expected: `newGameStarted` raised only in `GameplayBootstrap.cs`; `Raised` subscribed to only in `CutsceneDirector.cs` (plus the `VoidEventSO` type itself).

## 6. Assembly dependencies

| Assembly | References |
|---|---|
| `Core` (`Core/SceneControls`, `Core/Events`) | none |
| `App` (folder `App/`, asmdef `App`, namespace `Bootstrap`) | `Core` — no reference to any Feature; it reaches Cutscenes only *indirectly*, by raising a `VoidEventSO` the Cutscenes assembly listens to |
| `Features.Title` | `Core`, `Unity.Localization` |

---

# Cutscene System Architecture

**Status:** Implemented and **data-driven**. Each cutscene declares its own
trigger event; `CutsceneDirector` subscribes to all of them and plays on raise.
Adding a cutscene needs **no code change**. Play-once gating has been
**removed** — the director no longer touches the save system.
**Scope:** data-driven cutscene definitions, event-triggered dispatch, prefab +
Timeline playback.

## 1. Overview

A cutscene is data (`CutsceneDefinitionSO`) that carries both its content *and*
the event that triggers it (`VoidEventSO trigger`) — content declares its own
activation, the same shape as Unreal's GAS abilities. Definitions are listed in
a catalog (`CutsceneCatalogSO`). `CutsceneDirector` (now a single
`MonoBehaviour`) subscribes to every definition's `trigger` in its catalog and,
on raise, instantiates that cutscene's prefab and plays its `PlayableDirector`.
There is no central "which cutscene / when" logic, no scene-gating, and no
save-flag gating — the trigger *is* the decision, and it lives on the data.

The old static director + separate `CutscenePlayer` + `OnPlayRequested` hop +
`Resources.Load` catalog + `WorldState` played-flag are all gone (see git
history). `CutscenePlayer.cs` was deleted; its playback logic folded into the
director once the director became a `MonoBehaviour`.

## 2. Components

| Component | Location | Responsibility | Used by |
|---|---|---|---|
| `CutsceneCatalogSO` | `Features/Cutscenes/CutsceneCatalogSO.cs` | SO: hand-curated `List<CutsceneDefinitionSO> cutscenes`. Assigned to the director via a **serialized reference** (no longer `Resources.Load`). Menu: `Cleanbot/Cutscenes/Catalog` | `CutsceneDirector` |
| `CutsceneDefinitionSO` | `Features/Cutscenes/CutsceneDefinitionSO.cs` | SO: one cutscene's data — `id`, `cutscenePrefab` (`GameObject`, must carry a `PlayableDirector`), `trigger` (`VoidEventSO`, what fires it), `replayable` (bool, **currently vestigial** — no longer read). Menu: `Cleanbot/Cutscenes/Definition` | `CutsceneCatalogSO`, `CutsceneDirector` |
| `CutsceneDirector` | `Features/Cutscenes/CutsceneDirector.cs` | `MonoBehaviour`, one per scene that plays cutscenes. `[SerializeField] catalog`. `OnEnable` subscribes a per-definition handler to each `def.trigger.Raised` (storing `(VoidEventSO, Action)` bindings); `OnDisable` unsubscribes all. `Play(def)` instantiates `def.cutscenePrefab`, plays the prefab's `PlayableDirector`, destroys the instance on `stopped` | — |
| `CutsceneSaveKeys` | `Features/Cutscenes/CutsceneSaveKeys.cs` | Builds `"cutscene.{id}.played"`. **Currently unused** — a leftover of the removed play-once gating; no caller | — (dead) |

## 3. API

| Method / Event | Owner | Purpose |
|---|---|---|
| `VoidEventSO.Raised` (`event Action`) / `RaiseAction()` | `Core/Events/VoidEventSO.cs` | The trigger transport. A cutscene's `trigger` is one of these; whoever raises it (e.g. `GameplayBootstrap` raising `newGameStarted`) makes the director play that cutscene. See *Session Intent & New-Game Flow* |

## 4. Rules

1. **Adding a cutscene is code-free.** Create a `CutsceneDefinitionSO`, assign its prefab and its `trigger` (a `VoidEventSO`), add it to the catalog, and make sure *something* raises that event. The director never changes. (The old `TryPlay("<id>", …)` edit in `CheckAllCutscenes()` and the whole per-id dispatch are gone.)
2. **Playback model is prefab-instantiation, not swap-asset.** Each cutscene is a self-contained prefab carrying its own `PlayableDirector` and its own Timeline **bindings**. The director does *not* hold a shared `PlayableDirector` and does *not* `RequireComponent(PlayableDirector)` — the swap-`playableAsset` approach was considered but not taken. Bindings therefore travel inside the prefab (Rule 4).
3. **No play-once guard exists.** The director neither reads nor writes `WorldState`. `replayable` and `CutsceneSaveKeys` are vestigial. Replay is prevented *only* by the trigger firing once per session (e.g. `newGameStarted` is raised only on New Game). **If a cutscene's trigger can fire repeatedly, the cutscene replays** — there is no guard. Re-introduce gating (or a consume-once trigger) before wiring a repeatable trigger to a play-once cutscene.
4. Every `ActivationTrack` in a Timeline needs its bound GameObject dragged onto the track's binding slot explicitly — an unbound track plays silently with no error and does nothing visible. Trim clip durations to the gap before the next clip, or multiple slides stay simultaneously active.
5. **Timing:** the director subscribes in `OnEnable`; triggers are raised in `Start` (by `GameplayBootstrap`) — all `OnEnable` run before any `Start`, so no raise is missed. See *Session Intent & New-Game Flow* §4.
6. The `bindings` list exists solely so `OnDisable` can `-=` the per-definition lambdas `OnEnable` added — you cannot unsubscribe a lambda you did not store.

## 5. Verification

```
grep -rn "OnPlayRequested" Assets/Scripts --include=*.cs
```

Expected result: **no matches** (removed). `def.trigger.Raised` is subscribed to only in `CutsceneDirector.cs`.

## 6. Authoring workflow — adding a new cutscene

1. **Build the cutscene prefab.** Empty GameObject + `PlayableDirector`, with the visual content under it (e.g. a `Canvas` of full-rect `Image` slides, as `NewGameIntro` does). Leave per-slide objects inactive — Activation Tracks turn them on at runtime.
2. **Build the Timeline.** Assign a `TimelineAsset` to the prefab's `PlayableDirector`, add tracks/clips, **then drag each bound GameObject onto its track's binding slot** (Rule 4).
3. **Make it a prefab** by dragging it into the Project window (e.g. `Assets/Cutscenes/<Name>/`).
4. **Create a `CutsceneDefinitionSO`** (`Create > Cleanbot > Cutscenes > Definition`). Set `id`, assign `cutscenePrefab`, and assign a `trigger` `VoidEventSO`.
5. **Add the definition to the catalog** (`cutscenes` list).
6. **Make something raise the trigger** — reuse an existing signal like `newGameStarted`, or a new `VoidEventSO` raised from wherever the cutscene should start. **No director edit.**

## 7. Assembly dependencies

| Assembly | References |
|---|---|
| `Cutscenes` | `Core` (incl. `Core.Events.VoidEventSO`), `Unity.Timeline` |

---

# Player Possession & Module Input Architecture

**Status:** Implemented for one actor and one module (`WalkModule`). No
second possessable character exists yet. Nothing currently triggers initial
possession automatically — a manual `[ContextMenu]` test method is the only
way an actor becomes controlled right now. `TagSet` has no methods yet, so
module-blocking tags aren't usable yet.
**Scope:** which actor currently receives input, and how a received input
verb reaches whichever modules are attached to that actor

## 1. Overview

`Actor` (plain C#, deliberately no `MonoBehaviour`) holds a `TagSet` and a
list of `IModule`s — it's the thing that gets possessed. `Posession` tracks
which `Actor` is currently possessed and which others are merely available.
`ActorHost` is the one `MonoBehaviour` that gives an `Actor` a place in the
scene; modules find their owning `Actor` by walking up to it, never via a
hand-assigned reference. Raw input becomes a typed `Intent` via `ModuleInput`,
and only the currently-possessed `Actor` is ever subscribed to receive one —
modules belonging to a non-possessed actor are never called at all, not even
to check whether they should react.

## 2. Components

| Component | Location | Responsibility | Used by |
|---|---|---|---|
| `IPosessable` | `Core/Player/IPosessable.cs` | Contract for anything that can be possessed: `OnPosessed`/`OnUnposessed` | `Actor`, `Posession` |
| `Posession` | `Core/Player/Posession.cs` | Static. Tracks `current` (not yet publicly exposed) and `Available`; `Register`/`Unregister` (called by `ActorHost.OnEnable`/`OnDisable`); `Posess(next)` unpossesses whoever was current, then possesses `next` | `ActorHost` |
| `Actor` | `Core/Player/Actor.cs` | Plain C#, implements `IPosessable`. Owns a `TagSet` and the `IModule` list. On possessed, subscribes its `Send` method to `ModuleInput.OnIntent`; on unpossessed, unsubscribes. `Send` builds a `Command` and forwards it only to modules whose `ReactsTo` includes the incoming `Intent` | `ActorHost` |
| `ActorHost` | `Core/Player/ActorHost.cs` | `MonoBehaviour` — Core's one physical anchor for an `Actor` in the scene (see Rule 4 on its namespace). Registers/unregisters its `Actor` with `Posession` on enable/disable. Carries a `[ContextMenu("Possess This")]` test method that calls `Posession.Posess(Actor)` directly — currently the only way anything becomes possessed | Modules, via `GetComponentInParent` |
| `TagSet` | `Core/Player/TagSet.cs` | Intended to hold state tags an `Actor` currently carries (e.g. `"Climbing"`) for cross-module blocking — class exists, no members implemented yet | `Actor` |
| `Intent` | `Core/Player/Intent.cs` | Closed enum of command verbs: `Move`, `Interact` (raised from input via `ModuleInput`) plus `Charge`, `StopCharge` (raised from the world via `Actor.Dispatch`) | `ModuleInput`, `Command`, `IModule.ReactsTo`, `Actor.Dispatch` |
| `Command` | `Core/Player/Command.cs` | `readonly struct` — `WhatToDo` (`Intent`) and `ExtraInfo` (`Vector2`, the payload) | `Actor.Send`, `IModule.Handle` |
| `ModuleInput` | `Core/Input/ModuleInput.cs` | Static. The **execution projection** transport: `RaiseMove(Vector2)`/`RaiseInteract()` raise `OnIntent`. (The display projection is a separate seam — see *Input Routing Architecture*.) | `InputReader` (raises), `Actor` (subscribes only while possessed) |
| `IModule` | `Core/Player/IModule.cs` | Contract for a module: `ReactsTo` (`IEnumerable<Intent>`, declared data — not a runtime check) + `Handle(Actor owner, Command cmd)` | `WalkModule` |
| `InputReader` | `Features/Input/InputReader.cs` | `MonoBehaviour`. The single input adapter (see *Input Routing Architecture*). Owns the `Intent → InputAction` map; pumps `ModuleInput.RaiseMove`/`RaiseInteract` (execution); registers an `InputGlyphProvider` over the same map into `GlyphInput` (display) | — |
| `WalkModule` | `Features/Modules/WalkModule.cs` | `MonoBehaviour, IModule`. Finds its `ActorHost` via `GetComponentInParent` in `Awake`, self-registers in `OnEnable`/removes itself in `OnDisable`, reacts to `Intent.Move` by setting a `Rigidbody`'s `linearVelocity` | — |

## 3. API

| Method / Event | Owner | Purpose |
|---|---|---|
| `Posession.Posess(IPosessable next)` | `Posession` | Make `next` the currently-controlled actor; unpossesses whoever was current first |
| `Posession.Available` | `Posession` | Query which actors are currently registered (no public way to query *who's currently possessed* yet — see Rule 6) |
| `Posession.Register` / `Unregister` | `Posession` | Add/remove an actor from `Available` — called by `ActorHost.OnEnable`/`OnDisable`, never by content |
| `ModuleInput.OnIntent` (`Action<Intent, Vector2>`) | `ModuleInput` | Raised on every raw input sample; only the possessed `Actor` is ever subscribed |
| `Actor.RegisterModule` / `RemoveModule` | `Actor` | A module adds/removes itself from its owner's dispatch list — called from the module's own `OnEnable`/`OnDisable` |

## 4. Rules

1. **Nothing is possessed automatically yet.** There is no code path that calls `Posession.Posess(...)` on startup — the only trigger right now is `ActorHost`'s `[ContextMenu("Possess This")]`, run manually from the Inspector during Play. Don't assume a fresh scene has a controllable character without doing this first.
2. Modules never read `ModuleInput` directly — only `Actor` subscribes to it, and only while possessed. A module declares what it reacts to via `ReactsTo`; it never checks "am I the possessed one" itself, and is simply never called if it isn't.
3. A module finds its owner via `GetComponentInParent<ActorHost>()` in `Awake`, never via a hand-assigned Inspector reference — this is what lets the same module component be dropped under any possessable character without per-prefab wiring.
4. **Known inconsistency:** `ActorHost.cs` physically lives under `Core/Player/` (and so compiles into `Core.asmdef`) but is declared `namespace Features.Character` — matching neither its folder nor the `Features.Player.Characters` asmdef (which currently has nothing in it). `ActorHost` was deliberately placed in Core despite being a `MonoBehaviour` — the doc comment in the file itself says so — so the namespace should read `Core.Player` to match where it actually compiles. Not yet fixed.
5. `Features/Modules` (where `WalkModule` lives) now has `Features.Modules.asmdef` referencing only `Core`, so the dependency-direction test (architecture-foundations.md §6.2) **is** enforced — it cannot reference other Features. (This corrects an earlier state where it compiled into the default `Assembly-CSharp`.)
6. `Posession` exposes `Available` but not who's currently possessed — fine while nothing outside `Posession`/`Actor` needs to query it, but the first feature that does (camera switching, a UI indicator) will need a public `Current` accessor added.
7. `Posession.OnPossessionChanged` is declared but never raised inside `Posess` — dead until something needs to react to a switch.
8. `TagSet` has no `Add`/`Remove`/`HasAny` implemented — any module's blocking-tag check is a TODO, not yet functional.

## 5. Verification

```
grep -rn "ModuleInput\." Assets/Scripts --include=*.cs
```

Expected result: `RaiseMove`/`RaiseInteract` invoked only in `InputReader.cs`; `OnIntent` subscribed to only in `Actor.cs`.

## 6. Usage example

```csharp
// a module, self-registering, declaring what it reacts to
public class WalkModule : MonoBehaviour, IModule
{
    ActorHost host;
    Rigidbody rb;
    [SerializeField] int speed;

    void Awake()
    {
        host = GetComponentInParent<ActorHost>();
        rb = host?.GetComponent<Rigidbody>();
    }

    void OnEnable()  => host.Actor.RegisterModule(this);
    void OnDisable() => host.Actor.RemoveModule(this);

    static readonly Intent[] reactsTo = { Intent.Move };
    public IEnumerable<Intent> ReactsTo => reactsTo;

    public void Handle(Actor owner, Command cmd)
    {
        var direction = cmd.ExtraInfo;
        rb.linearVelocity = new Vector3(direction.x, 0, direction.y) * speed;
    }
}
```

## 7. Assembly dependencies

| Assembly | References |
|---|---|
| `Core` (includes `Core/Player/*`) | none |
| `Features.Input` | `Core`, `Unity.InputSystem` |
| `Features.Player.Characters` | `Core` — currently empty; `ActorHost.cs` compiles into `Core` instead (Rule 4) |
| `Features.Modules` | `Core` (Rule 5) |

---

# Input Routing Architecture — Execution & Display Projections

**Status:** Implemented for the **Player** context (one action map). `Move` and
`Interact` are wired end-to-end; the glyph **display** projection is implemented
for `label` (the `sprite` half of `Glyph` is unwired). No menu / second context
exists yet — the design is extension-ready, not extended.
**Scope:** how raw device input becomes a typed `Intent`, and how that *single*
source fans out to (A) the possessed player and (B) the UI prompt's button glyph.

## 1. Overview

`InputReader` is the one adapter that turns the `Player` action map into a single
`Intent → InputAction` map, then exposes that map as **two independent
projections** of the same data:

- **Execution (push)** — intents are broadcast on `ModuleInput`; the possessed
  `Actor` receives them and dispatches to its modules. (Continues in *Player
  Possession & Module Input*.)
- **Display (pull)** — an `InputGlyphProvider` wraps the *same* map and is
  registered into Core's `GlyphInput`; the UI asks it, by `Intent`, for a `Glyph`
  (letter/sprite) to draw.

Both projections read one map. Neither Core nor the UI ever sees an `InputAction`
— only `Intent` goes in and `Glyph` comes out. The Input System package is
quarantined inside `Features.Input`.

## 2. Components

| Component | Location | Responsibility | Used by |
|---|---|---|---|
| `InputReader` | `Features/Input/InputReader.cs` | `MonoBehaviour`. The single input adapter. Builds the `Intent → InputAction` map from the `Player` map; pumps `Move` each frame and `Interact` on `performed` into `ModuleInput` (execution); constructs an `InputGlyphProvider` over the same map and registers it into `GlyphInput` (display); owns the map's enable/disable | — |
| `ModuleInput` | `Core/Input/ModuleInput.cs` | Static. **Execution** transport: `RaiseMove`/`RaiseInteract` raise `OnIntent(Intent, Vector2)` | `InputReader` (raises), `Actor` (subscribes only while possessed) |
| `IInputGlyphProvider` | `Core/Input/IInputGlyphProvider.cs` | Core seam for **display**: `Glyph GetGlyph(Intent)` + `event DeviceChanged` | `GlyphInput`, UI |
| `GlyphInput` | `Core/Input/GlyphInput.cs` | Static Core registry — the query-shaped sibling of `ModuleInput`. `Register(provider)` (by `InputReader`), `Glyphs` getter (by UI); holds one `IInputGlyphProvider` | `InputReader` (registers), UI (reads) |
| `InputGlyphProvider` | `Features/Input/InputGlyphProvider.cs` | Plain C# (no `MonoBehaviour`). Resolves an `Intent` to a `Glyph` via the map + `GetBindingDisplayString` for the active control scheme; caches per intent; tracks the active device via the static `InputSystem.onActionChange` and raises `DeviceChanged` (clearing the cache) on a scheme switch. `IDisposable`, disposed by `InputReader` | `GlyphInput` |
| `Glyph` | `Core/Input/Glyph.cs` | `struct` — `label` (string) + `sprite` (`Sprite`). The only input-derived type that crosses into Core/UI | UI |
| `UIInteractPromptDisplayRequestSO` | `Core/Events/UIInteractPromptDisplayRequestSO.cs` | SO event channel carrying `(localized label, Intent, Transform)`. An interactable raises it on focus/unfocus; the prompt UI listens, then resolves the glyph for that `Intent` from `GlyphInput` | `ChargingStation` (raises), prompt UI (listens) |

## 3. The two routes

**Route A — input → player (execution):**

```
device → InputReader (Update / performed)
       → ModuleInput.OnIntent (broadcast)
       → possessed Actor.Send → Command → modules filtered by ReactsTo
       → WalkModule (Move → Rigidbody) / InteractionModule (Interact → Focus.Current.Interact())
```

**Route B — focus → UI prompt (display):**

```
InteractionSensor focuses an interactable
       → InteractionFocus.Set → IInteractable.OnFocus
       → UIInteractPromptDisplayRequestSO.RaiseShow(label, Intent, transform)
UI then pulls the glyph for that Intent:
       → GlyphInput.Glyphs.GetGlyph(Intent) → Glyph { label, sprite }
       → refresh on DeviceChanged
```

The interaction is split the same orthogonal way: the **sensor** decides *which*
interactable is focused ("what"); the **`Interact` intent** (Route A) fires
`InteractionModule`, which acts on whatever is focused ("when"). The world can
also originate commands back into the actor — `ChargingStation.Interact` calls
`Actor.Dispatch(Intent.Charge, dockAnchor)`, the world-side sibling of the
input-side `Actor.Send`.

## 4. Rules

1. The `Intent → InputAction` map lives **only** in `InputReader`. Nothing else
   maps keys to intents; both projections read this one map.
2. **Core never references `UnityEngine.InputSystem`.** The middleware stays in
   `Features.Input`; `InputAction`/`InputControlScheme` must never appear in a
   Core type's signature — only `Intent` goes in, only `Glyph` comes out.
3. The UI obtains a glyph **only** from Core (`GlyphInput.Glyphs`), never from
   `Features.Input`. `InputReader` registers the provider; consumers pull — the
   same Core-mediated handoff as `ModuleInput`, so neither feature references the
   other.
4. `GlyphInput.Glyphs` is `null` until `InputReader.Awake` registers — consumers
   must null-guard (`GlyphInput.Glyphs?.GetGlyph(...)`) and query at **show** time,
   not in `OnEnable`.
5. The display projection is read-only over the binding asset — rebinding a key in
   the `.inputactions` asset updates both the player's controls and every prompt
   glyph, with no code change.

## 5. Verification

```
grep -rn "InputSystem" Assets/Scripts/Core --include=*.cs
```

Expected: **no matches** — the Input System never reaches Core.

```
grep -rn "GlyphInput\." Assets/Scripts --include=*.cs
```

Expected: `Register` only in `InputReader.cs`; `Glyphs` read only in UI consumers.

## 6. How to work with these files

- **Add an input verb** (e.g. `Jump`): add it to `Intent`, add the binding in the
  `.inputactions` asset, add one `map[...] = FindAction(...)` line + a `Raise…`
  call in `InputReader`. The glyph side needs **no** change — it reads the map.
- **Show a prompt on a new interactable:** implement `IInteractable`, and in
  `OnFocus` raise the prompt SO with the relevant `Intent`. The glyph resolves
  automatically from `GlyphInput`.
- **Letters → sprites:** fill `Glyph.sprite` in `InputGlyphProvider.Resolve` using
  the `GetBindingDisplayString(i, out deviceLayout, out controlPath)` overload to
  key a sprite table. The UI, which only reads `Glyph`, is unchanged.
- **Add a menu context (future):** add a *parallel triple* — a reader on the `UI`
  action map + its own transport (e.g. `MenuInput`) — and switch contexts by
  enabling/disabling action maps. Do **not** branch `ModuleInput`. The glyph
  provider can serve any registered context (or add a parallel provider).

## 7. Assembly dependencies

| Assembly | References |
|---|---|
| `Core` (`Core/Input`, `Core/Events`) | none |
| `Features.Input` | `Core`, `Unity.InputSystem` |
| `Features.UI` (prompt consumers) | `Core` |
