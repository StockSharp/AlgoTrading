# Ma SAR ADX Strategy

## Overview
This strategy is a StockSharp high-level API conversion of the original **MaSarADX.mq5** MetaTrader 5 expert advisor. The system combines a simple moving average trend filter with Directional Movement Index (ADX) signals and the Parabolic SAR trailing stop. Trading decisions are evaluated only on completed candles, replicating the "first tick of a new bar" behavior from the MQL version. When the candle close is aligned with both the moving average trend and the ADX directional balance, a position is opened. Parabolic SAR guides both trade direction and exits by forcing a full liquidation when price crosses to the opposite side of the SAR dots.

## Indicators and Data
- **Simple Moving Average (SMA)** – provides the primary trend direction filter. Default length: 100 candles.
- **Average Directional Index (ADX)** – supplies +DI and −DI to confirm directional strength. Default length: 14.
- **Parabolic SAR** – acts as a stop-and-reverse overlay and defines exit conditions. Default acceleration: 0.02; maximum acceleration: 0.10.
- **Candles** – any timeframe can be requested. By default the strategy subscribes to 1-hour candles, but the parameter can be adjusted to match the symbol and testing regime.

The implementation subscribes to StockSharp candle streams and binds all three indicators using the `BindEx` helper so that every callback receives synchronized values for the same candle.

## Trading Logic
### Long Entry
1. Candle close is above the moving average.
2. +DI is greater than or equal to −DI, indicating bullish directional pressure.
3. Candle close is above the Parabolic SAR value.
4. No long position is currently open (`Position <= 0`).

When all rules align, a market buy order is sent for the configured base volume plus any size required to cover a short position.

### Short Entry
1. Candle close is below the moving average.
2. +DI is less than or equal to −DI, indicating bearish directional pressure.
3. Candle close is below the Parabolic SAR value.
4. No short position is currently open (`Position >= 0`).

A market sell order is placed when all short rules match.

### Exits
- **Long positions** are closed immediately once price falls below the Parabolic SAR.
- **Short positions** are covered when price rises above the Parabolic SAR.

No separate stop-loss or take-profit levels are added; the SAR crossing is the only exit signal, following the original expert advisor. Because exits are evaluated before new entries, the strategy will not flip from short to long (or vice versa) on the same candle, mirroring the two-step open/close cycle of the MQL script.

## Parameters
| Name | Description | Default | Notes |
| --- | --- | --- | --- |
| `MaPeriod` | Length of the simple moving average used to define the trend filter. | 100 | Optimizable, must be greater than zero. |
| `AdxPeriod` | Period of the ADX calculation that produces +DI and −DI. | 14 | Optimizable, must be greater than zero. |
| `SarStep` | Acceleration factor and increment for the Parabolic SAR. | 0.02 | Equivalent to the MQL `step` parameter. |
| `SarMax` | Maximum acceleration factor for Parabolic SAR. | 0.10 | Mirrors the MQL `maximum` setting. |
| `Volume` | Base order size for new entries. | 1 | Replaces the margin-based lot sizing from the MetaTrader version. The actual order size is `Volume + |Position|` so that reversals flatten existing exposure. |
| `CandleType` | The candle data type subscribed through StockSharp. | 1 hour | Adjustable to any timeframe. |

## Implementation Notes
- Indicator processing uses StockSharp’s high-level `BindEx` pipeline, ensuring that SMA, ADX, and SAR are updated in lock-step without manual buffering.
- Exits are executed even if `AllowTrading` is temporarily disabled, keeping risk controls active at all times.
- Charting helpers are included: the primary panel plots price, SMA, and SAR, while a secondary panel plots the ADX indicator for diagnostics.
- Logging statements describe every trade decision with the underlying indicator values to simplify forward testing and debugging.

## Usage Guidelines
1. Attach the strategy to a security and portfolio in the Designer or Backtester.
2. Adjust the candle type to match your trading horizon (e.g., M15, H1, or D1 candles).
3. Tune the moving average period, ADX period, and SAR parameters to fit the instrument’s volatility.
4. Set the `Volume` parameter to your preferred position size. If you need the adaptive money management used in the original script, integrate your own portfolio-based sizing before sending orders.
5. Run the strategy. Trades will trigger only after all indicators have produced enough historical values to be formed.

## Differences from the Original Expert Advisor
- Margin-based lot calculation has been replaced with a fixed `Volume` parameter to keep the strategy broker-neutral inside StockSharp.
- Trade management, indicator values, and the evaluation order (exit before entry) strictly follow the MetaTrader reference logic.
- All comments inside the source code are in English to comply with project guidelines.
