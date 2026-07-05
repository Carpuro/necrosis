using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// NECROSIS://PROTOCOLO — Setup automático de animación del Cazador.
/// 1) Marca modelos y clips de Mixamo como Humanoid (retargeteables entre sí).
/// 2) Construye el Animator Controller del Cazador con los parámetros que
///    HunterAI/HunterVoice ya envían (Speed / Statue / Attacking).
///
/// Ejecutar: menú "Necrosis > Setup animación del Cazador" o en batchmode con
/// -executeMethod HunterAnimatorSetup.Run
/// </summary>
public static class HunterAnimatorSetup
{
    const string HunterDir = "Assets/_Necrosis/Characters/Hunter";
    const string AnimDir = HunterDir + "/Animations";
    const string ControllerPath = HunterDir + "/HunterAnimator.controller";

    static readonly string[] Models =
    {
        HunterDir + "/model_zombie_tpose.fbx",
        HunterDir + "/model_parasite_tpose.fbx",
        HunterDir + "/model_zombiegirl_tpose.fbx",
    };

    // clip -> ¿en loop?
    static readonly (string file, bool loop)[] Anims =
    {
        (AnimDir + "/zombie_idle.fbx",   true),
        (AnimDir + "/zombie_walk.fbx",   true),
        (AnimDir + "/zombie_run.fbx",    true),
        (AnimDir + "/zombie_scream.fbx", true),
        (AnimDir + "/zombie_attack.fbx", false),
        (AnimDir + "/zombie_death.fbx",  false),
    };

    [MenuItem("Necrosis/Setup animación del Cazador")]
    public static void Run()
    {
        // --- 1) Import settings: Humanoid en modelos y clips ---
        foreach (var m in Models) SetHumanoid(m, null);           // los clips no aplican al modelo
        foreach (var (file, loop) in Anims) SetHumanoid(file, loop);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // --- 2) Animator Controller ---
        BuildController();

        Debug.Log("[NECROSIS] Setup de animación del Cazador completado.");
    }

    static void SetHumanoid(string path, bool? loop)
    {
        var imp = AssetImporter.GetAtPath(path) as ModelImporter;
        if (imp == null) { Debug.LogWarning($"[NECROSIS] No es modelo importable: {path}"); return; }

        // Ya Humanoid y sin cambios de loop: no reimportar (evita rehornear el zombi de 105MB)
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
        controller.AddParameter("Statue", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attacking", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;

        // Locomoción: blend 1D por Speed (idle -> walk -> run)
        var loco = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false; // respetar los umbrales en m/s reales (si no, Unity los reparte 0..1)
        tree.AddChild(LoadClip(AnimDir + "/zombie_idle.fbx"), 0f);
        tree.AddChild(LoadClip(AnimDir + "/zombie_walk.fbx"), 1f);
        tree.AddChild(LoadClip(AnimDir + "/zombie_run.fbx"), 2f); // la persecución baja a ~2.3 con poca luz; patrulla ~1.6 se queda en walk
        sm.defaultState = loco;

        // Ataque: desde cualquier estado cuando Attacking; vuelve al soltarlo
        var attack = sm.AddState("Attack");
        attack.motion = LoadClip(AnimDir + "/zombie_attack.fbx");
        var toAttack = sm.AddAnyStateTransition(attack);
        toAttack.AddCondition(AnimatorConditionMode.If, 0, "Attacking");
        toAttack.hasExitTime = false;
        toAttack.duration = 0.08f;
        toAttack.canTransitionToSelf = false;
        var fromAttack = attack.AddTransition(loco);
        fromAttack.AddCondition(AnimatorConditionMode.IfNot, 0, "Attacking");
        fromAttack.hasExitTime = false;
        fromAttack.duration = 0.15f;

        AssetDatabase.SaveAssets();
    }
}
