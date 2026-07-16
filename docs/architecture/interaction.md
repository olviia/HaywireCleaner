# Interaction & docking

*`Core/Interaction/*` + `Features/Modules/InteractionModule.cs`,`ChargingModule.cs` +
`Features/Interactables/*`. Map: [`README.md`](README.md). The module/possession
machinery these plug into is in [`core.md §7`](core.md#7-possession--module-input).*

**Status:** Live. Two interactables shipped (`SlidingDoors`, `ChargingStation`).

Interaction is split orthogonally: a **sensor** decides *what* is focused; the
**`Interact` intent** decides *when* to act on it.

## Components

| Component | Location | Responsibility |
|---|---|---|
| `IInteractable` | `Core/Interaction/IInteractable.cs` | `CanInteract(actor)`, `OnFocus(hitPoint)`, `OnUnfocus()`, `Interact(actor)`. |
| `InteractionFocus` | `Core/Player/InteractionFocus.cs` | Held by `Actor`. `Current`, `Set`/`Clear` with focus/unfocus callbacks. |
| `InteractionModule` | `Features/Modules/InteractionModule.cs` | `MonoBehaviour,IModule` = the sensor **and** the executor. `Update` does a `SphereCast` from the camera, sets `Actor.Focus`; `Handle(Interact)` calls `Focus.Current.Interact(owner)`. Blocked while `Interacting`/`Charging`. |
| `IDock` / `IChargeable` | `Core/Interaction/IDock.cs`, `Core/Player/IChargeable.cs` | `Dock/UnDock/Docked` ; `StartDocking(dock)`. |
| `ChargingModule` | `Features/Modules/ChargingModule.cs` | `IModule,IChargeable`. `StartDocking` docks the rigidbody + subscribes `Docked`→swap `Interacting`→`Charging` tag; pressing interact while charging → `StopCharge` (undock, clear tag). |
| `ChargingStation` | `Features/Interactables/ChargingStation.cs` | `IInteractable,IDock`. `Interact` → `actor.GetModule<IChargeable>().StartDocking(this)` + adds `Interacting`; `Dock` shows a stop-prompt, raises a static dock camera's depth, coroutine-lerps the body to `dockAnchor`, then fires `Docked`. |
| `SlidingDoors` | `Features/Interactables/SlidingDoors.cs` | `IInteractable`. Toggles an `Animator` bool; shows/hides a prompt ([ui §10](ui.md#10-ui-prompt--mount-channels)); `OnMotionFinished` (animation event) clears `isBusy`. |

## Notes

- **The world commands the actor via the same module dispatch** — this is the
  world-side sibling of input (`Actor.Dispatch` exists for it, though today
  `ChargingStation.Interact` calls `StartDocking` directly).
- **Tag mutual-exclusion (`Interacting`/`Charging`) is what stops walking while
  docked** — not a state machine, just modules declining via `BlockedBy`.
- **`ModuleInput.RaiseStopCharging` has no module reacting to `Intent.StopCharge`**
  (`ChargingModule` reacts to `Interact`) — effectively dead; clean up or wire
  (README Appendix B).
