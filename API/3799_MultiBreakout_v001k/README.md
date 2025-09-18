# MultiBreakout V001k Strategy

## Overview
The MultiBreakout V001k strategy reproduces the classic MT4 expert advisor "Multibreakout_v001k". It trades breakouts from the previous hourly session by stacking buy-stop and sell-stop orders once the reference hour finishes. Position management follows the original staged take-profit and break-even logic, including the optional moving break-even that trails stops using the latest hourly lows/highs.

## Trading Rules
1. **Reference hour** – Up to four trading sessions can be defined. After each enabled session hour closes, the strategy measures the finished hourly candle and prepares orders for the next hour.
2. **Entry placement** –
   - Buy-stop orders are positioned at the previous hour high plus the current spread and an additional entry buffer (`PipsForEntry`).
   - Sell-stop orders are positioned at the previous hour low minus the entry buffer.
   - Each side places `NumberOfOrdersPerSide` pending orders with identical volume.
3. **Take-profit ladder** – Every entry receives an individual profit target spaced by `TakeProfitIncrement` points. When the market touches each level, the strategy closes one tranche at market to mimic the original MT4 take-profit queue.
4. **Stop-loss management** – An initial stop is set `StopLoss` points away from the entry price. Once price moves `BreakEven` points in favour, the stop jumps to break-even. If `MovingBreakEven` is enabled and the configured delay passes, the stop trails using the most recent hourly lows (for longs) or highs (for shorts) when those levels continue to tighten.
5. **Session exit** – At `ExitMinute` within the configured session hour the strategy flat-out closes all positions and removes every pending order.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TradeVolume` | Volume for each breakout order. |
| `NumberOfOrdersPerSide` | Quantity of stacked pending orders for both directions. |
| `TakeProfitIncrement` | Distance (in points) between consecutive take-profit targets. |
| `PipsForEntry` | Extra points added to the breakout trigger above/below the session range. |
| `StopLoss` | Initial stop distance from the entry price. |
| `BreakEven` | Profit (in points) required before the stop moves to break-even. |
| `MovingBreakEven` | Enables the moving break-even trailing logic. |
| `MovingBreakEvenHoursToStart` | Delay (in hours) after the reference session before the moving break-even may trail. |
| `BrokerOffsetToGmt` | Hour offset between broker time and GMT used by the moving break-even scheduler. |
| `TradeSession1..4` | Toggles for the four independent trading sessions. |
| `SessionHour1..4` | Hour (0-23) defining each reference session. |
| `ExitMinute` | Minute within the session hour to liquidate positions and cancel orders. |
| `CandleType` | Candle type used to measure the reference hour (defaults to 1-hour candles). |

## Usage Notes
- Ensure the instrument has a valid `PriceStep` so point-value calculations match the MT4 version.
- The strategy assumes broker times are aligned with the candle timestamps. Adjust `BrokerOffsetToGmt` when a different MT4 server offset was used historically.
- Moving break-even evaluates the two latest finished hourly candles before tightening the stop, matching the behaviour from the original expert advisor.
