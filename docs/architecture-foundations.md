# Architecture Foundations: what "core" is in a modular RPG

*Deliverable of the architecture-foundations investigation, 2026-06-12.
Companion to `design.md` (which owns the design and the axes-of-change
framework) and `pre architectural investigation.md` (raw notes this document
answers). Scope: the conceptual model needed to design the vertical slice's
architecture — not the design itself.*

---

## 0. The short answer

Everything below is evidence for one sentence:

> **Core is the part of the game that knows *how* without knowing *what*.
> A module is a self-contained package of *what* that speaks only the
> vocabulary core defines.**

In every modular game architecture examined here — Skyrim's Creation Engine,
Pearl Abyss's ability-set layering, Unreal's Gameplay Ability System — core
turns out to be the same four things wearing different clothes:

1. **A vocabulary** — the shared identities and categories content is written
   against (record types, keywords, tags, attribute names, event names).
2. **Interpreters** — generic systems that execute *any* content speaking that
   vocabulary (the weapon system runs any WEAP record; the ability runner runs
   any granted ability).
3. **Plumbing** — the channels things communicate through without referencing
   each other (event bus, story manager, message queue). *Precision worth
   fixing here because it's a recurring confusion point: in every precedent
   below, "event bus" means a small set of named, typed channels — Skyrim's
   per-event Story Manager hooks, GAS's per-effect callbacks, this project's
   `Events.cs` (§6.2) — never one generic dispatcher keyed by strings. See
   §1.4 for a third, independent confirmation of this.*
4. **A registry** — the thing that resolves an identity to live content at
   runtime (the form table, the service locator, the owned-modules set).

And "modularity" has a precise, testable meaning — it is not a feeling of
tidiness, it is the **Open-Closed Property** (Meyer, 1988):

> Adding a new *what* (a module, an obstacle, a quest) requires **creating new
> files only — zero edits to existing files.** If adding slim mode means
> touching `Robot.cs`, the architecture isn't modular *at that boundary*, no
> matter how clean the folders look.

The rest of this document derives that shape from the two precedents, maps it
onto Nystrom's pattern catalog, audits the existing axes-of-change framework
against it, and answers the specific confusions ("do I define components up
front?", "what's the minimal core?", "how do modules connect?") directly.

---

## 1. Precedent A — Skyrim's Creation Engine: modularity of *content*

This is the best-documented real case, because fifteen years of modding has
reverse-engineered it nearly completely (UESP, Creation Kit wiki, xEdit). It
is the closest existing thing to the "how to build Skyrim" book.

### 1.1 Anatomy

**Everything is a record.** Every weapon, NPC, quest, room, perk, spell,
faction, and balance number in Skyrim is a **form** — a typed data record with
a unique 32-bit **FormID** — stored in ordered plugin files (`.esm`/`.esp`).
Record types are a fixed schema the engine defines: `WEAP` (weapon), `NPC_`,
`QUST` (quest), `CELL` (interior space), `PERK`, `KYWD` (keyword), `GMST`
(game setting), and a few hundred more.

The crucial negative fact: **the engine contains no class named `IronSword`.**
It contains a weapon *interpreter* (`TESObjectWEAP` machinery) that can hydrate
and run *any* WEAP record. The engine ships knowing the *category* "weapons
exist and have damage, reach, a model, keywords"; which weapons exist is
entirely the plugins' business. The engine references content only by FormID,
resolved through a global **form table** at load time — never by a compile-time
symbol.

**Base records vs. references.** A WEAP record is the *archetype* of a sword.
A `REFR` record is a *placed instance* — "that archetype, at this position in
this cell, with these per-instance overrides." Ten thousand placed iron swords
share one base record. (Hold this distinction; it returns as Nystrom's Type
Object + Flyweight in §3.)

**Keywords are the load-bearing trick.** A `KYWD` record is a pure identity —
a tag with no data: `WeapTypeSword`, `ArmorHeavy`, `ActorTypeUndead`. Systems
are written against keywords, never against specific items. The Bladesman perk
doesn't list every sword in the game; its condition tests
`HasKeyword(WeapTypeSword)`. This is why a modder can add a sword in 2024 and
every 2011 perk applies to it automatically — **the keyword is the contract
between content and systems.** Content declares what it *is*; systems declare
what they *care about*; neither ever names the other.

**Conditions: behavior as data.** Perks, magic effects, and dialogue lines
carry *condition lists* — data — evaluated by a generic condition engine. Code
supplies the predicate vocabulary (`GetIsRace`, `HasKeyword`,
`GetQuestCompleted`); designers compose predicates into logic without
recompiling. A tiny interpreter over a data language (Nystrom's *Bytecode*
chapter, in miniature).

**Perk entry points: override-as-data.** A perk doesn't contain arbitrary
code; it hooks one of a fixed list of engine extension points — "Modify Attack
Damage," "Mod Activate," "Modify Buy Price" — with conditions plus a value
function. The engine defines *where* behavior may be modified; data defines
*whether and how*. Note this carefully: it's pattern B (capability override)
implemented inside pattern A's data system. The two precedents are not rivals.

**Game settings.** Every tunable number — jump height, barter rates, carry
weight formula constants — is a `GMST` record. Your "tunable values" axis, in
Bethesda's architecture, is simply *more records*. Tunables aren't a separate
mechanism; they're the degenerate case of data-driven content (a record with
one float in it).

**Quests and the Story Manager.** A quest is a `QUST` record: stages,
objectives, *aliases* (generic slots — "the quest giver," "the dungeon" —
bound to concrete references at runtime by condition-driven search; this is
how radiant quests retarget), and script fragments. Quests *start* via the
**Story Manager**: gameplay raises events (`OnDeath`, `OnChangeLocation`,
`OnItemAdded`), and the story manager runs them through condition trees on
quest nodes. **The kill code does not know quests exist.** It announces; the
quest layer listens. This is your event-bus intuition, shipped at AAA scale.

