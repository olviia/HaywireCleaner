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
| `Bootstrap` | `Bootstrap/Bootstrap.cs` | Composition root. Configures the scene name map and connects `SceneStateMachine` to `SceneLoader` at startup | — |

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
4. Anything that needs to act only once a target scene's objects exist (e.g. finding a `MonoBehaviour` that just got loaded) must subscribe to `SceneLoader.OnSceneLoaded`, not `SceneStateMachine.OnGameSceneChanged`. The latter fires synchronously the instant a transition is *requested*, before the async load has even started — a real bug this session (`Features/Cutscenes/CutsceneDirector.cs` firing into a scene whose `CutscenePlayer` didn't exist yet) came from exactly this confusion.

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
| `Bootstrap` | `Core` |
| `Features.*` | `Core` |

---

# Save System Architecture

**Status:** Implemented — `flags` only (other dictionaries exist but have no accessors yet)
**Scope:** typed save-game persistence (JSON to disk)

## 1. Overview

World/save state lives in one mediator, `WorldState`. It owns the only
instance of `SaveData` in memory and is the only thing allowed to read or
write it. Features never see `SaveData` itself — they call `WorldState`'s
narrow, typed Get/Set API.

## 2. Components

| Component | Location | Responsibility | Used by |
|---|---|---|---|
| `WorldState` | `Core/SaveSystem/WorldState.cs` | Mediator: owns the current `SaveData` in memory, exposes typed Get/Set API, owns JSON file I/O | Features (read/write flags), `GameFlow` (`NewSave`) |
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
2. Features build their own save keys via small per-feature static classes shaped `"category.id.field"` (e.g. `Features/Cutscenes/CutsceneSaveKeys.Played(id)` → `"cutscene.{id}.played"`) — `Core` never knows feature category names.
3. Only `flags` has Get/Set methods on `WorldState` today. `counters`/`reactions`/`names`/`positions`/`attributeValues` exist in `SaveData` but are unused — add a same-shaped accessor pair on `WorldState` (mirroring `GetFlag`/`SetFlag`) the first time a feature actually needs one, rather than exposing all of them up front.

## 5. Verification

```
grep -rn "SaveData" Assets/Scripts --include=*.cs
```

Expected result: matches only in `Core/SaveSystem/WorldState.cs` and `Core/SaveSystem/SaveData.cs` itself.

## 6. Usage example

```csharp
// Features/Cutscenes/CutsceneDirector.cs
if (WorldState.GetFlag(CutsceneSaveKeys.Played(cutsceneId))) return;
// ...
WorldState.SetFlag(CutsceneSaveKeys.Played(def.id), true);
```

## 7. Assembly dependencies

`Core/SaveSystem` is a subfolder of `Core`, not a separate assembly — it
compiles under `Core.asmdef` (no references) like the rest of Core.

---

# Cutscene System Architecture

**Status:** Implemented for one cutscene (`NewGameIntro`) — trigger dispatch
is per-id and hardcoded, not yet data-driven
**Scope:** data-driven cutscene definitions, Timeline-based playback,
play-once gating via save flags

## 1. Overview

A cutscene is defined as data (`CutsceneDefinitionSO`) and listed in a
catalog (`CutsceneCatalogSO`). `CutsceneDirector` is the only thing that
decides *when* a cutscene plays and *whether* it's allowed to (checking and
writing the played-flag). `CutscenePlayer` is the only thing that knows
*how* to play one — instantiate the prefab, run its `PlayableDirector`,
clean up.

## 2. Components

| Component | Location | Responsibility | Used by |
|---|---|---|---|
| `CutsceneCatalogSO` | `Features/Cutscenes/CutsceneCatalogSO.cs` | ScriptableObject: hand-curated `List<CutsceneDefinitionSO> all`. Loaded once via `Resources.Load<CutsceneCatalogSO>("CutsceneCatalog")` — must live at `Assets/Resources/CutsceneCatalog.asset` | `CutsceneDirector` |
| `CutsceneDefinitionSO` | `Features/Cutscenes/CutsceneDefinitionSO.cs` | ScriptableObject: one cutscene's data — `id` (string), `cutscenePrefab` (`GameObject`, must carry a `PlayableDirector`), `replayable` (bool) | `CutsceneCatalogSO`, `CutsceneDirector` |
| `CutsceneDirector` | `Features/Cutscenes/CutsceneDirector.cs` | Static, no `MonoBehaviour`. Loads the catalog on boot, subscribes to `SceneLoader.OnSceneLoaded`, checks cutscenes only when the loaded scene is `GameScene.Gameplay`, looks up a definition by id, gates on the played-flag, raises `OnPlayRequested`, sets the played-flag (unless `replayable`) | — |
| `CutscenePlayer` | `Features/Cutscenes/CutscenePlayer.cs` | `MonoBehaviour`. Lives in any scene that should be able to show a cutscene (`Gameplay.unity`, `LoopTestScene.unity`). Subscribes to `CutsceneDirector.OnPlayRequested`, instantiates the prefab, fetches `PlayableDirector` **from the instantiated prefab** (not from its own GameObject, despite what its class doc-comment currently says), plays it, and on stop destroys the instance and redirects to `GameScene.Prototype1` (temporary, see Rules) | — |
| `CutsceneSaveKeys` | `Features/Cutscenes/CutsceneSaveKeys.cs` | Builds the `"cutscene.{id}.played"` key string used with `WorldState` | `CutsceneDirector` |

