# SuperForexV2 Strategy

## Overview
SuperForexV2 is a StockSharp port of the MetaTrader 4 expert advisor `SuperForexV2.mq4`. The original script combines a short-term
Relative Strength Index (RSI) oscillator with fixed take-profit, stop-loss and trailing stop distances. The C# implementation
rebuilds the same decision process with the high-level StockSharp API: the strategy observes finished candles, reacts to RSI
threshold crossings, and manages a single net position using pip-based risk limits.

## Trading Logic
1. **Indicator pipeline**
   - Subscribes to the configurable candle series (15-minute bars by default) and feeds every finished bar into an RSI indicator.
   - The RSI length is configurable and defaults to the original MT4 value of 4.
2. **Dynamic position sizing**
   - Before every entry the strategy derives a working lot size from the current portfolio value divided by `BalanceToVolumeDivider`.
   - The resulting volume is clamped by `InitialVolume` (fallback when the balance is unknown) and `MaxVolume`, then rounded to the
instrument’s volume step.
3. **Entry rules**
   - When there is no open position and RSI falls below `RsiLowerLevel`, a market buy order is placed.
   - When RSI rises above `RsiUpperLevel`, a market sell order is submitted.
4. **Exit and risk management**
   - Each position stores absolute stop-loss and take-profit levels computed from the pip-based distances.
   - On every finished candle the strategy checks whether the bar touched those levels; if so, it closes the position at market.
   - A trailing stop mimics the MT4 logic: once price has advanced by at least `TrailingStopPips`, the stop is pulled closer so the
current profit is locked in.
   - Positions are also closed whenever the RSI crosses to the opposite extreme (e.g., longs exit when RSI exceeds the upper level).
5. **Position scope**
   - The bot mirrors the EA’s “one trade per symbol” behaviour by enforcing a flat book before evaluating new entries.

## Parameters
| Name | Description | Default | Notes |
| --- | --- | --- | --- |
| `CandleType` | Candle series driving the indicator calculations. | `15m` time frame | Accepts any `DataType` supported by the connector. |
| `RsiPeriod` | RSI lookback length. | `4` | Must be greater than zero. |
| `RsiUpperLevel` | Overbought threshold used for shorts and long exits. | `62` | Matches the MT4 input `Pos`. |
| `RsiLowerLevel` | Oversold threshold used for longs and short exits. | `42` | Matches the MT4 input `Neg`. |
| `TakeProfitPips` | Take-profit distance expressed in pips. | `109` | Set to `0` to disable the take-profit. |
| `StopLossPips` | Stop-loss distance expressed in pips. | `9` | Set to `0` to disable the stop-loss. |
| `TrailingStopPips` | Trailing stop distance expressed in pips. | `6` | Set to `0` to turn off trailing behaviour. |
| `InitialVolume` | Fallback lot size when the portfolio balance is not available. | `0.1` | Also used if dynamic sizing yields a non-positive value. |
| `MaxVolume` | Maximum volume allowed per entry. | `100` | Prevents the balance-based sizing from overscaling. |
| `BalanceToVolumeDivider` | Divider applied to the account balance to compute volume. | `10000` | Replicates the MT4 formula `Lots = AccountBalance()/10000`. |

## Implementation Notes
- Candle processing happens only after `CandleStates.Finished` to mirror MT4’s `start()` tick-end behaviour while avoiding
incomplete data.
- Pip distances are converted into absolute prices using the instrument’s `PriceStep`. For 3- and 5-digit Forex symbols the code
multiplies the step by ten so the StockSharp “pip” matches the MetaTrader point definition.
- Stop-loss, take-profit and trailing levels are stored internally and checked against candle highs and lows, because StockSharp
does not automatically manage MT4-style order-level stops.
- The strategy rounds the computed volume to the nearest valid lot while respecting `MinVolume`, `MaxVolume` and `VolumeStep`
limits exposed by the security.
- Only one net position is allowed at a time; the entry logic exits early if the strategy is already long or short.

## Differences Compared to the MT4 Version
- The StockSharp port works on finished candles instead of individual ticks, so intrabar stop or target hits are detected on the
next bar close.
- MetaTrader’s `AccountFreeMargin()` guard is replaced by a safer balance-derived volume; if the connector cannot provide the
portfolio value the fallback `InitialVolume` is used instead of aborting.
- Order stop-loss and take-profit values are not sent to the broker. Instead, the strategy closes positions at market once a level
is breached, because high-level StockSharp orders rely on strategy-managed exits.
- The `NumeroMagico` input used to filter MT4 orders is unnecessary in StockSharp and has been omitted.
- Logging messages from the original EA are not reproduced; StockSharp’s own logging facilities should be used if further
instrumentation is needed.
