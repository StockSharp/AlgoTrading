# Extreme EA (StockSharp Conversion)

The **Extreme EA** strategy is a trend-following Expert Advisor originally written for MetaTrader. It combines two moving averages with a Commodity Channel Index (CCI) filter and an adaptive money-management module. This port keeps the trading logic intact while exposing all important knobs through StockSharp's high-level API. The strategy operates on finished candles only and is compatible with multiple timeframes by running the moving averages and CCI on independent candle subscriptions.

## Strategy Overview

1. **Trend filter:** Two moving averages are calculated on the configurable `MaCandleType`. The fast average tracks short-term momentum while the slow average defines the dominant trend slope. The strategy checks the slope of the slow average using the previous two values to mimic the original `CopyBuffer` array offsets from the MQL code.
2. **Momentum filter:** The CCI is evaluated on its own timeframe (`CciCandleType`) and price source. The latest completed value is cached and reused until a new CCI candle appears, which matches the behaviour of the MetaTrader buffers.
3. **Entry rules:**
   - Enter long when the slow MA is rising, the fast MA is rising, and the CCI drops below the lower level.
   - Enter short when the slow MA is falling, the fast MA is falling, and the CCI climbs above the upper level.
4. **Exit rules:**
   - Close all longs if the slow MA stops rising.
   - Close all shorts if the slow MA stops falling.

## Risk Management

- **MaximumRisk** controls the target position size based on current portfolio equity and the latest price. If the computed volume is zero or the portfolio values are unavailable, the strategy falls back to the configured `Volume` or the exchange minimum.
- **DecreaseFactor** reduces the calculated volume after two or more consecutive losing trades. The reduction mirrors the original formula `lot = lot - lot * losses / DecreaseFactor`.
- **HistoryDays** caps how long a loss streak is remembered. If a closing trade happens after the specified number of days, the streak is reset before applying the reduction.
- **MaxPositions** limits pyramiding by bounding the net exposure per direction. When the cap is reached, new entries are suppressed until the exposure drops.

## Parameters

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `MaximumRisk` | `decimal` | `0.05` | Fraction of equity used to size each new trade. |
| `DecreaseFactor` | `decimal` | `6` | Loss-streak reduction factor. Set to `0` to disable. |
| `HistoryDays` | `int` | `60` | Number of days preserved when counting consecutive losses. |
| `MaxPositions` | `int` | `3` | Maximum simultaneous entries per direction. |
| `FastMaPeriod` | `int` | `15` | Period for the fast moving average. |
| `SlowMaPeriod` | `int` | `75` | Period for the slow moving average. |
| `CciPeriod` | `int` | `12` | Lookback length for the CCI. |
| `CciUpperLevel` | `decimal` | `50` | Upper CCI threshold used for shorts. |
| `CciLowerLevel` | `decimal` | `-50` | Lower CCI threshold used for longs. |
| `MaCandleType` | `DataType` | `15m` | Timeframe for both moving averages and execution. |
| `CciCandleType` | `DataType` | `30m` | Timeframe for the CCI filter. |
| `MaMethod` | `MaMethod` | `Exponential` | Smoothing method (Simple, Exponential, Smoothed, LinearWeighted). |
| `MaPriceMode` | `AppliedPriceMode` | `Median` | Price input for the moving averages. |
| `CciPriceMode` | `AppliedPriceMode` | `Typical` | Price input for the CCI. |

## Implementation Notes

- The strategy subscribes to the moving-average timeframe once and optionally to a second subscription for the CCI. When both timeframes match, a single subscription feeds both components, reproducing the original single-chart workflow.
- Previous indicator values are cached in private fields to emulate the `ma_slow_array[1]`, `ma_slow_array[2]`, and `ma_fast_array[0]` comparisons without resorting to manual indicator buffers.
- Position sizing is normalised against the instrument volume step, minimum, and maximum to avoid rejected orders.
- The risk module records entry and exit prices to estimate realised PnL per completed position, which replaces the `HistoryDealGet` loop used in MetaTrader.

## Differences from the MQL Version

- MetaTrader-specific functions such as `FreeMarginCheck`, `MarginCheck`, and `HistorySelect` are approximated with StockSharp portfolio metrics and the internal loss streak tracker.
- The StockSharp port operates on net positions. Closing orders flatten the entire exposure in the relevant direction, aligning with the consolidated position model.
- Logging routines from the original EA were omitted in favour of StockSharp's built-in diagnostics.