## 3. API

| Method / Event | Owner | Purpose |
|---|---|---|
| `CutsceneDirector.OnPlayRequested` (`Action<GameObject>`) | `CutsceneDirector` | Raised with a cutscene's prefab when it should play |
| `SceneLoader.OnSceneLoaded` (`Action<GameScene>`) | `SceneLoader` | What `CutsceneDirector` listens to in order to know it's safe to check cutscenes (Scene Flow §4) |

## 4. Rules

1. New cutscene *content* (prefab, Timeline, images) never needs a code change — only new assets, registered in the catalog. See the authoring workflow below.
2. **Wiring a new cutscene's trigger condition currently does need a code change** — a `TryPlay("<id>", <condition>)` line added inside `CutsceneDirector.CheckAllCutscenes()`. There is no generic trigger-kind dispatch yet (the originally-sketched `CutsceneTriggerCondition`/`CutsceneTriggerKind` design would remove this step, but hasn't been built). Don't assume adding a cutscene is fully code-free yet.
3. `CutsceneDirector`'s checks run on *any* transition into `GameScene.Gameplay` (new game or loaded save) — the played-flag, not a new-game/load-game distinction, is what prevents replay.
4. Every `ActivationTrack` in a Timeline needs its bound GameObject dragged onto the track's binding slot explicitly — an unbound track plays silently with no error and does nothing visible. Clip durations must also be trimmed to the gap before the next clip starts, or multiple slides stay simultaneously active.
5. `CutscenePlayer`'s redirect to `GameScene.Prototype1` on every cutscene-end is a temporary, itch-demo-only hack (commented as such in the code). Remove it or move it to per-cutscene data before a second cutscene exists with different post-play needs.

## 5. Verification

```
grep -rn "OnPlayRequested" Assets/Scripts --include=*.cs
```

Expected result: invoked only in `CutsceneDirector.cs`, subscribed to only in `CutscenePlayer.cs`.

## 6. Authoring workflow — adding a new cutscene

1. **Build the cutscene prefab.** Create an empty GameObject, add a `PlayableDirector` component. Add whatever visual content the cutscene needs under it (e.g. a `Canvas` with one full-rect `Image` per slide, as `NewGameIntro` does). Leave per-slide objects inactive by default — Activation Tracks control active state at runtime.
2. **Build the Timeline.** In the Timeline window, assign a `TimelineAsset` to the `PlayableDirector`'s Playable field. Add tracks and clips, **then drag each bound GameObject onto its track's binding slot** — easy to skip, and an unbound track does nothing (Rule 4 above).
3. **Make it a prefab** by dragging the GameObject from the Hierarchy into the Project window (e.g. `Assets/Cutscenes/<CutsceneName>/`).
4. **Create a `CutsceneDefinitionSO`** asset (`Create > Cleanbot > Cutscenes > Definition`). Set `id`, assign the prefab from step 3 to `cutscenePrefab`, set `replayable` if it should be allowed to play more than once.
5. **Add the definition to the catalog** — open `Assets/Resources/CutsceneCatalog.asset` and add the new definition to its `all` list.
6. **Wire the trigger** (still code, see Rule 2) — add a `TryPlay("<id>", <condition>)` call inside `CutsceneDirector.CheckAllCutscenes()`.

## 7. Assembly dependencies

| Assembly | References |
|---|---|
| `Cutscenes` | `Core`, `Unity.Timeline` |

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
5. `Features/Modules` (where `WalkModule` lives) has no `.asmdef` of its own, so nothing mechanically stops it from referencing other Features directly — the dependency-direction test (architecture-foundations.md §6.2) isn't enforced here the way it is for `Cutscenes`/`Features.Title`/`Features.Player.Characters`.
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
| — (`Features/Modules`) | no asmdef; compiles into the default `Assembly-CSharp` (Rule 5) |

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
