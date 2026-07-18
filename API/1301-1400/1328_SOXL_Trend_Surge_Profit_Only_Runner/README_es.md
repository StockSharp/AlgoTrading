# Estrategia SOXL de Impulso de Tendencia Solo Ganancias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en operaciones largas cuando el precio está tendiendo por encima de la EMA 200 y el SuperTrend es alcista. Requiere ATR en aumento, volumen por encima del promedio, un filtro de sesión y que el precio se mantenga fuera de un pequeño búfer de EMA. El sistema toma beneficio parcial en un objetivo basado en ATR y hace trailing de la posición restante con un stop ATR.

## Detalles

- **Criterios de entrada**: precio por encima de la EMA, SuperTrend arriba, volumen sobre el promedio, ATR en aumento, fuera del búfer EMA, hora entre 14–19 horas, enfriamiento tras salidas
- **Largo/Corto**: Solo largos
- **Criterios de salida**: toma de beneficio parcial del 50% en objetivo ATR y trailing stop del resto
- **Stops**: Trailing
- **Valores predeterminados**:
  - `EmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultTarget` = 2.0
  - `CooldownBars` = 15
  - `SupertrendFactor` = 3.0
  - `SupertrendAtrPeriod` = 10
  - `MinBarsHeld` = 2
  - `VolFilterLen` = 20
  - `EmaBuffer` = 0.005
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Solo largos
  - Indicadores: EMA, ATR, SuperTrend, Volumen
  - Stops: Trailing
  - Complejidad: Moderado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