**Papyrus scripts** attach to records for bespoke behavior, calling into a
fixed, safe engine API. A script can move an actor or start a quest; it cannot
reach into the renderer. (Nystrom's *Subclass Sandbox*: many varied behaviors,
one protected toolkit.)

**Load order and the Rule of One.** Plugins load in order; if two plugins
define the same FormID, the last one wins entirely. Override semantics at the
record level — this is how patches and mod compatibility work.

### 1.2 So what is "core" in the Creation Engine, literally?

| In core (engine) | NOT in core (plugins/content) |
|---|---|
| The record schema (what fields a WEAP has) | Any actual weapon |
| The form table (FormID → record resolution) | Any actual FormID's meaning |
| Per-category interpreters (weapon system, magic system, quest engine, AI) | Any quest, spell, or behavior tuning |
| The keyword *mechanism* and condition *evaluator* | Which keywords exist*, which conditions any perk uses |
| Event plumbing (story manager dispatch) | Who listens and why |
| The Papyrus VM and its API surface | Every script |

\* Mostly — a few keywords are engine-known anchors; the open set is content.

**What makes a module a module here:** a plugin is valid if and only if it
speaks the schema — right record types, right field shapes, references by
FormID, gates by keyword/condition. It never links against engine internals
and never against other plugins' code (only their records, by ID). That's the
whole contract. The module boundary is a *grammar*, not a folder.

### 1.3 The Parnas reading, and why studios need this

Parnas (1972): decompose by *what is likely to change*, and hide each such
"secret" behind an interface so change never ripples. The Creation Engine's
secret is **"what content exists."** Content churns daily — hundreds of
designers, then DLC, then thousands of modders. The schema changes rarely. So
the boundary goes exactly there: schema stable, content fluid.

The org chart mirrors it (Conway's law): engine programmers own
schema + interpreters; quest and level designers author records in the
Creation Kit all day *without recompiling and without talking to engine
programmers*. Modding is the extreme proof — thousands of strangers extend
the game with zero coordination, which is only possible because the contract
is data-shaped.

**The solo-dev translation** (this matters for HaywireCleaner): you don't have
parallel *people*, you have parallel *roles separated by time*. The two
collaborators the boundary protects are:

- **You-in-the-designer-hat** vs **you-in-the-programmer-hat** — when adding a
  module or obstacle means authoring an asset in the inspector instead of
  editing code, design iteration stays in flow (Koster: fun is *found* by
  iterating; iteration cost is therefore a design constraint, not a
  programming nicety).
- **Present-you** vs **future-you** — in three months you'll have forgotten
  the internals. A boundary that survives forgetting is the same boundary that
  would survive a coworker.

### 1.4 A third confirmation, independently sourced: CryEngine's typed entity events

Pearl Abyss's BlackSpace internals (§2.1) aren't public, but a different,
currently-shipping AAA RPG running a fork of a *publicly documented* engine
answers the same "what is plumbing, actually" question independently:
**Kingdom Come: Deliverance II**, built on a heavily modified CryEngine fork
(Warhorse Studios — confirmed in the game's own technical coverage; they had
source access and rewrote rendering, AI, and quest systems on top of it).

CryEngine's documented entity-component layer has no generic event bus. A
component declares which typed `SEntityEvent` values it cares about via
`GetEventMask()`; the engine calls `ProcessEvent()` only on components that
asked for that specific event, scoped to that entity — never a string-keyed
broadcast to the whole game. Above that sits **Schematyc**, the engine's
quest/level-logic layer: it listens to those typed component signals and
turns them into named, queryable state. That's a *designed aggregation
layer*, not a second bus — the same shape as the Story Manager (§1.1) and
this project's `ModuleSystem` (§6.3's acquisition-flow trace).

The point isn't "CryEngine specifically." It's that three independent
lineages — Bethesda's Creation Engine, Epic's GAS, and CryEngine as used by
Warhorse — converge on the identical answer: **typed, scoped signals out; a
dedicated aggregator in; no generic dispatcher anywhere in the stack.** When
three unrelated AAA engines built for different genres land on the same
shape, that's the shape, not a house style.

---

## 2. Precedent B — Pearl Abyss-style capability layering: modularity of *behavior*

### 2.1 What's observable (and an honesty note)

Pearl Abyss's internals (Black Desert's engine, Crimson Desert's BlackSpace
engine) are not public the way Creation Engine internals effectively are. What
follows separates *observable design* from *inferred implementation*.

Observable in Black Desert / Crimson Desert:

- **Mounts carry their own ability sets.** A horse knows Drift and Sprint; an
  elephant knows different verbs. While mounted, *your controllable surface is
  the mount's* — your inputs resolve against its skill set, not your
  character's.
- **Weapon stances swap movesets.** Drawing the awakening weapon replaces most
  of the main-hand skill set with a different one on the same inputs.
- **Succession vs. Awakening** (BDO, documented in patch notes and skill
  windows): choosing Succession *upgrades and replaces* your base main-hand
  skills in place and locks the awakening set. The same input slot resolves to
  a different skill depending on a character-level choice. Likewise "Absolute"
  skill versions override lower-rank versions of the same skill on the same
  command.
- Skills themselves are visibly *table-shaped data*: ID, rank, input command,
  damage coefficients, animation reference, cooldown — the standard Korean
  MMO data-table lineage.

Inferred (flagged as such): the runtime mechanism is almost certainly an
**action-slot table with layered resolution** — input maps to a slot, and
what fills the slot is looked up through a priority stack (mount layer over
stance layer over base layer). The inference is safe because there's a fully
public, documented industrial implementation of exactly this pattern to check
against: **Unreal's Gameplay Ability System (GAS)** — source-available,
shipped in Fortnite. Where Pearl Abyss specifics are unknowable, GAS is the
citable stand-in for pattern B.

