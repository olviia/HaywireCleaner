# Save System

Reference for adding save support to new features. Not a design-upfront doc —
update it as each feature is built. The skeleton ships a minimal `SaveData`; new
fields and sub-objects are added only when the feature they belong to exists.

**This document is a memory and a reference note, not a specification to follow
one-to-one.** Names, structures, and patterns here may change as the project
grows and as better approaches emerge through building. Treat it as accumulated
reasoning — the *why* behind decisions — not as a contract. When something here
conflicts with what you learn later, update the doc and move on.

---

## Architecture

| Piece | Location | Note |
|---|---|---|
| `SaveData` | `Core/` | Shared contract — every Feature references it, so it must live in Core |
| `ISaveBackend` | `Core/` | Seam definition only. Two methods: `Read(slot) → SaveData`, `Write(slot, SaveData)` |
| `LocalFileSaveBackend` | `Bootstrap/` | Concrete implementation. Writes to `Application.persistentDataPath` |
| `SteamCloudSaveBackend` | `Bootstrap/` | Added when Steam App ID exists. Plugs into same `ISaveBackend` seam |
| `SaveSystem` | `Core/` or `Bootstrap/` | Orchestrates: gather state → populate `SaveData` → call backend |

Bootstrap decides which `ISaveBackend` is active. Features never reference a
concrete backend — only `ISaveBackend` through Core.

---

## The three tiers

Every piece of state falls into one of these. Getting the tier wrong is how
saves become large (Sims problem) or incomplete.

**Tier 1 — Permanent facts** (sparse, never reset, tiny)
Quest completion, abilities acquired, collection flags, tutorial seen, permanent
world changes. Boolean or numeric facts keyed by stable string ID. Lives in
`facts` / `numericFacts` in `SaveData`, or in a dedicated sub-object when the
system owning them is complex enough to warrant it.

**Tier 2 — Mutable world state** (saved, changes during play, current snapshot)
Current dirt levels, resource values (battery, dust collector), NPC behavioral
state (timer, phase — not position), in-game clock. These are always saved
because they change continuously and can't be derived from authored data.

**Tier 3 — Transient state** (not saved — derived on load)
NPC positions (derived from schedule + in-game clock), physics object positions,
particle/VFX state, visual-only MonoBehaviour fields. If you're tempted to save
something in this tier, ask: "can I compute where this should be from Tier 1 +
Tier 2 + authored data?" Usually yes.

**The Sims problem** = accidentally putting Tier 3 in the save file.
**The Skyrim save-bloat problem** = Tier 2 data that never gets pruned when it
returns to authored default (see Pruning below).

---

## The factstore

`facts: Dictionary<string, int>` is the lightweight Tier 1 store. It covers:

- **Boolean flags**: `"intro_cutscene_seen" = 1`
- **Quest stages**: `"main_quest_stage" = 2`
- **Counts**: `"times_returned_to_dock" = 3`
- **Permanent world changes**: `"closet_door_permanently_open" = 1`

`numericFacts: Dictionary<string, float>` covers float state that doesn't
belong as a named field (NPC timers, area-local float state).

When a system grows complex enough that its facts have structure
(inventory items with multiple properties, quests with aliases and per-stage
data), it graduates to a **dedicated sub-object** (see below).

---

## Adding save support to a new feature

Do this when the feature is built, not before. Three rules:

**1. Authoritative state in plain C# objects, not MonoBehaviour fields.**
The feature's runtime state lives in a plain C# class that `SaveData` can hold
directly. MonoBehaviours read from it — they do not hold the authoritative value.
If a MonoBehaviour field is the only record of something, it cannot be saved.

**2. Every authored entity gets a stable string ID on its ScriptableObject.**
Set it once in the Inspector. Never rename it — renaming breaks existing saves.
The save file references this ID, not the asset path or scene hierarchy index.
Unity object references (direct SO or GameObject references) are never stored
in `SaveData`; stable string IDs are.

**3. Classify each piece of state by tier before writing any save code.**
Write the tier on paper first. Tier 3 is never saved. Tier 1 goes into `facts`
unless the system is complex enough for a sub-object. Tier 2 gets its own field
or sub-object in `SaveData`.

