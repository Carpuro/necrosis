# GDD v0.3 — Expansión: SOCIEDADES QUE NACEN
## NECROSIS://PROTOCOLO — Evolución de comunidades y sistemas recomendados

*Complementa a v0.1 y v0.2. Expande el §2 de v0.2 (comunidades dinámicas) con evolución civilizatoria a largo plazo.*

---

## 1. Evolución de comunidades: de campamento a sociedad

### 1.1 La idea
En The Walking Dead lo interesante nunca fueron los caminantes: fueron Alexandria volviéndose un pueblo real, el Reino inventándose una monarquía teatral, los Salvadores construyendo un protectorado extorsivo, la Commonwealth restaurando burocracia y clases sociales. Las comunidades no solo crecen en número — **desarrollan identidad, instituciones y patologías**.

Mecánicamente: cada comunidad avanza (o retrocede) por **etapas civilizatorias**, y su vector ideológico (v0.2 §3.1) determina *qué tipo* de sociedad se vuelve en cada etapa.

### 1.2 Etapas civilizatorias
| Etapa | Población típica | Qué la define | Qué desbloquea en la simulación |
|---|---|---|---|
| **1. Grupo** | 3–10 | Sobrevivir hoy. Sin roles fijos | Solo saqueo y huida |
| **2. Campamento** | 10–25 | Base fija, primeras reglas ("aquí no se roba") | Defensas, guardias, primeras normas |
| **3. Asentamiento** | 25–60 | Producción propia (cultivos, taller), roles especializados | Comercio estable, ley escrita, líder formal |
| **4. Colonia** | 60–150 | Instituciones: consejo/caudillo, milicia, escuela, "moneda" local | Diplomacia compleja, puestos avanzados, tributos |
| **5. Proto-estado** | 150+ | Múltiples asentamientos federados, identidad cultural fuerte | Guerras organizadas, rutas comerciales protegidas, expansionismo |

**Ascender requiere:** población + recursos sostenidos + moral + *tiempo sin catástrofes*. **Descender es fácil:** una masacre, un cisma o la muerte del líder pueden tirar una Colonia a Campamento en una semana. La historia del mundo es comunidades subiendo y cayendo por esta escalera.

### 1.3 Identidad emergente: qué sociedad se vuelven
Al cruzar cada etapa, el vector ideológico "cristaliza" en instituciones. Mismos ejes, resultados muy distintos:

- **Método = saqueo + Autoridad = caudillismo** → al llegar a Colonia se vuelve un **protectorado extorsivo** (arquetipo Salvadores): exige tributo a comunidades menores a cambio de "protección". Genera un mapa de vasallos, y misiones emergentes de resistencia.
- **Apertura alta + Método = comercio** → **ciudad franca** (arquetipo Hilltop/Alexandria): mercado regional, neutralidad activa, caravanas que conectan el mapa.
- **Nano-postura = tolerancia extrema + moral baja sostenida** → deriva a **culto de la Plaga** (arquetipo Susurradores adaptado): humanos que veneran el enjambre, se untan con nanogel de infectados para camuflar su firma y *conviven cerca de las manadas*. Son la única "interfaz" entre humanos y manadas — y la más perturbadora facción del juego: humanos con inteligencia plena e intenciones hostiles que los sistemas de detección de manadas ignoran.
- **Relación ÁTRIA = restauración + Autoridad = institucional** → **tecnocracia** (arquetipo Commonwealth): restauran distritos enteros con torretas y luz… y clases sociales, papeleo y expulsiones de "no productivos".
- **Autoridad = horizontal + Apertura media** → **comuna** frágil pero de moral altísima; vulnerable a caudillos externos e infiltración.

Ninguna es "la facción buena". Cada cristalización resuelve un problema de supervivencia creando uno moral.

### 1.4 Líderes con arco (capa TWD sobre el sistema némesis)
Los Alfas de manada ya tienen memoria (v0.2 §4.4); los **líderes humanos merecen lo mismo**:
- Cada líder es un NPC generado con rasgos, historia previa (de la simulación de 30 días pre-día-1) y **presión narrativa**: sus rasgos evolucionan con los eventos. Un líder benevolente que sufre dos masacres puede endurecerse hasta la tiranía; el juego registra *por qué*.
- **Sucesión:** cuando un líder muere o cae, la transición depende de la Autoridad de su comunidad: elección, golpe, fragmentación. Las sucesiones son los momentos de mayor inestabilidad del mapa — y de mayor oportunidad para el jugador.
- El jugador puede **influir**: apoyar facciones internas de una comunidad ajena, asilar a un rival del líder, o asesinar (con consecuencias de reputación en cascada).

