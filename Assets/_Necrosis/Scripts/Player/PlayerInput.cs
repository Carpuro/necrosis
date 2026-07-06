using UnityEngine;

/// <summary>
/// NECROSIS://PROTOCOLO — Player input.
///
/// Single responsibility: read the keyboard/mouse each frame and expose the raw
/// INTENT (axes and key edges). It holds no game state — toggle state (walk/run,
/// crouch), stance, etc. live in PlayerController. This is where weapon/ability
/// inputs will be added later.
///
/// PlayerController calls <see cref="Sample"/> at the start of its Update.
/// </summary>
public class PlayerInput : MonoBehaviour
{
    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }
    public bool Moving { get; private set; }

    public bool SprintHeld { get; private set; }   // Shift held
    public bool AimHeld { get; private set; }       // right mouse held
    public float MouseX { get; private set; }       // horizontal mouse delta (180 side)

    public bool RunTogglePressed { get; private set; }    // C down
    public bool CrouchPressed { get; private set; }        // Ctrl down (crouch, or roll while running)
    public bool JumpPressed { get; private set; }          // Space down

    /// <summary>Stance key pressed this frame: 0 fists · 1 melee · 2 gun · -1 none.</summary>
    public int StancePressed { get; private set; }

    public void Sample()
    {
        Horizontal = Input.GetAxisRaw("Horizontal");
        Vertical = Input.GetAxisRaw("Vertical");
        Moving = new Vector3(Horizontal, 0f, Vertical).sqrMagnitude > 0.01f;

        SprintHeld = Input.GetKey(KeyCode.LeftShift);
        AimHeld = Input.GetMouseButton(1);
        MouseX = Input.GetAxis("Mouse X");

        RunTogglePressed = Input.GetKeyDown(KeyCode.C);
        CrouchPressed = Input.GetKeyDown(KeyCode.LeftControl);
        JumpPressed = Input.GetKeyDown(KeyCode.Space);

        StancePressed = Input.GetKeyDown(KeyCode.Alpha1) ? 0
                      : Input.GetKeyDown(KeyCode.Alpha2) ? 1
                      : Input.GetKeyDown(KeyCode.Alpha3) ? 2
                      : -1;
    }
}