---

## When a feature needs a sub-object instead of the factstore

Use the generic factstore until the feature's state has structure the factstore
can't express cleanly. The signal: an item/entity has more than one property
that needs saving (item definition + quantity + durability = three properties →
needs a struct, not a dict entry).

Pattern:

```csharp
// Feature defines its own save data class (plain C#, [Serializable])
public class InventorySaveData
{
    public List<InventoryItemSaveData> items;
}

// SaveData gains one nullable field per system, added when that system ships
public class SaveData
{
    // ... existing fields ...
    public InventorySaveData inventory;   // null until inventory feature exists
    public QuestSaveData quests;          // null until quest system exists
}
```

Each system owns its format. `SaveData` is the envelope. Never pre-add a
sub-object for a system that doesn't exist yet.

---

## Pruning: removing stale Tier 2 entries

Tier 2 data should be removed when it returns to the authored default. Keeping
stale zero-value entries is how save files balloon (Skyrim bloat).

- `dirtLevels[patchId]` — remove the entry when dirt returns to authored default
  (cat regrowth complete and at max). Absent key = "at authored default."
- `numericFacts["cat.shedTimer"]` — remove when timer resets to zero.
- Quest stage facts — remove per-stage keys when a quest completes; keep only
  the completion flag.

The rule: **whoever sets a value is responsible for removing it when it's no
longer meaningful.**

---

## Serialization format

**Format**: Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`).
Covers `Dictionary<>` and polymorphism; `JsonUtility` does not.
For release builds, wrap the JSON in `GZipStream` — compresses 60–80%.

**Atomic write** — always write to a temp file, then rename:

```
serialize → Application.persistentDataPath/slot0.tmp
flush + close
File.Move(slot0.tmp → slot0.sav)   // atomic at OS level
```

Direct writes to the final file corrupt saves on crash. The rename is atomic —
the old save survives until the new one is complete.

**Never use `BinaryFormatter`** — deprecated, insecure, not cross-platform.

---

## Versioning

`SaveData` has `public int saveVersion = 1;` from day one.

On load: check version, run migrations for each version gap, then proceed.
Never try to load a newer save version than the current game supports — show
an error instead.

Bump `saveVersion` whenever a field is removed or its meaning changes.
Adding a new nullable field with a sensible default doesn't require a bump,
but bumping is cheap and makes migration logic explicit — prefer to bump.

---

## Steam Cloud

Steam Cloud syncs files in `Application.persistentDataPath` automatically —
no per-write API calls needed for basic sync. Configure the watched paths in
the Steamworks partner portal once an App ID exists.

`ISaveBackend` seam already supports a `SteamCloudSaveBackend` slot. Until
a Steam App ID is registered, `LocalFileSaveBackend` is the active backend.
Steam Cloud only activates when the game is launched via the Steam client.

Settings (graphics, audio, keybindings) live in a separate file and are
configured independently in the Steam partner portal — they may or may not
be synced, player's choice. Never mix settings into `SaveData`.

---

## Web builds (itch.io)

In a Unity WebGL build, `Application.persistentDataPath` maps to IndexedDB
(browser local storage). Save code does not change, but IndexedDB is fragile:
browser cache clears wipe it, private/incognito loses it on tab close.

Decision: web builds (itch.io demos) are short-session prototypes where
save is not needed. The real game requires a download. No `WebGLSaveBackend`
is planned unless this decision changes.

---

## Anti-patterns

- **MonoBehaviour field as authoritative state** — can't be saved without
  finding all instances at runtime; breaks on scene reorganization.
- **Unity object reference in `SaveData`** — meaningless across sessions.
  Always use stable string IDs.
- **Scene hierarchy index or asset path as ID** — breaks when scene is
  edited or assets moved. Use the stable ID field on the SO.
- **Pre-adding fields for systems that don't exist** — adds maintenance
  burden and false structure. Add when the system ships.
- **Saving Tier 3 state** — NPC position, physics object position, visual
  state. Derive it on load instead.
- **Not pruning Tier 2 entries that return to default** — Skyrim bloat.
