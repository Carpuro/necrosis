using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NECROSIS://PROTOCOLO — Control de la misión de fase 0: "carrera de extracción".
/// El jugador aparece en un borde del mapa y debe cruzar vivo hasta la zona de
/// extracción del borde opuesto. Muestra el objetivo, la distancia restante y el
/// resultado (LOGRADA / MUERTO).
///
/// No añade contenido nuevo: solo le da DIRECCIÓN y un criterio de éxito a la
/// escena greybox existente, para que al pulsar Play haya algo que intentar.
/// </summary>
public class MissionController : MonoBehaviour
{
    [Header("Referencias (las cablea el builder)")]
    public Transform player;
    public ExtractionZone extraction;
    public PlayerHealth playerHealth;

    [Header("Textos")]
    public string objectiveLabel = "OBJETIVO: llega a la extracción";
    public string successLabel = "EXTRACCIÓN LOGRADA";
    public string failLabel = "MUERTO";
    public string retryHint = "R para reintentar";

    [Header("Presentación")]
    public int bannerFontSize = 20;
    public int resultFontSize = 46;

    enum State { InProgress, Success, Failed }
    State state = State.InProgress;

    void Update()
    {
        switch (state)
        {
            case State.InProgress:
                if (extraction != null && extraction.Reached) state = State.Success;
                else if (playerHealth != null && playerHealth.IsDead) state = State.Failed;
                break;

            // Tras morir, PlayerHealth recarga la escena solo (3 s); en el éxito
            // esperamos a que el jugador pulse R. En ambos casos R reinicia ya.
            case State.Success:
            case State.Failed:
                if (Input.GetKeyDown(KeyCode.R)) Restart();
                break;
        }
    }

    void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    float RemainingMeters()
    {
        if (player == null || extraction == null) return 0f;
        Vector3 a = player.position; a.y = 0f;
        Vector3 b = extraction.transform.position; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void OnGUI()
    {
        float w = Screen.width;

        var banner = new GUIStyle(GUI.skin.label)
        {
            fontSize = bannerFontSize,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        banner.normal.textColor = Color.white;

        if (state == State.InProgress)
        {
            // Banner de objetivo arriba al centro, siempre visible durante la ronda.
            string txt = $"{objectiveLabel}  —  {RemainingMeters():0}m";
            GUI.Label(new Rect(w * 0.5f - 300f, 16f, 600f, 40f), txt, banner);
            return;
        }

        // Resultado a pantalla completa.
        var result = new GUIStyle(banner) { fontSize = resultFontSize };
        result.normal.textColor = state == State.Success
            ? new Color(0.4f, 1f, 0.5f)
            : new Color(1f, 0.3f, 0.3f);
        string big = state == State.Success ? successLabel : failLabel;
        GUI.Label(new Rect(0f, Screen.height * 0.5f - 60f, w, 80f), big, result);

        GUI.Label(new Rect(0f, Screen.height * 0.5f + 20f, w, 40f), retryHint, banner);
    }
}
