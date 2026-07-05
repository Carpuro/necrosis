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
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerAnimatorDriver))]
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

    [Header("Discrete start turn (from idle)")]
    [Tooltip("Min angle between facing and pressed direction to play a discrete turn. " +
             "Below it you just start walking forward.")]
    public float startTurnMinAngle = 45f;
    public float startTurn90Duration = 0.4f;
    public float startTurn180Duration = 0.55f;

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
    PlayerInput input;
    PlayerAnimatorDriver animDriver;
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

    // Discrete start-turn runtime (idle → face pressed direction, then move).
    bool discreteTurning;
    float discreteTimer, discreteDuration;
    Quaternion discreteFrom, discreteTo;
    float turnSelect;            // -2 180L · -1 90L · +1 90R · +2 180R
    float prevYaw;

    // Dodge roll runtime.
    bool rolling;
    float rollTimer;
    Vector3 rollDir;

    // Per-frame scratch (recomputed each Update).
    float inH, inV;
    bool sprintHeld, moving, faceCamera, crouched, startingWalk;
    Vector3 camForward, camRight, frameMove;
    float animAimX, animAimY;

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region Unity lifecycle
    // ───────────────────────────────────────────────────────────────────────

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInput>();
        animDriver = GetComponent<PlayerAnimatorDriver>();
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
        UpdateStartTurn();   // before walk-start: a big turn angle wins over the ramp
        UpdateWalkStart();
        ApplyCrouchHeight();
        HandleMovement();
        ApplyGravityAndMove();
        UpdatePlanarSpeed();
        UpdateTurnSignal();
        UpdateAnimator();
        EndFrame();
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region Input
    // ───────────────────────────────────────────────────────────────────────

    void ReadInput()
    {
        input.Sample();
        inH = input.Horizontal;
        inV = input.Vertical;
        moving = input.Moving;
        sprintHeld = input.SprintHeld;

        // Interpret input edges into game state (toggles/stance live here, not in input).
        if (input.RunTogglePressed) runToggled = !runToggled;              // walk ↔ run
        if (input.CrouchTogglePressed) crouchToggled = !crouchToggled;     // crouch
        switch (input.StancePressed)
        {
            case 0: CurrentStance = Stance.Fists; break;
            case 1: CurrentStance = Stance.Melee; break;
            case 2: CurrentStance = Stance.Gun; break;
        }

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
        if (!input.RollPressed || !controller.isGrounded) return false;
        // Committed animations: can't roll (or interrupt) while a turn is playing.
        if (discreteTurning || turning180) return false;

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
        animDriver.PlayRoll();
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
        // Suppress aim while a turn is running so aim/turn don't flicker against each
        // other when inputs are spammed (one action at a time).
        Aiming = input.AimHeld && cameraTransform != null && !discreteTurning && !turning180;
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
        // Never ramp while a discrete turn is running/queued (turns don't ramp).
        if (prevState == MoveState.Idle && groundMove && !faceCamera && !crouched
            && !turning180 && !discreteTurning && idleTime >= walkStartIdleDelay)
        {
            walkStartTimer = walkStartDuration;
            startWalkQueued = true;
        }

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

        // A discrete turn ALWAYS completes (even a quick tap or spammed inputs) so it
        // can't flicker against walk/aim/roll. Aim is already suppressed while turning.
        if (discreteTurning) UpdateDiscreteTurn();      // gates movement like the 180
        else if (faceCamera) AimMovement();
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
        if (turning180 || discreteTurning) return;
        if (Vector3.Angle(lastMoveDir, worldDir) <= turn180Threshold) return;

        turning180 = true;
        turn180Timer = 0f;
        turn180Queued = true;

        // 180 takes priority over the start step.
        startWalkQueued = false;
        walkStartTimer = 0f;
        startingWalk = false;

        // Turn side by signed angle vs. the last heading.
        turn180Dir = Vector3.SignedAngle(lastMoveDir, worldDir, Vector3.up) >= 0f ? 1f : -1f;

        turn180From = transform.rotation;
        // Rotate exactly 180° (matches the clip) rather than to the arbitrary input
        // angle — no foot slide. Steering handles any small remainder afterwards.
        turn180To = Quaternion.Euler(0f, transform.eulerAngles.y + 180f * turn180Dir, 0f);

        // Freeze the speed tier at trigger time so idle/walk/run pick the right clip.
        turn180Tier = currentSpeed < 0.5f ? 0f
                    : (currentSpeed < runSpeed - 1f ? 1f : 2f);
    }

    /// <summary>While turning 180: don't advance, keep the speed (so it resumes
    /// straight into run without a phantom ramp), rotate over the clip.</summary>
    void UpdateTurn180()
    {
        frameMove = Vector3.zero;
        turn180Timer += Time.deltaTime;

        // Rotate in lockstep with the Turn180 clip (not a fixed timer) so the feet
        // don't slide — same fix as the discrete turn. Timer fallback during the
        // crossfade / when there's no model.
        bool hasProgress = animDriver.TryGetStateProgress("Turn180", out float p);
        float t = hasProgress ? Mathf.Clamp01(p)
                              : Mathf.Clamp01(turn180Timer / turn180Duration);
        transform.rotation = Quaternion.Slerp(turn180From, turn180To, t);

        bool done = hasProgress ? p >= 1f : turn180Timer >= turn180Duration;
        if (done && turn180Timer >= 0.1f) turning180 = false;
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region Discrete start turn (idle → face pressed direction)
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>From idle, if the pressed direction is far off the current facing,
    /// play a discrete turn (90 L/R or 180) in place; movement resumes after.</summary>
    void UpdateStartTurn()
    {
        if (discreteTurning) return; // already turning; HandleMovement drives it

        // Fresh start from a real stop, moving forward-ish input, not aiming/crouched.
        bool freshStart = prevState == MoveState.Idle && moving && !faceCamera && !crouched
                          && !turning180 && idleTime >= walkStartIdleDelay
                          && cameraTransform != null;
        if (!freshStart) return;

        Vector3 worldDir = (camForward * inV + camRight * inH).normalized;
        if (worldDir.sqrMagnitude < 0.01f) return;

        float ang = Vector3.SignedAngle(transform.forward, worldDir, Vector3.up); // -180..+180
        if (Mathf.Abs(ang) < startTurnMinAngle) return; // forward-ish → normal walk start

        // Pick clip: 90 or 180, and side.
        if (Mathf.Abs(ang) > 135f)
        {
            // 180: side by mouse (fallback to signed angle, then right).
            float side = Mathf.Abs(input.MouseX) > 0.01f ? Mathf.Sign(input.MouseX)
                       : (Mathf.Abs(ang) < 179f ? Mathf.Sign(ang) : 1f);
            turnSelect = 2f * side;
            discreteDuration = startTurn180Duration;
        }
        else
        {
            turnSelect = Mathf.Sign(ang); // ±1 = 90 L/R
            discreteDuration = startTurn90Duration;
        }

        discreteTurning = true;
        discreteTimer = 0f;
        discreteFrom = transform.rotation;
        // Rotate by the CLIP'S exact amount (90 or 180), not the arbitrary input
        // angle — so the body rotation matches the clip's foot steps (no slide).
        // Post-turn steering smooths any small remainder toward the real input dir.
        float turnSide = Mathf.Sign(turnSelect);
        float deg = (Mathf.Abs(turnSelect) > 1.5f ? 180f : 90f) * turnSide;
        discreteTo = Quaternion.Euler(0f, transform.eulerAngles.y + deg, 0f);
        // Cancel the forward walk-start; the discrete turn takes over.
        startWalkQueued = false; walkStartTimer = 0f; startingWalk = false;
    }

    /// <summary>Rotates over the discrete turn clip without advancing. Called from
    /// HandleMovement so it gates movement just like the 180.</summary>
    void UpdateDiscreteTurn()
    {
        frameMove = Vector3.zero;
        currentSpeed = 0f;
        discreteTimer += Time.deltaTime;

        // Rotate the body in lockstep with the CLIP'S playback progress so the feet
        // don't slide. Fall back to a timer if the animator/state isn't available yet
        // (e.g. during the 0.1s crossfade into the turn state, or no model).
        // Committed: rotates to the fixed snapped target (no mid-turn re-aim), so the
        // body rotation always matches the clip amount and the feet don't slide.
        bool hasProgress = animDriver.TryGetStateProgress("TurnInPlace", out float p);
        float t = hasProgress ? Mathf.Clamp01(p)
                              : Mathf.Clamp01(discreteTimer / discreteDuration);

        transform.rotation = Quaternion.Slerp(discreteFrom, discreteTo, t);

        // Stay frozen (no movement at all) until the turn clip is FULLY finished, so
        // walking never starts before the turn ends (avoids the odd first step).
        bool done = hasProgress ? p >= 1f : discreteTimer >= discreteDuration;
        if (!done || discreteTimer < 0.1f) return;
        discreteTurning = false;

        // Concatenate turn → step-off → walk: if you're STILL holding the direction
        // when the turn finishes, play the walk-start step-off then move. Slower but
        // the most natural (feet plant cleanly). Both 90 and 180. A single tap just
        // leaves you turned, standing.
        if (moving)
        {
            walkStartTimer = walkStartDuration;
            startWalkQueued = true;
        }
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
        AimX = animAimX; AimY = animAimY; // expose for debug/other systems

        animDriver.Apply(new PlayerAnimatorDriver.Frame
        {
            speed = PlanarSpeed,
            turn = TurnSignal,
            crouch = CurrentState == MoveState.Crouch,
            aiming = Aiming,
            strafeLock = StrafeLock,
            stance = (int)CurrentStance,
            aimX = animAimX,
            aimY = animAimY,
            turningInPlace = discreteTurning,
            turnInPlaceDir = turnSelect,
            startWalk = startWalkQueued,
            turn180 = turn180Queued,
            turn180Tier = turn180Tier,
            turn180Dir = turn180Dir,
        });
    }

    void EndFrame()
    {
        startWalkQueued = false;
        turn180Queued = false;
        prevState = CurrentState;
        // Idle timer updated at END of frame so start-turn/walk-start both read the
        // accumulated stand time (not a value already zeroed mid-frame).
        if (moving) idleTime = 0f; else idleTime += Time.deltaTime;
    }

    #endregion
}
