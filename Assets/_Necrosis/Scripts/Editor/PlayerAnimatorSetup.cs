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
        (AnimDir + "/locomotion/animation_ybot_movement_run_straight_fast.fbx", true),
        (AnimDir + "/locomotion/animation_ybot_crouch_idle.fbx",              true),
        (AnimDir + "/locomotion/animation_ybot_crouch_movement_straight.fbx", true),
        (AnimDir + "/melee/animation_ybot_melee_kick.fbx",                    false),
        (AnimDir + "/melee/animation_ybot_melee_swing.fbx",                   false),
        (AnimDir + "/death/animation_ybot_death_1.fbx",                       false),
        (AnimDir + "/death/animation_ybot_death_2.fbx",                       false),
    };

    // El clip de giro a la derecha es el de la izquierda ESPEJADO (no hay right nativo).
    const string MirrorClip = "animation_ybot_movement_walk_turn_right.fbx";

    [MenuItem("Necrosis/Setup animación del Jugador")]
    public static void Run()
    {
        foreach (var m in Models) SetHumanoid(m, null);
        foreach (var (file, loop) in Anims) SetHumanoid(file, loop);
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
        var runF    = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_run_straight_fast.fbx");
        tree.AddChild(idle,  new Vector2( 0f, 0f));
        tree.AddChild(walkS, new Vector2( 0f, 3.5f)); // walkSpeed
        tree.AddChild(walkL, new Vector2(-1f, 3.5f)); // giro izq caminando
        tree.AddChild(walkR, new Vector2( 1f, 3.5f)); // giro der caminando
        tree.AddChild(runF,  new Vector2( 0f, 6f));   // sprint (<runSpeed 6.5)
        tree.AddChild(walkL, new Vector2(-1f, 6f));   // giro izq corriendo (reusa walk turn)
        tree.AddChild(walkR, new Vector2( 1f, 6f));   // giro der corriendo
        sm.defaultState = loco;

        // Agachado: blend por Speed (crouch_idle quieto -> crouch_walking en movimiento)
        var crouch = controller.CreateBlendTreeInController("Crouch", out BlendTree crouchTree, 0);
        crouchTree.blendType = BlendTreeType.Simple1D;
        crouchTree.blendParameter = "Speed";
        crouchTree.useAutomaticThresholds = false;
        crouchTree.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_crouch_idle.fbx"), 0f);
        crouchTree.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_crouch_movement_straight.fbx"), 1.5f); // crouchSpeed

        var toCrouch = loco.AddTransition(crouch);
        toCrouch.AddCondition(AnimatorConditionMode.If, 0, "Crouch");
        toCrouch.hasExitTime = false;
        toCrouch.duration = 0.15f;

        var fromCrouch = crouch.AddTransition(loco);
        fromCrouch.AddCondition(AnimatorConditionMode.IfNot, 0, "Crouch");
        fromCrouch.hasExitTime = false;
        fromCrouch.duration = 0.15f;

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
