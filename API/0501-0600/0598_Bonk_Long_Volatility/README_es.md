# BONK Volatilidad en Largo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia solo en largo entra bajo condiciones alcistas fuertes combinando medias móviles, volatilidad y filtros de volumen. Compra cuando el mercado está en tendencia alcista, la volatilidad se expande y los indicadores de momentum confirman la fortaleza. Las salidas usan take profit fijo, stop loss y un trailing stop basado en ATR.

## Detalles

- **Criterios de entrada**: MA rápida por encima de la MA lenta, rango de precio mayor que ATR * `AtrMultiplier`, RSI entre `RsiOversold` y `RsiOverbought`, línea MACD por encima de la señal y de cero, volumen por encima de SMA * `VolumeThreshold`, cierre por encima de la MA rápida, vela dentro de los últimos `LookbackDays`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Take profit, stop loss o trailing stop basado en ATR.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `ProfitTargetPercent` = 5.0m
  - `StopLossPercent` = 3.0m
  - `AtrLength` = 10
  - `AtrMultiplier` = 1.5m
  - `RsiLength` = 14
  - `RsiOverbought` = 65
  - `RsiOversold` = 35
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeSmaLength` = 20
  - `VolumeThreshold` = 1.5m
  - `MaFastLength` = 5
  - `MaSlowLength` = 13
  - `LookbackDays` = 30
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo
  - Indicadores: SMA, ATR, RSI, MACD, Volumen
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

