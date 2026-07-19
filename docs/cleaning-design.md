# Cleaning feature — design

One sentence: **the bot paints a mask texture as it moves; the floor's shader reads
that mask; how much of the mask is white decides how much dust was gained.**

Everything else follows from that.

---

## Pieces

Three components and one shader. Nothing in Core except two small contracts.

```
Bot (ActorHost)
 └─ CleaningModule            Features/Cleaning
      raycasts down, paints, credits dust

Floor (any mesh — Plane, ProBuilder, imported)
 ├─ MeshCollider              what the ray hits
 ├─ MeshRenderer + shader     samples _DirtMask, draws clean vs dirty
 └─ DirtSurface               Features/Cleaning
      owns the mask RT, paints into it, knows its own clean fraction,
      re-accumulates dust over time, raises the threshold edge

Core additions (only these two)
 ├─ Tag.InventoryFull         added to the existing Tag enum
 └─ ICollector                Core/Player — mirrors IChargeable
```

## Flow

```
each frame   CleaningModule: if actor has any blockedBy tag → do nothing
                             raycast down → GetComponentInParent<DirtSurface>()
                             paint a swept segment from last position to current

~4× / sec    DirtSurface:    generate mips on mask, async-read the 1×1 mip
                             that average IS the clean fraction
                             crossed the threshold? → raise SO channel event

~4× / sec    CleaningModule: Δ = surface.CleanFraction − remembered
                             dust = Δ × surface.Area × dustPerSquareMetre
                             actor.GetModule<ICollector>().TryCollect(...)

each frame   UI:             polls the number, damps toward it (the climb is the juice)
```

No events fire during normal cleaning. UI polls, quests listen for edges only.

---

## Decisions locked

**Representation**
- The mask is the only source of truth. Anything derivable from it is never saved.
- World-space XZ projection, not UV2. Mesh UVs are ignored, so any mesh works.
- Resolution authored as **texels per metre**, computed from bounds, power-of-two,
  clamped for memory. Designer gets a quality multiplier.

**Painting**
- One **swept segment** per frame, last position → current. Not N stamps.
  One draw call, gap-free at any speed, no spacing constant to tune.
- Standing still degenerates to a circle, which is correct.

**Accounting**
- Clean fraction = average of the mask = its smallest mip. One 1×1 async readback.
- `dust = Δ fraction × area × dustPerSquareMetre`. Cleaning clean floor yields zero,
  so it is self-correcting with no tuning constants.
- Clamp negatives — re-accumulation must never pay out.
- Refresh the remembered fraction on first contact, or stale values pay wrongly.
- Stored as **integer hundredths** (4237 = 42.37). Exact arithmetic, exact save
  round-trip, fits the existing `WorldState.counters`. Displayed as two decimals.

**Thresholds**
- At 8/10 a surface is declared clean and its mask wipes to full white.
- The wipe is **animated (~0.5s), never an instant pop.**
- It pays out the remaining 20% as a completion bonus. Deliberate.

**Boundaries**
- Cleaning is one feature. `CleaningModule` and `DirtSurface` talk directly.
- Cleaning never writes quest state. It publishes two things: a live clean fraction
  (polled) and a threshold crossing (SO channel, fires both directions).
- Blocking is by **tag mask**, serialized on the module — GAS Activation Blocked Tags.
  Inventory sets `InventoryFull`; cleaning never learns inventory exists.
- Dust transfer is via `ICollector` on the actor, item-agnostic. Core never says "dust".

**Scope**
- No rooms. Only surfaces that accumulate dirt. Quests target surfaces directly.
- No aggregator.

---

## Deliberately deferred

Not forgotten — waiting for a second case to teach us the right shape.

- `IDirtSurface` interface — one implementation exists, so a concrete reference is fine.
  Cheap to extract later because it never crosses a feature boundary.
- `SubstanceSO` strategy hierarchy — comes back when water is real.
- Per-object UV2 masks (`DirtSkin`) — try a per-prop scalar dissolve first; it is
  ~80% as convincing for ~5% of the pipeline cost.
- Dirt patches as discrete interactables — a different verb, stays a separate mechanism.
- Assembly definitions to enforce layering at compile time.

---

## Build order

1. **DirtSurface + shader.** Debug key paints. No module, no bot, no accounting.
   Get the trail *looking right* — this is the only artistic, iterative step.
2. **CleaningModule.** Raycast down, paint the swept segment. Still no dust.
3. **Clean fraction.** Mip readback, threshold event.
4. **Dust accounting + UI.** Deltas, `ICollector`, damped readout.
5. **Re-accumulation over time.**
6. **Save: folders + atomic rename, masks as PNG.**

Start at 1. Nothing else can be judged until the trail looks good.
