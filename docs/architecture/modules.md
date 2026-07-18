# Modules (abilities)

*`Features/Modules/*` over the `Core/Player` module contracts. Map:
[`README.md`](README.md). The dispatch half — `Actor`, `IModule`, `Command`,
`TagSet` — is documented in [`core.md`](core.md#7-possession--module-input);
this file covers **ownership**: which modules the bot has, and how they get there.*

**Status:** Live. The bot starts with no `InteractionModule`; completing the second
tutorial stage grants it, and it survives save/load.

---

## The shape: grants are data, instances are spawned from data

The dispatch layer already existed. Mapped onto Unreal's GAS decomposition (a
*design* reference — we are in Unity):

| GAS | Here |
|---|---|
| `GameplayAbility` | `IModule` |
| `AbilitySystemComponent` | `Actor` |
| Gameplay tags | `TagSet` / `Tag` |
| `GameplayAbilitySpec` (the grant record) | **`module.{id}.owned` fact** |

The missing piece was the *spec*: the record that an actor **owns** an ability,
deliberately separate from the live ability object. That separation is the whole
system — the same authoritative-data/derived-runtime split as `WorldState` and its
consumers.

## Ownership is a fact, not a list

`SaveData.ownedModuleId` was removed. Ownership is a flag,
`FactKeys.ModuleOwned(id)` → `module.{id}.owned`, which means:

- granting is the **existing `FactSetterSO`**, authored in the inspector
- gating a quest on ownership is the **existing `FactCondition`**
- save and load are **free**
- `QuestRuntime` re-evaluates on grant automatically, because it is a fact

Two records of the same truth would be a bug farm; there is exactly one.

## Components

| Component | Location | Responsibility |
|---|---|---|
| `ModuleDefinitionSO` | `Features/Modules/ModuleDefinitionSO.cs` | `id` (save-file identity), `LocalizedString displayName`, `GameObject prefab` (must carry an `IModule`). Menu `Cleanbot/Modules/Definition`. |
| `ModuleCatalogSO` | `Features/Modules/ModuleCatalogSO.cs` | `List<ModuleDefinitionSO> modules`. Menu `Cleanbot/Modules/Catalog`. Plain list, mirroring `QuestCatalogSO` — the reconciler walks every entry anyway. |
| `ModuleLoadout` | `Features/Modules/ModuleLoadout.cs` | `MonoBehaviour` on the `ActorHost`. Subscribes `WorldState.FactChanged`, self-primes in `OnEnable`. Walks the catalog and drives spawned instances to match the owned flags. Raises `ModuleInstalled(def)` for fresh grants only. |
| `ModuleKeySource` | `Editor/FactKeyRegistry/ModuleKeySource.cs` | Yields `FactKeys.ModuleOwned(id)` for every `ModuleDefinitionSO` asset, so grants are authored from the dropdown ([editor-tooling](editor-tooling.md)). |

## The wiring

```
quest stage 2's tutorial popup completed (UIHoldToConfirm.onCompleted)
   └─ FactSetterSO.Write  →  module.InteractModule.owned = true
        └─ WorldState.FactChanged
             └─ ModuleLoadout: walks catalog, spawns the prefab under ActorHost
                  └─ InteractionModule.Awake  GetComponentInParent<ActorHost>()
                     InteractionModule.OnEnable  Actor.RegisterModule(this)
```

`FactChanged(null)` on load runs the same path, so a loaded save restores the
loadout with **no restore-specific code** — the third system to get that property
free (after `QuestRuntime` and `QuestInfoPasser`). Reconciliation is the house
style: converge on the correct state from any starting point rather than handling
each transition.

## Rules & gotchas

1. **`Instantiate(prefab, moduleRoot, false)` — the parent overload is not
   cosmetic.** Modules resolve their owner with `GetComponentInParent<ActorHost>()`
   in `Awake`, which runs *inside* `Instantiate`. Instantiating first and reparenting
   after leaves `host` null and throws from a place that looks unrelated. The `false`
   keeps the prefab's authored local transform, which matters once modules have
   meshes bolted to attach points.
2. **Uninstall needs no cooperation.** `Destroy` triggers `OnDisable`, which already
   calls `Actor.RemoveModule(this)` in every module written before revocation
   existed — a sign the `IModule` boundary was cut in the right place.
3. **Restore ≠ grant.** `ModuleLoadout` treats the first reconcile *and any
   `FactChanged(null)`* as a restore, and only raises `ModuleInstalled` otherwise.
   Without this, loading a save while the bot exists would replay the install
   presentation for the entire loadout at once. GAS draws the same line between
   granting and activating.
4. **Presentation lives on the definition, not the loadout.** The loadout announces;
   a presenter listens and decides what (if anything) to play, per module. The
   install animation is opt-in by leaving the field empty — same
   authoritative/view split as everywhere else.
5. **Ids are frozen once shipped.** `id` is embedded in the save schema via
   `module.{id}.owned`. Rename the asset freely; never the id.
6. **The catalog is what makes revocation possible.** Without it the loadout could
   only react to modules it already knew about; with it, it can enumerate everything
   that *could* exist and drive each to its correct state.
7. **Every fact change walks the whole catalog.** Negligible at ten modules; the
   line to look at if the fact bus ever carries high-frequency values
   ([core](core.md#rules--gotchas) gotcha 6).
