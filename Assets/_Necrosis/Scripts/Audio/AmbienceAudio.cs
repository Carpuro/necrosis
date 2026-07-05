using UnityEngine;

/// <summary>
/// NECROSIS://PROTOCOLO — Ambiente sonoro global (2D).
/// Una cama atmosférica en loop + gritos lejanos aleatorios que suben de noche.
/// No es diegético/posicional: es la "sensación" de la ciudad muerta.
/// (El Coro y las voces 3D de los Cazadores van aparte.)
///
/// Setup: GameObject vacío con este script; el builder le pasa los clips.
/// </summary>
public class AmbienceAudio : MonoBehaviour
{
    [Header("Cama ambiente (loop)")]
    public AudioClip ambientLoop;
    [Range(0f, 1f)] public float ambientVolume = 0.35f;

    [Header("Gritos lejanos (al azar)")]
    public AudioClip[] distantScreams;
    public Vector2 screamInterval = new Vector2(12f, 30f);
    [Range(0f, 1f)] public float screamVolume = 0.5f;
    [Tooltip("De noche los gritos lejanos suben (más tensión).")]
    public float nightScreamBoost = 1.4f;

    AudioSource bed;
    AudioSource oneShot;
    float nextScream;

    void Awake()
    {
        // Cama 2D en loop
        bed = gameObject.AddComponent<AudioSource>();
        bed.clip = ambientLoop;
        bed.loop = true;
        bed.spatialBlend = 0f;
        bed.volume = ambientVolume;
        bed.playOnAwake = false;
        if (ambientLoop != null) bed.Play();

        // Fuente 2D aparte para los gritos puntuales
        oneShot = gameObject.AddComponent<AudioSource>();
        oneShot.spatialBlend = 0f;
        oneShot.playOnAwake = false;

        ScheduleNextScream();
    }

    void Update()
    {
        if (Time.time < nextScream) return;
        ScheduleNextScream();

        if (distantScreams == null || distantScreams.Length == 0) return;
        AudioClip clip = distantScreams[Random.Range(0, distantScreams.Length)];
        if (clip == null) return;

        float vol = screamVolume;
        if (DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight)
            vol *= nightScreamBoost;
        oneShot.PlayOneShot(clip, Mathf.Clamp01(vol));
    }

    void ScheduleNextScream() =>
        nextScream = Time.time + Random.Range(screamInterval.x, screamInterval.y);
}
