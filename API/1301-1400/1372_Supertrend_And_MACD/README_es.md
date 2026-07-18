# Supertrend y MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina Supertrend, MACD y filtro EMA 200.

## Detalles

- **Criterios de entrada**: Precio respecto a Supertrend y EMA, línea MACD frente a su línea de señal.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cruce de MACD o stop basado en extremos recientes.
- **Stops**: Stops de seguimiento de máximos/mínimos.
- **Valores predeterminados**:
  - `AtrPeriod` = 10
  - `Factor` = 3
  - `EmaPeriod` = 200
  - `StopLookback` = 10
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SuperTrend, EMA, MACD, Highest, Lowest
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
