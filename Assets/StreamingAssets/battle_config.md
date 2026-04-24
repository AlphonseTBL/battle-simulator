# battle_config.json - Guia de configuracion

## Objetivo
Este archivo controla el combate completo: arena, reglas globales y lista de canicas con sus habilidades.

Ruta del archivo:
- Assets/StreamingAssets/battle_config.json

---

## Reglas importantes antes de editar
- El JSON no permite comentarios ni comas finales.
- Deben existir al menos 2 canicas.
- Los id de canica no se pueden repetir.
- Si falta un campo, Unity JsonUtility usa el valor por defecto del codigo.
- Si hay un valor invalido, el combate no inicia y veras error en consola.

Validaciones clave (BattleConfigLoader):
- arena.width y arena.height > 1
- arena.sizeScale > 0
- arena.wallBounciness >= 0
- arena.marbleBounciness >= 0
- rules.maxBattleSeconds >= 5
- rules.restartDelaySeconds >= 0
- rules.speedBoostCriticalSeconds > 0
- rules.maxExtraSpeedPercentAtCritical >= 0
- marble.maxHealth > 0
- marble.speed > 0
- marble.radius > 0.05
- marble.turnRateJitterInterval >= 0.1
- marble.missingHealthToBonusPercentPerPoint >= 0

---

## Estructura general
- arena: tamano y rebote del mapa.
- rules: tiempo de combate y escalado de velocidad global.
- marbles: lista de personajes seleccionables en menu.

Cada elemento de marbles tiene:
- atributos base de la canica
- abilities: arreglo de habilidades (normalmente 1, pero puede ser mas)

---

## Arena
Campos:
- width: ancho base
- height: alto base
- forceSquare: si true, usa lado cuadrado
- sizeScale: escala final del lado
- wallThickness: grosor de muros
- wallBounciness: rebote de paredes
- marbleBounciness: rebote entre canicas

Comportamiento real:
- lado base:
  - forceSquare true: max(width, height)
  - forceSquare false: min(width, height)
- lado final = lado base x sizeScale
- wallBounciness y marbleBounciness se limitan internamente a 0..1
  - valores mayores a 1 no dan mas rebote efectivo

---

## Rules
Campos:
- maxBattleSeconds: duracion maxima
- restartDelaySeconds: heredado de versiones previas; en flujo actual no se usa para auto reinicio
- autoRestart: heredado de versiones previas; en flujo actual no realiza recarga automatica
- showDebugHud: muestra HUD superior y paneles inferiores
- speedBoostCriticalSeconds: define cuando comienza el tramo critico
- maxExtraSpeedPercentAtCritical: bonus maximo al llegar al tramo critico

Escalado de velocidad global:
- Se aplica un suavizado (smoothstep) para no acelerar de golpe.
- Multiplicador final = 1 + extraPercent/100.

---

## Canica (Marble)
Campos generales:
- id: identificador unico
- displayName: nombre mostrado en HUD/menu
- colorHex: color en formato #RRGGBB
- radius: radio fisico y visual
- maxHealth: vida maxima
- speed: velocidad base
- turnRateDegreesPerSecond: giro visual base
- turnRateJitter: variacion aleatoria del giro visual
- turnRateJitterInterval: validado, actualmente no altera el runtime
- collisionDamage: dano por choque directo entre canicas
- missingHealthToBonusPercentPerPoint: escalado de dano por vida faltante
- hasteMultiplier, hasteDuration: reservados para haste (si alguna habilidad lo usa)
- abilities: lista de habilidades

Escalado por vida faltante:
- damageMultiplier = 1 + (vidaFaltante x missingHealthToBonusPercentPerPoint)/100
- Se aplica en:
  - dano por choque
  - zonas de dano
  - proyectil de larga distancia

---

## Habilidades por tipo
Usa solo los campos de su tipo para mantener el JSON limpio.

### 1) SideRectangles (pasiva)
Descripcion:
- Crea dos rectangulos laterales permanentes alrededor de la canica.

Campos usados:
- type
- name
- power
- sizeX
- sizeY
- sideOffset

Notas:
- Es pasiva: se crea una vez al iniciar.
- baseCooldown, randomJitter y range no afectan esta habilidad.

