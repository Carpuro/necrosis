using UnityEngine;

/// <summary>
/// NECROSIS://PROTOCOLO — Third-person player controller.
///
/// Camera-relative locomotion with a rich move set:
///   walk / run / sprint · crouch · aim-strafe · dodge roll ·
///   ARC-style walk start · in-place turn · 180 turn.
///
/// Requires a CharacterController on the same GameObject and the "Player" tag.
/// The camera is handled separately by ShoulderCamera.cs. If an Animator is
/// assigned it is driven every frame; otherwise the bare capsule still moves.
///
/// Update() reads input once, then delegates to one method per feature so each
/// concern stays isolated and readable.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ───────────────────────────────────────────────────────────────────────
    #region Inspector — tunables
    // ───────────────────────────────────────────────────────────────────────

    [Header("Speeds (m/s)")]
    public float crouchSpeed = 1.5f;
    [Tooltip("Matched to the walk clip stride to avoid foot sliding.")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 6.5f;
    public float sprintSpeed = 8f;
    [Tooltip("Speed while aiming/strafing (right mouse), State-of-Decay style.")]
    public float aimSpeed = 2.8f;

    [Header("Physics")]
    public float gravity = -20f;
    public float rotationSmoothness = 12f;

    [Header("Acceleration (idle→walk→run ramp)")]
    [Tooltip("Speed gained per second; makes the blend pass through walk before run.")]
    public float acceleration = 10f;
    public float deceleration = 16f;

    [Header("Walk start (ARC style)")]
    [Tooltip("Seconds the player stays planted (no advance) during the start step.")]
    public float walkStartDuration = 0.18f;
    [Tooltip("Seconds standing still before the start step is allowed. Prevents it " +
             "from firing right after moving (reversals, turns).")]
    public float walkStartIdleDelay = 0.4f;

    [Header("In-place turn (standing)")]
    [Tooltip("Angle vs. camera that triggers the standing turn.")]
    public float turnInPlaceThreshold = 50f;
    [Tooltip("Angular speed (deg/s) when turning in place.")]
    public float turnInPlaceSpeed = 240f;

    [Header("180 turn (reverse direction)")]
    [Tooltip("Angle vs. last heading that triggers the 180.")]
    public float turn180Threshold = 135f;
    public float turn180Duration = 0.45f;

    [Header("Turn blend")]
    [Tooltip("Angular speed (deg/s) mapped to a full turn blend. Lower = turns show sooner.")]
    public float turnRateForFullBlend = 90f;

    [Header("Dodge roll")]
    public float rollSpeed = 7f;
    public float rollDuration = 0.7f;

    [Header("References")]
    public Transform cameraTransform;     // Main Camera
    [Tooltip("Rigged model Animator (e.g. Mixamo). If null, only the capsule moves.")]
    public Animator animator;

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region Public state (read by Animator, footsteps, HUD, other systems)
    // ───────────────────────────────────────────────────────────────────────

    public enum MoveState { Idle, Crouch, Walk, Run, Sprint }
    public enum Stance { Fists, Melee, Gun }

    public MoveState CurrentState { get; private set; } = MoveState.Idle;
    public Stance CurrentStance { get; private set; } = Stance.Fists;

    /// <summary>Real horizontal speed (m/s). Read by Animator and footsteps.</summary>
    public float PlanarSpeed { get; private set; }
    /// <summary>Normalized turn: -1 (left) .. +1 (right). Read by Animator.</summary>
    public float TurnSignal { get; private set; }
    /// <summary>True while aiming/strafing (right mouse held).</summary>
    public bool Aiming { get; private set; }
    /// <summary>True while free-strafing without aiming (currently unused).</summary>
    public bool StrafeLock { get; private set; }
    /// <summary>Strafe axes sent to the Animator (also exposed for debug).</summary>
    public float AimX { get; private set; }
    public float AimY { get; private set; }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region Private state
    // ───────────────────────────────────────────────────────────────────────

    CharacterController controller;
    float normalHeight, normalCenterY;
    float verticalVelocity;

    // Toggles: C = walk/run, Ctrl = crouch (Shift = sprint is held, not toggled).
    bool runToggled, crouchToggled;

    // Smoothed speed used for the natural accel ramp.
    float currentSpeed;

    // Walk-start (ARC) runtime.
    MoveState prevState = MoveState.Idle;
    float walkStartTimer, idleTime;
    bool startWalkQueued;

    // 180 turn runtime.
    bool turning180;
    float turn180Timer;
    Quaternion turn180From, turn180To;
    bool turn180Queued;
    float turn180Tier;                       // 0 idle · 1 walk · 2 run (frozen on trigger)
    float turn180Dir;                        // -1 left · +1 right (turn side)
    Vector3 lastMoveDir = Vector3.forward;   // last heading (persists through brief pauses)

    // In-place turn runtime.
    bool turningInPlace;
    float prevYaw;

    // Dodge roll runtime.
    bool rolling;
    float rollTimer;
    Vector3 rollDir;

    // Per-frame scratch (recomputed each Update).
    float inH, inV;
    bool sprintHeld, moving, faceCamera, crouched, startingWalk;
    Vector3 camForward, camRight, frameMove;
    float animAimX, animAimY, animTurnInPlaceDir;

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region Unity lifecycle
    // ───────────────────────────────────────────────────────────────────────

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        normalHeight = controller.height;
        normalCenterY = controller.center.y;
        prevYaw = transform.eulerAngles.y;
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        ReadInput();

        // Dodge roll fully overrides movement/animation while active.
        if (rolling) { UpdateRoll(); return; }
        if (TryStartRoll()) return;

        UpdateStanceAndAiming();
        UpdateMoveState();
        UpdateWalkStart();
        ApplyCrouchHeight();
        HandleMovement();
        ApplyGravityAndMove();
        UpdatePlanarSpeed();
        UpdateTurnSignal();
        UpdateTurnInPlace();
        UpdateAnimator();
        EndFrame();
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region Input
    // ───────────────────────────────────────────────────────────────────────

    void ReadInput()
    {
        inH = Input.GetAxisRaw("Horizontal");
        inV = Input.GetAxisRaw("Vertical");
        moving = new Vector3(inH, 0f, inV).sqrMagnitude > 0.01f;

        if (Input.GetKeyDown(KeyCode.C)) runToggled = !runToggled;              // walk ↔ run
        if (Input.GetKeyDown(KeyCode.LeftControl)) crouchToggled = !crouchToggled; // crouch
        sprintHeld = Input.GetKey(KeyCode.LeftShift);                            // sprint (held)

        if (Input.GetKeyDown(KeyCode.Alpha1)) CurrentStance = Stance.Fists;
        if (Input.GetKeyDown(KeyCode.Alpha2)) CurrentStance = Stance.Melee;
        if (Input.GetKeyDown(KeyCode.Alpha3)) CurrentStance = Stance.Gun;

        // Camera basis on the ground plane, reused by every movement path.
        if (cameraTransform != null)
        {
            camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        }
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region Dodge roll
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>Starts a roll on Space (grounded). Returns true if a roll began.</summary>
    bool TryStartRoll()
    {
        if (!Input.GetKeyDown(KeyCode.Space) || !controller.isGrounded) return false;

        rolling = true;
        rollTimer = 0f;

        // Roll toward input direction, or straight ahead if idle.
        Vector3 wish = cameraTransform != null
            ? (camForward * inV + camRight * inH).normalized
            : Vector3.zero;
        rollDir = wish.sqrMagnitude > 0.01f
            ? wish
            : Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        transform.rotation = Quaternion.LookRotation(rollDir, Vector3.up);
        if (animator != null) animator.SetTrigger("Roll");
        UpdateRoll();
        return true;
    }

    /// <summary>Drives the player at rollSpeed along rollDir for rollDuration.</summary>
    void UpdateRoll()
    {
        rollTimer += Time.deltaTime;

        frameMove = rollDir * rollSpeed;
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        frameMove.y = verticalVelocity;
        controller.Move(frameMove * Time.deltaTime);

        PlanarSpeed = rollSpeed;
        if (rollTimer >= rollDuration) rolling = false;
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region Stance & movement state
    // ───────────────────────────────────────────────────────────────────────

    void UpdateStanceAndAiming()
    {
        // Strafe only while aiming (right mouse), works in any stance. Normal
        // movement turns to face the move direction instead of strafing.
        Aiming = Input.GetMouseButton(1) && cameraTransform != null;
        StrafeLock = false;                       // no auto-strafe
        faceCamera = Aiming;
        crouched = crouchToggled && !Aiming;      // aiming forces standing
    }

    void UpdateMoveState()
    {
        // Priority: aim > crouch > sprint > run > walk.
        if (faceCamera) CurrentState = moving ? MoveState.Walk : MoveState.Idle;
        else if (crouched) CurrentState = MoveState.Crouch;
        else if (!moving) CurrentState = MoveState.Idle;
        else if (sprintHeld) CurrentState = MoveState.Sprint;
        else if (runToggled) CurrentState = MoveState.Run;
        else CurrentState = MoveState.Walk;
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region Walk start (ARC style)
    // ───────────────────────────────────────────────────────────────────────

    void UpdateWalkStart()
    {
        bool groundMove = CurrentState == MoveState.Walk ||
                          CurrentState == MoveState.Run ||
                          CurrentState == MoveState.Sprint;

        // Only fire when we've been genuinely stopped for a moment, so a reversal
        // or turn right after moving does not count as a fresh start.
        if (prevState == MoveState.Idle && groundMove && !faceCamera && !crouched
            && !turning180 && idleTime >= walkStartIdleDelay)
        {
            walkStartTimer = walkStartDuration;
            startWalkQueued = true;
        }

        // Update the idle timer AFTER the check above.
        if (moving) idleTime = 0f; else idleTime += Time.deltaTime;

        startingWalk = walkStartTimer > 0f;
        if (startingWalk) walkStartTimer -= Time.deltaTime;
        if (!moving || faceCamera || crouched) { walkStartTimer = 0f; startingWalk = false; }
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region Crouch collider
    // ───────────────────────────────────────────────────────────────────────

    void ApplyCrouchHeight()
    {
        // Lower the capsule when crouched; drop the center by half the height loss
        // so the feet stay on the ground instead of the capsule floating.
        float targetHeight = crouched ? normalHeight * 0.55f : normalHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, 10f * Time.deltaTime);
        Vector3 center = controller.center;
        center.y = normalCenterY - (normalHeight - controller.height) * 0.5f;
        controller.center = center;
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region Movement
    // ───────────────────────────────────────────────────────────────────────

    void HandleMovement()
    {
        frameMove = Vector3.zero;
        animAimX = animAimY = 0f;
        if (cameraTransform == null) return;

        if (faceCamera) AimMovement();
        else if (moving) GroundMovement();
        else DecelerateWhileIdle();

        // During the start step, plant in place (no advance, no shuffle).
        if (startingWalk) { frameMove.x = 0f; frameMove.z = 0f; }
    }

    /// <summary>Aim/strafe: body faces the camera, WASD strafes around it.</summary>
    void AimMovement()
    {
        Quaternion look = Quaternion.LookRotation(camForward, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look,
            rotationSmoothness * Time.deltaTime);

        if (!moving) return;

        float speed = aimSpeed;
        Vector3 wish = (camForward * inV + camRight * inH).normalized;
        frameMove = wish * speed;

        // Strafe axes RELATIVE TO THE BODY so the animation matches the movement
        // even while the body is still rotating toward the camera.
        Vector3 local = transform.InverseTransformDirection(wish);
        animAimX = local.x;
        animAimY = local.z;
    }

    /// <summary>Free locomotion: accelerate toward the move dir; handle 180 turns.</summary>
    void GroundMovement()
    {
        Vector3 worldDir = (camForward * inV + camRight * inH).normalized;

        TryTrigger180(worldDir);
        lastMoveDir = worldDir;

        // Target speed by state; zero during the start step (planted).
        float targetSpeed = startingWalk ? 0f : CurrentState switch
        {
            MoveState.Crouch => crouchSpeed,
            MoveState.Sprint => sprintSpeed,
            MoveState.Run => runSpeed,
            _ => walkSpeed
        };

        if (turning180)
        {
            UpdateTurn180();
        }
        else
        {
            float rate = targetSpeed > currentSpeed ? acceleration : deceleration;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);
            frameMove = worldDir * currentSpeed;

            Quaternion targetRot = Quaternion.LookRotation(worldDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                rotationSmoothness * Time.deltaTime);
        }
    }

    /// <summary>Idle: bleed speed off gradually so a one-frame input gap doesn't
    /// reset the "was moving" memory (which would misfire the start ramp).</summary>
    void DecelerateWhileIdle()
    {
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region 180 turn
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>Fires a 180 when the requested dir opposes the last heading.
    /// Compares against lastMoveDir (persists through pauses), not current facing.</summary>
    void TryTrigger180(Vector3 worldDir)
    {
        if (turning180) return;
        if (Vector3.Angle(lastMoveDir, worldDir) <= turn180Threshold) return;

        turning180 = true;
        turn180Timer = 0f;
        turn180Queued = true;

        // 180 takes priority over the start step.
        startWalkQueued = false;
        walkStartTimer = 0f;
        startingWalk = false;

        turn180From = transform.rotation;
        turn180To = Quaternion.LookRotation(worldDir, Vector3.up);

        // Freeze the speed tier at trigger time so idle/walk/run pick the right clip.
        turn180Tier = currentSpeed < 0.5f ? 0f
                    : (currentSpeed < runSpeed - 1f ? 1f : 2f);
        // Turn side by signed angle vs. the last heading.
        turn180Dir = Vector3.SignedAngle(lastMoveDir, worldDir, Vector3.up) >= 0f ? 1f : -1f;
    }

    /// <summary>While turning 180: don't advance, keep the speed (so it resumes
    /// straight into run without a phantom ramp), rotate over the clip.</summary>
    void UpdateTurn180()
    {
        frameMove = Vector3.zero;
        turn180Timer += Time.deltaTime;
        float t = Mathf.Clamp01(turn180Timer / turn180Duration);
        transform.rotation = Quaternion.Slerp(turn180From, turn180To, t);
        if (t >= 1f) turning180 = false;
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region In-place turn (standing)
    // ───────────────────────────────────────────────────────────────────────

    void UpdateTurnInPlace()
    {
        animTurnInPlaceDir = 0f;

        if (Aiming || CurrentState != MoveState.Idle || cameraTransform == null)
        {
            turningInPlace = false;
            return;
        }

        Vector3 camF = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
        if (camF.sqrMagnitude <= 0.001f) return;

        float camYaw = Quaternion.LookRotation(camF).eulerAngles.y;
        float diff = Mathf.DeltaAngle(transform.eulerAngles.y, camYaw);

        if (Mathf.Abs(diff) > turnInPlaceThreshold) turningInPlace = true;
        if (!turningInPlace) return;

        float step = Mathf.Clamp(diff, -turnInPlaceSpeed * Time.deltaTime,
                                        turnInPlaceSpeed * Time.deltaTime);
        transform.Rotate(0f, step, 0f);
        animTurnInPlaceDir = Mathf.Sign(diff);
        if (Mathf.Abs(diff) < 5f) turningInPlace = false; // aligned
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region Gravity, apply, derived signals
    // ───────────────────────────────────────────────────────────────────────

    void ApplyGravityAndMove()
    {
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        frameMove.y = verticalVelocity;
        controller.Move(frameMove * Time.deltaTime);
    }

    void UpdatePlanarSpeed()
    {
        // While turning 180 we don't advance, but report currentSpeed so the blend
        // resumes at the previous speed instead of ramping up from zero.
        PlanarSpeed = turning180
            ? currentSpeed
            : new Vector3(frameMove.x, 0f, frameMove.z).magnitude;
    }

    void UpdateTurnSignal()
    {
        float yaw = transform.eulerAngles.y;
        float yawRate = Mathf.DeltaAngle(prevYaw, yaw) / Mathf.Max(Time.deltaTime, 1e-4f);
        prevYaw = yaw;
        TurnSignal = Mathf.Clamp(yawRate / turnRateForFullBlend, -1f, 1f);
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region Animator
    // ───────────────────────────────────────────────────────────────────────

    void UpdateAnimator()
    {
        if (animator == null) return;

        // Damped so the blend eases idle→walk→run ("start then run").
        animator.SetFloat("Speed", PlanarSpeed, 0.12f, Time.deltaTime);
        animator.SetFloat("Turn", TurnSignal, 0.1f, Time.deltaTime);
        animator.SetBool("Crouch", CurrentState == MoveState.Crouch);

        // Aim / strafe (2D directional blend, shares AimX/AimY).
        animator.SetBool("Aiming", Aiming);
        animator.SetBool("StrafeLock", StrafeLock);
        animator.SetInteger("AimStance", (int)CurrentStance);
        animator.SetFloat("AimX", animAimX, 0.1f, Time.deltaTime);
        animator.SetFloat("AimY", animAimY, 0.1f, Time.deltaTime);
        AimX = animAimX; AimY = animAimY;

        // In-place turn.
        animator.SetBool("TurningInPlace", turningInPlace);
        animator.SetFloat("TurnInPlace", animTurnInPlaceDir, 0.08f, Time.deltaTime);

        // One-shot triggers detected earlier this frame.
        if (startWalkQueued) animator.SetTrigger("StartWalk");
        if (turn180Queued) animator.SetTrigger("Turn180");
        animator.SetFloat("Turn180Tier", turn180Tier);
        animator.SetFloat("Turn180Dir", turn180Dir);
    }

    void EndFrame()
    {
        startWalkQueued = false;
        turn180Queued = false;
        prevState = CurrentState;
    }

    #endregion
}
