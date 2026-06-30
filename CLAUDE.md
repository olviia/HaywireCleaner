# Working with this developer

Game design and project status live in `docs/design.md` and `docs/plan.md`.
This file is about *how we work together*, not about the game itself.

## Development principles

Build this game in accordance with industry standards, established best
practices, and game design/development theory and research — apply real
frameworks and references (MDA, Parnas's decomposition criteria, established
architecture patterns, etc.) rather than ad-hoc guesses.

## Collaboration style

- **Code policy**: never use Edit or Write on code files (.cs). Explain
  the approach and show code as chat code blocks only — the developer types it
  themselves. This is a hard preference, not a default to override even if it would
  be faster to just write the file.
- **Reasoning style**: check with how aaa games implement the thing in question. don't check standard tutorials because they use spaghetti code. Pay close attention to the pattern combinations, with how they are used as building blocks for architecture.
- **Conversational tone**: concise and direct. For coding questions, don't reply as a one long message with all the code together. Instead, communicate how elements are wired together, how they work together. Then when I ask, walk me through small chanks of code, like just one script, but in good details with giving me understanding how it is used in aa and aaa studios. Ask clarifying questions before
  assuming scope, especially when a request could mean several different things.
  Don't pad responses with recap/summary of what was just discussed. You are allowed to not know something. When it is appropriate, you can use bisociations for the sake of more interesting conversation and topic
- **Process discipline**: stick to the agreed step-by-step process (see
  `docs/design.md` and `docs/plan.md`)

## Architecture working method

The premise: there is no single source of truth for game architecture that i could find, so to get a good architecture, there is a need to consult with the literature. some examples listed below

Foundations (boundaries & dependency graph):

- Parnas, *On the Criteria To Be Used in Decomposing Systems into Modules* (1972) +
  *…Ease of Extension and Contraction* — boundaries & uses-graph.
- Robert Nystrom, *Game Programming Patterns* (free online) — patterns in game context.
- Jason Gregory, *Game Engine Architecture* — how real engines layer.
- Mike Acton, *Data-Oriented Design* (CppCon 2014) / Richard Fabian,
  *Data-Oriented Design* — the perf-first counterweight to OOP "clean."

RPG / gameplay-systems anchors:

- "Overwatch Gameplay Architecture and Netcode," Tim Ford (GDC 2017, free on
  YouTube) — ECS in a shipped AAA game; the reference for taming gameplay
  complexity at scale.
- Unreal's Gameplay Ability System (GAS) + tranek/GASDocumentation — the
  battle-tested decomposition for ability/attribute/effect/tag systems. A *design*
  reference only (we're in Unity), not code to copy.
- Unity Open Project #1 "Chop Chop" + DOTS/Entities samples — official open-source
  Unity code. Read "Chop Chop" critically: its SO-event-channel backbone is
  opinionated; ask which edge each channel cuts (Part 1) before adopting it.