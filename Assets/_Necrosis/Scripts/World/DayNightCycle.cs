using System;
using UnityEngine;

/// <summary>
/// NECROSIS://PROTOCOLO — Ciclo día/noche (GDD v0.5: El Ciclo Invertido)
/// Singleton que gobierna el tiempo, el sol y el "factor solar" que
/// alimenta a las nanomáquinas (la IA de los infectados lo consulta).
///
/// Setup: GameObject vacío "DayNightCycle" + este script.
/// Asignar la Directional Light de la escena en 'sunLight'.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    public enum Phase { Dawn, Day, Dusk, Night }

    [Header("Configuración de tiempo")]
    [Tooltip("Duración de un día completo de juego, en minutos reales")]
    public float dayLengthMinutes = 20f;
    [Range(0f, 24f)] public float timeOfDay = 9f; // arrancamos a las 9:00

    [Header("Horas de fase")]
    public float dawnStart = 6f;   // 6:00  — MAREA DEL ALBA
    public float dayStart = 7f;    // 7:00
    public float duskStart = 19f;  // 19:00 — los infectados se congelan
    public float nightStart = 20f; // 20:00

    [Header("Referencias")]
    public Light sunLight;
    public Gradient sunColor;            // opcional: color del sol por hora
    public AnimationCurve sunIntensity;  // opcional: intensidad 0-1 por hora normalizada

    /// <summary>Fase actual del ciclo.</summary>
    public Phase CurrentPhase { get; private set; } = Phase.Day;

    /// <summary>
    /// Energía solar disponible para las nanomáquinas [0..1].
    /// 1 = mediodía pleno. 0 = noche cerrada.
    /// La IA de infectados escala velocidad/percepción con esto.
    /// </summary>
    public float SolarFactor { get; private set; }

    /// <summary>True durante la Marea del Alba (primera hora tras despertar).</summary>
    public bool IsDawnSurge => CurrentPhase == Phase.Dawn;

    public bool IsNight => CurrentPhase == Phase.Night;

    /// <summary>Se dispara cada vez que cambia la fase (Dawn/Day/Dusk/Night).</summary>
    public event Action<Phase> OnPhaseChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        // Avance del reloj
        float hoursPerSecond = 24f / (dayLengthMinutes * 60f);
        timeOfDay += hoursPerSecond * Time.deltaTime;
        if (timeOfDay >= 24f) timeOfDay -= 24f;

        UpdatePhase();
        UpdateSun();
        UpdateSolarFactor();
    }

    void UpdatePhase()
    {
        Phase newPhase;
        if (timeOfDay >= dawnStart && timeOfDay < dayStart) newPhase = Phase.Dawn;
        else if (timeOfDay >= dayStart && timeOfDay < duskStart) newPhase = Phase.Day;
        else if (timeOfDay >= duskStart && timeOfDay < nightStart) newPhase = Phase.Dusk;
        else newPhase = Phase.Night;

        if (newPhase != CurrentPhase)
        {
            CurrentPhase = newPhase;
            OnPhaseChanged?.Invoke(CurrentPhase);
        }
    }

    void UpdateSun()
    {
        if (sunLight == null) return;

        // Rotación del sol: -90° a las 0:00 (bajo el horizonte), 90° al mediodía
        float sunAngle = (timeOfDay / 24f) * 360f - 90f;
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        float t = timeOfDay / 24f;
        if (sunColor != null) sunLight.color = sunColor.Evaluate(t);
        if (sunIntensity != null && sunIntensity.length > 0)
            sunLight.intensity = Mathf.Max(0f, sunIntensity.Evaluate(t)) * 1.3f;
        else
            sunLight.intensity = Mathf.Clamp01(Mathf.Sin((timeOfDay - 6f) / 12f * Mathf.PI)) * 1.3f;
    }

    void UpdateSolarFactor()
    {
        // Curva solar simple: 0 antes de las 6 y después de las 20, pico a las 13
        float f = Mathf.Sin((timeOfDay - 6f) / 14f * Mathf.PI);
        SolarFactor = Mathf.Clamp01(f);
    }

    /// <summary>Hora legible para debug/HUD.</summary>
    public string ClockString()
    {
        int h = Mathf.FloorToInt(timeOfDay);
        int m = Mathf.FloorToInt((timeOfDay - h) * 60f);
        return $"{h:00}:{m:00}";
    }
}
