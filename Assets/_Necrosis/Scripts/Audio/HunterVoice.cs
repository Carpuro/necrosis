using UnityEngine;

/// <summary>
/// NECROSIS://PROTOCOLO — Voz del Cazador (audio 3D posicional).
/// Emite gruñidos/gemidos/chillidos según el estado de HunterAI, para que puedas
/// LOCALIZAR a los Cazadores por el oído (estilo Project Zomboid) y para dar
/// carácter al ambiente. De noche, en estatua, CALLA: el silencio es la amenaza.
///
/// Setup: mismo GameObject que HunterAI (el builder lo cablea con los clips).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class HunterVoice : MonoBehaviour
{
    [Header("Voces por estado (se elige una al azar)")]
    public AudioClip[] patrolVoices;   // gemido, gruñido, quieto (rutina)
    public AudioClip[] huntVoices;     // chillido / gruñido agresivo (te caza)
    public AudioClip frenzyVoice;      // descarga nocturna
    public AudioClip attackClip;       // mordida (sincronizada al daño)
    public AudioClip nearClip;         // aviso de proximidad

    [Header("Intervalos entre voces (s)")]
    public Vector2 patrolInterval = new Vector2(4f, 9f);
    public Vector2 huntInterval = new Vector2(1.5f, 3.5f);
    public float frenzyInterval = 1.2f;

    [Header("Proximidad")]
    public float nearDistance = 6f;
    public float nearCooldown = 6f;

    [Header("Audio 3D")]
    public float minDistance = 2f;
    public float maxDistance = 35f;
    [Range(0f, 1f)] public float volume = 0.9f;

    AudioSource src;
    HunterAI ai;
    Transform player;
    float nextVoice;
    float nextNear;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.spatialBlend = 1f;                 // totalmente 3D
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.playOnAwake = false;
        src.loop = false;

        ai = GetComponent<HunterAI>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void OnEnable()  { if (ai != null) ai.OnAttackLanded += PlayAttack; }
    void OnDisable() { if (ai != null) ai.OnAttackLanded -= PlayAttack; }

    void Update()
    {
        if (ai == null) return;
        HandleStateVoices();
        HandleProximity();
    }

    void HandleStateVoices()
    {
        if (Time.time < nextVoice) return;

        switch (ai.CurrentState)
        {
            case HunterAI.State.Patrol:
            case HunterAI.State.Investigate:
                PlayRandom(patrolVoices);
                nextVoice = Time.time + Random.Range(patrolInterval.x, patrolInterval.y);
                break;

            case HunterAI.State.Flank:
            case HunterAI.State.Attack:
                PlayRandom(huntVoices);
                nextVoice = Time.time + Random.Range(huntInterval.x, huntInterval.y);
                break;

            case HunterAI.State.Frenzy:
                if (frenzyVoice != null) src.PlayOneShot(frenzyVoice, volume);
                nextVoice = Time.time + frenzyInterval;
                break;

            default: // Statue / Exhausted: silencio (la noche calla)
                nextVoice = Time.time + 1f;
                break;
        }
    }

    void HandleProximity()
    {
        if (player == null || nearClip == null || Time.time < nextNear) return;
        if (Vector3.Distance(transform.position, player.position) < nearDistance)
        {
            src.PlayOneShot(nearClip, volume);
            nextNear = Time.time + nearCooldown;
        }
    }

    void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null) src.PlayOneShot(clip, volume);
    }

    void PlayAttack()
    {
        if (attackClip != null) src.PlayOneShot(attackClip, volume);
    }
}
