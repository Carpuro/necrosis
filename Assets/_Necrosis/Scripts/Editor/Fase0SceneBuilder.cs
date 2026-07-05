using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NECROSIS://PROTOCOLO — Constructor de la escena greybox de fase 0.
/// Automatiza los pasos manuales de README_FASE0.md para que la escena sea
/// reproducible desde cero (menú: Necrosis > Construir escena Fase 0).
///
/// Contenido: jugador + cámara al hombro + ciclo día/noche + NavMesh horneado
/// + 4 Cazadores + clip de estática para el Coro. Fase 0 completa.
/// </summary>
public static class Fase0SceneBuilder
{
    const string ScenePath = "Assets/_Necrosis/Scenes/Fase0.unity";
    const string NavMeshPath = "Assets/_Necrosis/Scenes/Fase0_NavMesh.asset";
    const string HunterMatPath = "Assets/_Necrosis/Materials/Hunter_Red.mat";
    const string ExtractionMatPath = "Assets/_Necrosis/Materials/Extraction_Green.mat";
    const string StaticClipPath = "Assets/_Necrosis/Audio/static_noise.wav";
    const string AudioDir = "Assets/_Necrosis/Audio/";
    const string FootstepDir = "Assets/_Necrosis/Audio/Footsteps/";
    const string HunterDir = "Assets/_Necrosis/Characters/Hunter/";
    const string HunterAnimatorPath = HunterDir + "HunterAnimator.controller";

    // Roster de Cazadores: zombie/zombiegirl comunes; parásito = nivel superior (raro).
    // Los pesos deciden la frecuencia de spawn. Si un modelo falta (p. ej. el zombi
    // gitignoreado en un clon), ese Cazador se queda como cápsula (null-safe).
    static readonly (string file, float weight, bool higherTier)[] HunterModels =
    {
        ("model_zombie_tpose.fbx",     40f, false),
        ("model_zombiegirl_tpose.fbx", 40f, false),
        ("model_parasite_tpose.fbx",   20f, true),
    };
    const float ParasiteSpeedMult = 1.2f;   // nivel superior: más rápido
    const float ParasiteDamageMult = 1.6f;  // pega más fuerte
    const float ParasiteScale = 1.25f;      // un poco más grande
    const float HunterModelYOffset = -1f;   // baja el modelo para que los pies toquen el suelo

    const string PlayerDir = "Assets/_Necrosis/Characters/Player/";
    const string PlayerAnimatorPath = PlayerDir + "PlayerAnimator.controller";
    const string PlayerModelFile = "model_ybot_tpose.fbx"; // coincide con las animaciones (Y Bot)

    // La ronda es una carrera de extracción de borde a borde (suelo 100x100).
    static readonly Vector3 PlayerSpawn = new Vector3(0f, 1.1f, -45f); // borde SUR
    static readonly Vector3 ExtractionPos = new Vector3(0f, 0f, 45f);  // borde NORTE

    [MenuItem("Necrosis/Construir escena Fase 0")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // --- Iluminación ambiente casi negra: la noche debe ser NOCHE ---
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.02f, 0.02f, 0.03f);

        // --- Suelo ---
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(10f, 1f, 10f); // 100x100 m
        ground.isStatic = true;

