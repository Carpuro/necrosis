# Documento de Diseño de Juego (GDD)
## Título provisional: **NECROSIS://PROTOCOLO**

*Versión 0.1 — Documento vivo, sujeto a iteración*

---

## 1. Visión general

### 1.1 Concepto en una frase
Un simulador de supervivencia zombie con gestión de comunidad, ambientado en una megaciudad inteligente colapsada, donde el brote no es biológico sino nanotecnológico, y sobrevivir significa dominar tanto la tecnología moribunda de la ciudad como las necesidades humanas más básicas.

### 1.2 Pitch extendido
Es *Project Zomboid* en su profundidad de simulación (necesidades, oficios, mundo persistente, "así es como moriste") combinado con *State of Decay* en su capa de comunidad (sobrevivientes únicos, bases, moral colectiva), pero trasladado 60 años al futuro. La ciudad no está muerta: sus sistemas automáticos —torretas, drones de limpieza, tránsito autónomo, la IA municipal— siguen funcionando sin supervisión humana, y son tan peligrosos o útiles como los propios infectados.

### 1.3 Pilares de diseño
1. **La simulación es la historia.** No hay campaña lineal; las historias emergen de sistemas que interactúan (hambre + moral + clima + horda = drama).
2. **La tecnología es un recurso que se degrada.** Nada futurista funciona gratis: todo consume energía, requiere mantenimiento y puede fallar en el peor momento.
3. **El infectado evoluciona; tú también.** La nanoplaga se adapta con el tiempo de partida. La dificultad no sube por números, sino por comportamiento.
4. **Muerte con consecuencias.** Permadeath por sobreviviente (estilo State of Decay): pierdes al personaje, no la partida. La comunidad continúa.

### 1.4 Género, plataforma y audiencia
- **Género:** Survival sim / gestión de comunidad / RPG de sistemas, vista isométrica o top-down.
- **Plataforma inicial:** PC (Steam).
- **Audiencia:** Jugadores de Zomboid, State of Decay, RimWorld, CDDA. Tolerantes a la dificultad, amantes de la emergencia sistémica.
- **Rating estimado:** M/18+ (violencia, temas oscuros).

---

## 2. Ambientación y worldbuilding

### 2.1 El mundo
**Año 2087. Ciudad de Vesper**, una megaciudad latinoamericana "inteligente" de 12 millones de habitantes gestionada por ÁTRIA, una IA municipal que controlaba tránsito, energía, seguridad y logística.

### 2.2 El brote: la Nanoplaga (cepa "NCR-0")
No es un virus. Es un enjambre de nanomáquinas médicas de reparación celular que sufrió una corrupción en cascada durante una actualización. En lugar de curar, las nanomáquinas **mantienen el cuerpo funcional después de la muerte cerebral**, priorizando la búsqueda de biomasa y energía para replicarse.

Consecuencias de diseño (esto diferencia al juego):
- **Los infectados buscan energía, no solo carne.** Se agrupan cerca de fuentes eléctricas activas. Encender tu generador es encender un faro.
- **El pulso electromagnético (PEM) es un arma**, pero también fríe tu propio equipo.
- **Infectados "aumentados":** un cadáver con implantes o exoesqueleto conserva parte de su hardware. Categorías de amenaza únicas.
- **La infección del jugador es medible:** un biomonitor de muñeca muestra el % de saturación de nanomáquinas. Hay tratamientos paliativos (no cura), lo que genera decisiones morales dentro de la comunidad.

### 2.3 ÁTRIA, la IA municipal
Sigue operando fragmentada por distritos. Algunos nodos te ayudan (abren puertas, apagan luces para ocultarte), otros te consideran "biomasa no autorizada". Hackear nodos de ÁTRIA es un árbol de progresión completo.

### 2.4 Distritos de Vesper (mapa)
| Distrito | Riesgo | Recursos clave | Rasgo |
|---|---|---|---|
| Periferia agrícola vertical | Bajo | Comida, agua, semillas | Invernaderos automatizados aún activos |
| Zona residencial media | Medio | Medicinas, ropa, básicos | Densidad alta de infectados comunes |
| Corredor industrial | Medio-alto | Componentes, impresoras 3D, químicos | Robots industriales erráticos |
| Centro corporativo | Alto | Tecnología de punta, células de energía | Torretas de ÁTRIA activas, aumentados |
| Subsuelo / metro autónomo | Muy alto | Rutas rápidas, secretos de la trama | Oscuridad total, enjambres |

---

## 3. Mecánicas núcleo

