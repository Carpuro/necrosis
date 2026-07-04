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

**FASE 0 (greybox).** Proyecto de Unity completo y versionado (ProjectSettings/,
Packages/, .meta). Editor instalado en esta máquina: **Unity 6000.5.2f1**.
Input Manager clásico (`Input.GetAxis`/`GetKey`); render pipeline built-in por ahora.
La escena `Assets/_Necrosis/Scenes/Fase0.unity` se genera con
`Fase0SceneBuilder` (menú *Necrosis > Construir escena Fase 0*, o batchmode con
`-executeMethod Fase0SceneBuilder.BuildScene`) — editar el builder, no la escena a mano.
Hito actual: POV + movimiento. Pendiente del builder: Cazadores + NavMesh
(paquete **AI Navigation**, aún no instalado) y clip de estática para el Coro.

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

- C# estándar de Unity; **comentarios, mensajes de commit y docs en español**.
- Un script = una responsabilidad. Nada de god-classes.
- **Todos los valores de gameplay expuestos en el Inspector** (`[SerializeField]` o
  públicos) para tunear sin recompilar. Nunca hardcodear números de tuning.
- Commits pequeños y frecuentes, mensajes en español.
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