GAS in one paragraph, because it names the parts cleanly: an actor carries an
`AbilitySystemComponent` (the shell). **Abilities** are granted and revoked at
runtime — a class object/data definition per ability, instantiated when
granted. **Attributes** (health, stamina, *anything numeric*) live in attribute
sets and are modified only through **GameplayEffects** — declarative modifiers
(add / multiply / override, with duration and stacking rules), never by direct
assignment. **GameplayTags** — hierarchical identities, GAS's keywords — gate
everything: an ability declares tags it requires, tags that block it, tags it
applies while active. Activation resolves through the tag state. Swap which
abilities are granted and the same actor shell *is* a different character — or
a mount.

### 2.2 The shape of pattern B

The fixed part — the **actor shell** — is:

- **Action slots**: a small fixed set of semantic inputs (Attack, Interact,
  SlotQ...). Input maps to slots; slots resolve to whatever ability currently
  fills them. Input never maps to functions.
- **An ability runner**: generic activation machinery — can-it-run gating
  (tags, costs, cooldowns), lifecycle (start/tick/end), hookup to animation
  and effects. It runs *any* ability; it knows none.
- **An attribute pipeline**: named numeric attributes resolved as
  *base value + ordered modifiers*, where modifiers come and go with their
  sources (effects, stances, equipment). Nothing ever sets an attribute
  directly; everything contributes a modifier it later removes.
- **Resolution rules**: what happens when several granted things claim the
  same slot or attribute — a defined layer order (mount > stance > base) or
  explicit priorities. This is the part newcomers don't realize is a
  *component of the architecture* rather than ad-hoc if-statements.

The fluid part — the **modules** — are ability sets and effect bundles: a
weapon's moveset, a mount's verbs, a buff. What makes one a module: it acts
*only* through the defined channels — grant/revoke abilities, add/remove
modifiers, raise/consume tags. It never edits another module and never reaches
into the shell's internals.

### 2.3 Core vs. module in pattern B

| In core (the shell) | NOT in core (modules) |
|---|---|
| Slot table + input routing | What fills any slot |
| Ability runner (gating, lifecycle, anim/FX hookup) | Any specific ability |
| Attribute names + modifier pipeline | Any specific modifier |
| Tag mechanism | Most specific tags |
| The layer/priority resolution rule | — (modules *declare* priority, core *applies* it) |

Same sentence as before, different clothes: core knows **how abilities
activate, stack, and resolve**; modules know **which abilities exist**.

### 2.4 A vs. B — what's actually different

Pattern A is modularity of **nouns in the world**: what exists. Its core is a
content pipeline — schema, registry, interpreters. Pattern B is modularity of
**verbs on an actor**: what you can currently do. Its core is a runtime
resolver — slots, grants, layers.

They compose, and mature engines do both: Skyrim's perk entry points and
shouts are pattern B (hooks, overrides, granted verbs) *authored as* pattern A
(records). BDO's layered skills are pattern B *stored as* pattern A data
tables. The composition rule:

> **Author capabilities as data (A); resolve them through a layered runtime
> (B).**

That sentence is the architecture of "this sort of RPG."

---

## 3. The Nystrom mapping: these precedents ARE the patterns, combined

*Game Programming Patterns* reads as a parts catalog; the precedents are
assembled machines. Same parts:

**Pattern A — Creation Engine:**

| Creation Engine mechanism | Nystrom chapter | The mapping |
|---|---|---|
| Base record vs. placed reference | **Type Object** + **Flyweight** | The "class" of a sword is a data record, not a C# class (Type Object); thousands of instances share it (Flyweight — his tree-rendering example, applied to game data) |
| NPC record aggregating AI + inventory + factions + spells | **Component** | An actor is a bag of orthogonal data/behavior parts, not a god-class |
| Form table, FormID resolution | **Service Locator** (data flavor) | Systems reach content through one registry, never via compile-time links |
| Story manager, script events | **Event Queue** / Observer | Gameplay announces; quest layer subscribes; neither knows the other |
| Condition lists, Papyrus | **Bytecode** | Behavior expressed as data, run by a small interpreter — the maximal version of "data-driven" |
| Papyrus API surface | **Subclass Sandbox** | Unbounded variety of scripted behaviors, all through one fixed, safe toolkit |
| Leveled lists, actor templates | **Prototype** | New content stamped from existing content with deltas |

**Pattern B — capability layering / GAS:**

| Mechanism | Nystrom chapter | The mapping |
|---|---|---|
| Input → action slots → commands | **Command** | Inputs reified as objects; rebindable, queueable, replayable — chapter one of the book is literally this |
| Mounted / stance / on-foot moveset swap | **State** (hierarchical / pushdown) | Each stance is a state whose job is to define the available action set |
| Slot filled by highest-priority granted ability | **Strategy** (GoF, via his intro) — with override layering, **Decorator** | The slot holds a swappable behavior; layers wrap/replace lower ones |
| Each ability implements Activate() against a fixed actor API | **Subclass Sandbox** | His chapter's running example is literally superpowers — this is that, shipped |
| Ability definition as data, instances point at it | **Type Object** | Same move as base records |
| Attribute base + modifiers, recomputed on change | (no single chapter; Update Method adjacent) | The one load-bearing piece Nystrom doesn't cover directly — see §5 |
| Effects announce application/expiry | **Event Queue** / Observer | Feedback systems (UI, VFX, audio) attach without entangling |

The practical takeaway: **you already know the parts.** What the book doesn't
say — and what this document is for — is the assembly diagram: *vocabulary +
interpreters + plumbing + registry in the middle; typed data packages around
the outside; events flowing out; resolution flowing in.*

---

## 4. Validating the axes-of-change framework against both precedents

`design.md` already commits to four buckets: data-driven content /
event-driven communication / tunable values / plain code, applied per-behavior
(not per-noun), only where an axis of change is identified. **Verdict: this is
genuinely the same architecture the precedents point at** — bucket 1 is
pattern A's records, bucket 2 is the story-manager move, bucket 3 is GMST
(note: in CE, tunables are literally just more records — buckets 1 and 3 are
one mechanism with two authoring conveniences), bucket 4 is the interpreters
themselves. The instinct in the pre-architectural notes — "small core,
features never reach into each other, communicate through core" — is the
correct shape.

