# HaywireCleaner (working title: "Dust & Glory")

A small RPG about a round robot vacuum cleaner that goes haywire — instead of just
cleaning, it descends into the hidden spaces beneath the house, chasing the household
pets and uncovering what's underneath everything. Played from the robot's own
floor-level perspective.

Tagline: "Crimson Desert, but sparkly cleanness."

## Genre & feel

RPG, not a puzzle/strategy game. The premise (vacuum, house) is the skin; the goal is
for it to feel as weighty as "a humanoid in an open world," compressed into a small,
lived-in space. What carries that weight at small scale:

- **Earned growth with consequence** — upgrades genuinely reopen/reframe places you
  already explored, not just unlock new areas.
- **A world that reacts to you** — dirt rebuilds, objects get moved, pets do pet
  things (a dog sheds more if neglected, a kid drops a box in your path, a cat claims
  your favorite route and blocks half your view with its tail).
- **Quests that make you care about specific places**, not just waypoints.

This is "Metroidvania in spirit, not in size": ONE small, dense, fully handcrafted
space. No procedural generation, no luck-based systems.

## The core loop

**seek** (a goal pulls you somewhere) → **clean** (the sparkly payoff) → **find** (an
optional detour reward off the path) → **grow** (earn a module) → the new module
**reopens** the space you already explored.

Standard challenge → action → reward triad, with growth feeding back into the space
you already know. This loop *is* the game; everything else is meta layered on later.

## MVP scope

The core loop (above) is validated — two prototypes confirmed it feels right. The
remaining unknown isn't "is this fun," it's "does the architecture hold across a
whole game's worth of systems." MVP is therefore a **walking skeleton** (Cockburn):
every system in the player-facing flow written out in `docs/designers vision.md`
(splash → title → save/load → settings → new game → cutscene → tutorial → core
loop → return-to-base) exists and is wired end-to-end through Core, each at
minimal/stub depth. Breadth now, depth later — see
`docs/architecture-foundations.md` for why stub-now/enrich-later doesn't require
reopening the seams between systems.

**Content scope** (depth/quantity — separate axis from the above, can grow without
touching the skeleton):

- ONE small handcrafted house, 3-4 rooms. Authored once — this is the entire "world."
- THREE modules max, each a tangible EARNED ability that reopens cleaned areas:
  start = open floor only → slim mode (fit under furniture) → one more (suction OR
  climb). Depth comes from **module × space** combinations, not module count.
- ONE renewable mess source: a single shedding cat — texture that re-dirties the
  house, not a chase mechanic.
- A minimal main-quest/subquest hierarchy (per `docs/designers vision.md`'s
  opening sequence), not a full authoring-driven quest system.

**Deferred past v1** (content, not systems — the systems above exist as stubs
regardless): decoration, trading, economy, full story/dialogue beyond the opening,
multiple pets, chase mechanic, additional rooms/modules beyond the above. The goal
is a real RPG, small in scope — these are likely to come back once the skeleton is
proven, not permanently cut.

## Hard constraints (filter every decision through these)

- Growth must feel EARNED — skill/knowledge only. **No luck, gacha, roguelike
  drafting, or casino/compulsion mechanics.** Hard boundary.
- Embodied character moving and interacting — not a card/menu game.
- **Spine-and-detour**: a goal pulls you through space, with optional detours worth
  taking. Pure open exploration bores the designer; pure linear corridors too. The
  game lives in the tension between "go finish the goal" and "ooh, what's that."
- Small enough for one person to finish.
- Depth over complexity: meaningful play PER RULE, not number of systems.

## Resolved design questions

**Where does the depth live — spatial discovery vs. systemic routing?**
Resolved: **spatial discovery** (Rumu-shaped). The fun comes from exploring a real,
lived-in house and getting better at navigating it — content lives in *places* you
uncover, gated by what your robot can currently do. (Systemic routing — depth via
optimizing abstract paths/layouts — was considered and rejected: it would require
either procedural variety or heavy replayability to stay interesting, both of which
conflict with the "handcrafted, no procgen" constraints.)

