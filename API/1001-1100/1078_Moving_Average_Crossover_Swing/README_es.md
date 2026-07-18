# Estrategia de Cruce de Medias Móviles Swing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera cuando una media móvil exponencial rápida cruza una media intermedia, con confirmación opcional de una MA lenta y el histograma MACD. Utiliza stop loss y take profit basados en ATR y puede salir en un cruce secundario de MA.

## Detalles

- **Criterios de entrada**:
  - EMA rápida cruza por encima de la EMA media para largo, por debajo para corto.
  - Opcional: precio por encima/debajo de la EMA lenta.
  - Opcional: histograma MACD por encima/debajo de cero.
- **Largo/Corto**: Configurable.
- **Criterios de salida**: Stop loss y take profit basados en ATR o cruce de MA de salida opcional.
- **Stops**: Sí, múltiplos de ATR.
- **Valores predeterminados**:
  - `FastPeriod` = 5
  - `MediumPeriod` = 10
  - `SlowPeriod` = 50
  - `FastExitPeriod` = 5
  - `MediumExitPeriod` = 10
  - `AtrPeriod` = 14
  - `AtrStopMultiplier` = 1.4
  - `AtrTakeMultiplier` = 3.2
  - `EnableSlow` = true
  - `EnableMacd` = true
  - `EnableLong` = true
  - `EnableShort` = false
  - `EnableCrossExit` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Configurable
  - Indicadores: EMA, MACD, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: 1m (predeterminado)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
