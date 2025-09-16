# Separate Trade Strategy

## Overview
The Separate Trade strategy is a conversion of the MetaTrader 5 expert advisor "Separate trade". It preserves the original multi-filter logic while adopting the StockSharp high-level API for robust order management and indicator handling. The strategy attempts to capture quiet market turns when volatility and dispersion are suppressed. Only one net position is maintained at a time, which mirrors the intent of the original code that limited the number of simultaneous positions.

## Indicators and Data
- Two moving averages with configurable method (SMA, EMA, SMMA or LWMA) and shared price source.
- Average True Range (ATR) with separate periods and thresholds for long and short filters.
- Standard deviation using the same applied price as the moving averages, again with direction-specific periods and ceilings.
- Candles are supplied through a configurable `DataType` parameter so the strategy can be attached to any timeframe or custom candle builder.

## Parameters
| Parameter | Description | Default |
| --- | --- | --- |
| `TradeVolume` | Order size expressed in lots. | `1` |
| `SlowMaPeriod` | Period of the slower moving average. | `65` |
| `FastMaPeriod` | Period of the faster moving average. | `14` |
| `MaMethod` | Smoothing method applied to both moving averages (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `Exponential` |
| `PriceType` | Price input for the moving averages and standard deviation (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). | `Close` |
| `StopLossBuyPips` / `StopLossSellPips` | Stop-loss distance for long and short trades in pips (0 disables the stop). | `50` |
| `TakeProfitBuyPips` / `TakeProfitSellPips` | Take-profit distance for long and short trades in pips (0 disables the take-profit). | `50` |
| `TrailingStopPips` | Trailing stop distance in pips. | `5` |
| `TrailingStepPips` | Minimum profit advance in pips before the trailing stop is moved. Must be positive when trailing is enabled. | `5` |
| `MaxPositions` | Maximum allowed simultaneous net positions. The StockSharp version operates with a single aggregated position even when the value is greater than one. | `1` |
| `DeltaBuyPips` / `DeltaSellPips` | Maximum allowed distance between the fast and slow moving averages (per direction). A value of zero disables the distance filter. | `2` |
| `AtrPeriodBuy` / `AtrPeriodSell` | ATR lookback period for the long and short filters. | `26` |
| `AtrLevelBuy` / `AtrLevelSell` | Upper ATR threshold that must not be exceeded before entering a trade. | `0.0016` |
| `StdDevPeriodBuy` / `StdDevPeriodSell` | Standard deviation lookback period for the long and short filters. | `54` |
| `StdDevLevelBuy` / `StdDevLevelSell` | Standard deviation ceiling that must not be exceeded before entering a trade. | `0.0051` |
| `CandleType` | Candle data type used by the subscription. | `TimeSpan.FromMinutes(15)` |

## Trading Logic
1. **Bar synchronisation** – the strategy acts only on finished candles received from the configured subscription. This replicates the `OnTick` new-bar guard from the MetaTrader script.
2. **Indicator filters** – for long entries the slow MA must be below the fast MA, ATR must be below `AtrLevelBuy`, standard deviation must be below `StdDevLevelBuy`, and the MA distance must be smaller than `DeltaBuyPips` (if the delta is positive). Short entries invert the conditions and use their own ATR and deviation parameters.
3. **Position gating** – trades are only taken when there is no open position and the latest entry time for the respective side is older than the current candle. This prevents re-entries within the same bar, matching the `m_last_deal_IN_*` check in the source EA.
4. **Order execution** – market orders are placed with the configured volume. Reversal trades automatically flatten the current position before opening a new one thanks to the `Volume + Math.Abs(Position)` quantity that matches the MQL behaviour of closing opposite exposure.

## Risk Management
- **Pip conversion** – pip distances are converted using the security `PriceStep`. For instruments quoted with 3 or 5 decimals the pip size equals `PriceStep * 10`, mirroring the original `digits_adjust` logic.
- **Stop-loss / take-profit** – the strategy tracks price levels internally and exits when the candle range touches the specified stop or target. Both can be disabled by setting the pip distance to zero.
- **Trailing stop** – once price advances beyond `TrailingStopPips + TrailingStepPips`, the stop is moved to maintain the trailing distance. The trailing step requirement matches the MetaTrader implementation and avoids moving the stop by an insignificant amount.

## Implementation Notes
- The strategy uses a single aggregated position because StockSharp works with net positions by default. Although the `MaxPositions` parameter is retained for compatibility, exceeding one simply prevents new entries until the current position is closed.
- Indicator values are calculated using the StockSharp indicator classes and the `Bind` infrastructure to avoid manual buffer access as required by the project guidelines.
- The conversion keeps all comments in English and maps every original input to a dedicated `StrategyParam` so that optimisation and Designer integration remain available.
- When `TrailingStopPips` is positive, `TrailingStepPips` must also be positive. The code stops the strategy early and writes an error message if this requirement is violated, reproducing the safety check from the MQL expert.