But the framework as written is missing **five pieces** that both precedents
treat as load-bearing. These are the answer to "what am I missing to write the
core myself":

### 4.1 The identity vocabulary (Skyrim's keywords)

The framework says modules are "data assets plugging into one generic system"
— but doesn't say what the *shared language* between content and systems is.
The robot asks "do I have capability X?"; a sofa gate asks "does the robot
have capability X?" — **X itself must be a first-class identity that both
sides reference**, defined in core, owned by neither.

In Unity terms: a `CapabilityTag` ScriptableObject (an empty asset — its
*identity* is its content, exactly like a KYWD record). The slim-mode module
asset references the `SlimProfile` tag; the low-sofa gate volume references
the *same asset*. Adding a fourth module = creating a tag asset + module
asset; no enum edited, no switch extended. (An enum would be a code edit per
module — an Open-Closed violation hiding in plain sight.)

### 4.2 The resolution layer (pattern B's contribution)

The worked example in `design.md` covers events-out and has-capability checks
— but not what happens when a module **changes how an existing action
resolves** (suction changes the clean action; slim changes effective height).
Boolean "do I have X?" checks don't scale to that. The missing concept is the
**attribute pipeline**: named numeric attributes (Clearance, SuctionPower,
MoveSpeed) resolved as base + modifiers contributed by owned/active modules,
plus a defined rule for conflicts (§6.3). At three modules this is a few dozen
lines — but it must be a *concept*, not scattered if-statements.

### 4.3 Events tell you what changed; state tells you what is

An event bus alone is not enough plumbing — both precedents pair it with
queryable state (the form table; quest stage state; attribute queries). The
principle, worth engraving:

> **Provide events for *changes* and queryable state for *current truth*.
> Never force a system to reconstruct current truth by replaying event
> history.**

A UI panel enabled mid-game must be able to *ask* "what modules are owned?
how clean is area 3?" rather than depending on having witnessed every
`ModuleAcquired` since boot. This single rule kills the entire
late-subscriber class of bugs — the most common failure mode of event-bus
architectures.

### 4.4 Stable IDs and the state/view split (the save/load contract)

The pre-architectural notes correctly flag save/load as expensive to
retrofit. Pattern A says exactly what to do *now*, cheaply: **FormIDs are why
Skyrim saves work.** Identity (stable, serializable) is separated from
instance (the live object). The HaywireCleaner translation:

- Every stateful entity — each area in the space graph, each module, each
  authored dirt patch — gets a **stable ID** (the asset itself can serve as
  identity in-editor; a string/GUID on it serves persistence).
- Authoritative game state lives in **plain C# objects** — the space-graph
  state (per-area: dirt remaining, discovered, accessible), the owned-module
  set, bead count — *not* scattered across MonoBehaviour fields.
  MonoBehaviours are *views* that read and display that state.
