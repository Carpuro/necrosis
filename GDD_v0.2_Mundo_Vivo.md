# GDD v0.2 — Expansión: EL MUNDO VIVO
## NECROSIS://PROTOCOLO — Módulo de simulación mundial

*Complementa al GDD v0.1. Estos sistemas reemplazan y expanden las secciones 2.4, 3.2 y 3.4 del documento original.*

---

## 1. Filosofía del cambio

El mundo deja de ser un escenario estático que el jugador saquea, y se convierte en un **ecosistema de agentes en competencia**: comunidades humanas que crecen, se dividen y colapsan; facciones que emergen de abajo hacia arriba; y manadas de infectados inteligentes que también compiten por territorio y recursos — entre sí y, sobre todo, contra los humanos.

**Nueva regla de oro del diseño:** todo lo que le puede pasar al jugador le puede pasar a cualquier comunidad NPC. Si a ti te puede asediar una manada, a ellos también. Si tú puedes crecer y reclutar, ellos también. El jugador no es especial para la simulación.

---

## 2. Comunidades dinámicas y crecientes

### 2.1 Ciclo de vida de un asentamiento
Cada comunidad NPC (incluida la tuya) se simula con los mismos parámetros:

| Parámetro | Qué representa |
|---|---|
| Población | Miembros vivos, con roles abstractos (combatiente, técnico, médico, productor) |
| Recursos | Comida, energía, medicina, materiales (stocks abstractos) |
| Moral | Cohesión interna; baja moral → deserciones, cismas |
| Seguridad | Defensas vs. presión de infectados/hostiles de la zona |
| Ideología | Vector de valores (ver facciones, §3) |
| Nano-política | Postura ante infectados en fase temprana: expulsar / tratar / ocultar |

**Estados posibles:** naciente → estable → próspera → en crisis → colapsada / absorbida / migrada.

### 2.2 Eventos de comunidad (simulados aunque el jugador no esté presente)
- **Crecimiento:** una comunidad próspera atrae refugiados, funda un puesto avanzado, o coloniza un edificio vecino (su territorio en el mapa crece físicamente).
- **Cisma:** si la moral cae o hay conflicto ideológico interno, la comunidad se divide: un grupo se va y funda un asentamiento nuevo — así nacen facciones nuevas orgánicamente.
- **Migración:** ante presión de manadas o falta de recursos, la comunidad entera se muda. El jugador puede encontrar su antigua base saqueable… o encontrársela en ruta, vulnerable.
- **Colapso:** una comunidad arrasada deja ruinas, loot, supervivientes dispersos reclutables… y a veces, sus miembros convertidos en una manada nueva de infectados que "recuerda" su antiguo hogar.

### 2.3 Simulación por niveles de detalle (clave técnica)
- **LOD 0 (cerca del jugador):** NPCs individuales con rutinas visibles.
- **LOD 1 (distrito activo):** comunidades como agentes con ticks por hora de juego.
- **LOD 2 (resto del mundo):** ticks diarios, resolución estadística de conflictos (ataque de manada vs. seguridad = resultado probabilístico + narración en el registro mundial).

El jugador se entera del mundo por **radio, rumores de comerciantes y exploración**: llegar a un enclave y encontrarlo destruido cuenta una historia sin scripts.

---

## 3. Sistema de facciones orgánico

### 3.1 Principio: las facciones no se escriben, emergen
No hay "la facción militar" y "los bandidos" predefinidos. Cada comunidad tiene un **vector ideológico** con ejes continuos:

- **Apertura** (acogen forasteros ↔ aislacionistas)
- **Método** (comercian ↔ saquean)
- **Nano-postura** (purgan infectados ↔ toleran/experimentan)
- **Relación con ÁTRIA** (tecno-restauradores ↔ neo-luditas que destruyen nodos)
- **Autoridad** (horizontal ↔ caudillismo)

Las etiquetas nacen del vector: una comunidad con saqueo alto + caudillismo alto será percibida como "banda"; dos comunidades con vectores cercanos y frontera común tienden a **federarse** (nace una facción multi-asentamiento con nombre generado).

### 3.2 Dinámica entre facciones
- **Reputación por facción y por comunidad** (puedes estar bien con un asentamiento y mal con su federación).
- **Casus belli sistémicos:** disputas por un recurso escaso en la frontera, robo, asilo a desertores del otro, diferencias de nano-postura (los purgadores atacan a quienes albergan infectados tempranos).
- **Guerras de baja intensidad:** redadas, bloqueo de rutas comerciales, sabotaje de nodos ÁTRIA del rival. Las guerras se resuelven en LOD 2 pero generan misiones/oportunidades en LOD 0 cuando el jugador está cerca.
- **El jugador como actor faccional:** tu comunidad tiene su propio vector (definido por tus decisiones acumuladas, no por un menú). Expulsar infectados tres veces te mueve hacia "purgador" y las facciones reaccionan a eso.