        // --- Greybox: cubos como edificios/muros, con esquinas y pasillos ---
        var blocks = new List<(Vector3 pos, Vector3 scale)>
        {
            (new Vector3( 10f, 2.5f,  8f), new Vector3( 6f, 5f, 10f)),
            (new Vector3( -9f, 2.0f, 14f), new Vector3( 8f, 4f,  4f)),
            (new Vector3(-16f, 3.0f, -6f), new Vector3( 5f, 6f, 12f)),
            (new Vector3(  4f, 1.5f, -14f), new Vector3(12f, 3f,  3f)),
            (new Vector3( 20f, 2.5f, -10f), new Vector3( 4f, 5f,  8f)),
            (new Vector3( -4f, 2.0f, 26f), new Vector3(14f, 4f,  5f)),
            (new Vector3( 24f, 3.5f, 18f), new Vector3( 7f, 7f,  7f)),
            (new Vector3(-24f, 2.0f, 20f), new Vector3( 4f, 4f, 14f)),
            (new Vector3(-28f, 2.5f, -20f), new Vector3(10f, 5f,  4f)),
            (new Vector3( 12f, 2.0f, 30f), new Vector3( 3f, 4f, 10f)),
            (new Vector3( 32f, 2.0f,  2f), new Vector3( 3f, 4f, 16f)),
            (new Vector3( -2f, 1.0f,  6f), new Vector3( 3f, 2f,  3f)), // cobertura baja
        };
        var greybox = new GameObject("Greybox");
        foreach (var (pos, scale) in blocks)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Block";
            cube.transform.SetParent(greybox.transform);
            cube.transform.position = pos;
            cube.transform.localScale = scale;
            cube.isStatic = true;
        }

        // --- Zona de extracción: la META de la ronda, en el borde NORTE ---
        //     Baliza emisiva verde (se ve desde el otro extremo, incluso de noche)
        //     + volumen disparador que completa la misión al entrar el jugador.
        //     Se crea ANTES del bake para que su geometría se tenga en cuenta.
        var extractionMat = AssetDatabase.LoadAssetAtPath<Material>(ExtractionMatPath);
        if (extractionMat == null)
        {
            extractionMat = new Material(Shader.Find("Standard")) { color = new Color(0.15f, 0.9f, 0.35f) };
            extractionMat.EnableKeyword("_EMISSION");
            extractionMat.SetColor("_EmissionColor", new Color(0.15f, 1f, 0.4f) * 2f);
            AssetDatabase.CreateAsset(extractionMat, ExtractionMatPath);
        }

        var extractionGo = new GameObject("Extraction");
        extractionGo.transform.position = ExtractionPos;

        // Placa en el suelo: dónde pararse.
        var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pad.name = "Pad";
        pad.transform.SetParent(extractionGo.transform);
        pad.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        pad.transform.localScale = new Vector3(6f, 0.2f, 6f);
        pad.GetComponent<MeshRenderer>().sharedMaterial = extractionMat;
        pad.isStatic = true;

        // Baliza vertical: referencia visual desde lejos.
        var beacon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beacon.name = "Beacon";
        beacon.transform.SetParent(extractionGo.transform);
        beacon.transform.localPosition = new Vector3(0f, 6f, 0f);
        beacon.transform.localScale = new Vector3(0.6f, 12f, 0.6f);
        beacon.GetComponent<MeshRenderer>().sharedMaterial = extractionMat;
        Object.DestroyImmediate(beacon.GetComponent<BoxCollider>()); // no debe estorbar al pathing

        // Luz de la baliza: que "llame" en la oscuridad.
        var beaconLightGo = new GameObject("BeaconLight");
        beaconLightGo.transform.SetParent(extractionGo.transform);
        beaconLightGo.transform.localPosition = new Vector3(0f, 3f, 0f);
        var beaconLight = beaconLightGo.AddComponent<Light>();
        beaconLight.type = LightType.Point;
        beaconLight.color = new Color(0.3f, 1f, 0.45f);
        beaconLight.range = 18f;
        beaconLight.intensity = 2f;

        // Volumen disparador: entrar aquí = misión cumplida.
        var trigger = extractionGo.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 1.5f, 0f);
        trigger.size = new Vector3(6f, 3f, 6f);
        var extractionZone = extractionGo.AddComponent<ExtractionZone>();

        // --- NavMesh: hornear ANTES de crear cápsulas (el bake usa render meshes;
        //     si jugador/Cazadores ya existieran, dejarían agujeros en el mesh) ---
        var surface = ground.AddComponent<NavMeshSurface>();
        surface.BuildNavMesh();
        AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);

        // --- Ciclo día/noche ---
        var sun = GameObject.Find("Directional Light").GetComponent<Light>();
        sun.shadows = LightShadows.Soft;
        var cycleGo = new GameObject("DayNightCycle");
        var cycle = cycleGo.AddComponent<DayNightCycle>();
        cycle.sunLight = sun;

        // --- Ambiente sonoro global: cama atmosférica + gritos lejanos ---
        var ambienceGo = new GameObject("Ambience");
        var ambience = ambienceGo.AddComponent<AmbienceAudio>();
        ambience.ambientLoop = Clip("terror_ambience.mp3");
        ambience.distantScreams = new[] { Clip("zombie_distance_scream.wav") };

        // --- Jugador (cápsula + todos los componentes) ---
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>()); // lo sustituye el CharacterController
        player.transform.position = PlayerSpawn;

        var controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.4f;
        controller.center = Vector3.zero;

        var movement = player.AddComponent<PlayerController>();
        var signature = player.AddComponent<PlayerSignature>();
        player.AddComponent<PlayerHealth>();

        var audio = player.AddComponent<AudioSource>();
        audio.loop = true;
        audio.playOnAwake = true;
        // Clip de estática placeholder (ruido blanco generado); sustituir por uno
        // con más carácter (freesound.org) cuando toque pulir audio
        audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(StaticClipPath);
        player.AddComponent<ChorusAudio>();
        player.AddComponent<DebugHUD>();

        // Pivote de cámara a la altura del hombro
        var pivot = new GameObject("CameraPivot");
        pivot.transform.SetParent(player.transform);
        pivot.transform.localPosition = new Vector3(0f, 0.6f, 0f); // 1.1 + 0.6 ≈ 1.7 m del suelo

        // Linterna (Spot) apuntando al frente
        var flashGo = new GameObject("Flashlight");
        flashGo.transform.SetParent(player.transform);
        flashGo.transform.localPosition = new Vector3(0f, 0.4f, 0.3f);
        flashGo.transform.localRotation = Quaternion.identity;
        var flash = flashGo.AddComponent<Light>();
        flash.type = LightType.Spot;
        flash.range = 15f;
        flash.spotAngle = 50f;
        flash.intensity = 2.5f;
        signature.flashlight = flash;

        // --- Cámara al hombro ---
        var cam = GameObject.Find("Main Camera");
        var shoulder = cam.AddComponent<ShoulderCamera>();
        shoulder.target = pivot.transform;
        movement.cameraTransform = cam.transform;

        // Modelo rigged del jugador (null-safe: sin modelo, sigue la cápsula)
        AttachPlayerModel(player, movement);

        // Pasos del jugador (bucle por postura; superficie urbana por defecto)
        var steps = player.AddComponent<FootstepAudio>();
        steps.walkLoop = Footstep("player_walking_sidewalk.wav");
        steps.runLoop = Footstep("player_walking_road.wav");
        steps.crouchLoop = Footstep("player_crouch_slow_indoors_carpet.wav");

        // --- Cazadores: 4 cápsulas rojas repartidas, deambulan (sin puntos de patrulla) ---
        var hunterMat = AssetDatabase.LoadAssetAtPath<Material>(HunterMatPath);
        if (hunterMat == null)
        {
            hunterMat = new Material(Shader.Find("Standard")) { color = new Color(0.7f, 0.08f, 0.08f) };
            AssetDatabase.CreateAsset(hunterMat, HunterMatPath);
        }
        var hunters = new GameObject("Hunters");
        var spawnPoints = new[]
        {
            new Vector3( 15f, 1.1f,  15f),
            new Vector3(-14f, 1.1f, -14f),
            new Vector3(  2f, 1.1f,  22f),
            new Vector3(-20f, 1.1f,   8f),
        };
        foreach (var pos in spawnPoints)
        {
            var hunter = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            hunter.name = "Hunter";
            hunter.transform.SetParent(hunters.transform);
            hunter.transform.position = pos;
            hunter.GetComponent<MeshRenderer>().sharedMaterial = hunterMat;
            hunter.AddComponent<NavMeshAgent>();
            var ai = hunter.AddComponent<HunterAI>();
            ai.obstacleMask = 1 << 0; // capa Default: los muros bloquean su visión

            // Voz 3D del Cazador (gruñidos/chillidos posicionales; localizables por oído)
            var voice = hunter.AddComponent<HunterVoice>();
            voice.patrolVoices = new[]
            {
                Clip("zombie_moaning.wav"), Clip("zombie_growling.wav"), Clip("zombie_standing_noise.mp3")
            };
            voice.huntVoices  = new[] { Clip("zombie_scream.wav"), Clip("zombie_growling.wav") };
            voice.frenzyVoice = Clip("zombie_scream.wav");
            voice.attackClip  = Clip("zombie_attack.wav");
            voice.nearClip    = Clip("zombie_near.wav");

            // Modelo rigged (aleatorio, con nivel superior parásito); null-safe
            AttachHunterModel(hunter, ai);
        }

        // --- Control de misión: objetivo, distancia restante y resultado ---
        var missionGo = new GameObject("Mission");
        var mission = missionGo.AddComponent<MissionController>();
        mission.player = player.transform;
        mission.extraction = extractionZone;
        mission.playerHealth = player.GetComponent<PlayerHealth>();

        // --- Guardar y registrar en Build Settings (PlayerHealth recarga por buildIndex) ---
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        Debug.Log($"[NECROSIS] Escena de fase 0 construida y guardada en {ScenePath}");
    }

    static AudioClip Clip(string file) =>
        AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + file);

    static AudioClip Footstep(string file) =>
        AssetDatabase.LoadAssetAtPath<AudioClip>(FootstepDir + file);

    // Elige un modelo del roster por peso, lo instancia como hijo de la cápsula,
    // le pone el Animator del Cazador y oculta la cápsula. Parásito = nivel superior.
    static void AttachHunterModel(GameObject capsule, HunterAI ai)
    {
        var pick = WeightedPick(HunterModels);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HunterDir + pick.file);
        if (prefab == null) return; // modelo ausente (p. ej. zombi gitignoreado): sigue cápsula

        var model = (GameObject)Object.Instantiate(prefab);
        model.name = "Model";
        model.transform.SetParent(capsule.transform, false);
        model.transform.localPosition = new Vector3(0f, HunterModelYOffset, 0f);
        model.transform.localRotation = Quaternion.identity;

        var animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HunterAnimatorPath);
        animator.applyRootMotion = false; // el movimiento lo manda el NavMeshAgent
        ai.animator = animator;

        // Ocultar la cápsula (mantiene collider/lógica; quita solo el render)
        var rend = capsule.GetComponent<MeshRenderer>();
        if (rend != null) rend.enabled = false;

        // Nivel superior (parásito): más rápido, pega más fuerte y algo más grande
        if (pick.higherTier)
        {
            ai.baseChaseSpeed *= ParasiteSpeedMult;
            ai.attackDamage *= ParasiteDamageMult;
            capsule.transform.localScale *= ParasiteScale;
        }
    }

    static void AttachPlayerModel(GameObject player, PlayerController movement)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerDir + PlayerModelFile);
        if (prefab == null) return;

        var model = (GameObject)Object.Instantiate(prefab);
        model.name = "Model";
        model.transform.SetParent(player.transform, false);
        model.transform.localPosition = new Vector3(0f, HunterModelYOffset, 0f);
        model.transform.localRotation = Quaternion.identity;

        var animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerAnimatorPath);
        animator.applyRootMotion = false;
        movement.animator = animator;

        var rend = player.GetComponent<MeshRenderer>();
        if (rend != null) rend.enabled = false;
    }

    static (string file, float weight, bool higherTier) WeightedPick(
        (string file, float weight, bool higherTier)[] options)
    {
        float total = 0f;
        foreach (var o in options) total += o.weight;
        float r = Random.Range(0f, total);
        foreach (var o in options)
        {
            r -= o.weight;
            if (r <= 0f) return o;
        }
        return options[options.Length - 1];
    }
}
