using UnityEngine;

/// <summary>
/// NECROSIS://PROTOCOLO — Cámara sobre el hombro (estilo RE2/Dead Space).
/// Órbita con mouse, colisión contra paredes, y acercamiento automático
/// de noche (GDD v0.4: la cámara "respira" con el peligro).
///
/// Setup: colocar este script en la Main Camera. Asignar 'target' al jugador
/// (idealmente un hijo vacío "CameraPivot" a la altura del hombro, y ~1.6m).
/// </summary>
public class ShoulderCamera : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;                 // pivote sobre el hombro del jugador
    public Vector3 shoulderOffset = new Vector3(0.55f, 0f, 0f); // desplazamiento lateral

    [Header("Órbita")]
    public float sensitivity = 2.2f;
    public float minPitch = -35f;
    public float maxPitch = 65f;

    [Header("Distancia")]
    public float dayDistance = 3.2f;
    public float nightDistance = 2.1f;       // más cerca de noche = más tensión
    public float distanceLerpSpeed = 2f;
    public float collisionRadius = 0.25f;
    public LayerMask collisionMask = ~0;     // por defecto, todo colisiona

    float yaw;
    float pitch = 12f;
    float currentDistance;

    void Start()
    {
        currentDistance = dayDistance;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // --- Input de órbita ---
        yaw += Input.GetAxis("Mouse X") * sensitivity;
        pitch -= Input.GetAxis("Mouse Y") * sensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // --- Distancia objetivo según fase del día ---
        float targetDistance = dayDistance;
        if (DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight)
            targetDistance = nightDistance;
        currentDistance = Mathf.Lerp(currentDistance, targetDistance,
            distanceLerpSpeed * Time.deltaTime);

        // --- Posición deseada ---
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + rotation * shoulderOffset;
        Vector3 desiredPos = pivot - rotation * Vector3.forward * currentDistance;

        // --- Colisión: no atravesar paredes ---
        // Se ignoran los colliders del propio jugador (con collisionMask = ~0 el
        // SphereCast puede pescar su cápsula y pegar la cámara a su espalda).
        Vector3 dir = desiredPos - pivot;
        float dist = dir.magnitude;
        float closest = dist;
        foreach (var hit in Physics.SphereCastAll(pivot, collisionRadius, dir.normalized,
            dist, collisionMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.root == target.root) continue;
            if (hit.distance < closest) closest = hit.distance;
        }
        if (closest < dist)
            desiredPos = pivot + dir.normalized * (closest - 0.05f);

        transform.position = desiredPos;
        transform.rotation = rotation;
    }
}
