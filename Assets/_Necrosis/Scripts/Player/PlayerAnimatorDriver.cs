using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NECROSIS://PROTOCOLO — Player animator output.
///
/// Single responsibility: translate the player's per-frame state into Animator
/// parameters. It never reads input or moves anything. PlayerController calls
/// <see cref="Apply"/> once per frame (in a fixed order) so one-shot triggers
/// are never missed to a stale-order Update.
///
/// Lives on the player capsule; the Animator itself is on the rigged model child.
/// </summary>
public class PlayerAnimatorDriver : MonoBehaviour
{
    [Tooltip("Animator of the rigged model (child). If null, nothing is driven.")]
    public Animator animator;

    // ── Debug state tracking (for the on-screen HUD) ─────────────────────────
    static readonly string[] KnownStates =
    {
        "Locomotion", "Crouch", "CrouchEnter", "CrouchExit", "WalkStart",
        "TurnInPlace", "Turn180", "Aim_fists", "Aim_melee", "Aim_gun",
        "Strafe", "Roll", "Death",
    };
    readonly Dictionary<int, string> stateNames = new();

    /// <summary>Name of the animator state playing now (for debug HUD).</summary>
    public string CurrentAnimState { get; private set; } = "-";
    /// <summary>Recent state changes, newest first (for debug HUD).</summary>
    public readonly List<string> History = new();

    void Awake()
    {
        foreach (var s in KnownStates) stateNames[Animator.StringToHash(s)] = s;
    }

    void TrackState()
    {
        if (animator == null) return;
        int hash = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        string name = stateNames.TryGetValue(hash, out var n) ? n : "?";
        if (name == CurrentAnimState) return;
        CurrentAnimState = name;
        History.Insert(0, name);
        if (History.Count > 8) History.RemoveAt(History.Count - 1);
    }

    /// <summary>All the values the Animator needs for one frame.</summary>
    public struct Frame
    {
        public float speed;            // planar m/s
        public float turn;             // -1..+1 yaw rate
        public bool crouch;
        public bool aiming;
        public bool strafeLock;
        public int stance;             // 0 fists · 1 melee · 2 gun
        public float aimX, aimY;       // strafe axes (body-relative)
        public bool turningInPlace;
        public float turnInPlaceDir;   // -1..+1
        public bool startWalk;         // one-shot trigger
        public bool turn180;           // one-shot trigger
        public float turn180Tier;      // 0 idle · 1 walk · 2 run
        public float turn180Dir;       // -1 left · +1 right
        public float turnInPlaceSpeed; // playback multiplier for the idle-turn state
    }

    /// <summary>Feeds one frame of state to the Animator. No-op if unassigned.</summary>
    public void Apply(in Frame f)
    {
        if (animator == null) return;
        float dt = Time.deltaTime;

        // While turning in place, force Speed to 0 INSTANTLY so no walk cycle bleeds
        // under the turn clip; otherwise damp so the blend eases idle→walk→run.
        if (f.turningInPlace) animator.SetFloat("Speed", 0f);
        else animator.SetFloat("Speed", f.speed, 0.12f, dt);
        animator.SetFloat("Turn", f.turn, 0.1f, dt);
        animator.SetBool("Crouch", f.crouch);

        // Aim / strafe (2D directional blend).
        animator.SetBool("Aiming", f.aiming);
        animator.SetBool("StrafeLock", f.strafeLock);
        animator.SetInteger("AimStance", f.stance);
        animator.SetFloat("AimX", f.aimX, 0.1f, dt);
        animator.SetFloat("AimY", f.aimY, 0.1f, dt);

        // In-place turn.
        animator.SetBool("TurningInPlace", f.turningInPlace);
        animator.SetFloat("TurnInPlace", f.turnInPlaceDir, 0.08f, dt);
        animator.SetFloat("TurnInPlaceSpeed", f.turnInPlaceSpeed);

        // One-shot triggers detected this frame.
        if (f.startWalk) animator.SetTrigger("StartWalk");
        if (f.turn180) animator.SetTrigger("Turn180");
        animator.SetFloat("Turn180Tier", f.turn180Tier);
        animator.SetFloat("Turn180Dir", f.turn180Dir);

        TrackState(); // update the debug state name + history
    }

    /// <summary>Normalized playback progress (0..1+) of the named state on layer 0,
    /// so movement code can rotate the body in lockstep with the clip (no slide).
    /// Returns false if no animator or that state isn't the current one.</summary>
    public bool TryGetStateProgress(string stateName, out float t)
    {
        t = 0f;
        if (animator == null) return false;
        var s = animator.GetCurrentAnimatorStateInfo(0);
        if (!s.IsName(stateName)) return false;
        t = s.normalizedTime;
        return true;
    }

    /// <summary>Fires the dodge-roll animation (roll starts outside the Apply path).</summary>
    public void PlayRoll()
    {
        if (animator != null) animator.SetTrigger("Roll");
    }

    /// <summary>Fires the death animation (called by PlayerHealth).</summary>
    public void PlayDeath()
    {
        if (animator != null) animator.SetTrigger("Die");
    }
}
