# Editor tooling

*`Assets/Scripts/Editor/**`. Map: [`README.md`](README.md).*

All of it compiles into the predefined `Assembly-CSharp-Editor`, which
auto-references **every asmdef and every package**. So editor tools may use
`Unity.InputSystem` freely without breaking the runtime quarantine
([input](input.md)) — the *runtime* views stay package-free; only their drawers
don't.

**The shared premise: keys are never hand-typed.** Both tools here exist so an
authored string is picked from a guarded list instead of typed, and so a typo
becomes impossible rather than merely unlikely. Tooling was built *before* the first
asset was authored.

---

## Fact-key registry

**Status:** Live. Lets an authored `FactCondition` pick its key from a hierarchical
dropdown of *every* valid key in the project.

Namespace `Tools.FactKeyRegistry`.

| Component | Location | Responsibility |
|---|---|---|
| `IFactKeySource` | `Editor/FactKeyRegistry/IFactKeySource.cs` | Contract: `IEnumerable<string> GetFactKeys()`. Implement to contribute keys. |
| `ConstKeySource` | `…/ConstKeySource.cs` | Reflects every `[FactKeySource]`-marked class's public-static-const strings (code-born keys, e.g. the tutorial consts). |
| `CutsceneKeySource` | `…/CutsceneKeySource.cs` | Enumerates `CutsceneDefinitionSO` assets that opt in, yields `FactKeys.CutsceneFinished(id)`. |
| `QuestKeySource` | `…/QuestKeySource.cs` | Enumerates `QuestDefinitionSO` assets, yields `FactKeys.QuestCompleted(id)` + `QuestStage(id)`. |
| `FactKeyRegistry` | `…/FactKeyRegistry.cs` | Static. `Collect()` concatenates all sources, de-dupes. The list backing the dropdown. |
| `FactKeyDropdown` / `FactKeyDropdownItem` | `…/FactKeyDropdownItem.cs` | `AdvancedDropdown` that splits keys on `.` into a tree; leaf carries the full key. |
| `FactConditionDrawer` | `…/FactConditionDrawer.cs` | `[CustomPropertyDrawer(typeof(FactCondition))]`. Renders key-dropdown + `test` + (`value` only when `CounterAtLeast`). |

**Invariant:** the *same* `FactKeys` method computes the key both at author time
(sources, in-editor) and at runtime (writers/readers). Adding a new derived key
family = one `FactKeys` method + (if asset-derived) one `IFactKeySource`, then
register it in `FactKeyRegistry.Sources`.

---

## Input action-key drawer

**Status:** Live. The smaller sibling of the above — same intent, simpler widget.

| Component | Location | Responsibility |
|---|---|---|
| `InputGlyphDrawer` | `Editor/InputGlyphDrawer.cs` | `[CustomPropertyDrawer(typeof(InputActionKeyAttribute))]` ([input](input.md#the-0-glyph-text-view-localized-text-with-inline-key-labels)). Enum-style `EditorGUI.Popup` of `(none)` + every `"Map/Action"` read from the single `InputActionAsset`. |
| `SceneSwitchOverlay` | `Editor/SceneSwitchOverlay.cs` | Scene-view overlay for jumping between scenes. Convenience, not architecture. |
| `FontToSprite` | `Editor/FontToSprite.cs` | Asset utility. |

### Design notes

**Flat popup, not `AdvancedDropdown`.** `AdvancedDropdown` earns its keep at many
entries with a natural tree (fact keys split on `.`). With ~5 actions, the enum-style
`EditorGUI.Popup` is the honest call.

**`BeginChangeCheck` guards against silent wipes.** A stale or renamed key isn't in
`_options`, so `Array.IndexOf` returns `-1` and the popup displays `(none)` — but the
value is only *written back* if the user actually touches it. Merely viewing the
inspector must not destroy data.

**`BeginProperty`/`EndProperty` must be balanced.** They bracket the rect and tell
Unity "this region *is* this SerializedProperty" — which is what powers the prefab
override bar, right-click Revert/Copy/Paste, mixed-value display on multi-select, and
label dragging. `EndProperty` once sat inside the `if (EndChangeCheck())` block, so it
only popped on frames where the value changed; `actionKeys` is an **array**, so every
element pushed without popping on every repaint. Unity degrades quietly here rather
than throwing, which is what makes it a bad bug — the override bar silently attaches
to the wrong field. Fixed 2026-07-16.

*Still open:* the drawer ignores `BeginProperty`'s **return value**. It returns a
property-aware label carrying override state and mixed-value handling; feeding it back
into the `Popup` is the idiomatic form, and without it those features no-op even with
the pairing fixed.

**`_options ??= BuildOptions()` caches for the drawer's lifetime** — a newly-added
action won't appear until a domain reload or reselect. That's deliberate:
`BuildOptions` does an `AssetDatabase.FindAssets` + full asset load, and running it
every repaint of every array element would make the inspector crawl.
