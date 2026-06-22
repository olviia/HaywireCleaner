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
