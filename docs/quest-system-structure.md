# Quest system — working guideline

A loose roadmap, not a spec. The one idea everything hangs on: **quests are
conditions over a facts database, and the journal is just a projection of that
state.** We already have the facts database — it's `WorldState`.

The July 2026 research pass (BG3/Osiris, Hades, Wildermyth/storylets, Yarn
Spinner 3, KCD2) confirmed this is not legacy design but the convergent one:
every reactive narrative system of the last five years runs on a flat fact
store + generic predicates + content gated by those predicates. Larian kept
facts+rules for BG3 with every opportunity to replace it.

## Settled decisions

1. **One store.** `WorldState`'s dictionaries are the single source of truth
   for narrative/progress state. No second store, no state in SOs, no state in
   the quest system itself. (BG3 runs two stores — asset flags for writers,
   Osiris DBs for scripters — because it has two authoring audiences. Solo dev
   collapses that to one.)

2. **Conditions are data, not classes.** One generic condition record
   (`factKey` + comparator + value), evaluated against `WorldState`. New
   requirement type = new data row, never a new condition class. (Hades'
   requirement tables, Osiris rule conditions, storylet prerequisites — nobody
   modern writes `LocationVisitedCondition` subclasses.)

3. **Facts wake logic; live reads refine it.** Write-through for discrete and
   historical state: systems write facts on enter/exit/finish/count, and
   `FactChanged` is the only wake-up signal the quest brain listens to.
   Continuous values (positions, distances, health) are never mirrored into
   facts — if a condition ever needs one, it reads it live *during* a recheck
   that a fact write already triggered. Osiris enforces exactly this: rules
   wake on events/DB writes only; queries "never get automatically
   (re)evaluated". No fact-provider infrastructure until a real need appears.
   - Write-through's cost: mirror desync. Whoever writes an enter-fact owns
     the exit-fact on *every* path out (triggers, teleports, scene loads).

4. **Writing a fact is an authorial act** (opt-in), like W3's "Add fact" nodes
   and Osiris rule actions — not automatic telemetry from every system. The
   reason is debugger hygiene, not memory: facts are ~bytes; BG3 ships
   thousands per save and just prunes dead DBs. Rule of thumb for a writer:
   "will anyone ever read this?" For cutscenes: write the finished-fact iff
   `!replayable || marksProgress`.

5. **Keys are never hand-typed, and `FactKeys` is the single string
   authority.** Every derived key format (`cutscene.{id}.finished`) exists in
   exactly one method, called symmetrically by writer and reader. Key strings
   are the save schema — renaming one breaks saves (Larian's patch-8 lesson) —
   so `FactKeys` stays stable and append-only.
   Authoring-side referencing = a key **registry + dropdown drawer**
   (GameplayTags-style): registry concatenates key sources — an
   asset-enumerating source (cutscene defs → `FactKeys.CutsceneFinished(id)`,
   computed in-editor from the same method), plus a reflection source over
   `[FactKeySource]` consts for code-born keys. Built *before* the first quest
   asset is authored (tooling before authoring, minimal version).

6. **Quest progress is itself facts.** Active/complete stage lives in
   `quest.{id}.stage` (a counter, only goes up). Consequences, all free:
   saves need nothing quest-specific; on load the brain rebuilds from stage
   facts, never from remembering triggers; the journal is a pure projection;
   quest chains are just conditions on other quests' stage facts (BG3's
   helper-DB move: the mapping from facts to journal is itself facts).

