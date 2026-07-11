# Cutscene System — Next Steps

**Status:** Planned. Triggered by building the *second* cutscene (the robot's
first wake-up prompt, fired by the existing event after the intro cutscene).
**Through-line:** all three items are the same graduation — the
`CutsceneDirector` moving from *"spawn a self-contained UI prefab whose Timeline
only drives objects it owns"* to *"a sequencer that reaches outward: targets live
scene actors by role, and owns a gameplay-state transition around itself."*
See `architecture-actual.md` → *Cutscene System* / *Player Possession* / *Input
Routing* for the seams these build on.

---


## Scope 2 — Diegetic (in-world / 3D) cutscenes

**Goal:** cutscenes that drive scene objects — camera orbits the bot, the cat
walks around the bot when enough is cleaned.

- **The break:** current model = each cutscene is a self-contained prefab
  carrying its own Timeline bindings (true only because the driven objects live
  *inside* the prefab). An in-world shot must drive objects that already exist
  in the scene (the possessed bot, the cat, the real camera). A freshly-
  instantiated prefab can't serialize a reference to a scene instance → those
  tracks come up **unbound** and play silently (Rule 4, now unavoidable).
- **Camera (AAA-in-Unity standard):** never move the real camera. Main camera
  keeps a **CinemachineBrain** permanently; gameplay runs one vcam; the cutscene
  Timeline has a **Cinemachine track** that blends to a cutscene vcam (orbit rig
  around the bot) and back.
- **Binding to live actors:** timeline targets **roles**, resolved role→instance
  at `Play()` time, not in the asset. (Unreal names this exactly: *possessables*
  = bound to existing scene actors, vs *spawnables* = created by the sequence.
  Current UI slides are spawnables; the bot/cat shot needs possessables.)
  Unity API: `PlayableDirector.SetGenericBinding(track, sceneObject)` for tracks,
  `SetReferenceValue(...)` for a clip's exposed references (e.g. which vcam a
  Cinemachine Shot uses), plain field assignment for the vcam's Follow/LookAt.
- **Role registry already exists:** the "bot" role = `Posession.Current` (the
  accessor the possession doc flags as needed). Cutscene vcam Follow/LookAt →
  possessed actor's transform; cat → its scene instance.
- **The addition:** a small **binding step** — the definition declares which
  scene roles it needs; the director resolves them (from Possession / a scene
  registry) before `Play()`. Data-driven catalog otherwise unchanged.
- **Open fork:** keep instantiating the prefab and bind at play time (keeps the
  data-driven catalog) **vs** make the director scene-resident and drive the
  real objects directly (easier to bind, less self-contained).