Architectural implication: the core data model is a graph of authored spaces, each
carrying dirt/discovery/access state, gated by owned modules. No simulation/pathing
layer needed — plain state objects + content authoring, not algorithmic complexity.

## Design references / vocabulary in use

- Core gameplay loop = challenge → action → reward triad
- Daniel Cook, *The Chemistry of Game Design* — skill atoms / skill chains (model for
  ability-gated earned growth; closest theory to this design)
- Raph Koster, *A Theory of Fun* — fun = learning a pattern; boredom = nothing left to
  learn (why the loop must deepen, not just repeat)
- Sid Meier — a game is "a series of interesting decisions" (the depth test)
- Mark Brown / GMTK — applied Metroidvania + rewarding-exploration design
- **MDA framework** (Hunicke, LeBlanc, Zubek — *MDA: A Formal Approach to Game
  Design and Game Research*, 2004): Mechanics produce Dynamics produce Aesthetics;
  design backward from the intended Aesthetic. **This is the working tool for
  deciding which RPG systems (inventory, stats, appearance, quests, ...) actually
  belong in this game**: for each candidate system, trace whether it produces a
  Dynamic that serves one of the stated Aesthetics below — "epic feeling of growth
  and consequence," "dopamine of dirty-to-clean as discovery," "feels like a
  serious RPG despite the premise." If the chain traces cleanly, the system is
  foundational and worth building deliberately. If it doesn't, it's genre
  furniture inherited from the "RPG" label, not something *this* game's experience
  needs — at least not yet.

## About the developer

~7 years in games/software (tech art on Star Trek Online, asset QC in CryEngine,
currently building XR broadcast systems in Unity/C# — NDI, WebRTC, OBS WebSocket,
Netcode). Strong on systems, architecture, shaders, pipelines; C++ on CV. Primary
stack is Unity + C#. Vision-first, not mechanics-first — wants design settled before
committing to architecture. Explicitly leaving behind a prior project that became
spaghetti under deadline pressure; this time wants structure from the start —
**but informed by a felt prototype, not assumptions** (see Process below).

## Architecture philosophy: build a skeleton that absorbs iteration

*See `docs/architecture-foundations.md` for the full treatment — what "Core" is,
how it differs from "modules"/"features," and how this maps onto Nystrom's Game
Programming Patterns plus real precedents (Creation Engine, Pearl Abyss-style
ability layering). This section stays as the short summary.*

Game design at this scale is necessarily iterative — you cannot fully predict what
will be fun or what systems the game will need before playing it (this is well
documented; "100-page design doc written before any prototype" is a largely
abandoned approach precisely because most of its guesses turn out wrong once played).
So the architecture's job isn't to anticipate every future feature — it's to be
**built so that what iteration reveals can be absorbed without rewrites.**

The proven principle for this (Parnas, *On the Criteria To Be Used in Decomposing
Systems into Modules*, 1972): you usually can't predict the *specific* change
coming, but you usually CAN predict the *category* of thing likely to change —
and that's enough. Identify each such axis, and hide it behind a stable boundary
so change within it never ripples outward. Apply this *targeted* — only where an
axis is actually identified, not everywhere (over-applying it produces
over-engineered mush, which is its own form of spaghetti).

**Axes of change identified so far, and the technique that fits each:**

