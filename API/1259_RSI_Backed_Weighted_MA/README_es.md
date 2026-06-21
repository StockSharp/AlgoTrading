# Estrategia RSI & MA Ponderado Invertido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia utiliza el Índice de Fuerza Relativa y una media móvil ponderada de manera inversa con un filtro de tasa de cambio. Las posiciones largas se abren cuando el RSI supera el umbral y el ROC del MA está por debajo del nivel establecido, mientras que las posiciones cortas se abren en condiciones opuestas. El sistema aplica un stop trailing basado en ATR y dimensionamiento de posición por ratio fijo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `RSI >= RsiLongSignal` y `MA ROC <= RocMaLongSignal`
  - **Corto**: `RSI <= RsiShortSignal` y `MA ROC >= RocMaShortSignal`
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta, stop loss o stop trailing.
- **Stops**: Sí, stop trailing ATR y porcentaje de pérdida máxima.
- **Valores predeterminados**:
  - `RsiLength` = 20
  - `MaType` = RWMA
  - `MaLength` = 19
  - `RsiLongSignal` = 60
  - `RsiShortSignal` = 40
  - `TakeProfitActivation` = 5
  - `TrailingPercent` = 3
  - `MaxLossPercent` = 10
  - `FixedRatio` = 400
  - `IncreasingOrderAmount` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: RSI, Moving Average, ATR
  - Stops: Sí
  - Complejidad: Alto
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