### 3.1 Simulación del sobreviviente (capa Zomboid)
Cada personaje tiene necesidades y estados simulados en tiempo real:
- **Vitales:** hambre, sed, sueño, temperatura, salud por zonas corporales (heridas, fracturas, infección de herida vs. nanoinfección).
- **Mentales:** estrés, moral, pánico, fatiga mental. El pánico reduce precisión y puede provocar acciones involuntarias.
- **Saturación de nanomáquinas (0–100%):** sube con mordidas, exposición a enjambres y aire contaminado sin filtro. A ciertos umbrales aparecen síntomas (temblores, visión con "glitches", susurros de ÁTRIA). Al 100%: conversión.

**Habilidades por uso** (estilo Zomboid): sigilo, armas cortas, armas de energía, mecánica, electrónica, **nanotecnología**, **hackeo**, medicina, agricultura hidropónica, cocina, fabricación.

### 3.2 Gestión de comunidad (capa State of Decay)
- Controlas a **un sobreviviente a la vez**; el resto vive en la base con rutinas simuladas.
- Cada sobreviviente tiene rasgos, habilidades, relaciones y opiniones. La **moral colectiva** afecta productividad, peleas internas y deserciones.
- **Permadeath real:** si muere tu personaje activo, cambias a otro miembro. El duelo afecta la moral de quienes lo querían.
- Eventos comunitarios emergentes: enfermos que ocultan su % de saturación, dilemas (¿expulsas al infectado al 60%?), llegadas de forasteros con historias verificables o falsas.

### 3.3 Base y construcción
- Bases reclamables en edificios existentes (estilo SoD) **más** construcción modular libre dentro del perímetro (estilo Zomboid).
- Instalaciones futuristas: hidroponia, purificador atmosférico, taller de nanoensamblaje (impresión 3D avanzada), enfermería con cámara de supresión nano, muro de energía, radar de hordas.
- **Red eléctrica propia:** paneles solares degradados, generadores, células de fusión portátiles (raras). La energía es EL recurso estratégico: todo la consume y los infectados la detectan.

### 3.4 El enemigo: taxonomía de infectados
| Tipo | Descripción | Contramedida |
|---|---|---|
| **Husk** (común) | Cadáver sostenido por nanomáquinas. Lento, en masa. | Cualquiera; el peligro es el número |
| **Aumentado** | Conserva implantes: piernas protésicas (rápido), placas dérmicas (tanque), ojo térmico (te ve de noche) | Identificar el implante y explotarlo |
| **Nodo** | Infectado-antena que coordina a los cercanos (comportamiento de enjambre) | Eliminarlo desorganiza la horda 30s |
| **Enjambre libre** | Nube de nanomáquinas sin cuerpo. Atraviesa rendijas, infecta por aire | Filtros, PEM, fuego |
| **Símil** | Fase tardía: imita conducta humana básica (golpea puertas, sigue rutas). No habla, pero casi. | Observación; genera paranoia con NPCs |

**Evolución temporal:** cada X días de partida, la cepa "compila una actualización" (evento mundial anunciado por interferencias en radio). Los infectados ganan un comportamiento nuevo. La partida a 60 días no se parece a la del día 1.

### 3.5 Combate y sigilo
- Combate deliberado y letal, no power fantasy. Melé con resistencia/stamina, armas de fuego escasas y ruidosas, armas de energía potentes pero dependientes de células.
- **Herramientas futuristas de sigilo:** señuelos holográficos, capa térmica (contra aumentados con ojo térmico), granadas PEM (aturden infectados 5–8s… y apagan tu propio equipo).
- El ruido y la **firma energética** son los dos vectores de detección. Gestionar ambos es el minijuego constante.

### 3.6 Looteo y economía
- Recursos clásicos (comida, medicina, materiales) + recursos futuristas (células de energía, chips, filamento de impresión, nanogel médico, núcleos de ÁTRIA).
- **Impresoras 3D:** convierten chatarra + filamento + planos (loot de conocimiento) en objetos. La fabricación reemplaza parcialmente al looteo en el late game.
- Comercio con enclaves NPC; la reputación entre facciones importa.

### 3.7 Hackeo y ÁTRIA
Árbol paralelo de progresión: con habilidad de hackeo y "llaves" (núcleos), puedes capturar nodos de distrito para:
- Ver cámaras de la zona (revela hordas).
- Controlar puertas, luces, alarmas (atraer/desviar infectados).
- Activar torretas municipales a tu favor (consumen la red del distrito).
- Desbloquear fragmentos de la historia: qué pasó realmente con la actualización de NCR-0.

---

## 4. Bucle de juego

**Bucle corto (minuto a minuto):** explorar → gestionar ruido/energía → lootear → combatir o evadir → volver con carga limitada.

**Bucle medio (día a día):** asignar tareas en base → mantener instalaciones → curar/alimentar comunidad → decidir expedición del día → responder a eventos.

