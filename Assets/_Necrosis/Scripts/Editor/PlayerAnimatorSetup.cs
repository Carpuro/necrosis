using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// NECROSIS://PROTOCOLO — Setup automático de animación del Jugador.
/// 1) Marca el modelo y los clips (Mixamo) como Humanoid.
/// 2) Construye PlayerAnimator.controller con los parámetros que PlayerController
///    ya envía: float 'Speed' (velocidad planar) + bool 'Crouch'.
///
/// Ejecutar: menú "Necrosis > Setup animación del Jugador" o batchmode
/// -executeMethod PlayerAnimatorSetup.Run
/// </summary>
public static class PlayerAnimatorSetup
{
    const string PlayerDir = "Assets/_Necrosis/Characters/Player";
    const string AnimDir = PlayerDir + "/Animations";
    const string ControllerPath = PlayerDir + "/PlayerAnimator.controller";

    static readonly string[] Models =
    {
        PlayerDir + "/Models/model_x_bot_tpose.fbx",
        PlayerDir + "/Models/model_y_bot_tpose.fbx",
    };

    // clip -> ¿loop?
    static readonly (string file, bool loop)[] Anims =
    {
        (AnimDir + "/locomotion/animation_ybot_idle.fbx",                     true),
        (AnimDir + "/locomotion/animation_ybot_movement_walk_straight.fbx",   true),
        (AnimDir + "/locomotion/animation_ybot_movement_walk_turn_left.fbx",  true),
        (AnimDir + "/locomotion/animation_ybot_movement_walk_turn_right.fbx", true),
        (AnimDir + "/locomotion/animation_ybot_movement_run_straight.fbx",    true),
        (AnimDir + "/locomotion/animation_ybot_movement_sprint_straight.fbx", true),
        (AnimDir + "/locomotion/animation_ybot_crouch_idle.fbx",              true),
        (AnimDir + "/locomotion/animation_ybot_crouch_movement_straight.fbx", true),
        (AnimDir + "/locomotion/animation_ybot_standing_movement_crouch.fbx", false), // entrar a agacharse
        (AnimDir + "/locomotion/animation_ybot_crouch_movement_stand.fbx",    false), // salir de agacharse
        (AnimDir + "/melee/animation_ybot_melee_kick.fbx",                    false),
        (AnimDir + "/melee/animation_ybot_melee_swing.fbx",                   false),
        (AnimDir + "/death/animation_ybot_death_1.fbx",                       false),
        (AnimDir + "/death/animation_ybot_death_2.fbx",                       false),
    };

    // Sets de strafe por postura (puños / melé / arma): idle, forward, backward, left, right.
    static readonly string[] Stances = { "fists", "melee", "gun" };
    static readonly string[] AimDirs = { "idle", "forward", "backward", "left", "right" };

    // El clip de giro a la derecha es el de la izquierda ESPEJADO (no hay right nativo).
    const string MirrorClip = "animation_ybot_movement_walk_turn_right.fbx";

