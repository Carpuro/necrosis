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
        // Briefcase walk-turn clips (correct FEET/body, native L+R). Used for the walk
        // turns for now; the arms carry a briefcase — arm cleanup deferred.
        (AnimDir + "/locomotion/animation_ybot_movement_walk_turn_left_briefcase.fbx",  true),
        (AnimDir + "/locomotion/animation_ybot_movement_walk_turn_right_briefcase.fbx", true),
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
        // Jump clips (one-shot). 0 neutral · 1 forward · 2 backward · 3 running. low = spare.
        (AnimDir + "/locomotion/animation_ybot_idle_movement_jump_idle_high.fbx",      false),
        (AnimDir + "/locomotion/animation_ybot_idle_movement_jump_idle_forward.fbx",   false),
        (AnimDir + "/locomotion/animation_ybot_idle_movement_jump_idle_backwards.fbx", false),
        (AnimDir + "/locomotion/animation_ybot_run_movement_jump_run.fbx",             false),
        (AnimDir + "/locomotion/animation_ybot_idle_movement_jump_idle_low.fbx",       false),
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

    // Mirroring a looping gait flips its footfall half a cycle out of phase, so the
    // mirrored run-left stutters against run_straight (the native right is clean).
    // cycleOffset 0.5 shifts the loop start half a cycle to realign the feet. This is
    // only valid because the run clips share run_straight's cycle length (a fixed
    // offset on mismatched lengths drifts and pops — that's why the old walk-turn
    // offset attempt failed; walk turns now use native clips instead).
    static readonly (string file, float offset)[] CycleOffsetClips =
    {
        ("animation_ybot_movement_run_turn_left.fbx", 0.5f),
    };

    const string YBotModel = PlayerDir + "/Models/model_y_bot_tpose.fbx";

    [MenuItem("Necrosis/Setup animación del Jugador")]
    public static void Run()
    {
        // 0) Import any newly-dropped .fbx first so SetHumanoid never skips a clip that
        //    Unity hasn't imported yet. Makes a single rebuild self-sufficient (no need
        //    to focus the editor first).
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

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
        // Version stamp: bump when the controller layout changes, so a stale build is
        // obvious in the Console (run-before-compile-finished has bitten us already).
        Debug.Log("[NECROSIS] Setup v7 (jump exits on landing via Grounded) completado."); // controller unchanged since v7
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
        if (imp == null)
        {
            // Not imported yet (just dropped in) — force a synchronous import and retry so
            // the rebuild handles new clips without needing the editor focused first.
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            imp = AssetImporter.GetAtPath(path) as ModelImporter;
        }
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
        float cycleOffset = 0f;
        foreach (var c in CycleOffsetClips) if (path.EndsWith(c.file)) cycleOffset = c.offset;
        if (loop.HasValue || mirror || cycleOffset != 0f)
        {
            var clips = imp.defaultClipAnimations;
            for (int i = 0; i < clips.Length; i++)
            {
                if (loop.HasValue) clips[i].loopTime = loop.Value;
                if (mirror) clips[i].mirror = true; // right turn = mirrored left (and vice versa)
                if (cycleOffset != 0f) clips[i].cycleOffset = cycleOffset; // realign mirrored feet
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
        controller.AddParameter("TurnInPlaceSpeed", AnimatorControllerParameterType.Float); // multiplicador (90=2, 180=4)
        controller.AddParameter("Aiming", AnimatorControllerParameterType.Bool);
        controller.AddParameter("StrafeLock", AnimatorControllerParameterType.Bool); // strafe libre (Left Alt)
        controller.AddParameter("Roll", AnimatorControllerParameterType.Trigger);    // esquiva (Ctrl al correr)
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);    // salto (Espacio)
        controller.AddParameter("JumpType", AnimatorControllerParameterType.Float);  // 0 low/1 high/2 fwd/3 back/4 run
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);   // en el suelo (sale del salto al aterrizar)
        controller.AddParameter("StartWalk", AnimatorControllerParameterType.Trigger); // arranque idle->caminar
        controller.AddParameter("WalkStartSpeed", AnimatorControllerParameterType.Float); // playback speed of the step-off (tunable)
        controller.AddParameter("Turn180", AnimatorControllerParameterType.Trigger);   // giro 180 al invertir
        controller.AddParameter("Turn180Dir", AnimatorControllerParameterType.Float);  // -1 izq / +1 der
        controller.AddParameter("Turn180Tier", AnimatorControllerParameterType.Float); // 0 idle / 1 caminar / 2 correr
        controller.AddParameter("AimStance", AnimatorControllerParameterType.Int); // 0 puños,1 melé,2 arma
        controller.AddParameter("AimX", AnimatorControllerParameterType.Float); // strafe -1..+1
        controller.AddParameter("AimY", AnimatorControllerParameterType.Float); // atrás/adelante -1..+1

        var sm = controller.layers[0].stateMachine;

        // Shared locomotion clips (idle + walkS also feed TurnInPlace and strafe below).
        var idle    = LoadClip(AnimDir + "/locomotion/animation_ybot_idle.fbx");
        var walkS   = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_walk_straight.fbx");
        var runS    = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_run_straight.fbx");
        var runL    = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_run_turn_left.fbx");
        var runR    = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_run_turn_right.fbx");
        var runF    = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_sprint_straight.fbx");
        // Walk turns — KNOWN-GOOD combo: left = the plain walk_turn_left clip (looked
        // right in play), right = the NATIVE briefcase right clip (fills the hole the
        // bad mirrored right left behind; carries a briefcase in hand — arm cleanup
        // deferred). No timeScale/cycleOffset tricks: playing the turn clips at their
        // natural speed is what looked correct in play.
        var walkL = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_walk_turn_left.fbx");
        var walkR = LoadClip(AnimDir + "/locomotion/animation_ybot_movement_walk_turn_right_briefcase.fbx");

        // Locomotion 2D blend (X=Turn, Y=Speed). idle->walk->run + walk/run turns.
        var loco = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
        FillLocoTree(tree, idle, walkS, walkL, walkR, runS, runL, runR, runF);
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
        // Playback speed driven by the WalkStartSpeed param so it's tunable live from the
        // PlayerController Inspector (walkStartAnimSpeed) — 8x looked like a spasm.
        walkStart.speed = 1f;
        walkStart.speedParameterActive = true;
        walkStart.speedParameter = "WalkStartSpeed";
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
        fromTurn180.hasExitTime = true; fromTurn180.exitTime = 0.92f; fromTurn180.duration = 0.1f;

        // Discrete start turn (from idle): 1D blend by TurnInPlace select value:
        // -2 180-left · -1 90-left · 0 idle · +1 90-right · +2 180-right.
        // Entered from locomotion while TurningInPlace; exits when it clears.
        var turnIP = controller.CreateBlendTreeInController("TurnInPlace", out BlendTree tip, 0);
        // Playback speed driven by TurnInPlaceSpeed param (90=2x, 180=4x) so the 180
        // is twice as fast as the 90. Rotation follows normalizedTime, stays synced.
        turnIP.speed = 1f;
        turnIP.speedParameterActive = true;
        turnIP.speedParameter = "TurnInPlaceSpeed";
        tip.blendType = BlendTreeType.Simple1D;
        tip.blendParameter = "TurnInPlace";
        tip.useAutomaticThresholds = false;
        tip.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_idle_movement_turn_left_180.fbx"),  -2f);
        tip.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_idle_movement_turn_left_90.fbx"),   -1f);
        tip.AddChild(idle, 0f);
        tip.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_idle_movement_turn_right_90.fbx"),   1f);
        tip.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_idle_movement_turn_right_180.fbx"),  2f);

        // Enter from AnyState (not just Locomotion) so an in-place turn can interrupt
        // the WalkStart step-off too. If it could only be reached from Locomotion, a
        // quick forward-then-reverse (W→S) left the animator stuck in WalkStart playing
        // the whole step-off clip while the body tried to turn — looked like sliding
        // forward instead of pivoting. canTransitionToSelf=false so a chained turn
        // doesn't re-enter here (RestartTurnInPlace handles the clean replay).
        var toTurnIP = sm.AddAnyStateTransition(turnIP);
        toTurnIP.AddCondition(AnimatorConditionMode.If, 0, "TurningInPlace");
        toTurnIP.hasExitTime = false; toTurnIP.duration = 0.1f; toTurnIP.canTransitionToSelf = false;
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

        // Salto (Space): 1D blend by JumpType picks the clip (0 low/1 high/2 fwd/3 back/4 run).
        // AnyState → Jump on the Jump trigger; the clip plays as a one-shot while gravity
        // does the arc, then exits back to locomotion. (Uses the same 1D-select pattern as
        // TurnInPlace; useAutomaticThresholds off so JumpType 0..4 map to the real clips.)
        var jump = controller.CreateBlendTreeInController("Jump", out BlendTree jtree, 0);
        jtree.blendType = BlendTreeType.Simple1D;
        jtree.blendParameter = "JumpType";
        jtree.useAutomaticThresholds = false;
        jtree.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_idle_movement_jump_idle_low.fbx"),       0f);
        jtree.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_idle_movement_jump_idle_high.fbx"),      1f);
        jtree.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_idle_movement_jump_idle_forward.fbx"),   2f);
        jtree.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_idle_movement_jump_idle_backwards.fbx"), 3f);
        jtree.AddChild(LoadClip(AnimDir + "/locomotion/animation_ybot_run_movement_jump_run.fbx"),             4f);
        var toJump = sm.AddAnyStateTransition(jump);
        toJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
        toJump.hasExitTime = false; toJump.duration = 0.08f; toJump.canTransitionToSelf = false;
        // Exit the jump the moment we LAND (Grounded true), not on a fixed timer — otherwise
        // the clip keeps playing after touchdown (or gets cut mid-air). Grounded is false
        // from takeoff until landing, so this only fires when we're actually back on ground.
        var fromJump = jump.AddTransition(loco);
        fromJump.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        fromJump.hasExitTime = false; fromJump.duration = 0.12f;

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

    /// <summary>Fills a locomotion 2D blend (X=Turn, Y=Speed) with the shared clips and
    /// the given walk-turn clips. IMPORTANT: only AddChild (which sets each child's 2D
    /// position) is used — do NOT read/modify/reassign tree.children afterward, because
    /// the BlendTree.children setter wipes FreeformCartesian2D positions back to (0,0),
    /// collapsing the whole blend so no walk/run turn cell is reachable.</summary>
    static void FillLocoTree(BlendTree tree, AnimationClip idle, AnimationClip walkS,
                             Motion turnL, Motion turnR,
                             AnimationClip runS, AnimationClip runL, AnimationClip runR, AnimationClip runF)
    {
        tree.blendType = BlendTreeType.FreeformCartesian2D;
        tree.blendParameter = "Turn";    // X
        tree.blendParameterY = "Speed";  // Y
        tree.AddChild(idle,  new Vector2( 0f, 0f));
        tree.AddChild(walkS, new Vector2( 0f, 2.5f)); // walk (C off) = walkSpeed
        tree.AddChild(turnL, new Vector2(-1f, 2.5f)); // walk turn left
        tree.AddChild(turnR, new Vector2( 1f, 2.5f)); // walk turn right
        tree.AddChild(runS,  new Vector2( 0f, 6.5f)); // run (C on)
        tree.AddChild(runL,  new Vector2(-1f, 6.5f)); // run turn left
        tree.AddChild(runR,  new Vector2( 1f, 6.5f)); // run turn right
        tree.AddChild(runF,  new Vector2( 0f, 8f));   // sprint (Shift)
        tree.AddChild(runL,  new Vector2(-1f, 8f));   // sprint turn left
        tree.AddChild(runR,  new Vector2( 1f, 8f));   // sprint turn right
    }


}
