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

    [Header("Física")]
    public float gravity = -20f;
    public float rotationSmoothness = 12f;

    [Header("Referencias")]
    public Transform cameraTransform; // asignar la Main Camera

    public enum MoveState { Idle, Crouch, Walk, Run }
    public MoveState CurrentState { get; private set; } = MoveState.Idle;

    CharacterController controller;
    float verticalVelocity;
    float normalHeight;
    float normalCenterY;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        normalHeight = controller.height;
        normalCenterY = controller.center.y;
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // --- Input ---
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool wantsRun = Input.GetKey(KeyCode.LeftShift);
        bool wantsCrouch = Input.GetKey(KeyCode.LeftControl);

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;
        bool moving = inputDir.sqrMagnitude > 0.01f;

        // --- Estado de movimiento (PlayerSignature lo lee para el ruido) ---
        if (!moving) CurrentState = wantsCrouch ? MoveState.Crouch : MoveState.Idle;
        else if (wantsCrouch) CurrentState = MoveState.Crouch;
        else if (wantsRun) CurrentState = MoveState.Run;
        else CurrentState = MoveState.Walk;

        // Altura del collider al agacharse.
        // El centro baja la mitad de lo que baja la altura para que los pies
        // sigan en el suelo (si no, la cápsula encoge flotando alrededor del centro).
        float targetHeight = wantsCrouch ? normalHeight * 0.55f : normalHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, 10f * Time.deltaTime);
        Vector3 center = controller.center;
        center.y = normalCenterY - (normalHeight - controller.height) * 0.5f;
        controller.center = center;

        // --- Movimiento relativo a cámara ---
        Vector3 move = Vector3.zero;
        if (moving && cameraTransform != null)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            Vector3 worldDir = (camForward * v + camRight * h).normalized;

            float speed = CurrentState switch
            {
                MoveState.Crouch => crouchSpeed,
                MoveState.Run => runSpeed,
                _ => walkSpeed
            };
            move = worldDir * speed;

            // Rotar el cuerpo hacia la dirección de movimiento
            Quaternion targetRot = Quaternion.LookRotation(worldDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                rotationSmoothness * Time.deltaTime);
        }

        // --- Gravedad ---
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }
}
