using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NECROSIS://PROTOCOLO — IA del Cazador (GDD v0.2 §4.5 + v0.5).
///
/// DÍA (con energía solar): Patrol -> Detect -> Flank -> Attack.
///   Flanquea: no corre en línea recta hacia ti, rodea hacia tu costado/espalda.
///   Su velocidad y percepción escalan con DayNightCycle.SolarFactor.
///
/// NOCHE (ahorro de energía): Statue (inmóvil, sensores pasivos).
///   Si detecta movimiento cercano, ruido o LUZ -> Frenzy (descarga: 15s brutales)
///   -> Exhausted (lento) -> Statue de nuevo.
///
/// AMANECER: Marea del Alba — despierta con reserva llena, agresividad máxima.
///
/// Setup: cápsula con NavMeshAgent + este script. Player con tag "Player".
/// Hornear NavMesh en la escena (Window > AI > Navigation).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class HunterAI : MonoBehaviour
{
    public enum State { Patrol, Investigate, Flank, Attack, Statue, Frenzy, Exhausted }

    [Header("Percepción diurna")]
    public float visionRange = 22f;
    public float visionAngle = 110f;
    public LayerMask obstacleMask;            // capas que bloquean la visión (Default)

    [Header("Percepción nocturna (modo estatua)")]
    public float statueProximityTrigger = 3.5f;  // te acercaste demasiado
    public float statueLightTrigger = 1f;         // se compara con EnergyRadius del jugador

    [Header("Movimiento")]
    public float basePatrolSpeed = 1.6f;
    public float baseChaseSpeed = 4.2f;
    public float frenzySpeedMultiplier = 1.8f;
    public float exhaustedSpeedMultiplier = 0.4f;

    [Header("Combate")]
    public float attackRange = 1.9f;
    public float attackCooldown = 1.2f;
    public float attackDamage = 15f;

    [Header("Flanqueo")]
    public float flankDistance = 6f;             // qué tan lejos rodea
    [Range(0f, 1f)] public float flankChance = 0.75f;

    [Header("Frenesí nocturno")]
    public float frenzyDuration = 15f;
    public float exhaustedDuration = 10f;

    [Header("Patrulla")]
    public Transform[] patrolPoints;             // opcional; si está vacío, deambula
    public float wanderRadius = 15f;

    [Header("Animación (opcional)")]
    [Tooltip("Animator del modelo rigged (p. ej. zombi de Mixamo). Si es null, sigue la cápsula.\n" +
             "Parámetros esperados: float 'Speed', bool 'Statue', bool 'Attacking'.")]
    public Animator animator;

    [Header("Memoria (dejar de perseguir)")]
    [Tooltip("Estilo Project Zomboid: al perderte de vista/oído persigue tu ÚLTIMA posición " +
             "conocida; si no te vuelve a percibir en estos segundos, se rinde. No te sigue eterno.")]
    public float memoryDuration = 7f;
    [Tooltip("Cuánto rebusca en el último punto conocido antes de volver a patrullar.")]
    public float investigateLinger = 3f;

    public State CurrentState { get; private set; } = State.Patrol;

    /// <summary>Se dispara al conectar una mordida (para sonido/efectos). Ver HunterVoice.</summary>
    public event System.Action OnAttackLanded;

    // --- Memoria: dónde y cuándo percibió al jugador por última vez ---
    Vector3 lastKnownPosition;
    float lastPerceivedTime = -999f;
    bool hasMemory;

    /// <summary>True mientras el recuerdo del jugador sigue "fresco".</summary>
    public bool RemembersPlayer => hasMemory && (Time.time - lastPerceivedTime) < memoryDuration;
    /// <summary>Segundos desde la última percepción (para el HUD de debug).</summary>
    public float MemoryAge => Time.time - lastPerceivedTime;

    NavMeshAgent agent;
    Transform player;
    PlayerSignature playerSignature;
    PlayerHealth playerHealth;

    int patrolIndex;
    float stateTimer;
    float attackTimer;
    float flankSide; // +1 = derecha del jugador, -1 = izquierda; se elige al entrar en Flank
    bool subscribed;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerSignature = p.GetComponent<PlayerSignature>();
            playerHealth = p.GetComponent<PlayerHealth>();
        }
    }

    void Start()
    {
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.OnPhaseChanged += OnPhaseChanged;
            subscribed = true;
            // Estado inicial coherente con la hora de arranque
            // (en Dusk ya están congelados: el evento Dusk->Statue ocurrió "antes de empezar")
            var phase = DayNightCycle.Instance.CurrentPhase;
            if (phase == DayNightCycle.Phase.Night || phase == DayNightCycle.Phase.Dusk)
                EnterStatue();
        }
    }

    void OnDestroy()
    {
        if (subscribed && DayNightCycle.Instance != null)
            DayNightCycle.Instance.OnPhaseChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(DayNightCycle.Phase phase)
    {
        switch (phase)
        {
            case DayNightCycle.Phase.Dusk:
            case DayNightCycle.Phase.Night:
                // El sol se fue: congelarse donde esté (salvo en pleno frenesí)
                if (CurrentState != State.Frenzy) EnterStatue();
                break;

            case DayNightCycle.Phase.Dawn:
                // MAREA DEL ALBA: despierta con reserva llena y hambre acumulada
                agent.isStopped = false;
                if (player == null) { SetState(State.Patrol); break; }
                if (PlayerVisible() || DistanceToPlayer() < visionRange * 1.5f)
                {
                    // Te huele: va por ti sin haberte visto bien. Sembrar la memoria
                    // para que persiga tu posición actual y no el origen (0,0,0).
                    RememberPlayer();
                    SetState(State.Flank);
                }
                else
                {
                    SetState(State.Patrol);
                }
                break;
        }
    }

    void Update()
    {
        if (player == null || DayNightCycle.Instance == null) return;
        stateTimer += Time.deltaTime;
        attackTimer += Time.deltaTime;

        bool night = DayNightCycle.Instance.IsNight ||
                     DayNightCycle.Instance.CurrentPhase == DayNightCycle.Phase.Dusk;

        // Velocidades escaladas por energía solar (día) o modo (noche)
        float solar = Mathf.Lerp(0.55f, 1f, DayNightCycle.Instance.SolarFactor);
        if (DayNightCycle.Instance.IsDawnSurge) solar = 1.15f; // marea del alba

        switch (CurrentState)
        {
            case State.Patrol:      TickPatrol(solar, night); break;
            case State.Investigate: TickInvestigate(solar); break;
            case State.Flank:       TickFlank(solar); break;
            case State.Attack:      TickAttack(solar); break;
            case State.Statue:      TickStatue(); break;
            case State.Frenzy:      TickFrenzy(); break;
            case State.Exhausted:   TickExhausted(night); break;
        }

        UpdateAnimator();
    }

    // Alimenta el Animator si hay un modelo rigged asignado (null-safe: sin modelo,
    // el Cazador sigue siendo la cápsula de siempre).
    void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat("Speed", agent.velocity.magnitude);
        animator.SetBool("Statue", CurrentState == State.Statue);
        animator.SetBool("Attacking", CurrentState == State.Attack || CurrentState == State.Frenzy);
    }

    // ---------------- ESTADOS DIURNOS ----------------

    void TickPatrol(float solar, bool night)
    {
        agent.speed = basePatrolSpeed * solar;

        if (Perceive())
        {
            // Decide: flanquear (inteligente) o carga directa
            SetState(Random.value < flankChance ? State.Flank : State.Attack);
            return;
        }
        if (DetectedPlayerEnergy())
        {
            RememberPlayer();
            SetState(State.Investigate);
            return;
        }

        // Patrullar puntos o deambular
        // (remainingDistance reporta 0 mientras el path se calcula: esperar pathPending)
        if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance < 0.6f))
        {
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[patrolIndex].position);
            }
            else
            {
                Vector3 random = transform.position + Random.insideUnitSphere * wanderRadius;
                if (NavMesh.SamplePosition(random, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
            }
        }
    }

    void TickInvestigate(float solar)
    {
        agent.speed = baseChaseSpeed * 0.7f * solar;
        agent.SetDestination(lastKnownPosition);

        if (Perceive())
        {
            SetState(Random.value < flankChance ? State.Flank : State.Attack);
            return;
        }
        // Llegó al último punto conocido y rebuscó un rato sin nada: se rinde.
        if (!agent.pathPending && agent.remainingDistance < 1f && stateTimer > investigateLinger)
        {
            hasMemory = false;
            SetState(State.Patrol);
        }
    }

    void TickFlank(float solar)
    {
        agent.speed = baseChaseSpeed * solar;
        bool perceived = Perceive();

        if (perceived)
        {
            // Punto de flanqueo: al costado/espalda del jugador respecto a su mirada.
            // El costado (flankSide) se eligió UNA vez al entrar al estado; re-sortearlo
            // cada frame hacía que el agente zigzagueara sin rodear nunca.
            Vector3 side = player.right * flankSide;
            Vector3 flankTarget = player.position + (side - player.forward).normalized * flankDistance;

            if (NavMesh.SamplePosition(flankTarget, out NavMeshHit hit, flankDistance, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
            else
                agent.SetDestination(player.position);

            // Llegó al costado (o simplemente ya está encima): a matar
            if (DistanceToPlayer() < attackRange * 2.2f ||
                (!agent.pathPending && agent.remainingDistance < 1.2f))
                SetState(State.Attack);
        }
        else if (RemembersPlayer)
        {
            // Te perdió de vista: corre a tu última posición conocida.
            agent.SetDestination(lastKnownPosition);
        }
        else
        {
            SetState(State.Investigate); // memoria fría: rebusca el punto y se rinde
        }
    }

    void TickAttack(float solar)
    {
        agent.speed = baseChaseSpeed * solar;
        bool perceived = Perceive();

        // Mientras te perciba va a por ti; si te pierde, carga tu último punto conocido.
        agent.SetDestination(perceived ? player.position : lastKnownPosition);

        if (perceived && DistanceToPlayer() <= attackRange && attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            if (playerHealth != null) playerHealth.TakeDamage(attackDamage);
            OnAttackLanded?.Invoke();
        }
        if (!perceived && !RemembersPlayer)
            SetState(State.Investigate); // te perdió y la memoria caducó: rebusca
    }

    // ---------------- ESTADOS NOCTURNOS ----------------

    void EnterStatue()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        SetState(State.Statue);
    }

    void TickStatue()
    {
        // Sensores pasivos: proximidad, ruido, o LUZ (firma energética)
        float d = DistanceToPlayer();
        bool proximity = d < statueProximityTrigger;
        bool noise = playerSignature != null && d < playerSignature.NoiseRadius;
        bool light = playerSignature != null &&
                     playerSignature.EnergyRadius > statueLightTrigger &&
                     d < playerSignature.EnergyRadius;

        if (proximity || noise || light)
        {
            // DESPERTAR: frenesí de descarga
            agent.isStopped = false;
            agent.speed = baseChaseSpeed * frenzySpeedMultiplier;
            SetState(State.Frenzy);
        }
    }

    void TickFrenzy()
    {
        agent.SetDestination(player.position);

        float d = DistanceToPlayer();
        if (d <= attackRange && attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            if (playerHealth != null) playerHealth.TakeDamage(attackDamage * 1.5f); // muerde más fuerte
            OnAttackLanded?.Invoke();
        }

        if (stateTimer >= frenzyDuration)
        {
            // Reserva agotada
            agent.speed = baseChaseSpeed * exhaustedSpeedMultiplier;
            SetState(State.Exhausted);
        }
    }

    void TickExhausted(bool night)
    {
        // Arrastra los pies hacia ti un rato; luego, si es de noche, vuelve a estatua
        agent.SetDestination(player.position);

        if (stateTimer >= exhaustedDuration)
        {
            if (night) EnterStatue();
            else SetState(State.Patrol);
        }
    }

    // ---------------- PERCEPCIÓN ----------------

    /// <summary>
    /// ¿Percibe al jugador AHORA (vista, oído o contacto)? Si sí, refresca la
    /// memoria (última posición conocida + marca de tiempo). Los estados de caza
    /// llaman a esto: solo saben dónde estás mientras te perciben; al perderte,
    /// van a tu último punto conocido y acaban rindiéndose (memoryDuration).
    /// </summary>
    bool Perceive()
    {
        bool contact = DistanceToPlayer() < attackRange; // encima de ti: te delata
        if (PlayerVisible() || HeardPlayer() || contact)
        {
            RememberPlayer();
            return true;
        }
        return false;
    }

    /// <summary>Fija la memoria en la posición actual del jugador (rastro fresco).</summary>
    void RememberPlayer()
    {
        lastKnownPosition = player.position;
        lastPerceivedTime = Time.time;
        hasMemory = true;
    }

    bool PlayerVisible()
    {
        // De noche en estatua no se usa la visión activa (sensores pasivos aparte)
        float solar = DayNightCycle.Instance.SolarFactor;
        // Estilo Project Zomboid: tu postura/movimiento modula qué tan lejos te ven.
        float visScale = playerSignature != null ? playerSignature.VisibilityScale : 1f;
        float effectiveRange = visionRange * Mathf.Lerp(0.5f, 1f, solar) * visScale;

        Vector3 toPlayer = player.position - transform.position;
        if (toPlayer.magnitude > effectiveRange) return false;
        if (Vector3.Angle(transform.forward, toPlayer) > visionAngle * 0.5f) return false;

        // Línea de visión: ¿hay obstáculo en medio?
        Vector3 eye = transform.position + Vector3.up * 1.6f;
        Vector3 targetPoint = player.position + Vector3.up * 1.2f;
        if (Physics.Linecast(eye, targetPoint, out RaycastHit hit, obstacleMask))
            return hit.transform == player || hit.transform.IsChildOf(player);
        return true;
    }

    bool HeardPlayer()
    {
        if (playerSignature == null) return false;
        return DistanceToPlayer() < playerSignature.NoiseRadius;
    }

    bool DetectedPlayerEnergy()
    {
        if (playerSignature == null) return false;
        return playerSignature.EnergyRadius > 0.1f &&
               DistanceToPlayer() < playerSignature.EnergyRadius;
    }

    float DistanceToPlayer() => Vector3.Distance(transform.position, player.position);

    void SetState(State s)
    {
        if (s == State.Flank)
            flankSide = Random.value < 0.5f ? 1f : -1f;
        CurrentState = s;
        stateTimer = 0f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, statueProximityTrigger);
    }
}
