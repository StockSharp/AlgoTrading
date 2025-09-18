# Universal Signal Demo
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the MetaTrader 5 "Universal Signal" expert using StockSharp high-level APIs. It evaluates eight weighted market patterns and aggregates them into a single composite score. When the score crosses configurable thresholds the strategy opens or closes long and short positions, optionally using pending limit orders that expire after a set number of bars.

## Strategy Parameters
- `CandleType` – candle data used for the analysis.
- `SignalThresholdOpen` – minimum composite score required to open a position.
- `SignalThresholdClose` – opposing score required to exit an existing position.
- `PriceLevel` – price offset for placing pending limit entries (0 means market execution).
- `StopLevel` / `TakeLevel` – absolute stop-loss and take-profit distances used by the built-in protection module.
- `SignalExpiration` – number of bars after which still-active pending entries are cancelled.
- `Pattern0Weight` … `Pattern7Weight` – weight applied to each pattern before aggregation.
- `UniversalWeight` – final multiplier applied to the sum of all pattern contributions.
- `ShortMaPeriod`, `LongMaPeriod`, `RsiPeriod`, `BollingerPeriod`, `BollingerWidth`, `TrendSmaPeriod`, `VolumeSmaPeriod` – indicator settings used inside the pattern checks.

## Trading Logic
1. Subscribe to the configured candle stream and bind EMA, RSI, MACD Signal, Bollinger Bands, and supporting SMAs.
2. After every finished candle, compute eight boolean patterns (trend alignment, RSI momentum, MACD histogram, Bollinger positioning, candle direction, and volume expansion).
3. Multiply each pattern by its weight, sum the contributions, and apply the global weight to obtain the final score.
4. Close open positions when the score crosses the closing threshold in the opposite direction.
5. Open new long or short positions when the score exceeds the opening threshold. If `PriceLevel` is positive, submit a limit order offset by the configured distance and cancel it automatically after `SignalExpiration` bars.
6. `StartProtection` sets fixed stop-loss and take-profit levels for all positions using StockSharp's risk management helpers.

The conversion keeps the flexible weighting workflow of the original MQL5 expert while following StockSharp coding conventions and indicator-based processing.