### 2) TrailCircles (activa)
Descripcion:
- Deja circulos de dano en el piso.

Campos usados:
- type
- name
- power
- baseCooldown
- randomJitter
- trailRadius
- trailLifetime
- maxActiveTrailCircles

Notas:
- maxActiveTrailCircles:
  - 0: sin limite
  - >0: al superar limite destruye los mas antiguos
- Cada circulo se consume al golpear un enemigo.

### 3) LongRangeRectangleShot (activa)
Descripcion:
- Lanza un rectangulo alargado en direccion del objetivo (o direccion de movimiento/fallback).
- Hace mas dano cuanto mas distancia recorre.

Campos usados:
- type
- name
- power
- baseCooldown
- randomJitter
- range
- projectileSpeed
- projectileLifetime
- projectileLength
- projectileWidth
- distanceDamageBonusPercentPerUnit
- projectileMaxDamageMultiplier

Formula de dano:
- distanceMultiplier = min(1 + distanciaRecorrida x bonusPorUnidad/100, projectileMaxDamageMultiplier)
- finalDamage = power x distanceMultiplier x damageMultiplierPorVidaFaltante

Notas:
- range:
  - 0 o menor: busca enemigo sin limite de distancia
  - mayor a 0: busca enemigo dentro de ese radio

---

## Configuracion minima recomendada por habilidad

SideRectangles:
- power: 6 a 14
- sizeX: 1.0 a 1.8
- sizeY: 0.12 a 0.3

TrailCircles:
- power: 5 a 10
- baseCooldown: 0.5 a 1.2
- trailRadius: 0.12 a 0.22
- trailLifetime: 5 a 10
- maxActiveTrailCircles: 5 a 20

LongRangeRectangleShot:
- power: 4 a 9
- baseCooldown: 0.7 a 1.4
- projectileSpeed: 8 a 14
- projectileLifetime: 1.6 a 3.0
- projectileLength: 1.2 a 2.8
- projectileWidth: 0.12 a 0.32
- distanceDamageBonusPercentPerUnit: 8 a 25
- projectileMaxDamageMultiplier: 1.5 a 4.0

---

## Ejemplos de cambios rapidos

Hacer a Ruby mas defensiva:
- subir maxHealth
- bajar speed un poco
- bajar power de TrailCircles y subir trailLifetime

Hacer a Azure mas agresiva:
- subir power de SideRectangles
- aumentar sizeX para mas alcance lateral
- subir missingHealthToBonusPercentPerPoint para escalar al final

Hacer a Verdant tipo sniper:
- bajar baseCooldown
- subir projectileSpeed
- subir distanceDamageBonusPercentPerUnit
- mantener projectileMaxDamageMultiplier controlado para no romper balance

---

## Problemas comunes y solucion

El combate no inicia:
- Revisa consola y valida tipos soportados:
  - SideRectangles
  - TrailCircles
  - LongRangeRectangleShot
- Revisa campos requeridos por cada tipo.

No noto mas rebote al subir bounciness arriba de 1:
- Es esperado. El runtime limita bounciness a 1.

Una habilidad parece no hacer nada:
- Verifica que los campos usados por su tipo existan y tengan valores > 0 donde aplica.

---

## Plantilla base para nueva canica
Copia este bloque dentro de marbles y ajusta valores.

{
  "id": "new_id",
  "displayName": "New Marble",
  "colorHex": "#FFFFFF",
  "radius": 0.4,
  "maxHealth": 120.0,
  "speed": 4.2,
  "turnRateDegreesPerSecond": 40.0,
  "turnRateJitter": 12.0,
  "turnRateJitterInterval": 1.0,
  "collisionDamage": 0.0,
  "missingHealthToBonusPercentPerPoint": 1.0,
  "hasteMultiplier": 1.7,
  "hasteDuration": 1.2,
  "abilities": [
    {
      "type": "TrailCircles",
      "name": "Custom Trail",
      "power": 7.0,
      "baseCooldown": 0.8,
      "randomJitter": 0.1,
      "trailRadius": 0.14,
      "trailLifetime": 8.0,
      "maxActiveTrailCircles": 10
    }
  ]
}
