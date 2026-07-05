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
        PlayerDir + "/model_xbot_tpose.fbx",
        PlayerDir + "/model_ybot_tpose.fbx",
    };

    // clip -> ¿loop?
    static readonly (string file, bool loop)[] Anims =
    {
        (AnimDir + "/idle.fbx",        true),
        (AnimDir + "/walk.fbx",        true),
        (AnimDir + "/run.fbx",         true),
        (AnimDir + "/run_fast.fbx",    true),
        (AnimDir + "/crouch_idle.fbx",    true),
        (AnimDir + "/crouch_walking.fbx", true),
        (AnimDir + "/melee_kick.fbx",  false),
        (AnimDir + "/melee_swing.fbx", false),
        (AnimDir + "/dying.fbx",       false),
        (AnimDir + "/death.fbx",       false),
    };

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

        if (loop.HasValue)
        {
            var clips = imp.defaultClipAnimations;
            for (int i = 0; i < clips.Length; i++) clips[i].loopTime = loop.Value;
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
        controller.AddParameter("Crouch", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        // Locomoción de pie: blend 1D por Speed (m/s reales del PlayerController)
        var loco = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false; // respetar umbrales en m/s reales
        tree.AddChild(LoadClip(AnimDir + "/idle.fbx"), 0f);
        tree.AddChild(LoadClip(AnimDir + "/walk.fbx"), 3.5f);  // walkSpeed
        tree.AddChild(LoadClip(AnimDir + "/run_fast.fbx"), 6f); // sprint; < runSpeed (6.5) para alcanzarlo con amortiguado
        sm.defaultState = loco;

        // Agachado: blend por Speed (crouch_idle quieto -> crouch_walking en movimiento)
        var crouch = controller.CreateBlendTreeInController("Crouch", out BlendTree crouchTree, 0);
        crouchTree.blendType = BlendTreeType.Simple1D;
        crouchTree.blendParameter = "Speed";
        crouchTree.useAutomaticThresholds = false;
        crouchTree.AddChild(LoadClip(AnimDir + "/crouch_idle.fbx"), 0f);
        crouchTree.AddChild(LoadClip(AnimDir + "/crouch_walking.fbx"), 1.5f); // crouchSpeed

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
        death.motion = LoadClip(AnimDir + "/dying.fbx");
        var toDeath = sm.AddAnyStateTransition(death);
        toDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");
        toDeath.hasExitTime = false;
        toDeath.duration = 0.1f;
        toDeath.canTransitionToSelf = false;

        AssetDatabase.SaveAssets();
    }
}
