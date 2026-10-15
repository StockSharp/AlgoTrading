# Estrategia Forex Fire EMA MA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de tendencia multi-marco temporal que utiliza EMA, MA y confirmación de RSI. Usa velas de 4h para confluencia y velas de 15m para entradas.

## Detalles

- **Criterios de entrada**:
  - Largo: EMA corta por encima de la EMA larga, precio por encima de la MA, RSI rápido por encima del RSI lento y >50, volumen creciente con confirmación del marco temporal superior.
  - Corto: Condiciones opuestas.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cruce de EMA o RSI alcanzando umbrales.
  - Stop loss, take profit, trailing stop y salida basada en ATR opcionales.
- **Stops**: Sí, configurable.
- **Valores predeterminados**:
  - `EmaShortLength` = 13
  - `EmaLongLength` = 62
  - `MaLength` = 200
  - `MaType` = MovingAverageTypeEnum.Simple
  - `RsiSlowLength` = 28
  - `RsiFastLength` = 7
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `UseStopLoss` = true
  - `StopLossPercent` = 2
  - `UseTakeProfit` = true
  - `TakeProfitPercent` = 4
  - `UseTrailingStop` = true
  - `TrailingPercent` = 1.5
  - `UseAtrExits` = true
  - `AtrMultiplier` = 2
  - `AtrLength` = 14
  - `EntryCandleType` = TimeSpan.FromMinutes(15).TimeFrame()
  - `ConfluenceCandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, MA, RSI, ATR
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Multi-marco temporal
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
