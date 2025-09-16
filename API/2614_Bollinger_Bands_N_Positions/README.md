# Bollinger Bands N Positions Strategy

## Overview

This strategy is a StockSharp port of the MetaTrader expert advisor **Bollinger Bands N positions**. It monitors closing prices relative to a Bollinger Bands envelope and enters a position whenever the market finishes a bar outside of the channel. Position management replicates the original expert by enforcing a cap on total exposure, placing fixed stop-loss and take-profit offsets, and activating a trailing stop once the trade is sufficiently in profit.

## Trading Logic

1. Subscribe to the configured candle type and calculate Bollinger Bands with the selected period and width.
2. On every finished candle the strategy first checks whether an existing position must be closed:
   - Long positions exit when price hits the fixed stop-loss, fixed take-profit, or when the trailing stop level is breached.
   - Short positions apply the symmetrical logic.
3. If trading is allowed and no exit has occurred on the current bar, entry signals are evaluated:
   - When the closing price is above the upper band the strategy flattens any short exposure and, if within the position cap, opens a new long position with the requested volume.
   - When the closing price is below the lower band it flattens any long exposure and opens a short position in the same manner.
4. Trailing stops move in increments defined by the trailing step parameter once the trade is ahead by the trailing distance plus the trailing step. The trailing level stays behind price by the trailing distance and only advances when the profit increases by at least one trailing step.

## Position Management

- **Max Positions** defines the maximum net exposure measured as `MaxPositions × Volume`. Because StockSharp operates in netting mode, the strategy can hold only one net position at a time. The parameter therefore acts as a safety cap that prevents the strategy from re-entering when the current absolute position already reaches the configured limit.
- Stop-loss and take-profit distances are specified in pips. The strategy converts them into prices using the security `PriceStep`. If the instrument uses fractional pip pricing you may need to adjust the values accordingly.
- Trailing stops require both the distance and the step to be positive. When the trailing stop distance is set to zero the trailing module is disabled.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `Volume` | Order size in lots used for every entry. | `0.1` |
| `MaxPositions` | Net position cap expressed in multiples of `Volume`. | `9` |
| `BollingerPeriod` | Lookback period for the Bollinger moving average. | `20` |
| `BollingerWidth` | Standard deviation multiplier for the Bollinger Bands. | `2` |
| `StopLossPips` | Stop-loss distance in pips. | `50` |
| `TakeProfitPips` | Take-profit distance in pips. | `50` |
| `TrailingStopPips` | Trailing stop distance in pips. Set to `0` to disable trailing. | `5` |
| `TrailingStepPips` | Minimum profit increment required before the trailing stop advances. | `5` |
| `CandleType` | Time-frame or custom candle type used to build the Bollinger Bands. | `1 minute time frame` |

## Differences from the MQL5 Expert

- The original expert operates in MetaTrader's hedging mode and can hold simultaneous long and short positions. StockSharp strategies are netted, so this port flattens opposite exposure before entering a new trade. The `MaxPositions` parameter therefore limits the absolute size of the net position instead of the number of independent tickets.
- Order stops are simulated inside the strategy instead of being sent as attached stop orders. This matches the trailing logic of the MQL implementation but means exits occur on the next finished candle.
- Trailing configuration is validated at startup. Enabling a trailing stop with a zero trailing step throws an exception to mimic the original initialization check.

## Usage Notes

1. Configure `Volume`, `MaxPositions`, and the risk parameters to match the instrument's contract size and tick value.
2. Ensure the security exposes a valid `PriceStep`. If the step is zero or missing the strategy falls back to `1`, which may not fit all markets.
3. Start the strategy only after the indicator warm-up period (Bollinger period) has completed to avoid acting on incomplete data.
4. Monitor logs for trailing-step validation errors when customizing the risk settings.
