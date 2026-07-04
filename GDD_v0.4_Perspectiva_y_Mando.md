# GDD v0.4 — Expansión: PERSPECTIVA Y MANDO
## NECROSIS://PROTOCOLO — Primera persona y gestión de base en tres capas

*Complementa v0.1–v0.3. Reemplaza el §5 de v0.1 (interfaz y presentación).*

---

## 1. Perspectiva: primera persona

- **Cámara:** primera persona en todo el juego (exploración, combate, base). Sin tercera persona conmutable.
- **Estética:** low-poly estilizado, "futurismo oxidado". Prioriza silueta, color y luz sobre detalle de textura — más barato, envejece mejor, y las siluetas legibles son críticas para identificar tipos de infectado a distancia.
- **UI diegética:** biomonitor de muñeca (vitales, saturación nano, radio), dron personal (linterna, escaneo, marcador). El HUD flotante se reserva para lo indispensable.
- **Audio como mecánica central:** audio 3D posicional; el "coro" (estática de coordinación de manadas) es el radar del jugador. Diseñar el juego para poder jugarse con los ojos cerrados a 5 metros de distancia — exageración útil como norte de diseño.
- **Consecuencia de diseño en enemigos:** grupos de 5–15 infectados inteligentes en vez de mareas de 100. La amenaza escala por conducta (flanqueo, asedio, cebos), no por número en pantalla. Las "hordas" gigantes existen en la simulación mundial, pero se encuentran como eventos evitables (las oyes/ves a distancia), no como combate estándar.

---

## 2. Gestión de base: sistema de tres capas

Principio rector: **cada capa tiene un propósito exclusivo y las tres cuentan la misma verdad.** Nada de duplicar funciones — cada acción de gestión vive en la capa donde mejor se siente.

### Capa A — A pie (inmersiva): *las personas*
En primera persona, dentro o fuera de la base:
- **Conversar con sobrevivientes:** estado, quejas, peticiones, rumores, relaciones. La información social SOLO existe aquí — para saber que dos miembros se odian, hay que convivir, no leer una barra.
- **Delegar en persona:** pedirle a alguien que te acompañe, que descanse, que entrene a otro.
- **Inspección física:** las instalaciones muestran su estado visualmente (el purificador chispea, la hidroponia amarillea). Reparar, cosechar o recargar son interacciones de mundo.
- **Eventos íntimos:** duelos, disputas, confesiones (alguien oculta su % de saturación) ocurren como escenas emergentes a pie, no como popups.

### Capa B — Panel de mando (estilo State of Decay): *los números*
UI de gestión clásica: instalaciones por ranura, cola de construcción/mejora, inventario global, roster con habilidades y estados, resumen de recursos y consumo energético diario, radio de la comunidad.
- **Acceso:** desde la **mesa de mando** de la base (objeto físico, mejorable) y, en versión reducida de *solo lectura + radio*, desde el biomonitor de muñeca en cualquier lugar. Decidir se decide en casa; enterarse, en cualquier parte. Esto hace que volver a la base importe.
- **Qué vive SOLO aquí:** decisiones de comunidad (dilemas de gobierno del v0.3 §2.5), prioridades de trabajo, comercio por radio con enclaves, elección de qué construir/mejorar.
- El panel usa el lenguaje diegético de ÁTRIA (terminal municipal reutilizada): misma ficción, cero menús "de videojuego" flotando en la nada.

### Capa C — Cámara táctica (solo dentro del perímetro): *el espacio*
Al activarla desde la mesa de mando (o atajo estando dentro del perímetro), la cámara asciende a vista isométrica limitada al territorio reclamado:
- **Qué vive SOLO aquí:** colocación física de construcciones y defensas, zonas (almacén, cultivo, prohibida), rutas de patrulla y puestos de guardia, cableado de la red eléctrica interna (qué instalación se conecta a qué fuente — el minijuego de firma energética a nivel base).
- **Reglas de tensión:** el tiempo NO se pausa en táctica; si suena la alarma de perímetro, la cámara marca la brecha y te devuelve a primera persona en tu cuerpo, donde sea que esté. Planificar es un lujo de la paz.
- **Límites duros:** no se controla a nadie como RTS — se asignan órdenes de zona/ruta y los NPCs las interpretan con su IA y sus rasgos. No hay visión táctica fuera del perímetro, nunca (la niebla informativa del v0.3 §2.2 se respeta).
- Justificación diegética: la vista táctica es el feed de los sensores/cámaras del perímetro + tu dron. Si la red de sensores se daña, la vista táctica tiene huecos literales — defender los sensores se vuelve mecánica.

### 2.1 Tabla resumen: dónde vive cada acción
| Acción | A pie | Panel | Táctica |
|---|---|---|---|
| Conocer estado emocional/social de un NPC | ✅ | resumen frío | — |
| Asignar prioridades de trabajo | — | ✅ | — |
| Elegir qué construir | — | ✅ (cola) | — |
| Colocar DÓNDE se construye | — | — | ✅ |
| Rutas de patrulla y zonas | — | — | ✅ |
| Reparar/cosechar/recargar | ✅ | — | — |
| Dilemas de gobierno | — | ✅ | — |
| Comercio por radio | — | ✅ | — |
| Ver consumo/red energética | lectura en muñeca | ✅ global | ✅ espacial |

---

## 3. Flujo de un día tipo (validación del diseño)
1. Despiertas (FP): el purificador chispea — lo reparas a mano; un NPC te confiesa a solas que su hermana lleva días rara (pista de saturación oculta).
2. Mesa de mando: revisas recursos, aceptas un intercambio por radio, encolas la mejora de la enfermería, resuelves un dilema (racionamiento).
3. Cámara táctica: reubicas una patrulla hacia el muro este (anoche el coro sonó por ahí) y reconectas la hidroponia al panel solar para apagar el generador ruidoso.
4. Sales de expedición (FP pura): el resto del día es Zomboid/STALKER.
5. Vuelves al anochecer: alarma — en táctica ves la brecha al noreste, el sistema te devuelve a FP y corres a defender con lo que tengas encima.

Si este bucle se siente bien, el sistema de tres capas está justificado. Si alguna capa se usa menos de una vez por día de juego, sobra o está mal alimentada.

---

## 4. Notas técnicas (Godot 4)
- **Cámara táctica:** misma escena, segunda cámara ortogonal con culling de techos/pisos superiores dentro del perímetro. No es un modo aparte: los NPCs e infectados siguen simulando idéntico.
- **Techos y cortes:** resolver desde el día 1 de la fase de base el corte de plantas superiores en vista táctica (shader de sección o visibilidad por piso). Es el problema técnico feo de esta feature; atacarlo temprano.
- **Panel de mando:** UI de Godot (Control nodes) proyectada en un viewport sobre la malla de la mesa = pantalla diegética real, barata.
- **Input:** perfiles de input separados por capa desde el inicio (FP / panel / táctica) para evitar el infierno de conflictos de bindings después.

## 5. Ajuste al roadmap
- **Fase 0 (sin cambios):** FP puro — movimiento, audio posicional, melé que se sienta bien, un Cazador que flanquea. Nada de base aún.
- **Fase 1:** + base reclamable con mesa de mando (panel v1: roster + cola de construcción) y cámara táctica v1 (solo colocar 3 tipos de estructura y 1 ruta de patrulla).
- **Fase 2+:** capas completas según v0.2/v0.3.
