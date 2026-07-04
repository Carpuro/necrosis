using UnityEngine;

/// <summary>
/// NECROSIS://PROTOCOLO — El Coro (GDD v0.6 §1: lenguaje aprendible).
/// Convierte el estado de los Cazadores cercanos en estática audible.
/// Fase 0 (versión mínima):
///   - Patrulla cerca  -> pulsos lentos y suaves
///   - Flanqueo/Ataque -> ráfaga ascendente (volumen y pitch suben)
///   - Noche (estatuas) -> SILENCIO TOTAL (tu radar muere)
///   - Frenesí nocturno -> chillido de descarga
///
/// Setup: en el jugador, AudioSource (loop ON, playOnAwake ON) con cualquier
/// clip de ruido blanco/estática + este script.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ChorusAudio : MonoBehaviour
{
    [Header("Alcance de escucha del coro (metros)")]
    public float listenRadius = 30f;

    [Header("Volúmenes por situación")]
    [Range(0f, 1f)] public float patrolVolume = 0.12f;
    [Range(0f, 1f)] public float huntVolume = 0.55f;
    [Range(0f, 1f)] public float frenzyVolume = 0.85f;

    [Header("Pulso (patrulla)")]
    public float patrolPulseSpeed = 1.2f;

    AudioSource source;
    HunterAI[] hunters;
    float refreshTimer;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;
        if (!source.isPlaying && source.clip != null) source.Play();
    }

    void Update()
    {
        // Refrescar lista de cazadores cada 2s (barato para fase 0)
        refreshTimer -= Time.deltaTime;
        if (hunters == null || refreshTimer <= 0f)
        {
            hunters = FindObjectsByType<HunterAI>(FindObjectsSortMode.None);
            refreshTimer = 2f;
        }

        float targetVolume = 0f;
        float targetPitch = 1f;

        foreach (var h in hunters)
        {
            if (h == null) continue;
            float d = Vector3.Distance(transform.position, h.transform.position);
            if (d > listenRadius) continue;

            float proximity = 1f - (d / listenRadius); // 1 = encima, 0 = al límite

            switch (h.CurrentState)
            {
                case HunterAI.State.Statue:
                case HunterAI.State.Exhausted:
                    // Silencio: las estatuas no coordinan. Tu radar está muerto.
                    break;

                case HunterAI.State.Patrol:
                case HunterAI.State.Investigate:
                    // Pulso lento: rutina
                    float pulse = (Mathf.Sin(Time.time * patrolPulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
                    targetVolume = Mathf.Max(targetVolume, patrolVolume * proximity * pulse);
                    targetPitch = Mathf.Max(targetPitch, 0.9f);
                    break;

                case HunterAI.State.Flank:
                case HunterAI.State.Attack:
                    // Ráfaga ascendente: presa detectada (¡tú!)
                    targetVolume = Mathf.Max(targetVolume, huntVolume * proximity);
                    targetPitch = Mathf.Max(targetPitch, 1.25f);
                    break;

                case HunterAI.State.Frenzy:
                    // Descarga: chillido
                    targetVolume = Mathf.Max(targetVolume, frenzyVolume * proximity);
                    targetPitch = Mathf.Max(targetPitch, 1.6f);
                    break;
            }
        }

        // Suavizado para que el coro "respire"
        source.volume = Mathf.Lerp(source.volume, targetVolume, 4f * Time.deltaTime);
        source.pitch = Mathf.Lerp(source.pitch, targetPitch, 3f * Time.deltaTime);
    }
}
