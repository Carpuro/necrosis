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
    [Tooltip("Max body turn speed (deg/s) WHILE MOVING. Caps how fast we rotate toward the " +
             "move direction so a turn is a deliberate, animated motion instead of an instant " +
             "snap (which leaves the straight clip playing while the body just yaws). Higher = " +
             "snappier/closer to old behavior; lower = more pronounced turn animation.")]
    public float maxTurnSpeed = 400f;

    [Header("Acceleration (idle→walk→run ramp)")]
    [Tooltip("Speed gained per second; makes the blend pass through walk before run.")]
    public float acceleration = 10f;
    public float deceleration = 16f;

    [Header("Walk start (ARC style)")]
    [Tooltip("Playback speed of the step-off (idle→walk) clip. 8x looked like a spasm; ~3-4x " +
             "reads as a brisk start. Tunable live — no rebuild needed.")]
    public float walkStartAnimSpeed = 3.5f;
    [Tooltip("Seconds the player stays planted (no advance) during the start step. Keep it " +
             "roughly in step with the clip speed so movement resumes as the step lands.")]
    public float walkStartDuration = 0.16f;
    [Tooltip("Seconds standing still before the start step is allowed. Prevents it " +
             "from firing right after moving (reversals, turns).")]
    public float walkStartIdleDelay = 0.4f;

    [Header("Discrete start turn (from idle)")]
    [Tooltip("Min angle between facing and pressed direction to play a discrete turn. " +
             "Below it you just start walking forward.")]
    public float startTurnMinAngle = 45f;
    public float startTurn90Duration = 0.4f;
    public float startTurn180Duration = 0.55f;
    [Tooltip("If the player has already slowed below this speed (m/s), a turn plays the " +
             "IN-PLACE idle turn even if walkStartIdleDelay hasn't elapsed. Prevents a " +
             "180 from firing the RUNNING turn after running and stopping.")]
    public float turnFromStopSpeed = 1f;
    [Tooltip("Max consecutive buffered follow-up turns in one chain. Fast clicks queue a " +
             "single next turn (latest wins); this caps how many chain back-to-back so a " +
             "burst can't spin forever. A fresh deliberate turn resets the count.")]
    public int maxQueuedTurns = 2;

    [Header("180 turn (reverse direction)")]
    [Tooltip("Angle vs. last heading that triggers the 180.")]
    public float turn180Threshold = 135f;
    public float turn180Duration = 0.45f;

    [Header("Turn blend")]
    [Tooltip("Body turn speed (deg/s) mapped to a FULL turn blend. The turn animation is driven " +
             "by how fast the body is actually rotating (capped by maxTurnSpeed), so a turn shows " +
             "the turn clip and straight shows straight. Lower = turns show sooner.")]
    public float turnRateForFullBlend = 180f;
    [Tooltip("Turn-signal magnitude (0..1) below which we stay fully on the STRAIGHT clip. " +
             "Gentle steering keeps rotating the body without blending in the turn clip, so " +
             "straight and turn animations don't overlap. Raise to require a sharper turn.")]
    [Range(0f, 0.9f)] public float turnDeadzone = 0.2f;

    [Header("Dodge roll")]
    public float rollSpeed = 7f;
    public float rollDuration = 0.7f;

    [Header("Jump")]
    [Tooltip("Upward launch speed (m/s) on jump. Height ≈ jumpSpeed² / (2·|gravity|).")]
    public float jumpSpeed = 7f;
    [Tooltip("Upward speed for the standing HIGH jump (double-tap Space). Usually > jumpSpeed.")]
    public float jumpSpeedHigh = 9f;
    [Tooltip("Seconds to wait for a second Space tap (idle only): single tap = low jump, " +
             "double tap = high jump. This adds a touch of latency to the standing jump.")]
    public float jumpDoubleTapWindow = 0.22f;
    [Tooltip("Takeoff point (0..1 of the clip) for STANDING/WALKING jumps (they crouch first, " +
             "so takeoff is later). The upward impulse fires here. Raise if it launches while " +
             "still crouched; lower if it launches too late.")]
    [Range(0f, 0.9f)] public float jumpTakeoffNormalized = 0.35f;
    [Tooltip("Takeoff point (0..1) for the RUNNING jump — a running leap has little crouch, so " +
             "this is usually much earlier than the standing takeoff.")]
    [Range(0f, 0.9f)] public float jumpTakeoffNormalizedRun = 0.12f;
    [Tooltip("Fallback windup time (s) used only until the jump clip's progress is readable.")]
    public float jumpWindupDuration = 0.28f;

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
    bool discreteTurning, turnClipReady, turnQueued;
    float discreteTimer, discreteDuration;
    Vector3 queuedTurnDir; // buffered next-turn direction (single slot, latest wins)
    int queuedTurnCount;   // consecutive chained turns; capped by maxQueuedTurns
    bool steerActive;      // true while GroundMovement is steering toward a move dir
    float prevYaw;         // last frame's body yaw, for the turn-rate signal
    Quaternion discreteFrom, discreteTo;
    float turnSelect;            // -2 180L · -1 90L · +1 90R · +2 180R

    // Dodge roll runtime.
    bool rolling, rollRequested;
    float rollTimer;
    Vector3 rollDir;
    bool airborne;          // true while off the ground (after a jump)
    Vector3 airVelocity;    // horizontal velocity carried through the air (0 for idle jump)
    bool jumpPending;       // an idle single-tap is buffered, waiting for a possible double-tap
    float pendingJumpTime;  // Time.time of that first tap
    bool jumpWindup;        // jump anim playing its anticipation; grounded until launch
    float jumpWindupTimer, pendingUpSpeed;
    int pendingJumpType;    // which jump is winding up (picks the takeoff timing)
    bool JumpActive => jumpWindup || airborne;

    // JumpType selector (matches the 1D blend in the animator).
    const int JUMP_LOW = 0, JUMP_HIGH = 1, JUMP_FWD = 2, JUMP_BACK = 3, JUMP_RUN = 4;

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
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        ReadInput();

        // Dodge roll fully overrides movement/animation while active.
        if (rolling) { UpdateRoll(); return; }
        if (TryStartRoll()) return;

        TryJump();          // begins a jump (fires anim + windup)
        UpdateJumpWindup(); // applies the launch impulse at the clip's takeoff moment

        UpdateStanceAndAiming();
        UpdateMoveState();
        UpdateStartTurn();   // before walk-start: a big turn angle wins over the ramp
        UpdateTurnQueue();   // buffer a follow-up turn if input changes mid-turn
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
        // Ctrl is context-sensitive: while running it triggers a dodge roll, otherwise
        // it toggles crouch. rollRequested is consumed by TryStartRoll this frame.
        if (input.CrouchPressed)
        {
            if (moving && (sprintHeld || runToggled)) rollRequested = true;
            else crouchToggled = !crouchToggled;
        }
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

    /// <summary>Starts a roll on Ctrl while running (grounded). Returns true if a roll began.</summary>
    bool TryStartRoll()
    {
        if (!rollRequested || !controller.isGrounded) return false;
        rollRequested = false;
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
    #region Jump
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>Jump on Space. Moving = directional/run jump that carries momentum and
    /// resumes on landing. Idle = jump in place: single tap = low, double tap = high (a
    /// short buffer distinguishes them). The arc is driven by gravity in ApplyGravityAndMove;
    /// this sets the impulse, the carried air velocity, and fires the animation.</summary>
    void TryJump()
    {
        if (jumpWindup) return; // already winding up a jump
        if (!controller.isGrounded) { jumpPending = false; return; } // can't jump in air
        if (discreteTurning || turning180) return;

        bool running = moving && (sprintHeld || runToggled);

        if (input.JumpPressed && moving)
        {
            // Moving jump (walk or run): LAUNCH IMMEDIATELY — a running leap has no crouch
            // to wait for, so a windup here just reads as "late". Uses the run-jump clip and
            // carries the takeoff momentum so the player flows through and keeps moving.
            Vector3 dir = (camForward * inV + camRight * inH).normalized;
            DoJump(JUMP_RUN, dir * (running ? runSpeed : walkSpeed), jumpSpeed, immediate: true);
            return;
        }

        if (input.JumpPressed && !moving)
        {
            // Idle jump stays in place (no horizontal velocity). Single tap = low; a second
            // tap within the window = high.
            if (jumpPending && Time.time - pendingJumpTime <= jumpDoubleTapWindow)
            {
                jumpPending = false;
                DoJump(JUMP_HIGH, Vector3.zero, jumpSpeedHigh, immediate: false);
            }
            else { jumpPending = true; pendingJumpTime = Time.time; }
            return;
        }

        // No second tap arrived in time → resolve the buffered idle tap as a low jump.
        if (jumpPending && Time.time - pendingJumpTime > jumpDoubleTapWindow)
        {
            jumpPending = false;
            DoJump(JUMP_LOW, Vector3.zero, jumpSpeed, immediate: false);
        }
    }

    /// <summary>Starts a jump. immediate=true launches on the spot (moving/running jumps,
    /// no crouch to wait for). immediate=false enters a windup and launches on the clip's
    /// takeoff moment (standing jumps, which crouch first).</summary>
    void DoJump(int type, Vector3 horizVelocity, float upSpeed, bool immediate)
    {
        animDriver.PlayJump(type);
        airVelocity = horizVelocity; // held through windup (0 idle) and the air
        pendingUpSpeed = upSpeed;
        pendingJumpType = type;
        if (immediate)
        {
            verticalVelocity = upSpeed;
            airborne = true;
            jumpWindup = false;
        }
        else
        {
            jumpWindup = true;
            jumpWindupTimer = 0f;
        }
    }

    /// <summary>During the windup the player is still grounded (anticipation crouch plays);
    /// the launch impulse fires when the jump CLIP reaches its takeoff point
    /// (jumpTakeoffNormalized), so it matches the animation regardless of clip length. A
    /// timer is the fallback for the few frames before the Jump state is readable.</summary>
    void UpdateJumpWindup()
    {
        if (!jumpWindup) return;
        jumpWindupTimer += Time.deltaTime;

        // The run jump takes off earlier in its clip than the standing/walking jumps.
        float takeoff = pendingJumpType == JUMP_RUN ? jumpTakeoffNormalizedRun : jumpTakeoffNormalized;
        bool reached = animDriver.TryGetStateProgress("Jump", out float p)
            ? p >= takeoff
            : jumpWindupTimer >= jumpWindupDuration;
        if (!reached) return;

        verticalVelocity = pendingUpSpeed;
        airborne = true;
        jumpWindup = false;
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
            && !turning180 && !discreteTurning && !JumpActive && idleTime >= walkStartIdleDelay)
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
        steerActive = false; // only GroundMovement's steering path sets this true
        if (cameraTransform == null) return;

        // Jump windup or airborne: carry the takeoff velocity (0 for idle jump), no ground
        // turns/walk-start. During windup the player holds position/momentum before launch.
        if (JumpActive) { AirMovement(); return; }

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

    /// <summary>While airborne: carry the takeoff horizontal velocity (zero for an idle
    /// jump, so it goes straight up and lands in place; the walk/run momentum for a moving
    /// jump, so it flows through and continues on landing). Vertical is handled by
    /// ApplyGravityAndMove; the one-shot jump clip plays over locomotion.</summary>
    void AirMovement()
    {
        frameMove = airVelocity; // y added later by gravity
    }

    /// <summary>Free locomotion: accelerate toward the move dir; handle 180 turns.</summary>
    void GroundMovement()
    {
        Vector3 worldDir = (camForward * inV + camRight * inH).normalized;

        TryTrigger180(worldDir);
        lastMoveDir = worldDir;

        // If we're basically stopped and the input points far off our facing, DON'T
        // slide forward — plant and let the discrete in-place turn (UpdateStartTurn)
        // rotate us first. A quick two-direction tap (or a turned camera) can produce a
        // transient input that dips just under startTurnMinAngle for one frame; without
        // this guard GroundMovement would advance in that direction with the walk-start
        // step-off instead of the intended 90/180 turn. Planting keeps currentSpeed 0 so
        // the turn's "stopped" test stays true and it fires next frame; once we've turned
        // (angle < startTurnMinAngle) normal movement resumes. Only near a stop — a real
        // moving turn (currentSpeed high) still steers/180s as before.
        float faceAngle = Vector3.Angle(transform.forward, worldDir);
        if (!turning180 && currentSpeed <= turnFromStopSpeed
            && faceAngle >= startTurnMinAngle && worldDir.sqrMagnitude > 0.01f)
        {
            currentSpeed = 0f;
            frameMove = Vector3.zero;
            return;
        }

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

            // Rotate toward the move dir at a CAPPED rate (deg/s) so a turn takes visible
            // time and the turn clip actually plays, instead of an instant snap that leaves
            // the straight clip yawing oddly. The turn signal (below) reads how far off the
            // desired dir we are, so the turn animation shows whenever the camera is off-axis.
            steerActive = true;
            Quaternion targetRot = Quaternion.LookRotation(worldDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot,
                maxTurnSpeed * Time.deltaTime);
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

        // Tier by the intended STATE (not instantaneous speed, which is still ramping
        // when you just started running → would wrongly pick the walk-180 clip).
        turn180Tier = (CurrentState == MoveState.Run || CurrentState == MoveState.Sprint) ? 2f
                    : (CurrentState == MoveState.Walk ? 1f : 0f);
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
        if (done && turn180Timer >= 0.1f)
        {
            turning180 = false;
            // If a reversal was buffered during the 180, run it as a deliberate turn.
            TryStartQueuedTurn();
        }
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region Discrete start turn (idle → face pressed direction)
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>From idle, if the pressed direction is far off the current facing,
    /// play a discrete turn (90 L/R or 180) in place; movement resumes after.</summary>
    void UpdateStartTurn()
    {
        if (discreteTurning || JumpActive) return; // already turning, or jumping

        // Fire the deliberate in-place turn whenever we're PHYSICALLY stopped and get a
        // reversing input, not just when prevState was Idle. Gating on currentSpeed (not
        // prevState) is what makes a chained turn stay deliberate: right after an idle
        // turn the player is still stopped (currentSpeed ~0) but prevState is already
        // Walk (input held), so the old prevState==Idle gate let GroundMovement's
        // TryTrigger180 grab the reversal and play the FASTER moving-180 instead of
        // another idle turn. "Stopped" = idleDelay elapsed (dead stop) OR slowed below
        // turnFromStopSpeed (run → release → reverse). If you reverse WITHOUT braking,
        // currentSpeed stays high, this doesn't fire, and TryTrigger180 does the running
        // pivot (correct).
        bool stopped = idleTime >= walkStartIdleDelay || currentSpeed <= turnFromStopSpeed;
        bool freshStart = stopped && moving && !faceCamera && !crouched
                          && !turning180 && cameraTransform != null;
        if (!freshStart) return;

        Vector3 worldDir = (camForward * inV + camRight * inH).normalized;
        if (worldDir.sqrMagnitude < 0.01f) return;

        float ang = Vector3.SignedAngle(transform.forward, worldDir, Vector3.up); // -180..+180
        if (Mathf.Abs(ang) < startTurnMinAngle) return; // forward-ish → normal walk start

        queuedTurnCount = 0; // fresh deliberate turn → start a new chain
        BeginDiscreteTurn(worldDir, ang);
    }

    /// <summary>Set up and start a discrete in-place turn toward worldDir (ang = signed
    /// angle from the current facing). Shared by the fresh-start path and the turn queue
    /// so a buffered follow-up turn behaves identically to a first one.</summary>
    void BeginDiscreteTurn(Vector3 worldDir, float ang)
    {
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
        // Force the turn clip to replay from frame 0 so a chained or mid-turn-interrupted
        // turn gets a clean anticipation and a fresh normalizedTime. Without this the
        // animator keeps accumulating time in the TurnInPlace state and the clip-synced
        // body rotation snaps 180° instantly ("turn happens very quickly"). turnClipReady
        // stays false until we've confirmed the fresh clip, so the timer drives the one
        // frame before Play() takes effect (avoids a stale-time snap that first frame).
        animDriver.RestartTurnInPlace();
        turnClipReady = false;
        discreteFrom = transform.rotation;
        // Rotate by the CLIP'S exact amount (90 or 180), not the arbitrary input
        // angle — so the body rotation matches the clip's foot steps (no slide).
        // Post-turn steering smooths any small remainder toward the real input dir.
        float turnSide = Mathf.Sign(turnSelect);
        float deg = (Mathf.Abs(turnSelect) > 1.5f ? 180f : 90f) * turnSide;
        discreteTo = Quaternion.Euler(0f, transform.eulerAngles.y + deg, 0f);
        // Remember this as the heading so GroundMovement doesn't immediately fire a
        // SECOND 180 afterwards (stale lastMoveDir was the pre-turn direction).
        lastMoveDir = worldDir;
        // Cancel the forward walk-start; the discrete turn takes over.
        startWalkQueued = false; walkStartTimer = 0f; startingWalk = false;
    }

    /// <summary>The direction the player will be facing once the active turn finishes.</summary>
    Vector3 TurnTargetForward()
    {
        if (discreteTurning) return discreteTo * Vector3.forward;
        if (turning180) return turn180To * Vector3.forward;
        return transform.forward;
    }

    /// <summary>While a turn is in motion, buffer the latest input as a follow-up turn so
    /// fast clicks serialize instead of overlapping. Single slot (latest wins) so mashing
    /// can't build a backlog or spin. Only buffers input that points away from where this
    /// turn will leave us facing; if the input realigns with the target, clear it.</summary>
    void UpdateTurnQueue()
    {
        if (!(discreteTurning || turning180)) return;
        if (!moving || faceCamera || crouched || cameraTransform == null) { turnQueued = false; return; }

        Vector3 wd = (camForward * inV + camRight * inH).normalized;
        if (wd.sqrMagnitude < 0.01f) return;

        if (Vector3.Angle(TurnTargetForward(), wd) >= startTurnMinAngle)
        {
            turnQueued = true;
            queuedTurnDir = wd;
        }
        else turnQueued = false; // input now matches where we'll face → no follow-up turn
    }

    /// <summary>Consume a buffered turn the instant the current one ends, so a fast second
    /// input becomes a proper discrete turn instead of leaking into walk-start / the
    /// moving-180. Returns true if it started one.</summary>
    bool TryStartQueuedTurn()
    {
        if (!turnQueued) return false;
        turnQueued = false;
        if (cameraTransform == null) return false;
        if (queuedTurnCount >= maxQueuedTurns) return false; // chain cap → stop, let it settle

        float ang = Vector3.SignedAngle(transform.forward, queuedTurnDir, Vector3.up);
        if (Mathf.Abs(ang) < startTurnMinAngle) return false; // already facing it → let it walk
        queuedTurnCount++;
        BeginDiscreteTurn(queuedTurnDir, ang);
        return true;
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
        // Only trust the clip time once the forced replay has taken effect (normalizedTime
        // back near 0). Until then drive by the timer, so an interrupted turn never reads
        // the previous turn's stale (>1) time and snaps.
        if (hasProgress && p < 0.5f) turnClipReady = true;
        float t = turnClipReady ? Mathf.Clamp01(p)
                                : Mathf.Clamp01(discreteTimer / discreteDuration);

        transform.rotation = Quaternion.Slerp(discreteFrom, discreteTo, t);

        // Stay frozen (no movement at all) until the turn clip is FULLY finished, so
        // walking never starts before the turn ends (avoids the odd first step).
        bool done = turnClipReady ? p >= 1f : discreteTimer >= discreteDuration;
        if (!done || discreteTimer < 0.1f) return;
        discreteTurning = false;

        // A buffered follow-up turn (from a fast second input) runs immediately, before
        // any walk-start, so chained turns stay deliberate and never overlap.
        if (TryStartQueuedTurn()) return;

        // NO walk-start step-off after a turn. The turn already moved the legs, so
        // stacking a step-off on top played two motions back-to-back = the doubled/
        // "spasm" start. If still holding a direction, flow straight into locomotion via
        // the normal accel ramp; a single tap just leaves you turned, standing. (The
        // step-off still plays for a normal idle→walk from a dead stop — UpdateWalkStart.)
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    #region Gravity, apply, derived signals
    // ───────────────────────────────────────────────────────────────────────

    void ApplyGravityAndMove()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
            airborne = false; // landed
        }
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
        // Drive the turn blend by the body's actual rotation speed (deg/s). Because the body
        // rotation is capped (maxTurnSpeed), a turn is a sustained rotation, so the turn clip
        // plays throughout instead of flickering. Track prevYaw every frame (so no spike when
        // steering resumes) but only feed the signal while actively steering on the ground.
        float yaw = transform.eulerAngles.y;
        float yawRate = Mathf.DeltaAngle(prevYaw, yaw) / Mathf.Max(Time.deltaTime, 1e-4f);
        prevYaw = yaw;

        // Deadzone: gentle steering (below the deadzone) stays fully on the straight clip so
        // straight and turn don't overlap; past it, ramp 0..1 into the turn clip.
        float raw = steerActive ? Mathf.Clamp(yawRate / turnRateForFullBlend, -1f, 1f) : 0f;
        float mag = Mathf.InverseLerp(turnDeadzone, 1f, Mathf.Abs(raw));
        TurnSignal = Mathf.Sign(raw) * mag;
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
            walkStartSpeed = walkStartAnimSpeed,
            grounded = !JumpActive, // false through windup + air so the jump state persists until landing
            turn180 = turn180Queued,
            turn180Tier = turn180Tier,
            turn180Dir = turn180Dir,
            // Idle 180 plays faster than the 90 (3x vs 2x base; 25% slower than 4x).
            turnInPlaceSpeed = Mathf.Abs(turnSelect) > 1.5f ? 3f : 2f,
        });
    }

    void EndFrame()
    {
        startWalkQueued = false;
        turn180Queued = false;
        rollRequested = false; // consumed this frame or dropped (e.g. not grounded)
        prevState = CurrentState;
        // Idle timer updated at END of frame so start-turn/walk-start both read the
        // accumulated stand time (not a value already zeroed mid-frame).
        if (moving) idleTime = 0f; else idleTime += Time.deltaTime;
    }

    #endregion
}
