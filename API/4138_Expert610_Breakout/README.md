# Expert610 Breakout Strategy

## Overview
The Expert610 Breakout strategy is a C# port of the MetaTrader 4 expert advisor `Expert610.mq4`. The original robot waits for a
wide candle and then parks both a buy stop and a sell stop order around the previous bar. Position size is derived from the
percentage of free capital the trader is willing to risk, and the stop-loss/take-profit distances are expressed in pips. This
StockSharp version mirrors that behaviour using the high-level API while exposing every tuning knob as a strategy parameter.

## Trading Logic
1. **Data collection**
   - The strategy subscribes to a configurable candle type and stores the most recent finished bar.
   - Order book updates are monitored to estimate the current bid/ask spread. When no depth is available the spread contribution
     defaults to zero, reproducing the original EA behaviour on brokers without live spreads.
2. **Volatility filter**
   - The previous candle high minus the current close and the current close minus the previous low must both exceed
     `ThresholdPips` (converted to absolute price units).
   - The current candle open must lie strictly below the prior high to allow a buy setup and strictly above the prior low to
     allow a sell setup. When both conditions hold the algorithm stages symmetric pending orders.
3. **Order placement**
   - Buy stops are placed at `previous high + BreakoutOffset + spread`, matching the MT4 code where the ask price is used.
   - Sell stops are placed at `previous low - BreakoutOffset`, also staying faithful to the original script that ignores the
     spread on the bid side.
   - Only one pair of pending orders may be active at any time. If an order is already working the new signals are skipped.
4. **Risk management**
   - The lot size is derived from free capital (`Portfolio.CurrentValue - Portfolio.BlockedValue`) multiplied by
     `RiskPercent / 100`. The amount is rounded to `RoundingDigits` and converted into lots using the same heuristic as the MT4
     code: `lot = risk / stopPips * 0.1`, which assumes one pip of a 0.1 lot equals one unit of account currency.
   - The computed lot is aligned to exchange limits and the `MinimumVolume` parameter before being sent to the venue.
   - `StartProtection` attaches price-based stops and targets to every resulting position, so that fills immediately receive the
     configured `StopLossPips` and `TakeProfitPips` offsets.

## Parameters
| Name | Description | Default | Notes |
| --- | --- | --- | --- |
| `RoundingDigits` | Decimal places used when rounding risk and volume calculations. | `2` | Must be non-negative. |
| `RiskPercent` | Percentage of free capital risked on each entry. | `1` | Set to `0` to disable dynamic sizing and fall back to `MinimumVolume`. |
| `MinimumVolume` | Hard lower bound for pending order volume. | `0.1` | Also respects the security's `MinVolume` and `VolumeStep`. |
| `ThresholdPips` | Minimum distance from the last close to the prior candle extremes. | `5` | Measured in pips and converted with the detected pip size. |
| `BreakoutOffsetPips` | Buffer added beyond the previous high/low when staging orders. | `2` | Applied symmetrically to both sides. |
| `StopLossPips` | Stop-loss distance attached to filled orders. | `5` | Expressed in pips and sent to `StartProtection`. |
| `TakeProfitPips` | Take-profit distance attached to filled orders. | `10` | Expressed in pips; set to `0` to disable the target. |
| `CandleType` | Candle series used to evaluate the breakout. | `1 hour` time frame | Accepts any `DataType` supported by StockSharp. |

## Implementation Notes
- The pip size is derived from the instrument's `PriceStep` and `Decimals` (5-digit and 3-digit Forex symbols receive a ×10
  adjustment) to keep the conversion identical to the MQL4 formula.
- Order size rounding honours `VolumeStep`, clamps to `MinVolume`/`MaxVolume`, and finally enforces the strategy-level
  `MinimumVolume` so that resulting requests are always tradable.
- Spread compensation uses the best bid/ask extracted from the subscribed order book. This produces the same entry price as the
  MT4 implementation when the platform provides live spreads and gracefully degrades otherwise.
- Pending orders are cleared from the internal state once StockSharp reports them as filled, cancelled, or failed, allowing the
  logic to submit fresh orders on the next qualified candle.

## Differences vs. the MQL Version
- The original EA rounded both risk and volume using `Digits2Round`. The port keeps that feature but additionally aligns the
  result to exchange-specific volume steps.
- Instead of attaching protective prices directly to the pending orders, the StockSharp strategy relies on `StartProtection` so
  every filled position automatically receives stop-loss and take-profit orders.
- Portfolio information replaces the MT4 functions `AccountBalance()` and `AccountMargin()` to obtain free capital; if this data
  is unavailable the strategy gracefully falls back to `MinimumVolume` sizing.
- All calculations operate on finished candles only, preventing intra-bar repainting and matching the `start()` tick-based loop
  once the bar closes.
