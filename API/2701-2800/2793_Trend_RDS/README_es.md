# Estrategia Trend RDS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trend RDS busca secuencias direccionales claras en la acción del precio. Cuando tres velas completadas forman mínimos estrictamente más altos, trata la estructura como un tramo de tendencia alcista. Tres máximos estrictamente más bajos marcan una configuración bajista. Una regla de protección bloquea entradas cuando las mismas tres barras crean simultáneamente tanto mínimos más altos como máximos más bajos, lo que generalmente indica un triángulo contrayente en lugar de un movimiento direccional. La estrategia puede invertir opcionalmente la dirección a través del parámetro `Reverse`.

El trading está limitado a una ventana de tiempo configurable (predeterminado 09:00–12:00). Cuando la ventana está abierta y aparece un patrón válido, la estrategia cierra cualquier exposición opuesta, abre una nueva posición a mercado al cierre de la vela, y coloca órdenes de stop-loss y take-profit medidas en pips. La distancia en pips se deriva del paso de precio del instrumento, reflejando la lógica original de MetaTrader. Un trailing stop opcional mueve el stop de protección hacia adelante una vez que el precio avanza por la distancia de trailing más el paso de trailing. Los ajustes de trailing se evalúan solo mientras la ventana de sesión está activa.

El tamaño de la posición se recalcula en cada entrada. La estrategia asigna una fracción del capital del portafolio definida por `RiskPercent` y la divide por el riesgo monetario representado por la distancia de stop elegida. Esto produce un dimensionamiento dinámico que escala con el tamaño de la cuenta y el ancho del stop, respetando el valor mínimo `Volume`. Establecer cualquier parámetro relacionado con el riesgo en cero deshabilita esa función, permitiendo entradas de tamaño fijo o sin protección cuando se desee.

## Detalles
- **Criterios de entrada**: Tres velas consecutivas con mínimos más altos activan largos (o cortos cuando `Reverse` es verdadero). Tres mínimos consecutivos más bajos activan cortos (o largos en modo reverso). Las señales se ignoran si las mismas tres barras también satisfacen ambas condiciones simultáneamente.
- **Largo/Corto**: Ambas direcciones con un interruptor de reversión opcional.
- **Criterios de salida**: Salidas a mercado cuando los niveles de stop-loss, take-profit o trailing stop rastreados son violados.
- **Stops**: Stop-loss y take-profit fijos en pips con un trailing stop incremental (requiere que ambos parámetros de trailing sean positivos).
- **Ventana de tiempo**: Opera solo entre `StartTime` y `EndTime` (predeterminado 09:00–12:00 hora de la bolsa).
- **Dimensionamiento de posición**: Dimensionamiento basado en riesgo usando `RiskPercent` del capital del portafolio relativo a la distancia de stop actual (recurre a `Volume` si el dimensionamiento no puede calcularse).
- **Valores predeterminados**:
  - `StopLossPips` = 30
  - `TakeProfitPips` = 65
  - `TrailingStopPips` = 0
  - `TrailingStepPips` = 5
  - `RiskPercent` = 3
  - `StartTime` = 09:00
  - `EndTime` = 12:00
  - `Reverse` = false
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Acción del precio (máximos/mínimos)
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
