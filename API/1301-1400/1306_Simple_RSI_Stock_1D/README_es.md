# Estrategia de RSI Simple para Acciones 1D
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este sistema entra en largo cuando el RSI cae por debajo de un nivel de sobreventa mientras el precio se mantiene por encima de la SMA de 200 días. La posición utiliza un stop basado en ATR y tres objetivos de beneficio.

## Detalles

- **Criterios de entrada**: RSI por debajo de `OversoldLevel` y cierre por encima del filtro SMA.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Stop ATR o alcance de cualquier nivel de toma de ganancias.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RsiPeriod` = 5
  - `OversoldLevel` = 30
  - `SmaLength` = 200
  - `AtrLength` = 20
  - `AtrMultiplier` = 1.5
  - `TakeProfit1` = 5
  - `TakeProfit2` = 10
  - `TakeProfit3` = 15
  - `StopLossPercent` = 25
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Largo
  - Indicadores: RSI, SMA, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
