# Battle Simulator (Unity 2D)

## Resumen
Battle Simulator es un juego 2D de combate automatico entre canicas dentro de una arena cerrada.
Cada canica se mueve sola, rebota en muros y rivales, usa habilidades automaticas y gana la ultima que quede con vida.

Este proyecto esta orientado a balance y experimentacion: casi todo se configura desde JSON sin tocar codigo.

## Requisitos
- Unity Editor: 6000.4.3f1
- Proyecto abierto desde la carpeta raiz

## Como ejecutar
1. Abre el proyecto en Unity.
2. Ejecuta Play en la escena principal (SampleScene).
3. En el menu inicial, selecciona una canica por lado.
4. Presiona Iniciar Combate.

## Flujo del juego
1. Menu principal con seleccion de canicas.
2. Inicio de combate en arena cuadrada.
3. Uso automatico de habilidades por cooldown.
4. Fin del combate por:
   - Ultima canica viva, o
   - Tiempo maximo agotado.
5. Al finalizar:
   - Se pausa la simulacion.
   - Se limpian objetos colocables runtime (trail circles y proyectiles).
   - Puedes elegir revancha o volver al menu.

## Mecanicas principales
- Movimiento continuo con Rigidbody2D.
- Rebote en paredes y canicas con material fisico configurable.
- Aleatoriedad en rebote para evitar estancamiento en ejes:
  - jitter angular entre 5 y 15 grados por colision.
- Escalado de dano por vida faltante:
  - entre menos vida tenga la canica, mas dano hace (segun config).
- Escalado global de velocidad por tiempo de combate:
  - aumenta progresivamente hacia el tramo critico final.

## Habilidades actuales
### 1) SideRectangles (pasiva)
- Crea dos rectangulos laterales fijos que danan por contacto.
- Ejemplo en juego: Azure (Twin Side Blades).

### 2) TrailCircles (activa)
- Deja circulos de dano en el piso.
- Puede limitarse el maximo de circulos activos.
- Ejemplo en juego: Ruby (Hazard Trail).

### 3) LongRangeRectangleShot (activa)
- Dispara un rectangulo alargado.
- El dano aumenta con la distancia recorrida (con tope configurable).
- Ejemplo en juego: Verdant (Piercing Lane).

## Configuracion del juego
Archivo principal de configuracion:
- Assets/StreamingAssets/battle_config.json

Documentacion detallada del JSON:
- Assets/StreamingAssets/battle_config.md

### Secciones del JSON
- arena: tamano, escala y rebote del mapa.
- rules: tiempo maximo, HUD y escalado global de velocidad.
- marbles: stats de cada canica y sus habilidades.

## Estructura tecnica relevante
- Assets/Scripts/Gameplay/BattleManager.cs
  - flujo de menu, combate y fin de partida
  - spawn de arena y canicas
- Assets/Scripts/Gameplay/MarbleAgent.cs
  - movimiento, colisiones, rotacion visual y scheduler de habilidades
- Assets/Scripts/Gameplay/MarbleAbilities.cs
  - ejecucion por tipo de habilidad y limpieza de placeables
- Assets/Scripts/Gameplay/DistanceScaledProjectile.cs
  - logica de proyectil con dano por distancia
- Assets/Scripts/Config/BattleConfig.cs
  - contratos de configuracion
- Assets/Scripts/Config/BattleConfigLoader.cs
  - carga y validacion del JSON

## Guia rapida de balance
- Combates mas rapidos:
  - sube speed de canicas
  - sube power de habilidades
  - baja maxHealth
- Combates mas tacticos:
  - baja power
  - sube cooldown (baseCooldown)
  - reduce wallBounciness/marbleBounciness
- Menos saturacion visual:
  - baja trailLifetime
  - baja maxActiveTrailCircles
  - sube baseCooldown de TrailCircles

## Errores comunes
- "Config file not found": verifica la ruta en StreamingAssets.
- "unsupported type": revisa type de la habilidad.
- Combate no inicia: valida campos requeridos y rangos minimos en battle_config.md.

## Extension recomendada
Para agregar nuevas canicas o habilidades:
1. Duplica una entrada existente en marbles.
2. Ajusta id (unico) y stats.
3. Define su habilidad en abilities.
4. Ejecuta y balancea en iteraciones cortas.