**Bucle largo (semana a semana):** expandir/mudar base → reclutar → capturar nodos de ÁTRIA → prepararse para la próxima "actualización" de la cepa → decisiones sobre miembros infectados → (opcional) perseguir el arco narrativo del origen del brote.

**Condición de derrota:** muerte de todos los sobrevivientes. **"Victoria":** no hay final obligatorio; hay metas opcionales (sintetizar el supresor definitivo, restaurar a ÁTRIA, evacuar la ciudad), todas con sacrificios.

---

## 5. Interfaz y presentación

- **Cámara:** isométrica/top-down con zoom (facilita la simulación de multitudes y la construcción).
- **Estética:** "futurismo oxidado" — neón muerto, hologramas con glitch, vegetación invadiendo lo smart. Paleta fría con acentos naranjas de emergencia.
- **UI diegética donde sea posible:** biomonitor en muñeca, dron personal que actúa como minimapa/linterna (y puede ser destruido).
- **Audio:** el sonido es mecánica: los infectados oyen; el jugador escucha la estática de enjambres cercanos.

---

## 6. Alcance técnico (para un dev con algo de programación)

### 6.1 Motor recomendado: **Godot 4**
- Gratis, open source, excelente para 2D/top-down, GDScript es amigable si vienes de Python; también soporta C#.
- Alternativas: Unity (más recursos de aprendizaje, licencia más restrictiva), Unreal (excesivo para este alcance).

### 6.2 Riesgos técnicos principales
1. **Simulación de multitudes** (pathfinding de 100+ agentes): mitigar con flow fields o navegación por jerarquías, LOD de IA (infectados lejanos se simulan en abstracto).
2. **Mundo persistente grande:** streaming por chunks/distritos, guardado incremental.
3. **Simulación de NPCs en base:** máquinas de estado simples primero; utility AI después.

### 6.3 Alcance realista: recortes sugeridos para v1.0
- 1 solo distrito jugable bien hecho > 5 mediocres.
- Sin multijugador (duplica o triplica el esfuerzo).
- 3 tipos de infectado en lanzamiento (Husk, Aumentado, Nodo); Enjambre y Símil como contenido post-lanzamiento.
- Arte: assets 2D top-down o 3D low-poly con packs base retexturizados.

---

## 7. Hoja de ruta (solo dev / equipo mínimo)

| Fase | Duración estimada | Entregable |
|---|---|---|
| **0. Prototipo de la diversión** | 4–8 semanas | Un personaje, un barrio pequeño, Husks con detección por ruido/energía, looteo, hambre y muerte. ¿Es tenso y divertido? Si no, iterar aquí. |
| **1. Vertical slice** | 3–4 meses | + Base reclamable, 2 sobrevivientes NPC, crafteo básico, 1 Aumentado, ciclo día/noche, guardado |
| **2. Alpha sistémica** | 4–6 meses | + Comunidad completa, moral, saturación nano, hackeo básico, evolución de cepa, 1 distrito completo |
| **3. Beta / Early Access** | 3–4 meses | + Balance, eventos, enclaves NPC, onboarding, optimización |

> Regla de oro: **no escribas ni una línea del sistema de comunidad hasta que el bucle corto (fase 0) sea divertido por sí solo.** Zomboid es divertido incluso solo; esa es la base de todo.

---

## 8. Referencias y diferenciación

| Juego | Qué tomamos | Qué NO tomamos |
|---|---|---|
| Project Zomboid | Profundidad de simulación, permadeath, habilidades por uso | Su curva de aprendizaje hostil sin onboarding |
| State of Decay 2 | Comunidad, cambio de personaje, bases prediseñadas | Su combate arcade y bucle repetitivo de misiones |
| RimWorld | Narrativa emergente, eventos, moral colectiva | Vista puramente de gestión (aquí sí encarnas al personaje) |
| CDDA | Evolución del infectado, crafteo profundo | Su interfaz |

**Nuestro gancho único (elevator pitch para la página de Steam):**
*"En Vesper, la energía es vida… y un faro para los muertos. Cada watt que consumes te acerca a la horda. Sobrevive a una plaga que se actualiza como software, en una ciudad inteligente que aún no sabe que sus ciudadanos murieron."*

---

## 9. Próximos pasos inmediatos

1. Validar el pilar 2 (energía como riesgo/recurso) en papel: diseñar 3 escenarios de decisión donde encender algo tenga costo-beneficio claro.
2. Definir el tono narrativo con 1 página de lore de ÁTRIA y NCR-0.
3. Instalar Godot 4 y seguir un tutorial de top-down (movimiento + tilemap) como calentamiento.
4. Prototipo fase 0: detección por ruido de un Husk contra el jugador. Ese será el corazón del juego.
