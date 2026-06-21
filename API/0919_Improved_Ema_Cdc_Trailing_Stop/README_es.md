# Estrategia EMA & CDC Trailing Stop Mejorada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina el filtro de tendencia EMA, la confirmación del MACD y un stop trailing CDC basado en ATR.

## Detalles

- **Criterios de entrada**:
  - **Largo**: precio > EMA60, EMA60 > EMA90, línea MACD > línea de señal.
  - **Corto**: precio < EMA60, EMA60 < EMA90, línea MACD < línea de señal.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Stop trailing o objetivo de ganancia basado en ATR.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Ema60Period` = 60
  - `Ema90Period` = 90
  - `AtrPeriod` = 24
  - `Multiplier` = 4
  - `ProfitTargetMultiplier` = 2
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA, MACD, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
