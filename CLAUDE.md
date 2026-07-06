# NECROSIS://PROTOCOLO

Videojuego de supervivencia zombie en Unity (C#), tercera persona sobre el hombro,
desarrollado por un solo dev. Ciudad futurista colapsada; plaga nanotecnológica
**fotovoltaica**: los infectados ("Cazadores") son rápidos y tácticos DE DÍA
(patrullan, flanquean) y de NOCHE se congelan como ESTATUAS con sensores pasivos
(proximidad, ruido, luz). Si algo los activa: FRENESÍ de 15s → agotamiento → estatua.
Al amanecer (6:00): "Marea del Alba" — todas las estatuas despiertan a la vez.

Sistemas clave:
- **Firmas del jugador**: RUIDO (agacharse < caminar < correr) y FIRMA ENERGÉTICA
  (linterna; de noche ×10 — "eres el faro"). Ver `PlayerSignature`.
- **El Coro**: estática de audio que delata a Cazadores activos cercanos
  (pulsos = patrulla, ráfaga = te cazan). De noche: SILENCIO TOTAL. Ver `ChorusAudio`.

Diseño completo en `GDD_v0.1` a `GDD_v0.6` (raíz del repo). Setup de la escena de
prueba en `README_FASE0.md`.

## Estado del proyecto

**FASE 0 (greybox) — ESCENA COMPLETA.** Proyecto de Unity completo y versionado
(ProjectSettings/, Packages/, .meta). Editor instalado: **Unity 6000.5.2f1**.
Input Manager clásico (`Input.GetAxis`/`GetKey`); render pipeline **built-in** (no URP).

La escena `Assets/_Necrosis/Scenes/Fase0.unity` se genera COMPLETA con
`Fase0SceneBuilder` (menú *Necrosis > Construir escena Fase 0*, o batchmode con
`-executeMethod Fase0SceneBuilder.BuildScene`) — **editar el builder, nunca la
escena a mano**. Contiene: suelo 100×100 + 12 bloques greybox, jugador (cápsula
con los 5 componentes + linterna + CameraPivot), cámara al hombro, ciclo día/noche,
NavMesh horneado (`Fase0_NavMesh.asset`, paquete AI Navigation 2.0.13 — viene
bundled con el editor, instala offline), 4 Cazadores rojos (deambulan, sin puntos
de patrulla, `obstacleMask` = Default) y clip de estática placeholder
(`Audio/static_noise.wav`, ruido blanco generado) conectado al Coro.

### Hecho (2026-07-04)
- Revisión de los 8 scripts: sin errores de compilación; 7 bugs lógicos corregidos
  (fase inicial en Awake, congelar en Dusk al arrancar, jitter del flanqueo,
  guardas pathPending, centro del crouch, volumen/pitch inicial del Coro,
  auto-colisión de la cámara). Detalle en el historial de git (commit 8a9aff3).
- Proyecto Unity generado por batchmode; escena fase 0 completa (commit 6dbefc0).
- Todo pusheado a github.com/Carpuro/necrosis (rama main).

### Player locomotion + jump (2026-07-06) — UNCOMMITTED, needs a commit
Big pass on the player controller (all English code/comments now). State machine files:
`PlayerInput`, `PlayerController`, `PlayerAnimatorDriver`, and the editor
`PlayerAnimatorSetup` (builds `PlayerAnimator.controller` — run menu **Necrosis > Setup
animación del Jugador** after any change to it; version stamp logs `Setup v7` in Console).

- **Turns finished**: idle discrete turns (90/180) + moving turns + 180. Turn blend is
  driven by the body's yaw-rate (capped by `maxTurnSpeed`) with a `turnDeadzone`, so a
  turn is a sustained, animated rotation (not an instant snap). Chained/interrupted turns
  serialize through a single-slot queue with a `maxQueuedTurns` cap. Walk turns use
  `..._briefcase` clips (native L+R, correct feet — the old mirror stuttered); run-left is
  the mirrored right with `cycleOffset 0.5` to realign the feet.
  - GOTCHA (cost hours): never reassign `BlendTree.children` — the setter wipes
    FreeformCartesian2D child positions to (0,0) and collapses the blend (all turns die).
    Edit child timeScale/cycleOffset via `SerializedObject` (see `TweakChildren` history)
    or set positions only through `AddChild`.
- **Walk-start step-off**: speed is live-tunable via `walkStartAnimSpeed` (Inspector) →
  animator `WalkStartSpeed` param. NO step-off after a discrete turn (it doubled the motion
  = spasm); the turn flows straight into locomotion.
- **JUMP (new feature, approved by Carlos)**: **Space = jump**; **Ctrl while running = dodge
  roll** (Ctrl otherwise = crouch toggle). Idle: single Space = low jump, double-tap =
  high jump, both in place (no horizontal move). Moving (walk/run): `jump_run` clip,
  launches IMMEDIATELY (no windup), carries momentum. Standing jumps use a windup that
  launches on the clip's takeoff (`jumpTakeoffNormalized`, driven by clip normalizedTime).
  Jump state exits on landing via the `Grounded` bool (not a timer). Clips: 5 wired
  (low/high/forward/backward/run) + 2 spares (`jump_race_obstacle`, `jump_vault_acrobatic`
  — deferred, need obstacle detection). `jump_idle_forward/backwards` are spares now too
  (moving jump uses run clip).

### Pendiente / roadmap
1. **COMMIT the locomotion+jump work** (uncommitted). Include the jump + `..._briefcase`
   clips (+ `.meta`). **Do NOT commit `Materials/ar15.glb`** (weapon asset in the wrong
   folder → belongs in `necrosis-external-assets/`, import only when a weapon system
   exists; the 9-mm pack once crashed the editor UI). `animation_ybot_idle_movement_sprint.fbx`
   is a loose spare — decide before staging.
2. **Play-test tuning** (Inspector, not code): jump feel (`jumpSpeed`, `jumpSpeedHigh`,
   `jumpTakeoffNormalized`, `jumpDoubleTapWindow`), turn feel (`maxTurnSpeed`,
   `turnRateForFullBlend`, `turnDeadzone`), walk-start (`walkStartAnimSpeed`).
3. Original phase-0 goal still stands: cross the map at night and feel TENSION (day
   patrol/flank, 19:00 freeze, night frenzy, Dawn Surge). Tune Inspector values.
4. Borrar la carpeta vacía `Necrosis/` de la lista de Unity Hub si reaparece.
5. Sustituir el clip de estática por uno con carácter (freesound.org) al pulir audio.
6. **Siguiente hito aprobado: melé del jugador (fase 0.5)** — empezar cuando Carlos lo pida.
7. Posibles mejoras futuras del salto: air-loop/land por `Grounded` (hoy el clip one-shot
   puede terminar antes de aterrizar desde alto); vault/obstacle con detección de obstáculos.

## Estructura

```
Assets/_Necrosis/
├── Scripts/
│   ├── Player/   PlayerController · ShoulderCamera · PlayerSignature · PlayerHealth
│   ├── AI/       HunterAI (máquina de estados: Patrol/Investigate/Flank/Attack/Statue/Frenzy/Exhausted)
│   ├── World/    DayNightCycle (singleton: fases, factor solar, evento OnPhaseChanged)
│   ├── Audio/    ChorusAudio (el Coro)
│   └── Debug/    DebugHUD (tecla H)
├── Scenes/  Prefabs/  Materials/  Audio/   (contenido local, mayormente sin versionar aún)
```

Dependencias entre scripts: `HunterAI` y `ChorusAudio` leen `DayNightCycle.Instance`
y los componentes del Player (encontrado por **tag "Player"** — obligatorio en la escena).

## Convenciones

- C# estándar de Unity; **código, comentarios, mensajes de commit y docs en INGLÉS**
  (todo en inglés de aquí en adelante; el historial de git existente se queda como está).
- Un script = una responsabilidad. Nada de god-classes.
- **Todos los valores de gameplay expuestos en el Inspector** (`[SerializeField]` o
  públicos) para tunear sin recompilar. Nunca hardcodear números de tuning.
- Commits pequeños y frecuentes, mensajes en inglés.
- El `.gitignore` de Unity está en la raíz: `Library/`, `Temp/`, `Obj/`, `Logs/`,
  `UserSettings/`, `Build*/` JAMÁS se versionan. Verificarlo antes de cualquier commit
  que toque estructura.

## REGLAS DE ALCANCE (innegociables)

- Estamos en **FASE 0**: greybox con cápsulas y cubos. El ÚNICO objetivo es que el
  ciclo día/noche + el comportamiento de los Cazadores + el Coro generen **TENSIÓN**.
- **NO agregar features nuevas** (inventario, base, melé, modelos, menús, etc.) aunque
  parezcan obvias. Si algo parece faltar: **proponerlo y esperar aprobación**.
- Preferir **corregir y tunear** lo existente antes que reescribir.
- Criterio de éxito de la fase 0: cruzar el mapa de noche sin HUD ni linterna debe dar
  miedo. Si no lo da, se iteran números en el Inspector, no se añade contenido.

## Siguiente objetivo aprobado

Melé del jugador (fase 0.5) — **solo cuando Carlos lo pida explícitamente**.
