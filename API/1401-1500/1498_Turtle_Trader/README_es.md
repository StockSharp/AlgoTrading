# Estrategia Turtle Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Turtle Trader sigue el clásico sistema de ruptura Turtle usando canales Donchian y gestión de capital basada en ATR. Compra cuando el precio supera los máximos recientes y vende cuando cae por debajo de los mínimos recientes. El piramidado añade a posiciones ganadoras a medida que el precio avanza a favor.

## Detalles

- **Criterios de entrada**: ruptura de máximos/mínimos de `S1` o `S2`
- **Largo/Corto**: Ambos
- **Criterios de salida**: ruptura opuesta o stop ATR
- **Stops**: basados en ATR
- **Valores predeterminados**:
  - `RiskPercent` = 1
  - `AtrPeriod` = 20
  - `StopMultiplier` = 1.5
  - `PyramidProfit` = 0.5
  - `S1Long` = 20
  - `S2Long` = 55
  - `S1LongExit` = 10
  - `S2LongExit` = 20
  - `S1Short` = 15
  - `S2Short` = 55
  - `S1ShortExit` = 7
  - `S2ShortExit` = 20
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: ATR, Highest, Lowest
  - Stops: ATR
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
