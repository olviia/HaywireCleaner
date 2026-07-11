# Quest system — working guideline

A loose roadmap, not a spec. We'll be at this for a while; order and details
can change as we learn. The one idea everything hangs on (stolen from Skyrim /
Witcher 3 / BG3 / KCD2 — they all do this): **quests are conditions over a
facts database, and the journal is just a projection of that state.** We
already have the facts database — it's `WorldState`.

## The steps, roughly in order

**1. Facts.**
Give `WorldState` counter get/set (it only has flags wired) and some
"a fact changed" signal so the quest side knows when to re-check.
Useful finds: Witcher 3 calls this the facts DB, Larian's Osiris is literally
nothing but facts + rules, and facts persisting in saves for free is the whole
trick — quest save/load costs nothing if all quest state lives here.

**2. Quest as data.**
A quest definition asset (like `CutsceneDefinitionSO`): stages, each stage has
objectives, each objective is a condition on a fact ("counter ≥ 3", "flag
set"). Bethesda's stage model — a quest is basically one number that only goes
up. Simplest thing that shipped a thousand quests.

**2½. The fact-key dropdown — before any asset is authored.**
No fact key is ever hand-typed into an asset. *How* is an OPEN DECISION —
decide when the first quest asset actually needs to reference a key. What the
studio survey (July 2026) settled either way: everywhere shipped, *creating* a
key and *referencing* a key are separate acts — creation is explicit (UE tag
registry from code/ini/datatables, Osiris generate-definitions, Creation Kit
forms), referencing picks from an index. Only Witcher 3 free-typed strings,
compensated by naming conventions + a live facts debugger.

Candidates:

- (a) String keys + registry + `AdvancedDropdown` property drawer
  (GameplayTags-style). Registry = concatenated key sources: an
  asset-enumerating source for data-born keys (`cutscene.{id}.finished`), plus
  — only once gameplay writes its first code-born fact (step 4) — a reflection
  source over `[FactKeySource]` classes' consts.
- (b) Fact-as-asset (Creation Kit GlobalVariable forms / Chop Chop style):
  a FactSO holding one string id; writers and readers reference the SO, which
  writes into WorldState's dictionaries. Unity's object picker gives search
  for free, zero editor code. Costs: one asset per fact, and derived key
  families (`cutscene.{id}.finished`) don't fit asset-per-fact.
- (c) Maybe another solution entirely — keep looking before committing.

The walking skeleton (cutscene bool → director derives key → WorldState flag)
doesn't depend on this choice: no key is hand-referenced yet.

Also: a debug facts window (live view of WorldState's
dictionaries) — declared keys vs actually-written keys are two different tools.

**3. The runtime brain.**
One thing (QuestLog-ish) that starts quests, re-checks the current stage's
objectives when facts change, advances the stage, writes completion.
Useful finds: conditions should be checked *live* against current state, never
"I saw the event once so it's done forever" — that's what makes the
collect-then-sell-then-turn-in case work by itself (recheck at turn-in, like
Crimson Desert / BG3). And on load, figure out active quests from the facts,
not from remembering triggers — that's what makes saves resume for free.

**4. Fact writers.**
Gameplay just increments counters / sets flags: cleaning dust, charging,
killing dust bunnies, riding meters. Gameplay never knows quests exist; the
quest side never knows what "riding" means. This is why wildly different
objectives don't need a quest-system feature each.

**5. Some journal UI.**
Show active quest + objectives + n/m progress. Pure display, stores nothing.

**6. First real quest — the awakening.**
Author it as data, trigger it off the intro flow, play it end to end.

## Things we figured out worth remembering

- Stage number only goes up → no weird oscillation when a counter later drops.
- Open question to decide per objective: does progress from *before* the quest
  started count? (lifetime dust vs. dust-since-accepted). Solvable with a
  baseline snapshot when the stage starts.
- Branching later = stages get "which stage next" conditions instead of just
  "next in list". The data model already allows it, no rework.
- Turn-in via NPC/dialogue later reuses the same condition check at the moment
  of the conversation.

## References

- [UESP Creation Kit wiki](https://skyrimck.uesp.net/wiki/Bethesda_Tutorial_Radiant_Quests) — Bethesda stages/objectives
- [W3 REDkit quest nodes](https://cdprojektred.atlassian.net/wiki/spaces/W3REDkit/pages/6327742/HOW-TO:+Use+quest+nodes) — quest graphs + facts DB
- [Larian Osiris overview](https://docs.larian.game/Osiris_Overview) — facts + rules, nothing else
- [UE Gameplay Tags](https://dev.epicgames.com/documentation/en-us/unreal-engine/using-gameplay-tags-in-unreal-engine) — central tag registry (code/ini/datatables) + picker, the (a) candidate
- [CK conditions](https://ck.uesp.net/wiki/Category:Conditions) — GlobalVariable forms in dropdowns, the (b) candidate
- [Warhorse, "Everything Is Connected" (2026)](https://www.youtube.com/watch?v=n-Gu1TQW1uU) — KCD2 quest web