### 3.3 Inspiraciones directas
Kenshi (facciones con territorio y economía real), RimWorld (ideología como mecánica), Shadow of Mordor (nemesis: líderes con memoria de sus encuentros contigo — ver §4.4), Dwarf Fortress (historia mundial generada).

---

## 4. Los infectados: rediseño tipo *Crossed*

### 4.1 Nuevo lore de la cepa NCR-0
Revisión clave: las nanomáquinas ya **no** operan cadáveres. Ahora **secuestran cerebros vivos**: suprimen empatía, miedo e inhibición, y sobreescriben la motivación con un imperativo doble — *replicar el enjambre* y *eliminar biomasa no asimilada* (humanos). El resultado: los infectados **conservan memoria procedimental e inteligencia práctica**. Saben abrir puertas. Saben usar un machete. Saben que tú tienes que dormir.

**Nota de tono:** el horror viene de la *intención* — son crueles y creativos, disfrutan la caza — pero el diseño lo comunica con conducta y sonido, no con gore gratuito como fin. La crueldad es mecánica (tienden trampas, torturan la moral del jugador), la brutalidad visual es ajustable en opciones.

### 4.2 Capacidades de los infectados
- **Herramientas y armas cuerpo a cuerpo** (no armas de fuego de forma competente: la motricidad fina degradada lo impide; pueden disparar, pero mal — esto preserva la ventaja humana clave).
- **Tácticas:** flanqueo, emboscadas en interiores, usar cebos (un infectado hace ruido mientras otros rodean), romper defensas por el punto débil, atacar de noche, **asediar** — saben que el hambre trabaja para ellos.
- **Memoria territorial:** recuerdan dónde vieron humanos, dónde hay bases, qué rutas usan los comerciantes.
- **Comunicación:** vocalizaciones + enlace de corto alcance entre nanomáquinas (el "coro": estática audible cuando coordinan — advertencia diegética para el jugador).

### 4.3 Facciones de infectados: las Manadas
Los infectados se organizan en **manadas** con identidad propia, simuladas como las comunidades humanas pero con sus reglas:

| Parámetro | Humanos | Manadas |
|---|---|---|
| Recurso principal | Comida/energía | Biomasa y fuentes de energía para replicar nanomáquinas |
| Crecimiento | Reclutar/nacer | Convertir humanos (cada humano capturado = infectado nuevo) |
| Territorio | Defensivo | Expansivo: marcan zonas (símbolos untados, cadáveres exhibidos — señal legible por el jugador) |
| Conflicto interno | Cismas por ideología | **Guerras entre manadas** por territorio y biomasa: se canibalizan, absorben a los vencidos |
| Constante | Diplomacia posible | **Contra humanos, siempre.** No hay diplomacia con manadas. Pueden ignorarte temporalmente si están en guerra entre sí — esa es toda la "tregua" posible |

**Consecuencia estratégica rica:** el jugador puede *manipular* manadas sin aliarse con ellas — atraer a la manada A al territorio de la B, dejar un rastro de biomasa hacia el enclave enemigo, reventar la fuente de energía que sostiene a una manada para forzarla a migrar… hacia otro lado. Jugar a ser el parásito del ecosistema.

### 4.4 Alfas: el sistema némesis
Cada manada tiene un **Alfa** (y tenientes): infectados con más cognición retenida, generados proceduralmente con nombre descriptivo, apariencia, rasgos tácticos y **memoria de sus encuentros con el jugador**:

- Un Alfa que sobrevive a tu emboscada aprende: la próxima vez manda exploradores primero.
- Si matas a un Alfa, la manada entra en frenesí caótico 24h (ventana de oportunidad) y luego un teniente asciende, heredando parte de la memoria.
- Los Alfas pueden desarrollar **fijación** con el jugador o con una comunidad concreta: la cazan a través del mapa. Esto crea antagonistas personales sin escribir ni una línea de guion.

### 4.5 Taxonomía revisada
| Tipo | Rol en la manada |
|---|---|
| **Ferales** | La masa. Inteligencia baja, agresión total. Carne de asalto |
| **Cazadores** | Inteligencia media. Emboscadas, sigilo, herramientas. Operan en grupos de 3–5 |
| **Tenientes** | Coordinan ferales, portan armas mejores, ejecutan las tácticas del Alfa |
| **Alfa** | Cognición alta, memoria, sistema némesis. Uno por manada |
| **Aumentados** | Cualquier tipo + implantes (transversal: puede haber un Alfa aumentado — pesadilla) |

*(El "Símil" del GDD v0.1 se elimina: ya no es necesario, todos los infectados son inquietantemente inteligentes. El "Enjambre libre" se conserva como fenómeno ambiental raro.)*

### 4.6 Presión evolutiva (reemplaza las "actualizaciones" programadas)
Ya no hay evento global cada X días. Ahora la evolución es **selección natural real**: las tácticas que les funcionan contra ti y contra los NPCs se propagan por el "coro" entre manadas cercanas. Si dependes siempre de la misma defensa, el mundo entero aprende a romperla. La dificultad emerge de tu propio comportamiento.

