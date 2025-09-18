# MACD Fixed PSAR Strategy

## Overview
This strategy is a C# port of the MetaTrader expert advisor **EA_MACD_FixedPSAR**. It trades trend reversals by combining a MACD crossover filter with an EMA trend check. Risk management mirrors the original implementation and supports both a fixed-distance trailing stop and a Parabolic SAR style trailing mode. All distances are configured in pips and internally converted to price units based on the instrument tick size.

## Indicators
- `MovingAverageConvergenceDivergenceSignal` (12, 26, 9) delivers MACD and signal lines.
- `ExponentialMovingAverage` (default 26) confirms the short-term trend direction.

## Trading Logic
1. **Entry Conditions**
   - **Long**: MACD crosses above its signal line while remaining below zero, the absolute MACD value exceeds the *MACD Open Level*, and the EMA is rising compared with the previous candle.
   - **Short**: MACD crosses below its signal line while remaining above zero, the absolute MACD value exceeds the *MACD Open Level*, and the EMA is falling compared with the previous candle.
2. **Exit Conditions**
   - MACD reversal that exceeds the *MACD Close Level* in the opposite direction.
   - Configurable take-profit and stop-loss levels, both measured in pips.
   - Optional trailing stop behaviour:
     - **Fixed**: maintains a constant distance from the latest close.
     - **Fixed PSAR**: emulates the incremental Parabolic SAR adjustment used by the MQL version.

## Parameters
| Name | Description |
| ---- | ----------- |
| `Volume` | Trading volume used for market orders. |
| `TakeProfitPips` | Take-profit distance in pips. |
| `StopLossPips` | Stop-loss distance in pips. |
| `TrailMode` | Trailing stop logic (`None`, `Fixed`, `FixedPsar`). |
| `TrailingStopPips` | Distance for the fixed trailing mode. |
| `PsarStep` | Initial acceleration factor for the PSAR trailing mode. |
| `PsarMaximum` | Maximum acceleration factor for the PSAR trailing mode. |
| `MacdOpenLevelPips` | Minimum MACD magnitude (in pips) required to open a position. |
| `MacdCloseLevelPips` | Minimum MACD magnitude (in pips) required to close a position. |
| `TrendPeriod` | EMA period used for trend confirmation. |
| `CandleType` | Candle series type for indicator calculations. |

## Notes
- All thresholds are stored in pips and translated into price units using the instrument tick size (with five or three decimal fixes emulating the MetaTrader adjustment).
- Trailing stop logic updates only on fully formed candles to avoid premature exits.
- The strategy draws candles, both indicators, and trade marks on the default chart area when available.