7. **Transient-key hygiene.** Momentary-but-discrete state ("currently in
   location") is still a fact, under a shared `current.` prefix
   (`current.location`). Owning systems rewrite them on scene load. The shared
   prefix makes a later save-pruner trivial (Larian's EXIT-section cleanup
   pattern) — don't build it until saves actually bloat.

## The four layers (BG3 mapping)

| BG3 | Us | Job |
|---|---|---|
| Engine events (dialog reached, char died) | C# events / `VoidEventSO`, `playable.stopped` | Push signals; know nothing about quests |
| Flags + Osiris DB facts | `WorldState` dictionaries via `FactKeys` | Persistent truth; the save |
| Osiris goals/rules + `DB_QuestDef_State` helper rows | Quest brain + `QuestDefinitionSO` condition rows | Wake on `FactChanged`, recheck stage conditions live, advance stage by writing facts |
| Journal | Journal UI | Projection; stores nothing |

Conditions checked *live* against current state, never "saw the event once so
done forever" — that's what makes collect-then-sell-then-turn-in work by
itself (recheck at turn-in) and what makes saves resume for free.

## Build order

1. **Cutscene fact writer** (walking skeleton, write side). Director writes
   `cutscene.{id}.finished` on `playable.stopped` when the def opts in; same
   fact gates replay for non-replayable cutscenes. No keys hand-referenced.
2. **Condition record + evaluator.** The one serializable
   `{factKey, comparator, value}` struct + static evaluate against
   `WorldState`. Pure code, no authoring yet.
3. **Key registry + dropdown drawer.** The minimal tool, before any asset
   authoring (decision 5).
4. **`QuestDefinitionSO`.** Stages → objectives → condition rows. Stage
   advance = all objectives met.
5. **Quest brain.** Starts quests, subscribes `FactChanged`, rechecks current
   stage, advances by writing `quest.{id}.stage`. On load: rebuild from facts.
6. **Facts debug window.** Live view of WorldState dictionaries; declared keys
   (registry) vs actually-written keys are two different views.
7. **Journal UI.** Projection of stage facts + objective progress (n/m).
8. **First real quest — the awakening.** Authored as data, triggered off the
   intro flow, played end to end.

## Things we figured out worth remembering

- Stage number only goes up → no oscillation when a counter later drops.
- Open question per objective: does progress from *before* the quest counts?
  (lifetime dust vs. dust-since-accepted). Solvable with a baseline snapshot
  fact written when the stage starts.
- Branching later = stages get "which stage next" conditions instead of "next
  in list". Data model already allows it.
- Turn-in via NPC/dialogue later reuses the same condition check at the moment
  of conversation.
- Storylets/saliency (content pools that self-select, Hades/YS3 style) are the
  genuinely new layer of the last five years — *not needed* for stage-based
  quests, but the fact + generic-condition core is exactly what they'd plug
  into if the game ever grows that way.

## References

- [Larian Osiris overview](https://docs.larian.game/Osiris_Overview) — facts + rules; events vs queries vs calls; DBs live in savegames
- [BG3: quests via flags + helper DBs](https://docs.baldursgate3.game/index.php?title=Journal%3A_Adding_a_Quest_to_a_Situation) — `DB_QuestDef_State` pattern
- [BG3 dialogue flags](https://wiki.bg3.community/en/Tutorials/dialogue-files-tutorial) — flags as UUID assets, checkflags/setflags
- [Hades' reactive dialogue](https://www.christi-kerr.com/post/how-the-dialogue-system-in-hades-rewards-failure) + [Game Developer piece](https://www.gamedeveloper.com/design/how-supergiant-weaves-narrative-rewards-into-i-hades-i-cycle-of-perpetual-death) — condition-gated content buckets at scale
- [Storylets Explained (Ian Thomas)](https://wildwinter.medium.com/storylets-explained-ff5a24842bd9) / [Wildermyth story I/O](https://wildermyth.com/wiki/Story_Inputs_and_Outputs)
- [Yarn Spinner 3](https://yarnspinner.dev/blog/yarn-spinner-30-what-to-expect) — smart variables, storylets, saliency (May 2025)
- [UE Gameplay Tags](https://dev.epicgames.com/documentation/en-us/unreal-engine/using-gameplay-tags-in-unreal-engine) — registry + picker model for step 3
- [UESP Creation Kit wiki](https://skyrimck.uesp.net/wiki/Bethesda_Tutorial_Radiant_Quests) — Bethesda stages/objectives
- [W3 REDkit quest nodes](https://cdprojektred.atlassian.net/wiki/spaces/W3REDkit/pages/6327742/HOW-TO:+Use+quest+nodes) — quest graphs + facts DB
- [Warhorse, "Everything Is Connected" (2026)](https://www.youtube.com/watch?v=n-Gu1TQW1uU) — KCD2 quest web