---

## 5. Mundo procedural

### 5.1 Enfoque honesto: híbrido, no 100% procedural
Una ciudad completamente procedural creíble es uno de los problemas más difíciles del gamedev. La solución probada (CDDA, Diablo, Kenshi en parte):

**Piezas hechas a mano + ensamblaje procedural.**

### 5.2 Pipeline de generación (al crear la partida)
1. **Macro:** semilla → topografía (río, colinas) → trazado de macro-distritos (agrícola, residencial, industrial, corporativo, subsuelo) con adyacencias lógicas.
2. **Red vial:** avenidas principales por L-systems o grafo de Voronoi relajado; calles secundarias en retícula deformada.
3. **Manzanas:** cada lote se llena con **plantillas de edificio hechas a mano** (50–200 plantillas: casas, torres, clínicas, estaciones) con variación procedural interior (mobiliario, loot, daños, tapiados).
4. **Capa de historia previa:** el generador simula ~30 días de brote antes del día 1 — dónde cayeron las barricadas, qué distritos se evacuaron, dónde nacieron las primeras manadas y comunidades. El mundo del día 1 ya tiene cicatrices coherentes y facciones posicionadas.
5. **Capa ÁTRIA:** nodos, torretas y sistemas activos distribuidos por distrito con estados variables (funcional/corrupto/apagado).

### 5.3 Qué gana el diseño
- Rejugabilidad real: cada partida, otra ciudad, otras facciones iniciales, otras manadas.
- Sinergia con el mundo vivo: como nada es hecho a mano, nada impide que las comunidades muevan, quemen o reconstruyan el mapa.

### 5.4 Costo real (que lo sepas desde ya)
La generación procedural multiplica el trabajo de herramientas y testing. **Recomendación firme:** las fases 0 y 1 del roadmap se hacen sobre un mapa fijo hecho a mano. El generador entra en fase 2, cuando el bucle ya es divertido. Muchos proyectos indie mueren por construir el generador antes que el juego.

---

## 6. Cómo se entrelazan los cuatro sistemas (el motor de historias)

Ejemplo de cadena emergente, sin un solo script:

1. El generador coloca a la comunidad "Los Herreros del Canal" junto al corredor industrial (recursos: componentes).
2. Prosperan → crecen → fundan un puesto avanzado que invade el territorio marcado de la manada del Alfa *"Mandíbula Rota"*.
3. La manada asedia el puesto. La federación de Los Herreros pide ayuda a las comunidades vecinas — incluida la tuya (evento de radio).
4. Si ignoras la llamada: el puesto cae, la manada crece con los convertidos, y ahora es lo bastante grande para amenazar tu distrito.
5. Si acudes: conoces a sus líderes, tu reputación sube, pero *Mandíbula Rota* te ve escapar… y desarrolla fijación contigo.

Ninguno de los cuatro sistemas produce esto por sí solo. Juntos, lo producen constantemente.

---

## 7. Impacto en el roadmap (revisión del §7 del GDD v0.1)

| Fase | Cambios |
|---|---|
| **0. Prototipo** | Sin cambios: 1 personaje, mapa fijo pequeño, pero ya con **Cazadores** (infectados que flanquean y usan puertas). Si un infectado inteligente básico no da miedo aquí, nada de lo demás importa |
| **1. Vertical slice** | + 1 manada con Alfa simple (memoria de 3 encuentros), 2 comunidades NPC en LOD 1, mapa fijo mediano |
| **2. Alpha sistémica** | + Generador procedural v1, simulación mundial LOD 2, cismas y migraciones, vectores ideológicos |
| **3. Beta / EA** | + Sistema némesis completo, guerras faccionales, propagación de tácticas entre manadas |
| **Post-lanzamiento** | Federaciones complejas, historia previa generada profunda, modding del generador |

**Advertencia de alcance, con cariño:** este diseño completo es un proyecto de 3–5 años para un equipo pequeño, o más en solitario. Es totalmente construible por capas — cada fase es un juego disfrutable en sí — pero el orden importa: *diversión del minuto a minuto primero, mundo vivo después, procedural al final.*

---

## 8. Referencias técnicas para estudiar
- **Utility AI / GOAP** para Cazadores y Alfas (comportamiento táctico sin árboles gigantes).
- **Wave Function Collapse y L-systems** para generación urbana.
- **Arquitectura de simulación por ticks con LOD** (charlas GDC de RimWorld y Dwarf Fortress sobre simular mundos "en abstracto").
- **Nemesis system** — la patente de WB cubre implementaciones específicas de su sistema; el concepto general de enemigos con memoria procedural es de uso común (CDDA, Watch Dogs Legion, Wildermyth lo hacen a su manera). Diseñar la propia variante, no copiar la de Mordor.
