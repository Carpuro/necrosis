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

    [Header("Aceleración (rampa idle->caminar->correr)")]
    [Tooltip("Cuánto sube la velocidad por segundo: pasa por caminar antes de correr.")]
    public float acceleration = 10f;
    public float deceleration = 16f;
    float currentSpeed; // velocidad actual suavizada (para la rampa natural)

    [Header("Giro 180 (invertir el sentido)")]
    [Tooltip("Ángulo (grados) contra tu rumbo para disparar el giro 180.")]
    public float turn180Threshold = 135f;
    public float turn180Duration = 0.45f;
    bool turning180;
    float turn180Timer;
    Quaternion turn180From, turn180To;
    bool turn180Queued;

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
    MoveState prevState = MoveState.Idle; // para detectar el arranque idle->caminar

    [Header("Arranque al caminar (estilo ARC)")]
    [Tooltip("Tiempo SIN avanzar al arrancar: hasta que el primer pie apoya (clip a 2x). " +
             "Luego empieza a caminar. Ajustar para que coincida con el apoyo del pie.")]
    public float walkStartDuration = 0.18f;
    float walkStartTimer;
    bool startWalkQueued;

    /// <summary>Velocidad horizontal real (m/s). La leen Animator y pasos.</summary>
    public float PlanarSpeed { get; private set; }

    /// <summary>Giro normalizado: -1 (izquierda) .. +1 (derecha). Lo lee el Animator.</summary>
    public float TurnSignal { get; private set; }

    /// <summary>True mientras se apunta/strafea (clic derecho mantenido).</summary>
    public bool Aiming { get; private set; }

    /// <summary>True mientras se strafea libre (sin apuntar).</summary>
    public bool StrafeLock { get; private set; }

    /// <summary>Ejes de strafe que se mandan al Animator (debug).</summary>
    public float AimX { get; private set; }
    public float AimY { get; private set; }

    [Header("Esquiva (rodar)")]
    public float rollSpeed = 7f;
    public float rollDuration = 0.7f;
    bool rolling;
    float rollTimer;
    Vector3 rollDir;

    // Postura de combate seleccionada (1 puños, 2 melé, 3 arma). Decide qué set
    // de animaciones de strafe usa el modo apuntar.
    public enum Stance { Fists, Melee, Gun }
    public Stance CurrentStance { get; private set; } = Stance.Fists;

    [Header("Giro")]
    [Tooltip("Velocidad angular (grados/s) que corresponde al giro completo de la animación. " +
             "Más bajo = los giros se notan antes.")]
    public float turnRateForFullBlend = 90f;
    float prevYaw;

    [Header("Giro en el sitio (parado)")]
    [Tooltip("Grados de diferencia con la cámara para disparar el giro en el sitio.")]
    public float turnInPlaceThreshold = 50f;
    [Tooltip("Velocidad de rotación (grados/s) al girar en el sitio.")]
    public float turnInPlaceSpeed = 240f;
    bool turningInPlace;

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

        // --- Esquiva (rodar): Espacio. Rueda en la dirección de input (o de frente).
        //     Es un override: durante la rodada el movimiento y la anim los manda esto. ---
        if (rolling) { UpdateRoll(); return; }
        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
        {
            rolling = true; rollTimer = 0f;
            Vector3 wish = Vector3.zero;
            if (cameraTransform != null)
            {
                Vector3 cF = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                Vector3 cR = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
                wish = (cF * v + cR * h).normalized;
            }
            rollDir = wish.sqrMagnitude > 0.01f
                ? wish
                : Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            transform.rotation = Quaternion.LookRotation(rollDir, Vector3.up);
            if (animator != null) animator.SetTrigger("Roll");
            UpdateRoll();
            return;
        }

        // Strafe SÓLO al apuntar (clic derecho, estilo State of Decay). Funciona con
        // cualquier postura (puños/melé/arma). El movimiento normal gira a mirar
        // hacia donde te mueves (no strafe), en cualquier postura.
        Aiming = Input.GetMouseButton(1) && cameraTransform != null;
        StrafeLock = false; // sin auto-strafe: caminar normal no strafea
        bool faceCamera = Aiming;
        bool crouched = crouchToggled && !Aiming; // al apuntar se está de pie

        // --- Estado de movimiento (prioridad: apuntar/strafe > agachado > esprint > correr > caminar) ---
        if (faceCamera) CurrentState = moving ? MoveState.Walk : MoveState.Idle;
        else if (crouched) CurrentState = MoveState.Crouch;
        else if (!moving) CurrentState = MoveState.Idle;
        else if (sprintHeld) CurrentState = MoveState.Sprint;
        else if (runToggled) CurrentState = MoveState.Run;
        else CurrentState = MoveState.Walk;

        // --- Arranque (estilo ARC): al pasar de parado a moverse de frente
        //     (caminar/correr/esprint), planta el paso de arranque y NO avanza
        //     mientras dura; luego la velocidad sube en rampa idle->caminar->correr. ---
        bool groundMove = CurrentState == MoveState.Walk || CurrentState == MoveState.Run ||
                          CurrentState == MoveState.Sprint;
        if (prevState == MoveState.Idle && groundMove && !faceCamera && !crouched)
        {
            walkStartTimer = walkStartDuration;
            startWalkQueued = true; // dispara el trigger del Animator abajo
        }
        bool startingWalk = walkStartTimer > 0f;
        if (startingWalk) walkStartTimer -= Time.deltaTime;
        // Cancelar si dejas de moverte o pasas a apuntar/agacharte
        if (!moving || faceCamera || crouched) { walkStartTimer = 0f; startingWalk = false; }

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

            if (faceCamera)
            {
                // Apuntar o strafe libre: el cuerpo mira SIEMPRE hacia la cámara.
                Quaternion look = Quaternion.LookRotation(camForward, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look,
                    rotationSmoothness * Time.deltaTime);
                // Apuntar usa aimSpeed (más lento); strafe libre desarmado usa walk.
                float faceSpeed = Aiming ? aimSpeed : walkSpeed;
                if (moving)
                {
                    Vector3 wish = (camForward * v + camRight * h).normalized;
                    move = wish * faceSpeed;
                    // Ejes de strafe RELATIVOS AL CUERPO (no input crudo): así la
                    // animación coincide con el movimiento aunque el cuerpo gire.
                    Vector3 local = transform.InverseTransformDirection(wish);
                    aimX = local.x; aimY = local.z;
                }
            }
            else if (moving)
            {
                Vector3 worldDir = (camForward * v + camRight * h).normalized;

                // Giro 180: si pides una dirección casi opuesta a tu rumbo actual,
                // dispara el giro con animación y rota el cuerpo durante el clip
                // (no de golpe). Sin filtro de velocidad: se mide facing vs input.
                if (!turning180 && !startingWalk &&
                    Vector3.Angle(transform.forward, worldDir) > turn180Threshold)
                {
                    turning180 = true; turn180Timer = 0f; turn180Queued = true;
                    startWalkQueued = false; // el 180 tiene prioridad sobre el arranque
                    turn180From = transform.rotation;
                    turn180To = Quaternion.LookRotation(worldDir, Vector3.up);
                }

                // Velocidad objetivo por estado; durante el arranque es 0 (no avanza).
                float targetSpeed = startingWalk ? 0f : CurrentState switch
                {
                    MoveState.Crouch => crouchSpeed,
                    MoveState.Sprint => sprintSpeed,
                    MoveState.Run => runSpeed,
                    _ => walkSpeed
                };
                float rate = targetSpeed > currentSpeed ? acceleration : deceleration;
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);
                move = worldDir * currentSpeed;

                if (turning180)
                {
                    // Durante el giro 180 rota el cuerpo por el clip y NO avanza.
                    turn180Timer += Time.deltaTime;
                    float t180 = Mathf.Clamp01(turn180Timer / turn180Duration);
                    transform.rotation = Quaternion.Slerp(turn180From, turn180To, t180);
                    move.x = 0f; move.z = 0f;
                    if (t180 >= 1f) turning180 = false;
                }
                else
                {
                    // Rotar el cuerpo hacia la dirección de movimiento
                    Quaternion targetRot = Quaternion.LookRotation(worldDir, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                        rotationSmoothness * Time.deltaTime);
                }
            }
            else { currentSpeed = 0f; turning180 = false; } // parado: reinicia
        }

        // Arranque al caminar: no avanzar mientras se planta el paso (sin shuffle).
        if (startingWalk) { move.x = 0f; move.z = 0f; }

        // --- Gravedad ---
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);

        // Velocidad horizontal real (para Animator y pasos)
        // Durante el giro 180 no avanzamos, pero mandamos currentSpeed para que el
        // blend elija bien caminar/correr 180.
        PlanarSpeed = turning180 ? currentSpeed : new Vector3(move.x, 0f, move.z).magnitude;

        // Señal de giro: velocidad angular en yaw, normalizada a -1 (izq) .. +1 (der)
        float yaw = transform.eulerAngles.y;
        float yawRate = Mathf.DeltaAngle(prevYaw, yaw) / Mathf.Max(Time.deltaTime, 1e-4f);
        prevYaw = yaw;
        TurnSignal = Mathf.Clamp(yawRate / turnRateForFullBlend, -1f, 1f);

        // --- Giro en el sitio (parado): al mirar lejos de tu eje, gira el cuerpo
        //     hacia la cámara con animación visible, estilo tercera persona. ---
        float turnInPlaceDir = 0f;
        if (!Aiming && CurrentState == MoveState.Idle && cameraTransform != null)
        {
            Vector3 camF = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
            if (camF.sqrMagnitude > 0.001f)
            {
                float camYaw = Quaternion.LookRotation(camF).eulerAngles.y;
                float diff = Mathf.DeltaAngle(transform.eulerAngles.y, camYaw);
                if (Mathf.Abs(diff) > turnInPlaceThreshold) turningInPlace = true;
                if (turningInPlace)
                {
                    float step = Mathf.Clamp(diff, -turnInPlaceSpeed * Time.deltaTime,
                                                    turnInPlaceSpeed * Time.deltaTime);
                    transform.Rotate(0f, step, 0f);
                    turnInPlaceDir = Mathf.Sign(diff);
                    if (Mathf.Abs(diff) < 5f) turningInPlace = false; // ya alineado
                }
            }
        }
        else turningInPlace = false;

        // --- Animación (opcional): alimenta el Animator si hay un modelo asignado ---
        if (animator != null)
        {
            // Amortiguado: el movimiento arranca instantáneo, pero la mezcla de
            // animación sube suave idle->walk->run ("arranca y luego corre").
            animator.SetFloat("Speed", PlanarSpeed, 0.12f, Time.deltaTime);
            animator.SetFloat("Turn", TurnSignal, 0.1f, Time.deltaTime);
            animator.SetBool("Crouch", CurrentState == MoveState.Crouch);
            // Apuntar / strafe libre (blend 2D direccional; comparten AimX/AimY)
            animator.SetBool("Aiming", Aiming);
            animator.SetBool("StrafeLock", StrafeLock);
            animator.SetBool("TurningInPlace", turningInPlace);
            animator.SetFloat("TurnInPlace", turnInPlaceDir, 0.08f, Time.deltaTime);
            animator.SetInteger("AimStance", (int)CurrentStance); // 0 puños, 1 melé, 2 arma
            animator.SetFloat("AimX", aimX, 0.1f, Time.deltaTime);
            animator.SetFloat("AimY", aimY, 0.1f, Time.deltaTime);
            AimX = aimX; AimY = aimY; // exponer para debug
            // Arranque al caminar (se detectó arriba)
            if (startWalkQueued) animator.SetTrigger("StartWalk");
            if (turn180Queued) animator.SetTrigger("Turn180");
        }
        startWalkQueued = false;
        turn180Queued = false;
        prevState = CurrentState;
    }

    // Rodada (esquiva): mueve al jugador en rollDir a rollSpeed durante rollDuration.
    void UpdateRoll()
    {
        rollTimer += Time.deltaTime;
        Vector3 move = rollDir * rollSpeed;
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);
        PlanarSpeed = rollSpeed;
        if (rollTimer >= rollDuration) rolling = false;
    }
}
