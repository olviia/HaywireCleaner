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

## 4. Rules

1. Features request transitions only via `SceneStateMachine.ChangeSceneTo`.
2. No code outside `Core/SceneLoader.cs` may call `UnityEngine.SceneManagement.SceneManager`.
3. `Bootstrap` is the only component permitted to call `SceneLoader` directly.

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
