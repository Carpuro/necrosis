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
        (AnimDir + "/locomotion/animation_ybot_idle_movement_walking.fbx",    false), // arranque al caminar
        (AnimDir + "/locomotion/animation_ybot_movement_walk_straight.fbx",   true),
        (AnimDir + "/locomotion/animation_ybot_movement_walk_turn_left.fbx",  true),
        (AnimDir + "/locomotion/animation_ybot_movement_walk_turn_right.fbx", true),
        (AnimDir + "/locomotion/animation_ybot_movement_run_turn_left.fbx",   true),
        (AnimDir + "/locomotion/animation_ybot_movement_run_turn_right.fbx",  true),
        (AnimDir + "/locomotion/animation_ybot_idle_movement_turn_left_180.fbx",  false),
        (AnimDir + "/locomotion/animation_ybot_idle_movement_turn_right_180.fbx", false),
        (AnimDir + "/locomotion/animation_ybot_idle_movement_turn_left_90.fbx",   false),
        (AnimDir + "/locomotion/animation_ybot_idle_movement_turn_right_90.fbx",  false),
        (AnimDir + "/locomotion/animation_ybot_movement_walk_turn_180_right.fbx", false),
        (AnimDir + "/locomotion/animation_ybot_movement_walk_turn_180_left.fbx",  false),
        (AnimDir + "/locomotion/animation_ybot_movement_run_turn_180_right.fbx",  false),
        (AnimDir + "/locomotion/animation_ybot_movement_run_turn_180_left.fbx",   false),
        (AnimDir + "/locomotion/animation_ybot_turninplace_left.fbx",         true),
        (AnimDir + "/locomotion/animation_ybot_turninplace_right.fbx",        true),
        (AnimDir + "/locomotion/animation_ybot_movement_run_straight.fbx",    true),
        (AnimDir + "/locomotion/animation_ybot_movement_sprint_straight.fbx", true),
        (AnimDir + "/locomotion/animation_ybot_movement_walk_backwards.fbx",  true),
        (AnimDir + "/locomotion/animation_ybot_movement_walk_strafe_left.fbx",  true),
        (AnimDir + "/locomotion/animation_ybot_movement_walk_strafe_right.fbx", true),
        (AnimDir + "/locomotion/animation_ybot_movement_jog_strafe_left.fbx",   true),
        (AnimDir + "/locomotion/animation_ybot_movement_jog_strafe_right.fbx",  true),
        (AnimDir + "/locomotion/animation_ybot_run_movement_roll_straight.fbx", false), // esquiva
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

    // Clips que se ESPEJAN al importar (no hay nativo del lado opuesto):
    //  - walk_turn_right = espejo del walk_turn_left (nativo izquierda)
    //  - run_turn_left   = espejo del run_turn_right (nativo derecha, lo agregó Carlos)
    static readonly string[] MirrorClips =
    {
        "animation_ybot_movement_walk_turn_right.fbx",
        "animation_ybot_movement_run_turn_left.fbx",
        "animation_ybot_movement_walk_turn_180_left.fbx",
        "animation_ybot_movement_run_turn_180_left.fbx",
    };

    const string YBotModel = PlayerDir + "/Models/model_y_bot_tpose.fbx";

    [MenuItem("Necrosis/Setup animación del Jugador")]
    public static void Run()
    {
        // 1) Modelos: avatar Humanoid creado desde sí mismos.
        foreach (var m in Models) SetHumanoid(m, null, null);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 2) Todos los clips COPIAN el avatar de y_bot (mismo esqueleto). Evita que
        //    cada clip genere su propio avatar y desmapee extremidades (pie izq).
        var ybotAvatar = LoadAvatar(YBotModel);
        if (ybotAvatar == null) Debug.LogWarning("[NECROSIS] No se encontró el avatar de y_bot.");

        foreach (var (file, loop) in Anims) SetHumanoid(file, loop, ybotAvatar);
        foreach (var s in Stances)
            foreach (var d in AimDirs)
                SetHumanoid($"{AnimDir}/aim/{s}/animation_ybot_aim_{s}_{d}.fbx", true, ybotAvatar);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        BuildController();
        Debug.Log("[NECROSIS] Setup de animación del Jugador completado.");
    }

    static Avatar LoadAvatar(string modelPath)
    {
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            if (a is Avatar av) return av;
        return null;
    }

    static void SetHumanoid(string path, bool? loop, Avatar sourceAvatar)
    {
        var imp = AssetImporter.GetAtPath(path) as ModelImporter;
        if (imp == null) { Debug.LogWarning($"[NECROSIS] No es modelo importable: {path}"); return; }

        imp.animationType = ModelImporterAnimationType.Human;
        if (sourceAvatar != null)
        {
            // Clip: copiar el avatar de y_bot (retarget consistente, arregla pies).
            imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            imp.sourceAvatar = sourceAvatar;
        }
        else
        {
            // Modelo: su propio avatar. Si ya es Humanoid, no re-hornear.
            if (imp.animationType == ModelImporterAnimationType.Human &&
                imp.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel) return;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        }

        bool mirror = System.Array.Exists(MirrorClips, m => path.EndsWith(m));
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
        controller.AddParameter("TurningInPlace", AnimatorControllerParameterType.Bool);
        controller.AddParameter("TurnInPlace", AnimatorControllerParameterType.Float); // -1 izq..+1 der
        controller.AddParameter("Aiming", AnimatorControllerParameterType.Bool);
        controller.AddParameter("StrafeLock", AnimatorControllerParameterType.Bool); // strafe libre (Left Alt)
        controller.AddParameter("Roll", AnimatorControllerParameterType.Trigger);    // esquiva (Espacio)
        controller.AddParameter("StartWalk", AnimatorControllerParameterType.Trigger); // arranque idle->caminar
        controller.AddParameter("Turn180", AnimatorControllerParameterType.Trigger);   // giro 180 al invertir
        controller.AddParameter("Turn180Dir", AnimatorControllerParameterType.Float);  // -1 izq / +1 der
        controller.AddParameter("Turn180Tier", AnimatorControllerParameterType.Float); // 0 idle / 1 caminar / 2 correr
        controller.AddParameter("AimStance", AnimatorControllerParameterType.Int); // 0 puños,1 melé,2 arma
        controller.AddParameter("AimX", AnimatorControllerParameterType.Float); // strafe -1..+1
        controller.AddParameter("AimY", AnimatorControllerParameterType.Float); // atrás/adelante -1..+1

        var sm = controller.layers[0].stateMachine;

        // Locomoción de pie: blend 2D (X=Turn, Y=Speed). idle->walk->run recto y
        // giros a izq/der al caminar y correr (giro derecha = izquierda espejado).
        var loco = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
        tree.blendType = BlendTreeType.FreeformCartesian2D; // locomoción: Turn/Speed (funciona)
        tree.blendParameter = "Turn";    // X
        tree.blendParameterY = "Speed";  // Y
        var idle    = LoadClip(AnimDir + "/locomotion/animation_ybot_idle.fbx");
        var walkS   = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_walk_straight.fbx");
        var walkL   = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_walk_turn_left.fbx");
        var walkR   = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_walk_turn_right.fbx");
        var runS    = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_run_straight.fbx");
        var runL    = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_run_turn_left.fbx");
        var runR    = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_run_turn_right.fbx");
        var runF    = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_sprint_straight.fbx");
        tree.AddChild(idle,  new Vector2( 0f, 0f));
        tree.AddChild(walkS, new Vector2( 0f, 2.5f)); // walk (C off) = walkSpeed
        tree.AddChild(walkL, new Vector2(-1f, 2.5f)); // giro izq caminando
        tree.AddChild(walkR, new Vector2( 1f, 2.5f)); // giro der caminando
        tree.AddChild(runS,  new Vector2( 0f, 6.5f)); // run (C on)
        tree.AddChild(runL,  new Vector2(-1f, 6.5f)); // giro izq corriendo (nativo)
        tree.AddChild(runR,  new Vector2( 1f, 6.5f)); // giro der corriendo (nativo)
        tree.AddChild(runF,  new Vector2( 0f, 8f));   // sprint (Shift)
        tree.AddChild(runL,  new Vector2(-1f, 8f));   // giro izq esprintando
        tree.AddChild(runR,  new Vector2( 1f, 8f));   // giro der esprintando
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

        // Arranque al caminar: al pasar de idle a caminar de frente, reproduce el
        // clip de arranque y luego entra a locomoción. Trigger StartWalk.
        var walkStart = sm.AddState("WalkStart");
        walkStart.motion = LoadClip(AnimDir + "/locomotion/animation_ybot_idle_movement_walking.fbx");
        walkStart.speed = 2f; // el doble de rápido (pedido de Carlos)
        var toWalkStart = loco.AddTransition(walkStart);
        toWalkStart.AddCondition(AnimatorConditionMode.If, 0, "StartWalk");
        toWalkStart.hasExitTime = false; toWalkStart.duration = 0.05f;
        // Sale a locomoción tras el primer apoyo (~mitad del clip); el jugador
        // empieza a avanzar en ese punto (ver walkStartDuration en PlayerController).
        var fromWalkStart = walkStart.AddTransition(loco);
        fromWalkStart.hasExitTime = true; fromWalkStart.exitTime = 0.5f; fromWalkStart.duration = 0.12f;

        // Giro 180 (al invertir el sentido de marcha): blend por Speed
        // (walk_180 lento, run_180 rápido). Trigger Turn180; vuelve al terminar.
        // Blend 2D: X = dirección (-1 izq / +1 der), Y = nivel (0 idle / 1 caminar / 2 correr).
        var turn180 = controller.CreateBlendTreeInController("Turn180", out BlendTree t180, 0);
        t180.blendType = BlendTreeType.FreeformCartesian2D;
        t180.blendParameter = "Turn180Dir";
        t180.blendParameterY = "Turn180Tier";
        t180.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_idle_movement_turn_left_180.fbx"),   new Vector2(-1f, 0f));
        t180.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_idle_movement_turn_right_180.fbx"),  new Vector2( 1f, 0f));
        t180.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_movement_walk_turn_180_left.fbx"),   new Vector2(-1f, 1f));
        t180.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_movement_walk_turn_180_right.fbx"),  new Vector2( 1f, 1f));
        t180.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_movement_run_turn_180_left.fbx"),    new Vector2(-1f, 2f));
        t180.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_movement_run_turn_180_right.fbx"),   new Vector2( 1f, 2f));
        var toTurn180 = sm.AddAnyStateTransition(turn180);
        toTurn180.AddCondition(AnimatorConditionMode.If, 0, "Turn180");
        toTurn180.hasExitTime = false; toTurn180.duration = 0.08f; toTurn180.canTransitionToSelf = false;
        var fromTurn180 = turn180.AddTransition(loco);
        fromTurn180.hasExitTime = true; fromTurn180.exitTime = 0.7f; fromTurn180.duration = 0.12f;

        // Discrete start turn (from idle): 1D blend by TurnInPlace select value:
        // -2 180-left · -1 90-left · 0 idle · +1 90-right · +2 180-right.
        // Entered from locomotion while TurningInPlace; exits when it clears.
        var turnIP = controller.CreateBlendTreeInController("TurnInPlace", out BlendTree tip, 0);
        tip.blendType = BlendTreeType.Simple1D;
        tip.blendParameter = "TurnInPlace";
        tip.useAutomaticThresholds = false;
        tip.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_idle_movement_turn_left_180.fbx"),  -2f);
        tip.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_idle_movement_turn_left_90.fbx"),   -1f);
        tip.AddChild(idle, 0f);
        tip.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_idle_movement_turn_right_90.fbx"),   1f);
        tip.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_idle_movement_turn_right_180.fbx"),  2f);

        var toTurnIP = loco.AddTransition(turnIP);
        toTurnIP.AddCondition(AnimatorConditionMode.If, 0, "TurningInPlace");
        toTurnIP.hasExitTime = false; toTurnIP.duration = 0.1f;
        var fromTurnIP = turnIP.AddTransition(loco);
        fromTurnIP.AddCondition(AnimatorConditionMode.IfNot, 0, "TurningInPlace");
        fromTurnIP.hasExitTime = false; fromTurnIP.duration = 0.1f;

        // Apuntar/strafe (State of Decay): un blend 2D direccional POR POSTURA
        // (puños=0, melé=1, arma=2). El cuerpo mira a cámara; WASD strafea.
        // AnyState entra a la postura correcta según Aiming + AimStance; al soltar
        // clic derecho o cambiar de postura, AnyState reevalúa.
        for (int i = 0; i < Stances.Length; i++)
        {
            string s = Stances[i];
            var aim = controller.CreateBlendTreeInController("Aim_" + s, out BlendTree t, 0);
            t.blendType = BlendTreeType.FreeformDirectional2D;
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

        // Strafe libre (sin apuntar): blend 2D direccional con clips normales
        // (no de combate). Comparte AimX/AimY. Left Alt lo activa.
        var strafe = controller.CreateBlendTreeInController("Strafe", out BlendTree st, 0);
        st.blendType = BlendTreeType.FreeformDirectional2D;
        st.blendParameter = "AimX";
        st.blendParameterY = "AimY";
        st.AddChild(idle, new Vector2(0f, 0f));
        st.AddChild(walkS, new Vector2(0f, 1f)); // adelante
        st.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_movement_walk_backwards.fbx"),   new Vector2( 0f, -1f));
        st.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_movement_walk_strafe_left.fbx"),  new Vector2(-1f, 0f));
        st.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_movement_walk_strafe_right.fbx"), new Vector2( 1f, 0f));

        var toStrafe = sm.AddAnyStateTransition(strafe);
        toStrafe.AddCondition(AnimatorConditionMode.If, 0, "StrafeLock");
        toStrafe.hasExitTime = false; toStrafe.duration = 0.12f; toStrafe.canTransitionToSelf = false;
        var fromStrafe = strafe.AddTransition(loco);
        fromStrafe.AddCondition(AnimatorConditionMode.IfNot, 0, "StrafeLock");
        fromStrafe.hasExitTime = false; fromStrafe.duration = 0.12f;

        // Esquiva (rodar): trigger "Roll" desde cualquier estado; vuelve al terminar.
        var roll = sm.AddState("Roll");
        roll.motion = LoadClip(AnimDir + "/locomotion/animation_ybot_run_movement_roll_straight.fbx");
        var toRoll = sm.AddAnyStateTransition(roll);
        toRoll.AddCondition(AnimatorConditionMode.If, 0, "Roll");
        toRoll.hasExitTime = false; toRoll.duration = 0.05f; toRoll.canTransitionToSelf = false;
        var fromRoll = roll.AddTransition(loco);
        fromRoll.hasExitTime = true; fromRoll.exitTime = 0.85f; fromRoll.duration = 0.1f;

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
