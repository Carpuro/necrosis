using UnityEngine;

/// <summary>
/// NECROSIS://PROTOCOLO — Controlador de jugador en tercera persona.
/// Movimiento relativo a cámara: caminar, correr, agacharse.
/// Requiere: CharacterController en el mismo GameObject, tag "Player".
/// La cámara la maneja ShoulderCamera.cs (aparte).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Velocidades (m/s)")]
    public float crouchSpeed = 1.5f;
    public float walkSpeed = 3.5f;
    public float runSpeed = 6.5f;
    public float sprintSpeed = 8f;
    [Tooltip("Velocidad al apuntar/strafear (clic derecho), estilo State of Decay.")]
    public float aimSpeed = 2.8f;

    [Header("Física")]
    public float gravity = -20f;
    public float rotationSmoothness = 12f;

    [Header("Referencias")]
    public Transform cameraTransform; // asignar la Main Camera

    [Header("Animación (opcional)")]
    [Tooltip("Animator del modelo rigged (p. ej. Mixamo). Si es null, sigue la cápsula.\n" +
             "Parámetros esperados en el Animator Controller: float 'Speed', bool 'Crouch'.")]
    public Animator animator;

    public enum MoveState { Idle, Crouch, Walk, Run, Sprint }
    public MoveState CurrentState { get; private set; } = MoveState.Idle;

    // C = caminar/correr (toggle), Ctrl = agacharse (toggle), Shift = esprintar (mantener)
    bool runToggled;
    bool crouchToggled;

    /// <summary>Velocidad horizontal real (m/s). La leen Animator y pasos.</summary>
    public float PlanarSpeed { get; private set; }

    /// <summary>Giro normalizado: -1 (izquierda) .. +1 (derecha). Lo lee el Animator.</summary>
    public float TurnSignal { get; private set; }

    /// <summary>True mientras se apunta/strafea (clic derecho mantenido).</summary>
    public bool Aiming { get; private set; }

    // Postura de combate seleccionada (1 puños, 2 melé, 3 arma). Decide qué set
    // de animaciones de strafe usa el modo apuntar.
    public enum Stance { Fists, Melee, Gun }
    public Stance CurrentStance { get; private set; } = Stance.Fists;

    [Header("Giro")]
    [Tooltip("Velocidad angular (grados/s) que corresponde al giro completo de la animación.")]
    public float turnRateForFullBlend = 160f;
    float prevYaw;

    CharacterController controller;
    float verticalVelocity;
    float normalHeight;
    float normalCenterY;

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
        // --- Input (toggles) ---
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (Input.GetKeyDown(KeyCode.C)) runToggled = !runToggled;             // caminar <-> correr (toggle)
        if (Input.GetKeyDown(KeyCode.LeftControl)) crouchToggled = !crouchToggled; // agacharse (toggle)
        bool sprintHeld = Input.GetKey(KeyCode.LeftShift);                     // esprint (mantener)
        if (Input.GetKeyDown(KeyCode.Alpha1)) CurrentStance = Stance.Fists;   // 1 puños
        if (Input.GetKeyDown(KeyCode.Alpha2)) CurrentStance = Stance.Melee;   // 2 melé
        if (Input.GetKeyDown(KeyCode.Alpha3)) CurrentStance = Stance.Gun;     // 3 arma

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;
        bool moving = inputDir.sqrMagnitude > 0.01f;

        // Apuntar/strafear: clic derecho mantenido (estilo State of Decay).
        Aiming = Input.GetMouseButton(1) && cameraTransform != null;
        bool crouched = crouchToggled && !Aiming; // al apuntar se está de pie

        // --- Estado de movimiento (prioridad: apuntar > agachado > esprint > correr > caminar) ---
        if (Aiming) CurrentState = moving ? MoveState.Walk : MoveState.Idle;
        else if (crouched) CurrentState = MoveState.Crouch;
        else if (!moving) CurrentState = MoveState.Idle;
        else if (sprintHeld) CurrentState = MoveState.Sprint;
        else if (runToggled) CurrentState = MoveState.Run;
        else CurrentState = MoveState.Walk;

        // Altura del collider al agacharse.
        float targetHeight = crouched ? normalHeight * 0.55f : normalHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, 10f * Time.deltaTime);
        Vector3 center = controller.center;
        center.y = normalCenterY - (normalHeight - controller.height) * 0.5f;
        controller.center = center;

        // --- Movimiento relativo a cámara ---
        Vector3 move = Vector3.zero;
        float aimX = 0f, aimY = 0f; // strafe (X) y avance/retroceso (Y) al apuntar
        if (cameraTransform != null)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

            if (Aiming)
            {
                // El cuerpo mira SIEMPRE hacia donde apunta la cámara; el movimiento
                // es lateral/atrás respecto a ese eje (strafe). WASD = ejes del cuerpo.
                Quaternion look = Quaternion.LookRotation(camForward, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look,
                    rotationSmoothness * Time.deltaTime);
                if (moving) move = (camForward * v + camRight * h).normalized * aimSpeed;
                aimX = h; aimY = v;
            }
            else if (moving)
            {
                Vector3 worldDir = (camForward * v + camRight * h).normalized;
                float speed = CurrentState switch
                {
                    MoveState.Crouch => crouchSpeed,
                    MoveState.Sprint => sprintSpeed,
                    MoveState.Run => runSpeed,
                    _ => walkSpeed
                };
                move = worldDir * speed;

                // Rotar el cuerpo hacia la dirección de movimiento
                Quaternion targetRot = Quaternion.LookRotation(worldDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                    rotationSmoothness * Time.deltaTime);
            }
        }

        // --- Gravedad ---
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);

        // Velocidad horizontal real (para Animator y pasos)
        PlanarSpeed = new Vector3(move.x, 0f, move.z).magnitude;

        // Señal de giro: velocidad angular en yaw, normalizada a -1 (izq) .. +1 (der)
        float yaw = transform.eulerAngles.y;
        float yawRate = Mathf.DeltaAngle(prevYaw, yaw) / Mathf.Max(Time.deltaTime, 1e-4f);
        prevYaw = yaw;
        TurnSignal = Mathf.Clamp(yawRate / turnRateForFullBlend, -1f, 1f);

        // --- Animación (opcional): alimenta el Animator si hay un modelo asignado ---
        if (animator != null)
        {
            // Amortiguado: el movimiento arranca instantáneo, pero la mezcla de
            // animación sube suave idle->walk->run ("arranca y luego corre").
            animator.SetFloat("Speed", PlanarSpeed, 0.12f, Time.deltaTime);
            animator.SetFloat("Turn", TurnSignal, 0.1f, Time.deltaTime);
            animator.SetBool("Crouch", CurrentState == MoveState.Crouch);
            // Apuntar/strafe (blend 2D direccional)
            animator.SetBool("Aiming", Aiming);
            animator.SetInteger("AimStance", (int)CurrentStance); // 0 puños, 1 melé, 2 arma
            animator.SetFloat("AimX", aimX, 0.1f, Time.deltaTime);
            animator.SetFloat("AimY", aimY, 0.1f, Time.deltaTime);
        }
    }
}
