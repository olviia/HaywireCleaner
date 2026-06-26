# WalkModule rewrite — movement + Cinemachine

## Why the rotation was snappy

The prototype used `rb.MoveRotation(...)` each FixedUpdate — that teleports
the rotation to an exact target, which feels rigid. Switching to
`rb.angularVelocity` lets the physics engine own the rotation continuously,
and the rigidbody's **Angular Drag** inspector value then controls how quickly
it settles.

## The Update/FixedUpdate split

`Handle` is called from `InputReader.Update()` — physics should only be
written in `FixedUpdate`. Fix: `Handle` caches inputs, `FixedUpdate` applies them.

```csharp
public class WalkModule : MonoBehaviour, IModule
{
    private ActorHost host;
    private Rigidbody rb;

    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float reverseMultiplier = 0.35f;
    [SerializeField] private float rotateSpeed = 120f;

    private float moveInput;
    private float rotateInput;

    private static readonly Tag BlockedBy = Tag.Interacting;
    private static readonly Intent[] reactsTo = { Intent.Move };
    public IEnumerable<Intent> ReactsTo => reactsTo;

    void Awake()
    {
        host = GetComponentInParent<ActorHost>();
        rb = host?.GetComponent<Rigidbody>();
    }

    void OnEnable()  => host.Actor.RegisterModule(this);
    void OnDisable() => host.Actor.RemoveModule(this);

    public void Handle(Actor owner, Command cmd)
    {
        if (owner.Tags.HasAny(BlockedBy))
        {
            moveInput = rotateInput = 0f;
            return;
        }
        moveInput   = cmd.ExtraInfo.y;  // W=1, S=-1
        rotateInput = cmd.ExtraInfo.x;  // D=1, A=-1
    }

    void FixedUpdate()
    {
        rb.angularVelocity = Vector3.up * (rotateInput * rotateSpeed * Mathf.Deg2Rad);

        float speed = moveInput >= 0 ? moveSpeed : moveSpeed * reverseMultiplier;
        Vector3 forward = transform.forward * (moveInput * speed);
        rb.linearVelocity = new Vector3(forward.x, rb.linearVelocity.y, forward.z);
    }
}
```

When `Interacting` blocks, inputs are zeroed so FixedUpdate stops the bot
rather than freezing it mid-slide.

## Cinemachine — Unity 6 / Cinemachine 3, no code

1. Package Manager → install **Cinemachine**
2. Hierarchy: right-click → **Cinemachine → CinemachineCamera**
3. `CinemachineCamera` component → `Follow` → drag the bot
4. Add component: **CinemachineFollow**
   - `Binding Mode` → **Lock To Target**
   - Tune `Follow Offset` (e.g. `0, 6, -5`)
5. Add component: **CinemachineRotateWithFollowTarget**

`Lock To Target` puts the offset in the bot's local space, so the camera yaws
with it. Zero code.

## Inspector checklist

- Rigidbody on the bot: freeze rotation **X** and **Z**, leave **Y** free —
  otherwise angularVelocity on Y can bleed into tipping.