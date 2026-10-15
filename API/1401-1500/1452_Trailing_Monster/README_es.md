# Estrategia Trailing Monster
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina detección de tendencia KAMA con filtro RSI y un trailing stop. Las posiciones se abren cuando el RSI cruza niveles extremos en la dirección de la tendencia KAMA. Después de un retraso, un trailing stop porcentual protege las ganancias.

## Detalles
- **Criterios de entrada**:
  - **Largo**: RSI > `RsiOverbought`, cierre por encima de SMA, KAMA en ascenso
  - **Corto**: RSI < `RsiOversold`, cierre por debajo de SMA, KAMA en descenso
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Trailing stop porcentual después de `DelayBars`
- **Stops**: Trailing stop en porcentaje
- **Valores predeterminados**:
  - `KamaLength` = 40
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `SmaLength` = 200
  - `BarsBetweenEntries` = 3
  - `TrailingStopPct` = 12m
  - `DelayBars` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: KAMA, RSI, SMA
  - Stops: Trailing
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
