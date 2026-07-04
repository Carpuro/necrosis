using UnityEngine;

/// <summary>
/// NECROSIS://PROTOCOLO — Firma del jugador (GDD pilar 2 + v0.5 §3.2 "Eres el faro").
/// Expone dos radios de detección que la IA consulta:
///  - NoiseRadius: ruido por movimiento (agachado &lt; caminar &lt; correr)
///  - EnergyRadius: firma energética (linterna). De noche se multiplica.
///
/// Setup: mismo GameObject que PlayerController. Asignar un Spot Light hijo
/// en 'flashlight' (la linterna). Tecla F para alternar.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerSignature : MonoBehaviour
{
    [Header("Ruido por movimiento (metros)")]
    public float idleNoise = 0.5f;
    public float crouchNoise = 1.5f;
    public float walkNoise = 6f;
    public float runNoise = 14f;

    [Header("Firma energética (metros)")]
    public float flashlightEnergyRadius = 8f;
    [Tooltip("De noche, la ciudad calla y tú brillas: multiplicador nocturno")]
    public float nightEnergyMultiplier = 10f;

    [Header("Referencias")]
    public Light flashlight;

    public float NoiseRadius { get; private set; }
    public float EnergyRadius { get; private set; }
    public bool FlashlightOn { get; private set; }

    PlayerController player;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        if (flashlight != null) flashlight.enabled = false;
    }

    void Update()
    {
        // --- Linterna (F) ---
        if (Input.GetKeyDown(KeyCode.F))
        {
            FlashlightOn = !FlashlightOn;
            if (flashlight != null) flashlight.enabled = FlashlightOn;
        }

        // --- Ruido según estado de movimiento ---
        NoiseRadius = player.CurrentState switch
        {
            PlayerController.MoveState.Idle => idleNoise,
            PlayerController.MoveState.Crouch => crouchNoise,
            PlayerController.MoveState.Walk => walkNoise,
            PlayerController.MoveState.Run => runNoise,
            _ => idleNoise
        };

        // --- Firma energética ---
        float energy = FlashlightOn ? flashlightEnergyRadius : 0f;
        bool night = DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight;
        EnergyRadius = night ? energy * nightEnergyMultiplier : energy;
    }

    // Visualización en el editor para tunear a ojo
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, NoiseRadius);
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, EnergyRadius);
    }
}
