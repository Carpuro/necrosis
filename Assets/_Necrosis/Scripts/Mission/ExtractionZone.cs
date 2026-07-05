using UnityEngine;

/// <summary>
/// NECROSIS://PROTOCOLO — Zona de extracción (meta de la fase 0).
/// Un volumen disparador (trigger) al extremo opuesto del mapa: llegar aquí
/// vivo completa la ronda. El <see cref="MissionController"/> lee
/// <see cref="Reached"/> para decidir la victoria.
///
/// Setup: GameObject con un Collider marcado como trigger.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ExtractionZone : MonoBehaviour
{
    /// <summary>True en cuanto el jugador entra en la zona (no se resetea solo).</summary>
    public bool Reached { get; private set; }

    void Reset()
    {
        // Si se añade a mano en el editor, garantizar que el collider sea trigger.
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (Reached) return;
        if (other.CompareTag("Player")) Reached = true;
    }
}
