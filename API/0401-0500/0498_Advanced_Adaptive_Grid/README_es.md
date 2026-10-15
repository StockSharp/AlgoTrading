# Estrategia de Cuadrícula Adaptativa Avanzada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Cuadrícula Adaptativa Avanzada utiliza múltiples indicadores técnicos para evaluar la dirección de la tendencia y construye una cuadrícula dinámica de niveles de entrada. El tamaño de la cuadrícula se adapta a la volatilidad mediante ATR y las órdenes se colocan cuando el precio toca los niveles de la cuadrícula en la dirección de la tendencia. Los controles de riesgo incluyen stop-loss fijo, take-profit, trailing stop, salida por tiempo y límite de pérdida diaria.

## Detalles

- **Criterios de entrada**:
  - En mercados con tendencia: precio que alcanza los niveles de cuadrícula calculados con confirmación del RSI.
  - En mercados laterales: RSI sobrecomprado/sobrevendido activa entradas en la cuadrícula.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Stop-loss, take-profit, trailing stop, reversión de tendencia o salida por tiempo.
- **Stops**: Fijo y trailing.
- **Valores predeterminados**:
  - `BaseGridSize` = 1
  - `MaxPositions` = 5
  - `UseVolatilityGrid` = True
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `ShortMaLength` = 20
  - `LongMaLength` = 50
  - `SuperLongMaLength` = 200
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 3
  - `UseTrailingStop` = True
  - `TrailingStopPercent` = 1
  - `MaxLossPerDay` = 5
  - `TimeBasedExit` = True
  - `MaxHoldingPeriod` = 48
- **Filtros**:
  - Categoría: Cuadrícula / Tendencia
  - Dirección: Ambos
  - Indicadores: ATR, SMA, MACD, RSI, Momentum
  - Stops: Sí
  - Complejidad: Alto
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