### 1.5 Cultura visible (para que la evolución se *sienta*)
La etapa e identidad deben leerse sin abrir menús:
- **Arquitectura:** un Campamento son lonas y láminas; una Colonia caudillista exhibe jaulas y trofeos; una tecnocracia enciende alumbrado público.
- **Ritual y lenguaje:** saludos, banderas generadas (símbolo + colores derivados del vector), festividades (día de la fundación), duelos públicos, ejecuciones.
- **Economía visible:** qué moneda improvisada usan (células de energía, cápsulas de nanogel, fichas selladas por el líder) dice quién manda de verdad.

---

## 2. Recomendaciones adicionales (lo que yo añadiría)

Ordenadas por impacto/costo. Las tres primeras las considero casi obligatorias para este diseño.

### 2.1 El Director (narrador IA, estilo RimWorld) — impacto enorme, costo medio
Un sistema que **observa** la partida (tensión reciente, pérdidas, prosperidad) y **modula el ritmo**: tras una tragedia deja respirar; tras dos semanas plácidas empuja presión (una manada migra hacia ti, sequía, un líder vecino se radicaliza). Sin Director, los mundos sistémicos producen o aburrimiento o injusticia constante. Es la pieza que convierte simulación en drama con ritmo de TWD.

### 2.2 Sistema de rumores e información imperfecta — impacto alto, costo bajo
El jugador no debe ver el estado del mundo en un mapa omnisciente. Todo llega por **radio, comerciantes, desertores y exploración**, con información desactualizada, incompleta o falsa. "Dicen que el Reino del Canal cayó" — ¿vas a verificarlo, a saquear, a rescatar? La niebla informativa convierte la simulación LOD 2 en misterio jugable y además **disimula los límites de la simulación** (truco de diseño: lo que no se ve con precisión no necesita simularse con precisión).

### 2.3 Legado generacional — impacto alto, costo medio
Con permadeath y comunidades que evolucionan durante años de juego, el paso natural: al morir tu personaje no solo cambias a otro — **el tiempo puede saltar** (opcional, semanas o meses) y retomas con la siguiente "generación": la comunidad cambió, los niños crecieron, las facciones se movieron. Refuerza la fantasía TWD de saga larga y le da propósito a construir instituciones que te sobrevivan.

### 2.4 Estaciones y clima con dientes — impacto medio, costo bajo-medio
Invierno = menos cultivos, más consumo de energía (calefacción = firma energética = manadas), migraciones de manadas hacia zonas cálidas/con energía. El clima como motor de conflicto estacional predecible: el mundo entero se prepara para el invierno, como en TWD las comunidades negocian cosechas.

### 2.5 Dilemas de gobierno interno — impacto medio, costo bajo
Cuando TU comunidad llega a Asentamiento/Colonia, te tocan las decisiones que definen ideología: ¿ley escrita o tu palabra? ¿racionamiento igualitario o por productividad? ¿juicio o exilio para el que robó? Cada decisión mueve tu vector, afecta moral por-NPC (según sus rasgos) y define qué cristalización te espera. El jugador debe poder convertirse, por deriva, en el villano de otra comunidad.

### 2.6 Registro del mundo ("crónica") — impacto medio, costo bajo
Un log narrado de eventos mundiales, estilo Dwarf Fortress: "Día 214: cayó la Comuna del Invernadero; sus sobrevivientes fundaron Los Cenizos". Barato de construir, multiplica la percepción de mundo vivo, y es oro para que los jugadores compartan historias (marketing orgánico gratuito).

### 2.7 Qué NO añadiría (igual de importante)
- **Multijugador:** con esta simulación, sincronizarla en red es otro juego entero. Ni lo planees para v1.
- **Vehículos complejos, mascotas, construcción libre estilo Fortnite:** ruido de alcance; ninguno sirve a los pilares.
- **Árbol de diálogo profundo por NPC:** con cientos de NPCs procedurales, la conversación debe ser sistémica (intercambios, rumores, peticiones generadas), no escrita a mano.

---

## 3. Ajuste final de prioridades

El diseño ya está completo a nivel visión. Congelaría aquí el documento y aplicaría esta regla de aquí en adelante:

> **Nada entra al GDD sin salir algo de la misma fase.** El enemigo del proyecto ya no son las ideas — es el alcance.

Orden de construcción recomendado (sin cambios a las fases de v0.2, solo dónde encaja lo nuevo):
- **Fase 1:** dilemas de gobierno básicos (2.5) y crónica (2.6) — baratos, dan vida temprano.
- **Fase 2:** Director v1 (2.1) y rumores (2.2) junto con la simulación mundial.
- **Fase 3:** etapas civilizatorias completas (§1) y estaciones (2.4).
- **Post-lanzamiento:** legado generacional (2.3), cristalizaciones exóticas (culto de la Plaga).
