# GDD v0.6 — FINAL: LAS FIRMAS DE ORIGINALIDAD
## NECROSIS://PROTOCOLO — Últimas adiciones y cierre de diseño

*Complementa v0.1–v0.5. **Este documento cierra la fase de diseño.** Regla en vigor: nada entra sin que salga algo de la misma fase.*

Criterio de selección: cada idea debe (a) reutilizar un sistema ya diseñado, (b) no existir en Zomboid/State of Decay/7DTD, y (c) generar historias que los jugadores cuenten.

---

## 1. El coro como lenguaje aprendible
**Reutiliza:** el audio-radar de v0.1/v0.5.

La estática de coordinación de las manadas no es ruido aleatorio: tiene **patrones**. Pulsos lentos = patrulla rutinaria; ráfagas ascendentes = presa detectada; barrido continuo = migración; silencio súbito en pleno día = te están rodeando *ahora*.

- El jugador **aprende a leerlo de verdad** (aprendizaje del jugador, no solo del personaje): tras 30 horas de juego, un veterano escucha una ráfaga y sabe qué pasa antes que cualquier HUD. Conocimiento que se transfiere entre partidas y muertes — el jugador progresa aunque el personaje muera.
- Capa de personaje: la habilidad *Nanotecnología* + un decodificador crafteable añaden anotaciones ("patrón: caza — dirección estimada NE"), útil para novatos, redundante para veteranos. La maestría es sensorial, no numérica.
- Joya emergente: los patrones son los mismos que usan los **nidos dormidos** en microimpulsos. Con oído entrenado (o decodificador mejorado), las "estatuas" nocturnas se delatan levemente — el contrajuego del campo minado existe, pero hay que ganárselo.

**Costo:** diseño de audio disciplinado (biblioteca de ~8 patrones). Casi todo es contenido, no código.

---

## 2. La saturación como pacto (riesgo/recompensa)
**Reutiliza:** el medidor de saturación nano de v0.1.

La saturación deja de ser solo una barra de condena. Las nanomáquinas intentan **integrarte**, y a media saturación empiezan a "ofrecerte" cosas:

| Rango | Efectos |
|---|---|
| 0–25% | Limpio. Sin efectos |
| 25–50% | **El coro se te vuelve más nítido** (bonus pasivo a lectura de patrones); visión leve de la niebla nano |
| 50–75% | Los infectados **dudan un instante** antes de atacarte (te leen como "parcialmente asimilado"); percibes la firma energética a simple vista (glitch visual útil). PERO: síntomas sociales — NPCs con biomonitor te detectan, comunidades purgadoras te expulsan o cazan; temblores; susurros |
| 75–99% | Los ferales pueden **ignorarte** si no los provocas. Estás a una mordida del final. Tu propia comunidad debate qué hacer contigo |
| 100% | Conversión. Ver §3 |

- El tratamiento supresor (caro, degradante con el uso) **baja** saturación: el jugador elige activamente en qué rango vivir. Jugar "sucio" a 60% es un estilo de juego completo — el del culto de la Plaga, disponible para el jugador.
- Dilema comunitario automático: tu mejor explorador rinde más saturado… y es una bomba de tiempo dentro del muro. Las reglas que tu comunidad adopte al respecto (v0.3, dilemas de gobierno) ahora te aplican **a ti**.

**Costo:** modificadores sobre sistemas existentes (detección, IA, social). Sin sistemas nuevos.

---

## 3. Tus muertos te conocen
**Reutiliza:** permadeath (v0.1), sistema némesis de Alfas (v0.2), saturación (§2).

Cuando un personaje jugable o un miembro de la comunidad muere con saturación alta (o es capturado por una manada), **se convierte — y conserva memoria procedimental**:

- Reaparece como infectado **con nombre, cara y equipo reconocibles**, integrado a la manada local. Si su cognición retenida es alta, asciende a Teniente o Alfa.
- **Recuerda lo que sabía:** la ubicación de tu base, el punto débil del muro que él mismo reparó, las rutas de tus patrullas que él diseñó, el horario de tus expediciones. La manada que lo absorbe **hereda ese conocimiento**.
- Consecuencia estratégica brutal y coherente: cuando alguien cae en territorio de manada, **recuperar el cuerpo antes del anochecer es una misión real** con razón mecánica, no sentimental. Y la decisión de gobierno más oscura del juego se vuelve sistémica: ¿qué hace tu comunidad con los miembros al 90%… sabiendo lo que saben?
- Con el jugador: tu personaje anterior, con su chaqueta y su cicatriz, apareciendo en el muro este que él conocía de memoria — es la escena que define el juego. Nadie la escribió.

**Costo:** medio. Los sistemas (némesis, conversión, memoria territorial) ya existen; esto los conecta. Es la conexión más rentable del GDD.

---

## 4. Los rostros de ÁTRIA
**Reutiliza:** nodos de ÁTRIA por distrito (v0.1), hackeo, generación procedural (v0.2).

Cada nodo de distrito se corrompió distinto durante el colapso. Al generarse el mundo, cada nodo recibe una **personalidad emergente** (2–3 rasgos combinados): uno es un burócrata que exige formularios de 2087 para abrir puertas; otro sigue narrando el clima y las noticias de hace años a calles vacías; otro es maternal y encierra "por tu seguridad"; otro negocia como mercader (energía a cambio de que elimines la manada que daña sus sensores); otro miente.

- No son quest-givers con guion: son **modificadores de reglas por distrito** con voz. El burócrata hace su distrito lento pero seguro; el paranoico lo hace letal pero rico en tecnología intacta.
- El hackeo (v0.1 §3.7) gana textura: no solo capturas nodos — **negocias, engañas o "curas" personalidades**, y el método mueve tu vector ideológico (restaurador vs. neo-ludita).
- Barato y potentísimo en identidad: es la voz del juego. Vesper no está muerta — está *senil*, y eso no lo tiene nadie en el género.

**Costo:** bajo-medio. Plantillas de personalidad × generación procedural + escritura de barks. Mucho texto, poco código.

---

## 5. Lo que se evaluó y quedó FUERA (registro de decisiones)
- **Falsificación de firmas energéticas / espionaje entre facciones:** original, pero añade un sistema entero. Post-lanzamiento si acaso.
- **Conocimiento como moneda que se propaga (tus planos vendidos usados en tu contra):** buena idea, complejidad de tracking alta. Simplificación posible en fase 3: los planos vendidos simplemente aparecen en el pool de la facción compradora.
- **Mascotas/animales sintéticos, vehículos, clima extremo scripted:** no sirven a los pilares.

---

## 6. CIERRE DE DISEÑO

El GDD queda congelado en v0.6. Resumen de la identidad final del juego en cinco frases:

1. Un survival sim en primera persona donde **la energía es vida y delación** a la vez.
2. Infectados **inteligentes y solares**: cazan de día en manadas con Alfas que te recuerdan; de noche son un campo minado de estatuas.
3. Un mundo **procedural y vivo** donde comunidades evolucionan de campamento a proto-estado y las facciones — humanas e infectadas — emergen, guerrean y caen sin guion.
4. La infección es **un pacto medible**, no solo una condena; y tus muertos regresan sabiendo dónde duermes.
5. Todo ocurre en Vesper, una ciudad inteligente **senil** que aún habla.

**Único paso siguiente válido: Fase 0 en Godot.** Movimiento FP + audio posicional del coro + un Cazador diurno que flanquea + cuatro estatuas nocturnas + una linterna. Nada más. Cuando eso dé miedo, el resto de estos documentos tendrá dónde vivir.
