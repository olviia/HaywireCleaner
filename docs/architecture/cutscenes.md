# Cutscenes

*`Features/Cutscenes/*` (asmdef name is `Cutscenes`, **not** `Features.Cutscenes`).
Map: [`README.md`](README.md).*

**Status:** Live, data-driven. Each cutscene declares its own trigger event;
`CutsceneDirector` subscribes to all of them and plays on raise. **Adding a cutscene
needs no code change.** Play-once gating writes/reads a fact, so the director *does*
touch `WorldState` — reversing an older "the director no longer touches the save
system" note.

## Components

| Component | Location | Responsibility |
|---|---|---|
| `CutsceneDefinitionSO` | `Features/Cutscenes/CutsceneDefinitionSO.cs` | `id`, `cutscenePrefab` (carries a `PlayableDirector`), `eventTrigger` (`VoidEventSO` — what fires it), `eventRaiseOnFinish` (`VoidEventSO` raised on stop — chains forward), `replayable`, `isTriggerForQuest`. Derived `WritesFinishedFact => !replayable \|\| isTriggerForQuest`. |
| `CutsceneCatalogSO` | `Features/Cutscenes/CutsceneCatalogSO.cs` | `List<CutsceneDefinitionSO> cutscenes`, serialized onto the director. |
| `CutsceneDirector` | `Features/Cutscenes/CutsceneDirector.cs` | `MonoBehaviour`. `OnEnable`: for each def, skip if no trigger or if play-once-gated (`!replayable && GetFlag(CutsceneFinished(id))`), else bind a handler to `eventTrigger.Raised` (stored for `OnDisable`). `Play`: `InputRouter.Enter(Cutscene)` on first active cutscene; instantiate prefab; wire skip via `CutsceneInput.SkipCutscene`; on `playable.stopped` → write finished-fact if opted in, raise `eventRaiseOnFinish`, destroy instance, `InputRouter.Exit(Cutscene)` when the last one ends. |
| `CutsceneTextScrambleReveal` | `Features/Cutscenes/CutsceneTextScrambleReveal.cs` | `ITimeControl` on a Timeline clip: scrambles then settles TMP text, char-by-char, driven by clip time. |

## Rules

1. **Adding a cutscene is code-free** — make the def, assign prefab + `eventTrigger`,
   add to catalog, ensure something raises the trigger.
2. **Playback is prefab-instantiation** — each cutscene is a self-contained prefab with
   its own `PlayableDirector` and Timeline bindings. Unbound `ActivationTrack`s fail
   **silently**.
3. **Replay is prevented by the finished-fact** (`WritesFinishedFact`), re-checked at
   subscription time in `OnEnable`. A `replayable` cutscene that also
   `isTriggerForQuest` still writes its fact (so a quest can gate on it) but is not
   play-once-blocked.
4. **Input context is the director's responsibility** — it enters/exits the `Cutscene`
   context around playback ([input](input.md)); skip is a `Cinematic`-map action.
5. **The finished-fact is the hand-off to the quest system.** It's the first link in
   the narrative trace (README) — the director doesn't know quests exist; a quest's
   `startConditions` just test `cutscene.{id}.finished`.
