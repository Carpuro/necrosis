using UnityEngine;

/// <summary>
/// NECROSIS://PROTOCOLO — HUD de debug de fase 0.
/// Muestra hora, fase, firma del jugador y estado del Cazador más cercano.
/// Quitar (o desactivar) para "sentir" el juego sin números — el objetivo
/// real de la fase 0 es que dé miedo SIN esta información.
///
/// Setup: en cualquier GameObject de la escena (p. ej. el jugador).
/// Tecla H para mostrar/ocultar.
/// </summary>
public class DebugHUD : MonoBehaviour
{
    bool visible = true;
    Transform player;
    PlayerSignature signature;
    PlayerHealth health;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            signature = p.GetComponent<PlayerSignature>();
            health = p.GetComponent<PlayerHealth>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H)) visible = !visible;
    }

    void OnGUI()
    {
        if (!visible) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        style.normal.textColor = Color.white;

        GUILayout.BeginArea(new Rect(12, 12, 420, 260), GUI.skin.box);

        var cycle = DayNightCycle.Instance;
        if (cycle != null)
        {
            GUILayout.Label($"⏱ {cycle.ClockString()}  |  Fase: {cycle.CurrentPhase}" +
                            (cycle.IsDawnSurge ? "  ⚠ MAREA DEL ALBA" : ""), style);
            GUILayout.Label($"☀ Factor solar: {cycle.SolarFactor:0.00}", style);
        }

        if (signature != null)
        {
            GUILayout.Label($"👣 Ruido: {signature.NoiseRadius:0.0}m   " +
                            $"⚡ Firma: {signature.EnergyRadius:0.0}m   " +
                            $"🔦 {(signature.FlashlightOn ? "ENCENDIDA" : "apagada")}", style);
            GUILayout.Label($"👁 Visibilidad: {signature.VisibilityScale:0.00}×  " +
                            "(agáchate para bajarla)", style);
        }

        if (health != null)
            GUILayout.Label($"❤ Salud: {health.CurrentHealth:0}", style);

        // Cazador más cercano
        if (player != null)
        {
            HunterAI nearest = null;
            float best = float.MaxValue;
            foreach (var h in FindObjectsByType<HunterAI>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(player.position, h.transform.position);
                if (d < best) { best = d; nearest = h; }
            }
            if (nearest != null)
            {
                string mem = nearest.RemembersPlayer
                    ? $"· te recuerda ({nearest.MemoryAge:0.0}s)"
                    : "· sin rastro";
                GUILayout.Label($"🧟 Cazador más cercano: {nearest.CurrentState} a {best:0.0}m  {mem}", style);
            }
        }

        GUILayout.Label("WASD · C caminar/correr · Shift esprint · Ctrl agacharse · Clic der. apuntar · 1/2/3 puños/melé/arma · F linterna · H", style);
        GUILayout.EndArea();
    }
}