    [MenuItem("Necrosis/Setup animación del Jugador")]
    public static void Run()
    {
        foreach (var m in Models) SetHumanoid(m, null);
        foreach (var (file, loop) in Anims) SetHumanoid(file, loop);
        foreach (var s in Stances)
            foreach (var d in AimDirs)
                SetHumanoid($"{AnimDir}/aim/{s}/animation_ybot_aim_{s}_{d}.fbx", true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        BuildController();
        Debug.Log("[NECROSIS] Setup de animación del Jugador completado.");
    }

    static void SetHumanoid(string path, bool? loop)
    {
        var imp = AssetImporter.GetAtPath(path) as ModelImporter;
        if (imp == null) { Debug.LogWarning($"[NECROSIS] No es modelo importable: {path}"); return; }

        // Ya Humanoid y sin cambios de loop: no reimportar.
        if (imp.animationType == ModelImporterAnimationType.Human && !loop.HasValue) return;

        imp.animationType = ModelImporterAnimationType.Human;
        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

        bool mirror = path.EndsWith(MirrorClip);
        if (loop.HasValue || mirror)
        {
            var clips = imp.defaultClipAnimations;
            for (int i = 0; i < clips.Length; i++)
            {
                if (loop.HasValue) clips[i].loopTime = loop.Value;
                if (mirror) clips[i].mirror = true; // giro derecha = izquierda espejado
            }
            if (clips.Length > 0) imp.clipAnimations = clips;
        }
        imp.SaveAndReimport();
    }

    static AnimationClip LoadClip(string fbxPath)
    {
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            if (a is AnimationClip c && !c.name.StartsWith("__preview"))
                return c;
        Debug.LogWarning($"[NECROSIS] Sin AnimationClip en {fbxPath}");
        return null;
    }

    static void BuildController()
    {
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Turn", AnimatorControllerParameterType.Float);   // -1 izq .. +1 der
        controller.AddParameter("Crouch", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Aiming", AnimatorControllerParameterType.Bool);
        controller.AddParameter("AimStance", AnimatorControllerParameterType.Int); // 0 puños,1 melé,2 arma
        controller.AddParameter("AimX", AnimatorControllerParameterType.Float); // strafe -1..+1
        controller.AddParameter("AimY", AnimatorControllerParameterType.Float); // atrás/adelante -1..+1

        var sm = controller.layers[0].stateMachine;

        // Locomoción de pie: blend 2D (X=Turn, Y=Speed). idle->walk->run recto y
        // giros a izq/der al caminar y correr (giro derecha = izquierda espejado).
        var loco = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
        tree.blendType = BlendTreeType.FreeformCartesian2D;
        tree.blendParameter = "Turn";    // X
        tree.blendParameterY = "Speed";  // Y
        var idle    = LoadClip(AnimDir + "/locomotion/animation_ybot_idle.fbx");
        var walkS   = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_walk_straight.fbx");
        var walkL   = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_walk_turn_left.fbx");
        var walkR   = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_walk_turn_right.fbx");
        var runS    = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_run_straight.fbx");
        var runF    = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_sprint_straight.fbx");
        tree.AddChild(idle,  new Vector2( 0f, 0f));
        tree.AddChild(walkS, new Vector2( 0f, 3.5f)); // walk (C off)
        tree.AddChild(walkL, new Vector2(-1f, 3.5f)); // giro izq caminando
        tree.AddChild(walkR, new Vector2( 1f, 3.5f)); // giro der caminando
        tree.AddChild(runS,  new Vector2( 0f, 6.5f)); // run (C on)
        tree.AddChild(walkL, new Vector2(-1f, 6.5f)); // giro izq corriendo (reusa walk turn)
        tree.AddChild(walkR, new Vector2( 1f, 6.5f)); // giro der corriendo
        tree.AddChild(runF,  new Vector2( 0f, 8f));   // sprint (Shift)
        tree.AddChild(walkL, new Vector2(-1f, 8f));   // giro izq esprintando
        tree.AddChild(walkR, new Vector2( 1f, 8f));   // giro der esprintando
        sm.defaultState = loco;

        // Agachado: blend por Speed (crouch_idle quieto -> crouch_walking en movimiento)
        var crouch = controller.CreateBlendTreeInController("Crouch", out BlendTree crouchTree, 0);
        crouchTree.blendType = BlendTreeType.Simple1D;
        crouchTree.blendParameter = "Speed";
        crouchTree.useAutomaticThresholds = false;
        crouchTree.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_crouch_idle.fbx"), 0f);
        crouchTree.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_crouch_movement_straight.fbx"), 1.5f); // crouchSpeed

        // Transiciones con clips dedicados: de pie -> agacharse -> (blend) -> levantarse.
        var crouchEnter = sm.AddState("CrouchEnter");
        crouchEnter.motion = LoadClip(AnimDir + "/locomotion/animation_ybot_standing_movement_crouch.fbx");
        var crouchExit = sm.AddState("CrouchExit");
        crouchExit.motion = LoadClip(AnimDir + "/locomotion/animation_ybot_crouch_movement_stand.fbx");

        // Locomoción --Crouch--> CrouchEnter --(al terminar)--> Crouch
        var toEnter = loco.AddTransition(crouchEnter);
        toEnter.AddCondition(AnimatorConditionMode.If, 0, "Crouch");
        toEnter.hasExitTime = false;
        toEnter.duration = 0.1f;
        var enterToCrouch = crouchEnter.AddTransition(crouch);
        enterToCrouch.hasExitTime = true; enterToCrouch.exitTime = 0.8f;
        enterToCrouch.duration = 0.1f;
        // Si suelta crouch a mitad de agacharse, vuelve a locomoción
        var enterCancel = crouchEnter.AddTransition(loco);
        enterCancel.AddCondition(AnimatorConditionMode.IfNot, 0, "Crouch");
        enterCancel.hasExitTime = false; enterCancel.duration = 0.1f;

        // Crouch --!Crouch--> CrouchExit --(al terminar)--> Locomoción
        var toExit = crouch.AddTransition(crouchExit);
        toExit.AddCondition(AnimatorConditionMode.IfNot, 0, "Crouch");
        toExit.hasExitTime = false;
        toExit.duration = 0.1f;
        var exitToLoco = crouchExit.AddTransition(loco);
        exitToLoco.hasExitTime = true; exitToLoco.exitTime = 0.8f;
        exitToLoco.duration = 0.1f;
        // Si vuelve a agacharse a mitad de levantarse, regresa a Crouch
        var exitCancel = crouchExit.AddTransition(crouch);
        exitCancel.AddCondition(AnimatorConditionMode.If, 0, "Crouch");
        exitCancel.hasExitTime = false; exitCancel.duration = 0.1f;

        // Apuntar/strafe (State of Decay): un blend 2D direccional POR POSTURA
        // (puños=0, melé=1, arma=2). El cuerpo mira a cámara; WASD strafea.
        // AnyState entra a la postura correcta según Aiming + AimStance; al soltar
        // clic derecho o cambiar de postura, AnyState reevalúa.
        for (int i = 0; i < Stances.Length; i++)
        {
            string s = Stances[i];
            var aim = controller.CreateBlendTreeInController("Aim_" + s, out BlendTree t, 0);
            t.blendType = BlendTreeType.SimpleDirectional2D;
            t.blendParameter = "AimX";
            t.blendParameterY = "AimY";
            string p = $"{AnimDir}/aim/{s}/animation_ybot_aim_{s}_";
            t.AddChild(LoadClip(p + "idle.fbx"),     new Vector2( 0f,  0f));
            t.AddChild(LoadClip(p + "forward.fbx"),  new Vector2( 0f,  1f));
            t.AddChild(LoadClip(p + "backward.fbx"), new Vector2( 0f, -1f));
            t.AddChild(LoadClip(p + "left.fbx"),     new Vector2(-1f,  0f));
            t.AddChild(LoadClip(p + "right.fbx"),    new Vector2( 1f,  0f));

            var toAim = sm.AddAnyStateTransition(aim);
            toAim.AddCondition(AnimatorConditionMode.If, 0, "Aiming");
            toAim.AddCondition(AnimatorConditionMode.Equals, i, "AimStance");
            toAim.hasExitTime = false; toAim.duration = 0.12f; toAim.canTransitionToSelf = false;

            var fromAim = aim.AddTransition(loco);
            fromAim.AddCondition(AnimatorConditionMode.IfNot, 0, "Aiming");
            fromAim.hasExitTime = false; fromAim.duration = 0.12f;
        }

        // Muerte: desde cualquier estado al disparar "Die" (PlayerHealth). No vuelve.
        var death = sm.AddState("Death");
        death.motion = LoadClip(AnimDir + "/death/animation_ybot_death_1.fbx"); // 1ª variante
        var toDeath = sm.AddAnyStateTransition(death);
        toDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");
        toDeath.hasExitTime = false;
        toDeath.duration = 0.1f;
        toDeath.canTransitionToSelf = false;

        AssetDatabase.SaveAssets();
    }
}