- **New modules / abilities, new obstacle types** → *data-driven content*: each is
  a small data asset (Unity ScriptableObject) plugging into one generic system,
  rather than a code branch per type. Adding one means authoring an asset, not
  writing new code. (Reference: Ryan Hipple, "Game Architecture with Scriptable
  Objects.")
- **Systems that will arrive later** (inventory, quests-as-system, stats — per the
  MDA pass below) → *event-driven communication* (the Observer pattern /
  Open-Closed Principle, Meyer 1988): the core loop emits events ("area cleaned,"
  "item picked up," "module acquired") without knowing who listens. A later system
  just subscribes — existing code is never touched to add it.
- **Mechanic feel that will get tuned through play** (move speed, clean radius,
  battery drain, dirt regrowth rate) → *tunable values*: numbers live as data/config
  the system reads, not constants baked into code, so tuning is "change a number,"
  not "edit and recompile."
- **Everything else** — write it as plain, direct code. Targeted hiding only where
  an axis is actually identified; plain code is the easiest thing to read, debug,
  and refactor later if it turns out to need a boundary after all.

## Worked example: classifying behaviors, not nouns

Important clarification that wasn't obvious at first: the four buckets above
(data-driven content / event-driven / tunable value / plain code) **don't apply to
whole nouns** ("the robot," "inventory") — they apply to *individual behaviors* a
noun has. A single noun is normally a mix of all four. Trying to put "the robot" as
a whole into one bucket is the wrong move and the source of the "I don't know which
one it is" feeling.

**The test for "is this behavior an event":** *Could something else in the game
plausibly need to know this happened — even if that something doesn't exist yet?*
If yes → raise it as an event; the robot doesn't need to know who's listening, and a
future system (inventory, quest tracker, stats) can subscribe later without the
robot ever changing. If no → it's just a method call, write it directly.

**Worked example — breaking "the robot" into its behaviors:**

- *Moves in response to input* → plain code (the mechanism is stable); the speed
  number is a tunable value.
- *Cleans dirt on contact/action* → plain code mechanism; possibly a tunable number
  (suction amount, radius).
- *Carries modules/abilities* → data-driven content. Each module is a small asset;
  the robot just asks "do I have capability X?" New module = new asset, not new code.
- *Announces things that just happened* ("cleaned a spot," "picked something up,"
  "ran out of battery") → event-driven (publisher side). Nothing subscribes yet
  (inventory/quests don't exist) — that's fine. You raise the event because you can
  *foresee* something will eventually care, not because a subscriber exists today.
- *Reacts to being switched off / reset* → event-driven (subscriber side) — reacts
  to an external trigger (human turns it off → resets, drops carried items). This is
  where the "turned off / inventory thrown away" mechanic lives architecturally.

Net result for "the robot": **mostly plain code that does its job, plus a few places
it announces things (publisher) and one place it reacts to an outside trigger
(subscriber).** That's a correctly-scoped v1 robot, not an under-designed one — most
of "moving and cleaning" genuinely is just straightforward code.

**Open task — apply the same breakdown (list behaviors, then classify each) to:**
modules/upgrades, obstacles (cat tail, boxes, fur), inventory, quests, dirt patches.

## Design/architecture process

1. ~~Resolve spatial-vs-systemic fork~~ — done, see above.
2. **Decide which RPG systems this game actually needs** — using MDA (above).
   In practice now driven by `docs/designers vision.md`: the full player-facing
   flow, written scene-by-scene, is the source for which systems exist. The MDA
   test ("does this serve a stated Aesthetic, or is it inherited genre furniture?")
   still applies per-system as each is built.
3. **Build a walking skeleton** (Cockburn) — every screen/system from
   `docs/designers vision.md`'s flow, wired end-to-end through Core, at minimal
   depth. Sequence: GameFlow seam (screen state machine) → splash → title →
   save/load → settings → new game/cutscene → tutorial → core loop (already
   prototyped, now rewired through Core instead of prototype code).
4. Set up architecture using the axes-of-change mapping above and
   `docs/architecture-foundations.md`'s core inventory (events, capability tags,
   attributes, game state, action slots, GameFlow): data-driven assets for
   modules/obstacles/cutscenes, events for cross-system communication, tunable
   values for anything play will need to adjust, plain direct code for everything
   else.
5. Playtest the skeleton end-to-end (splash through the first "ding") before
   deepening any one system's content.

Companion docs: `docs/architecture-foundations.md` (what Core is, Nystrom/Parnas
mapping), `docs/designers vision.md` (the flow the skeleton is built from),
`docs/pre architectural investigation.md` (early raw notes).

For current status and active work, see `docs/plan.md`.
