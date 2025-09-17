# Gap DM Strategy

## Overview
Gap DM is a contrarian gap-trading strategy that tracks the distance between the previous session close and the next session open. When the market opens with a visible gap, the strategy immediately trades in the opposite direction, expecting the price to revert and fill the gap. The implementation follows the original MetaTrader 5 algorithm "Gap DM" by cmillion, adapted to StockSharp's high-level API. All trading decisions are derived from completed candles of the selected timeframe, ensuring deterministic behaviour in backtests and live execution.

## Signal Logic
1. Subscribe to the candle series specified by `CandleType`.
2. Wait for each candle to finish (`CandleStates.Finished`).
3. Compare the closing price of the previous candle with the opening price of the current candle.
4. Calculate the gap size in pips using the instrument's price step. A multiplier of 10 is applied automatically for 3- and 5-digit quotes, reproducing the MT5 point-to-pip conversion.
5. If the current open is **below** the previous close by at least `Minimum Gap (pips)`, treat it as a bearish gap and **enter long**.
6. If the current open is **above** the previous close by at least `Minimum Gap (pips)`, treat it as a bullish gap and **enter short**.
7. Skip entries when trading is not allowed (e.g., the strategy is disconnected or still warming up).

## Position Sizing and Limits
- `Order Volume` specifies the lot size for each new trade. The strategy also uses the value to close or reverse existing exposure, keeping the net position consistent with StockSharp's netted accounting model.
- `Max Positions` defines the maximum aggregated volume (in lots) that can be held in one direction. When the limit is reached, new entries in the same direction are ignored.
- When reversing from short to long (or vice versa), the strategy automatically adds the volume needed to close the opposite exposure before opening the new position.

## Risk Management
- `Stop Loss (pips)` places a protective stop relative to the entry price. The stop is evaluated on every completed candle. If the candle's range trades through the stop level, the position is closed immediately with a market order.
- `Take Profit (pips)` works symmetrically to the stop-loss. Set the parameter to zero to disable the target.
- No trailing-stop is applied by default; the exit logic matches the source Expert Advisor.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `Order Volume` | Trading volume used for each entry in lots. | `1` |
| `Stop Loss (pips)` | Distance of the protective stop. Set to `0` to disable. | `0` |
| `Take Profit (pips)` | Distance of the profit target. Set to `0` to disable. | `0` |
| `Minimum Gap (pips)` | Minimum difference between the previous close and current open required to generate a signal. | `1` |
| `Max Positions` | Maximum accumulated exposure allowed in a single direction (in lots). | `15` |
| `Candle Type` | Timeframe used to measure session gaps. | `1 Hour` |

## Execution Flow
1. Reset cached state on every restart (gap thresholds, stop levels, previous close).
2. Start candle subscription and draw chart elements (candles and trades) when a chart area is available.
3. On each finished candle:
   - Update or reset the active stop and target depending on the current position.
   - Evaluate the gap conditions and place market orders when a valid signal appears.
   - Re-check protective orders so stop-loss or take-profit events inside the same candle are handled without delay.
4. Store the latest close for the next evaluation.

## Notes and Differences vs. the Original MT5 Version
- StockSharp strategies operate with net positions. The algorithm emulates multiple entries by scaling the net exposure rather than creating separate tickets. This keeps behaviour close to the MT5 Expert Advisor while respecting StockSharp's accounting model.
- All comments in the source code are in English, matching project guidelines.
- Money-management via percentage risk (`risk` mode in the MT5 script) is not reproduced; instead, a fixed volume parameter is provided. Set `Order Volume` to the lot size you want to trade.

## Requirements
- Compatible with any instrument that exposes a valid `PriceStep`.
- Works on time-based, volume-based, or range-based candles supported by StockSharp, as long as the gap concept is meaningful.
- Requires StockSharp environment capable of executing market orders and monitoring own trades.

