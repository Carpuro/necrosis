# NECROSIS://PROTOCOLO — Fase 0
## Paquete de arranque para Unity

Este paquete contiene el `.gitignore`, la estructura de carpetas y los 7 scripts C#
del prototipo fase 0: **tercera persona sobre el hombro + ciclo día/noche invertido +
Cazador que flanquea de día y se congela de noche + el Coro.**

---

## 1. Instalación

1. **Versión de Unity:** Unity 6 (6000.x) o 2022.3 LTS. Plantilla **3D (URP)** recomendada.
2. Copia la carpeta `Assets/_Necrosis/` dentro del `Assets/` de tu proyecto.
3. Copia el `.gitignore` a la **raíz del repo** (junto a `Assets/`) ANTES del primer commit.
4. Instala el paquete de navegación: `Window > Package Manager > Unity Registry > AI Navigation` (instalar).

## 2. Montar la escena de prueba (15 minutos)

### El mundo (greybox)
1. Escena nueva → guárdala en `_Necrosis/Scenes/Fase0.unity`.
2. Crea un **Plane** grande (escala 10,1,10) como suelo.
3. Reparte **Cubos** como edificios/muros (que haya esquinas para que el flanqueo se note).
4. **NavMesh:** selecciona suelo y cubos → marca `Static` (arriba a la derecha del Inspector).
   Luego `Window > AI > Navigation > Bake` → **Bake**.

### El ciclo día/noche
5. GameObject vacío `DayNightCycle` → añade el script `DayNightCycle`.
6. Arrastra la **Directional Light** de la escena al campo `Sun Light`.
7. (Opcional pero recomendado) `Window > Rendering > Lighting > Environment`:
   baja el ambient a casi negro para que la noche sea NOCHE.

### El jugador
8. **Capsule** → renómbrala `Player` → asígnale el **tag "Player"** (importante).
9. Quítale el Capsule Collider y añade: `CharacterController`, `PlayerController`,
   `PlayerSignature`, `PlayerHealth`, `ChorusAudio`.
10. Hijo vacío `CameraPivot` en posición local (0, 1.6, 0).
11. Hijo **Spot Light** `Flashlight` (apuntando al frente, rango ~15, ángulo ~50)
    → arrástralo al campo `Flashlight` de `PlayerSignature`.
12. `AudioSource` (lo pide ChorusAudio): loop ✔, playOnAwake ✔, y asígnale
    **cualquier clip de ruido blanco/estática** (uno gratis: busca "static noise" en freesound.org).
13. **Main Camera:** añade `ShoulderCamera` → arrastra `CameraPivot` al campo `Target`.
    Arrastra la Main Camera al campo `Camera Transform` de `PlayerController`.
14. `DebugHUD` en el Player (o cualquier objeto).

### El Cazador
15. **Capsule** → renómbrala `Hunter` → material rojo para distinguirla.
16. Añade `NavMeshAgent` + `HunterAI`.
17. En `HunterAI`, configura `Obstacle Mask` = **Default** (para que los muros bloqueen su visión).
18. (Opcional) Crea 3–4 vacíos como puntos de patrulla y arrástralos a `Patrol Points`.
19. Duplica el Hunter 3–4 veces y repártelos por el mapa.

### Play ▶
- Empieza a las 9:00 (día): los Cazadores patrullan; si te ven u oyen, la mayoría
  **flanquea** en vez de cargar de frente. Corre y haz ruido para comprobarlo.
- Espera al atardecer (19:00) con `Day Length Minutes = 20` (o súbelo a las 18h
  en el inspector para no esperar): **se congelan donde estén.**
- De noche: acércate a una estatua, o peor, **enciende la linterna (F)** cerca de una.
  Frenesí de 15 segundos. Buena suerte.
- A las 6:00: **Marea del Alba.** Si estás a la intemperie, corre.

## 3. Controles
| Tecla | Acción |
|---|---|
| WASD | Mover |
| Shift | Correr (ruido: 14m) |
| Ctrl | Agacharse (ruido: 1.5m) |
| F | Linterna (de noche: firma ×10) |
| Mouse | Cámara |
| H | Mostrar/ocultar HUD debug |

## 4. El test que importa (criterio de éxito de la fase 0)
Apaga el `DebugHUD` (tecla H) y cruza el mapa de noche, sin linterna,
entre las estatuas, guiándote solo por lo que ves y oyes.

**Si sientes tensión física — la fase 0 está cumplida y el juego existe.**
Si no: itera números (velocidades, radios, duración del frenesí) antes de añadir
NADA nuevo. Todos los valores están expuestos en el Inspector para tunear en caliente.

## 5. Qué NO tiene esto (a propósito)
Sin modelos, sin animaciones, sin melé del jugador, sin base, sin inventario.
Cápsulas y cubos. La fase 0 valida UNA cosa: que el comportamiento de los
Cazadores + el ciclo invertido + el Coro generan miedo. Lo demás viene después
(ver GDD v0.1–v0.6).

## 6. Estructura
```
Assets/_Necrosis/
├── Scripts/
│   ├── Player/    PlayerController · ShoulderCamera · PlayerSignature · PlayerHealth
│   ├── AI/        HunterAI (máquina de estados completa)
│   ├── World/     DayNightCycle (fases + factor solar + marea del alba)
│   ├── Audio/     ChorusAudio (el Coro: tu radar que muere de noche)
│   └── Debug/     DebugHUD
├── Scenes/  Prefabs/  Materials/  Audio/   (vacías, para tu contenido)
```

## 7. Siguientes pasos (fase 0.5 → 1)
1. Melé del jugador (el problema difícil que pospusimos — atacarlo ya con cápsulas).
2. Assets provisionales: Mixamo (animaciones) + Quaternius/Kenney (modelos low-poly).
3. Primer edificio interior con NavMesh (el flanqueo en interiores es otra bestia).
4. Solo entonces: mesa de mando y base (GDD v0.4).