- "Save" is then: serialize the plain state object. Even if v1 never ships
  saving, this discipline costs nothing now and is brutal to retrofit — and
  `design.md` already gestures at it ("plain state objects + content
  authoring").

### 4.5 A composition root (the minimal registry)

Something must create core's services in a defined order and make them
reachable. The precedents have heavyweight versions (the form table, engine
boot). The minimal Unity version: **one bootstrap object** that constructs/
wires core services explicitly, with one access point features use to reach
*core seams only*. Nystrom's Service Locator chapter warnings apply — global
access is a real cost — and the mitigation is scope: the locator exposes the
half-dozen core services and *nothing else*; features never expose statics of
their own. A full DI framework is cargo cult at this scale (§9); ambient
`FindObjectOfType` soup is the spaghetti being escaped. One bootstrap object
is the line between them. (No system scheduler is needed — Unity's player
loop is sufficient at this scope; explicit ordering exists only at boot, in
the composition root.)

This isn't just an indie-scale shortcut — it's what the engine authors do
too. Unreal's own answer to "how do systems get composed" is **Subsystems**
(`GameInstanceSubsystem`, `WorldSubsystem`): auto-instanced, explicitly
registered, with `InitializeDependency<T>()` called when one subsystem
genuinely needs another to exist first — not a reflection-based IoC
container resolving an object graph. Epic's own documentation states the
best practice as *minimizing direct subsystem-to-subsystem references in
favor of events/interfaces*. Composition root + typed events, not container
+ generic bus, is the documented choice at engine-author scale, not a
concession made for solo/jam scale.

---

## 5. The robot's modules under pattern B: does the mapping hold?

Claim under test: slim mode / suction / climb are Crimson Desert's
weapon-skill overrides in miniature. **It holds, with one refinement** — the
three modules are actually three *different* module-effect kinds, and seeing
that is more useful than the analogy itself:

| Module | Effect kind | Mechanism |
|---|---|---|
| **Slim mode** | *Attribute modifier* (+ possibly a toggled state) | Modifies `Clearance`; if active-toggle, it's a granted ability that enters/exits a state |
| **Suction** | *Modifier on an existing action* | Modifies `SuctionPower`/`CleanRadius` consumed by the clean action — or, stronger version, *overrides* the clean-action handler in its slot |
| **Climb** | *Granted contextual verb* | Adds a new ability into the Interact slot, gated on a world tag (`Climbable` surface) |

So the module system's contract — what a module asset is *allowed to declare*
— is exactly three channels, and this is the whole "ability resolution
mechanism" at this scale:

```
ModuleDef (a ScriptableObject — your WEAP record):
  grantsTags:      [CapabilityTag]          // identity claims (4.1)
  modifiers:       [(Attribute, op, value)] // attribute contributions (4.2)
  grantedActions:  [(slot, ability, priority)]  // new/overriding verbs
  activation:      passive | toggleable     // design decision, see below
```

**Worked trace 1 — slim mode and the low sofa.** Robot base `Clearance` =
12cm. Sofa gap = 8cm. The gate is not "is slim owned?" — the gate volume
checks `robot.Attr(Clearance) <= gap` (or blocks physically via the collider
the attribute drives). Acquire slim mode → `ModuleAcquired` event → robot's
capability cache recomputes → slim contributes `Clearance override 6cm` (while
active, if toggled) → same check now passes. Nobody edited the sofa, the
robot's movement code, or the other modules.

Note what the *numeric* gate buys over a boolean one: any future module that
also affects height composes automatically, and level design gains a free
expressive axis (7cm gap vs 9cm gap are different gates). This directly
serves `design.md`'s "depth comes from module × space combinations" — the
combinatorics live in data, costing zero code per combination.

**Tag vs. attribute gates — when to use which:** attribute-gate where the
quantity is physical and continuous (clearance, suction strength vs. debris
mass); tag-gate where the capability is categorical (can-climb-fabric:
yes/no). Skyrim uses both (keyword conditions and numeric conditions,
freely mixed). Both kinds of gate reference core vocabulary only.

**Worked trace 2 — suction and the clean action.** The clean ability (base,
priority 0, in the Clean slot) reads `Attr(CleanRadius)` when it fires.
Suction module contributes `CleanRadius +50%` — *no new handler at all*, the
existing action just resolves differently. If suction should instead *feel*
different (different sound, vfx, motion), ship it as an overriding handler:
`(Clean slot, SuctionCleanAbility, priority 10)` — the slot resolves to the
highest-priority granted handler whose `CanRun()` passes. Both options exist
inside the same contract; which to pick is a design call made later, with no
architectural consequence. That non-consequence is the architecture working.

**Worked trace 3 — climb.** Climb grants `(Interact slot, ClimbAbility,
priority 5)` with `CanRun()` = "facing a surface tagged Climbable." Press
Interact in front of a bookshelf: slot resolution walks granted handlers by
priority; climb's gate passes; it runs. In front of dirt: climb's gate fails;
resolution falls through to base interact/clean. **This fall-through is the
entire "override resolution" mechanism** — Crimson Desert's
weapon-supersedes-base-skills, scaled to one robot:

```
resolve(slot):
  for handler in slot.handlers ordered by priority desc:
    if handler.CanRun(context): return handler
  return null  // input does nothing — also a valid resolution
```

**Layer order at this scale:** active toggled module > owned passive modules >
base. With three modules an explicit integer priority on the module asset is
plenty — the AAA lesson to keep is only that *the rule is written down in one
place* (the resolver), not improvised per collision site.

**Passive vs. toggleable** is an open *design* question (is slim mode a
stance you enter — Daniel Cook skill atom with its own cue/action/feedback —
or a permanent fit-under-things upgrade?). The architecture above doesn't
care: `activation: passive | toggleable` on the asset, and the toggled case
is just a state whose enter/exit adds/removes the module's contributions. The
design question can stay open through the slice without blocking the build —
which is, again, the point of the boundary.

---

## 6. Direct answers to the standing confusions

### 6.1 "Do I need to strictly define components up front, or can this emerge?"

Split the question, because it has two different answers:

- **Contracts between features — deliberate, up front.** Event names and
  payloads, the capability-tag vocabulary, the attribute list, the ModuleDef
  shape, the save-state schema. These are expensive to change once two sides
  depend on them (Parnas: these *are* the interfaces; their stability is the
  whole payoff). For the slice this is a one-page decision, not a design
  phase.
- **Componentization inside a feature — emergent, freely.** Whether robot
  locomotion is one MonoBehaviour or three, how the dirt patch manages its
  vfx — refactor at will; nothing else can see in. Nystrom's own position
  (his architecture/performance intro) is that speculative internal structure
  is how over-engineering happens.

One Unity-specific disambiguation: Nystrom's *Component* pattern is what
GameObjects already are — that decomposition comes free. The decisions that
need making are one level up: *system* boundaries (feature ↔ feature,
feature ↔ core), which Unity does **not** provide and which is exactly what
§4's five pieces define. Slogan form: **define interfaces deliberately,
implementations opportunistically.**

### 6.2 "What's the minimal core that still HAS the shape?" — the line, made testable

The line between "architecture" and "three scripts talking to each other" is
not size or cleverness. It's four pass/fail tests:

1. **The Open-Closed test:** add a hypothetical 4th module (or 2nd obstacle
   type). Count files *edited* (not created). Zero = modular. One = a leak to
   fix. (This is the test to actually run, on paper, before writing the slice.)
2. **The dependency-direction test:** no feature script references another
   feature's concrete types. Features reference Core; Core references
   nothing above it. (`grep` for cross-feature `using`s; or enforce
   mechanically with one asmdef per feature — the solo-scale version of a
   studio's module build boundaries.)
3. **The late-join test:** any listener enabled mid-session reaches correct
   current truth by querying state (§4.3), not by having observed history.
4. **The save-dump test:** authoritative game state can be serialized and
   restored without touching scene objects (§4.4) — even if no save feature
   ships in v1.

**The literal minimal core inventory for the 3–4 room slice** — small enough
to be unintimidating, and everything in it earns its place via the tests:

```
Core/                        (assembly: references nothing above it)
  Events.cs                  typed events: AreaCleaned, DirtCleaned,
                             PickupCollected, ModuleAcquired, ModulesChanged,
                             RobotReset ...   [plumbing]
  CapabilityTag.cs           empty ScriptableObject = identity   [vocabulary]
  AttributeId.cs / Attributes.cs   names + base+modifier resolver [vocabulary,
                             interpreter]
  ModuleDef.cs               the SO schema from §5                [vocabulary]
  ModuleSystem.cs            owned/active sets; applies grants/modifiers;
                             raises ModulesChanged                [interpreter,
                             registry]
  ActionSlots.cs             slot table + priority resolver from §5
                             [interpreter]
  GameState.cs               plain-C# space-graph + robot state; stable IDs
                             [registry, save contract]
  Bootstrap.cs               composition root; sole access point to the above
Features/                    (each references Core only; never each other)
  Robot/      locomotion, input intents → slots   [mostly plain code]
  Cleaning/   clean ability, dirt patch behavior+vfx
  Gates/      clearance volumes, climbable tags
  Cat/        the renewable mess source
  UI/         subscribes to events, queries state
```

Eight or nine small files of core. **Core knows categories, never instances:**
it knows "modules grant capabilities" — it has never heard of slim mode. It
knows "areas hold dirt state" — it has never heard of the kitchen. The
grep-able leak detector: *if a specific noun (sofa, cat, suction) appears in
`Core/`, the boundary is breached.* (Skyrim's core knows WEAP exists, not
that Chillrend does.)

What stays *out* of core is just as defining: movement physics, the clean
action's feel, dirt vfx, the cat — all features, all plain direct code per
the fourth bucket, all replaceable without core noticing.

### 6.3 "How do modules connect to core — registration, discovery, lifecycle?"

The inversion to internalize: **core never news up features. Core defines
sockets; modules plug themselves in.** (Skyrim's engine doesn't load
"Falskaar" by name; Falskaar conforms to the plugin grammar and the loader
takes anything conforming.)

**Acquisition flow, end to end** (the worked example to hold onto):

```
player drives over bead pickup
  → Pickup (feature) raises PickupCollected(moduleDef)
  → ModuleSystem (core) adds def to owned set
      applies grantsTags / modifiers / grantedActions from the asset
      raises ModuleAcquired(def), then ModulesChanged
  → Robot's attribute cache (core machinery on the robot) recomputes
  → listeners react, each in its own feature, none knowing the others:
      UI shows "Slim Mode acquired"        (subscribes ModuleAcquired)
      gate volumes re-evaluate clearance   (subscribes ModulesChanged)
      sparkle vfx / sound fire             (subscribes ModuleAcquired)
      [future] quest tracker advances      (subscribes — exists later,
                                            changes nothing when it arrives)
```

**Lifecycle:** ordering is defined *once*, at the composition root (core
services first; features tolerate any order among themselves because of the
late-join rule — on enable, query current state, then subscribe to changes).
**Removal/reset:** every contribution is removed by its contributor — the
RobotReset subscriber (already in `design.md`'s worked example) clears
carried pickups; a toggled module's exit removes its modifiers. Nothing else
needs to know. That symmetry (whoever adds, removes) is what keeps the
attribute pipeline honest.

---

## 7. The Parnas table for HaywireCleaner

The 1972 paper's actual deliverable was a table: each module named by the
**secret it hides** (the thing allowed to change freely behind it). This
table *is* the vertical slice's architecture at the conceptual level — the
implementation of each row is yours to design:

| Boundary | Secret (free to change behind it) | Promise (stable interface) |
|---|---|---|
| Input | Device, bindings, controller vs. KB+M | Semantic intents: Move(dir), Interact, ToggleModule |
| Robot motor | Locomotion physics, tank-control tuning | Robot moves per intents; reads MoveSpeed/Clearance attributes |
| Capability system | Which modules exist; how effects stack | ModuleDef schema; Attr(id) queries; ModulesChanged |
| Action slots | Which ability handles an input today | resolve(slot) → highest-priority runnable handler |
| Space-graph state | Persistence format; area bookkeeping internals | Per-area dirt/discovered/accessible queries; AreaCleaned event |
| Dirt feature | Cleaning feel, vfx, patch lifecycle | Emits DirtCleaned/PickupCollected; reads clean-action attributes |
| Gates | Gate kinds (clearance, tag, future ones) | Reference core tags/attributes only |
| Event hub | Who listens to anything | Typed event signatures |
| Goal director | v1's hardcoded goals → future quest system | Subscribes to events; owns "current objective" |

The last row is the absorb-iteration story in miniature: v1's hardcoded goals
are a *feature* listening to events the loop already emits. When the real
quest system arrives (deferred per the MDA pass), it replaces that one
feature — the events were already flowing, nothing else moves. Same story for
inventory: `PickupCollected` is raised from day one; the inventory system, when
MDA justifies it, subscribes — months later, zero refactors.

Dependency picture (the only arrows allowed):

```
   Input   Robot   Cleaning   Gates   Cat   UI   GoalDirector
     \       \        |        |      /     /        /
      \       \       |        |     /     /        /
       └──────────────┴── Core ─────┴─────┴────────┘
                (events · tags · attributes · modules ·
                 slots · game state · bootstrap)

   Features → Core only.  Core → nothing.  Feature → Feature: never.
```

**Why these boundaries serve *this* game (the MDA check):** the stated core
aesthetic is "earned growth that reopens known space." The dynamic carrying it
is *revisit-with-new-module*; the mechanic carrying that is **gate resolution
against capabilities**. That makes the capability/gate seam the single most
load-bearing piece of architecture in the game — the one place where
generosity now (numeric attributes, shared tag vocabulary) buys design wealth
later (module × space combinations authored as data). Conversely, systems the
MDA pass hasn't justified yet (inventory, quests) correctly get *no
machinery* — only events already flowing. The architecture spends exactly
where the aesthetic lives. And per Cook's skill atoms: each module is a new
verb whose feedback loop (cue → action → feedback → reward) must be wireable
without code — which is precisely what feedback-as-event-subscribers gives
you (the sparkle, the sound, the UI ding attach to `ModuleAcquired` without
touching the mechanic).

---

## 8. What NOT to build (anti-cargo-cult, by precedent feature)

Each item below is real in the precedents and *wrong for this game* — with the
reason, so the judgment transfers:

- **Load order / override merging (Rule of One).** Exists to merge content
  from uncoordinated authors. One author — skip entirely.
- **Runtime content database / loading by string ID.** CE resolves FormIDs at
  runtime because mods arrive after shipping. Unity asset references resolve
  at edit time; ScriptableObjects referencing each other directly *are* your
  form table — simpler and type-safe. (Revisit only if mod support ever
  becomes real.)
- **A generic condition evaluator / scripting VM.** Bytecode-pattern machinery
  pays off at hundreds of designers' worth of content. With hardcoded v1
  goals, conditions are C# in the goal director. The future quest system is
  already absorbed at the event seam — no interpreter needed in advance.
- **Full GAS-style ability framework.** Three modules, three-ish slots: the
  resolver in §5 is a dictionary and a loop, a few dozen lines. Keep the
  *concepts* (slots, grants, modifiers, priority), skip the framework.
- **ECS / system scheduler.** Solves performance at extreme entity counts and
  composition at extreme cardinality. A house with one robot and one cat has
  neither. Unity's player loop + one composition root suffices.
- **Deep layer stacks.** Crimson Desert needs mount > stance > base because it
  has mounts and stances. The robot's stack is at most active-module > base.
  Keep the *rule written down*, implement it trivially.
- **DI framework.** One bootstrap object replaces it at this scale (§4.5).

What to keep *despite* small scale, because retrofit is brutal and the cost
now is near zero: stable IDs + state/view split (§4.4), the tag/attribute
vocabulary (§4.1–4.2), content-as-assets, events-out from day one, and the
cross-cutting contracts below.

---

## 9. Cross-cutting contracts (the expensive-to-retrofit list, settled now)

From the pre-architectural notes — each gets its cheap-now contract:

- **Save/load** → covered by §4.4: stable IDs, plain-object `GameState`,
  views rebuild from state. The slice ships the *shape* (state lives in
  GameState), not the feature (no save UI needed).
- **Localization** → the CE precedent: Skyrim stores no display text in code;
  strings live in localized string tables referenced by ID. Translation:
  *no player-facing string literal in any feature* — every UI string through
  a key lookup from a single table asset, even with one language. Costs
  minutes now; retrofitting string extraction later costs days.
- **Controller / mobile support** → the input boundary in the Parnas table:
  features consume semantic intents, never `Input.GetKey`. Unity's Input
  System action maps are literally this boundary already — adding a gamepad
  becomes binding work, not code work. (Nystrom's Command, applied.)
- **UI responsiveness** → dependency direction: UI subscribes to events and
  queries state; no feature ever references UI. UI is then rebuildable,
  restylable, or replaceable wholesale without gameplay edits — and the
  late-join rule (§4.3) means panels can appear/disappear freely.

(On RPGCore from the notes: its graph-based data-oriented design is pattern A
generalized into a reusable framework — built to power *many* games. Take its
boundary discipline, which this document's shape already encodes; skip its
graph abstraction, which is framework-scale generality this one game doesn't
need.)

---

## 10. Sources and further reading

Grounding for each load-bearing claim:

- **Parnas, "On the Criteria To Be Used in Decomposing Systems into Modules"
  (1972)** — modules as hidden secrets; the table format in §7. Short, read
  the original.
- **Meyer, *Object-Oriented Software Construction* (1988)** — the Open-Closed
  Principle; §0's modularity test.
- **Nystrom, *Game Programming Patterns*** — chapters mapped in §3 (free
  online; reread Component, Type Object, Subclass Sandbox, Event Queue,
  Service Locator, Command, State, Bytecode *after* this document — they read
  differently with the assembly diagram in hand).
- **Creation Engine, as documented by the modding community** — UESP
  ("Skyrim Mod" namespace: file format, FormIDs), the Creation Kit wiki
  (keywords, conditions, perk entry points, story manager, quest aliases),
  xEdit documentation (records, Rule of One). The closest existing "how
  Skyrim was built" text, in wiki form.
- **Unreal Gameplay Ability System documentation + source; the community
  "GASDocumentation" (tranek) deep-dive** — the public, documented industrial
  implementation of pattern B (abilities, attributes+modifiers, tags,
  granting). Read for concepts, not to port.
- **Unreal Engine Subsystems documentation** (`UGameInstanceSubsystem`,
  `InitializeDependency`) — Epic's own documented composition-root pattern;
  grounds §4.5's "DI framework is cargo cult at this scale" with an
  engine-author precedent, not just a solo-scale rationalization.
- **CryEngine Entity Components documentation** (`IEntityComponent`,
  `GetEventMask`/`ProcessEvent`, Schematyc signals) — the typed-event,
  no-generic-bus precedent in §1.4. Directly relevant because **Kingdom
  Come: Deliverance II** ships on a heavily modified fork of this engine,
  making it a live, current AAA RPG precedent rather than an inferred one.
- **Ryan Hipple, "Game Architecture with Scriptable Objects" (Unite Austin
  2017)** — the Unity-native instantiation of records-as-assets and
  SO event channels; already cited in `design.md`.
- **Black Desert skill system** (patch notes / skill UI: Awakening,
  Succession, Absolute skills; mount skills) — the observable evidence for
  §2.1. Internals inferred, flagged as such.
- **Hunicke, LeBlanc, Zubek, MDA (2004); Cook, "The Chemistry of Game
  Design"; Koster, *A Theory of Fun*** — the design-side frameworks this
  architecture answers to (§7's MDA check; iteration-cost-as-constraint).
- **Conway's law** — why studio org structure appears in engine architecture,
  and the solo translation in §1.3.

---

## Addendum: app-flow seam, as built (2026-06-12)

First concrete Core seam, built during the walking-skeleton start. Recorded here
because it's a worked instance of §6.3's "modules connect to core" answer, applied
to screens rather than gameplay modules.

*Naming note (corrected 2026-06-20): the names below were the conceptual
sketch written the same day this seam was built; the actual typed code used
`SceneStateMachine`/`GameScene`/`SceneLoader` from the first commit
(`184bc48`) onward, not `GameStateMachine`/`GameState`/`SceneFlowLoader` as
originally written here — drift between this write-up and the implementation
from day one, not a later rename. Corrected to match the shipped code; the
likely reason the implementation diverged is also a good one to keep:
`GameState` was free to mean the *save-data* object from §6.2's core
inventory (now `SaveData.cs`) instead of colliding with the screen-flow enum.*

- **`Core/SceneStateMachine`** — pure static class (no MonoBehaviour, no scene
  presence; §4.4's state/view split applied to flow itself). Holds
  `GameScene CurrentGameScene`, `event Action<GameScene, GameScene>
  OnGameSceneChanged` (from, to — not just "to"), `ChangeSceneTo(nextScene)`.
  `GameScene` is `public` — it's Core vocabulary Features must reference (§4.1
  pattern, applied to screens instead of capability tags).
- **Screens are separate Unity Scenes** (`Title.unity`, `Gameplay.unity`, ...), not
  Canvas panels in one scene. Stronger Feature↛Feature enforcement than asmdefs
  alone (no cross-scene Inspector references possible) — the scene boundary *is*
  the seam, physically. Also the practical win for solo step-by-step iteration:
  open `Gameplay.unity`, press Play, you're in it — no walking through Title first.
- **No Bootstrap scene, no `DontDestroyOnLoad`.** `Title.unity` is Build Settings
  index 0 (Unity's default loaded scene) *and* `GameScene.Title` is the enum's
  default value (0) — the defaults line up, so `CurrentGameScene` is already
  `Title` the instant the static field initializes, before any scene-load logic
  runs. Nothing needs to "load Title"; Unity already did.
- **`Bootstrap/Bootstrap`** — static class, `[RuntimeInitializeOnLoadMethod
  (BeforeSceneLoad)]`. Runs once, before Title's own `Awake`s. The one named
  exception to "Features → Core only, Core → nothing" (Seemann's composition
  root): the only place allowed to know both `Core` vocabulary and Feature scene
  names. Holds the `GameScene → scene name` map (configuration) and wires
  `Core/SceneLoader.LoadScene` to `SceneStateMachine.OnGameSceneChanged` (wiring).
  No behavior of its own, nothing depends on it — pure composition, per §4.5/§1.4's
  "composition root + typed events, not container + generic bus."
- **`Core/SceneLoader`** — static class, no MonoBehaviour. Given the
  `GameScene → scene name` map by Bootstrap via `Initialize(map)`; subscribed as
  the handler for `OnGameSceneChanged`, so `LoadScene(from, to)` runs on every
  transition: calls `SceneManager.LoadSceneAsync(sceneMap[to], Additive)` and
  unloads `sceneMap[from]` on `AsyncOperation.completed` (a plain C# event — no
  coroutine/MonoBehaviour needed). Generic plumbing: knows *how* to load a scene
  by name, never *which* names map to which `GameScene` — that's Bootstrap's
  configuration, not Core's.
- A debug-only scene (excluded from release builds) can host inspectable
  objects/tooling if needed — addresses the editor-visibility gap of having no
  Bootstrap scene, without reintroducing `DontDestroyOnLoad` or a persistent
  scene into the shipped seam.
- **Settings is NOT a `GameState`.** General rule (Nystrom's pushdown-automata
  extension to State, §3): top-level `GameState` states are mutually exclusive
  modes where "what was I doing before" doesn't matter (Title↔Gameplay). Anything
  reachable from multiple states that should *return to wherever it was opened
  from* — Settings now, Pause/Inventory/dialogue later — is an overlay/pushed
  state, not a `GameState`. Wired via `Core/UIEvents`
  (`OpenSettingsRequested`/`OpenSettingsClosed`-style events) so the opener and
  the panel never reference each other directly. A real stack isn't built yet
  (one overlay doesn't need it) but the event-based naming anticipates one.
- **Asmdefs**: `Core` (no asmdef references of its own — "Core → nothing" made
  physical, §7's dependency picture), `Features.Title` / `Features.Settings`
  (reference `Core` only, plus `UnityEngine.UI`).
- Splash screen dropped entirely (no publisher/engine requirement, no meaningful
  load time at this scale) — Title is the first screen and the seam's proof.

## Addendum: cutscene trigger shape, as decided (2026-06-21)

Three more precedents converge on the same vocabulary/interpreter split as
§1/§3, applied to *when does narrative content play*: Larian's Osiris
(event-driven, declarative — reacts to game events, queries state, fires
calls), CD Projekt's Facts system (flat key→value store; facts do nothing by
themselves, a separate generic comparison gates behavior — storage and
consequence fully decoupled), and HoYoverse's Miliastra Wonderland node
graphs (typed Event nodes as entry points, feeding generic, parameterized
Condition and Action nodes via data-flow wires — never a node-type per
trigger category). The shape, worth recording because it's now independently
confirmed three times:

> A discrete event enters → a generic, parameterized condition checks it →
> a generic action fires. Never special-case "kind of trigger" into a tagged
> struct with one field-set per case.

**This refines §8's "no generic condition evaluator" call — it doesn't
reverse it.** Adopt the *shape* now (one generic check, one generic action,
regardless of which event fired); keep the *condition* itself as plain C# (a
`Func<bool>` per cutscene id, written directly in `CutsceneDirector`), not a
serialized mini-language (`FactKind`/`Cmp`/key/value records, a condition
tree asset). Skyrim/CDPR/HoYoverse's conditions are *data* because hundreds
of designers author them without a programmer in the loop — that payoff
doesn't exist yet at one person's worth of cutscenes. Each origin system
(area, quest, cleanliness) still raises its own typed event *and* records
the corresponding fact in `WorldState` — a direct application of §4.3
("events for changes, queryable state for current truth") to this feature,
not a new rule. `CutsceneDirector` funnels every typed event into one
shared `Evaluate()` that reads `WorldState` through plain predicates, rather
than a bespoke `MatchesX` per trigger kind. Revisit the data-driven version
only if cutscene count outgrows what one programmer can keep up with by
hand — the same threshold §8 already names for the rest of the slice.

## Where this leaves the process

This document closes the "understand the concept" gap. Per `design.md`'s
process, the next steps remain: finish the MDA pass over candidate RPG
systems (step 2 — note `design.md`'s open task to behavior-classify
modules/obstacles/inventory/quests/dirt — §5's three-channel breakdown *is*
that method, demonstrated on modules), then design the slice's architecture —
which now means: fill in the Parnas table's implementations, run the four
tests of §6.2 on paper against "add a 4th module," and write the core
inventory of §6.2 yourself. The design is yours; this was the map.
