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

## Scope 1 — TypewriterText decode component (STARTING HERE)

**Goal:** cutscene lines (`...System initialization...`, `...Detected
anomalies...`, `...Resolving anomalies...`, `ERROR!`) appear character-by-
character with the Cyberpunk 2077 "translation" look (scramble → resolve).

- Not cutscene logic — a **reusable TMP effect the Timeline triggers**, same
  trigger model as an ActivationTrack turning a slide on today.
- **Reveal mechanism:** TextMeshPro `maxVisibleCharacters` walked up over time.
- **Decode look (Cyberpunk):** leading-edge characters drawn as random glyphs
  from a charset, cycling a few frames before locking to the real character.
  Via per-tick string rewrite, or `textInfo` vertex manipulation for
  alpha/color flicker.
- **Wiring:** a `TypewriterText` MonoBehaviour on the text object exposing
  `Play(string)`; fired by a Timeline **Signal** (SignalTrack + SignalReceiver)
  per beat, or on the slide's ActivationTrack enabling it.
- **Increment plan:** (a) plain left-to-right reveal core first, (b) layer the
  scramble/decode pass on top.
- Stays UI-space — works inside the current prefab model. Scope 2 is what
  breaks that model.

  The trick: a Control Track clip, pointed at a GameObject that implements
  ITimeControl, forwards its clip-local time into your component's
  SetTime(double) every frame Timeline evaluates — including editor scrubbing.
  So the timeline becomes the clock (that's what buys you preview, a coroutine
  can't), while charsPerSecond stays a field on the component (you still own   
  the speed), and dropping the clip is still just "start it here." No
  coroutine, no second class, no custom clip type.

  Single script, lives on the TMP object:

  using System.Text;
  using TMPro;
  using UnityEngine;
  using UnityEngine.Playables;   // ITimeControl

  namespace Features.Cutscenes
  {
  /// <summary>
  /// Cyberpunk-style "decode" reveal for one TMP line. Sits on the TMP    
  object.
  /// Driven by a Timeline Control Track via ITimeControl, so it previews  
  live
  /// while you scrub — the timeline is the clock, there is no coroutine.  
  /// </summary>
  public class TypewriterText : MonoBehaviour, ITimeControl
  {
  [TextArea] public string line = "System initialization";
  public float charsPerSecond = 24f;   // the speed you own
  public int   scrambleWindow = 4;      // the 3–5 glyph "train"       
  [SerializeField] string charset =
  "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#%&/\\@<>*";

          TMP_Text label;
          char[] pool;
          readonly StringBuilder sb = new();
          readonly System.Random rng = new();

          void EnsureRefs()
          {
              if (label == null) label = GetComponent<TMP_Text>();
              if (pool  == null) pool  = charset.ToCharArray();
          }

          // Timeline Control Track drives these — in play mode AND while      
  scrubbing.
  public void OnControlTimeStart() => EnsureRefs();
  public void SetTime(double time) => Render((float)time);
  public void OnControlTimeStop()  { EnsureRefs(); label.text = line; }

          void Render(float elapsed)
          {
              EnsureRefs();
              int n          = line.Length;
              int settled    = Mathf.Clamp(Mathf.FloorToInt(elapsed *
  charsPerSecond), 0, n);
  int visibleEnd = Mathf.Min(settled + scrambleWindow, n);

              sb.Clear();
              for (int i = 0; i < visibleEnd; i++)
              {
                  if (i < settled)         sb.Append(line[i]);      // locked  
                  else if (line[i] == ' ') sb.Append(' ');          // keep    
  word gaps
  else
  sb.Append(pool[rng.Next(pool.Length)]); // scrambling
  }
  label.text = sb.ToString();
  }
  }
  }

  The Render body is the same train algorithm I walked through last turn       
  (settled head, visibleEnd window, tail settles itself, rng re-rolls each     
  call). The part worth reading closely is the three ITimeControl methods —    
  that's the whole wiring:

    - SetTime(double time) — Timeline hands you the clip-local elapsed seconds   
      every evaluated frame. You just pass it to Render. Because Timeline calls    
      this during preview, not only Play mode, scrubbing the playhead animates the
      decode live in the Scene/Game view. This is the single reason it's
      ITimeControl and not a coroutine.
    - OnControlTimeStart / OnControlTimeStop — called as the playhead
      enters/leaves the clip. Start just caches refs; Stop snaps to the finished   
      line so leaving the clip never freezes mid-scramble.
    - EnsureRefs (lazy, not Awake) — deliberate. Awake/OnEnable don't run while  
      you're just scrubbing in the editor, so caching lazily inside the
      ITimeControl calls is what makes edit-mode preview work without
      [ExecuteAlways].

  charsPerSecond on the component vs. clip length: the clip only needs to be at
  least line.Length / charsPerSecond seconds long — that's the window during   
  which the reveal is allowed to run. Speed is set on the component; clip      
  length just has to contain it. Make it longer and the finished line simply   
  holds.

  Timeline setup (this is where "the timeline tells it to start" happens):

    1. TypewriterText on the TMP object; set line, charsPerSecond,
       scrambleWindow.
    2. In the cutscene's Timeline: Add → Control Track.
    3. Drag the TMP GameObject onto that track (or add a Control clip and set its
       Source Game Object to the TMP object). Timeline auto-detects the
       ITimeControl and drives it.
    4. Scrub — you'll see the decode animate. Each of your beats (...Detected    
       anomalies..., ERROR!) is a separate TMP object + control clip laid down the  
       track in sequence.

  One assembly note since your Features.Cutscenes.asmdef currently references  
  only Core + Unity.Timeline: add Unity.TextMeshPro to its Assembly Definition
  References or the TMP_Text reference won't compile. ITimeControl (from       
  UnityEngine.Playables) is a built-in engine module, so it needs no reference.



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

## Scope 3 — Lock player control during a cutscene

**Goal:** player can't move the bot while a cutscene plays.

- **Seam already exists** (Input Routing doc §6 spelled out the path): switch
  input contexts by **enabling/disabling action maps**. On cutscene start,
  disable the `Player` map (or switch to a `Cutscene`/`UI` map) → no
  `Move`/`Interact` intents raised → the possessed bot receives nothing. No
  scattered `if (inCutscene) return`. Re-enable on stop. (This is the God of War
  / Uncharted "gameplay input is a context a cinematic suspends" model.)
- **Wiring:** director already knows start (`Play`) and end (`playable.stopped`);
  raise `cutsceneStarted` / `cutsceneEnded` `VoidEventSO`s; `InputReader` (or a
  tiny input-context controller) toggles the map. Same shape as `newGameStarted`
  — event-channel, Core-mediated, no feature→feature reference.
- **Gotcha — velocity carry-over:** `WalkModule` sets `rb.linearVelocity` from
  Move. Kill input and no new Move arrives, so the bot keeps gliding at its last
  velocity. The transition must zero it — or `WalkModule` should damp to zero on
  absence of input (which real locomotion does anyway).
- **Growth path — stacking:** a single bool breaks once cutscene *and* pause
  *and* dialogue all want to suspend input. AAA uses an input-context **stack**
  (push "cutscene", pop it). Single flag is honest for now.
- **Textbook version:** a small gameplay-state (`Gameplay ↔ Cinematic`) owns the
  decision; director and input both observe it. At current size,
  director-raises-events → input-toggles-map is the right amount of structure.
