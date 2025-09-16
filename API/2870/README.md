# Binario Strategy

## Overview
Binario is a stop-entry breakout system that surrounds price with two moving-average envelopes calculated on candle highs and lows. When price trades between the envelopes the strategy places symmetrical stop orders to capture the next directional expansion. Orders inherit fixed stop-loss and take-profit offsets that mirror the MetaTrader 5 expert advisor.

The StockSharp port keeps the core idea while leveraging high-level API features such as candle subscriptions, indicator binding, and automated order management. Level-1 data is consumed to estimate the current bid/ask spread, which is required to reproduce the original entry offsets.

## Trading Logic
1. Build two moving averages (upper on highs, lower on lows) using configurable methods and period.
2. When the latest close is between the averages:
   - Place a buy-stop above the upper average plus the configured difference buffer and current spread.
   - Place a sell-stop below the lower average minus the same buffer.
3. Each pending order stores its own stop-loss and take-profit levels derived from the moving averages, `PointValue`, and pip-based parameters.
4. When an order fills, the opposite pending order is cancelled and fresh protective orders (stop-loss and take-profit) are registered for the open position.
5. Trailing stop logic tightens the stop when price advances by at least `TrailingStopPips + TrailingStepPips` from the entry price, matching the incremental behaviour of the MQL implementation.
6. Whenever the position flips from long to short (or vice versa), existing protective orders are cancelled to avoid conflicts.

## Parameters
- `CandleType` – time frame used for calculations.
- `MaPeriod` – length of both moving averages.
- `MaShift` – bar shift applied to each moving average (0 reproduces the default EA behaviour).
- `HighMaMethod` / `LowMaMethod` – smoothing methods (`SMA`, `EMA`, `SMMA`, `WMA`, `LWMA`).
- `PointValue` – absolute price value that represents one pip for the traded symbol (0.0001 for most FX majors, 0.01 for JPY pairs, etc.).
- `DifferencePips` – buffer between the averages and the pending orders, expressed in pips.
- `TakeProfitPips` – profit target distance in pips.
- `TrailingStopPips` – trailing stop distance in pips (set to zero to disable trailing).
- `TrailingStepPips` – minimum additional profit in pips required before tightening the stop again.
- `Volume` (inherited from `Strategy`) – base order size; reversal orders automatically add the absolute position size to fully flip the exposure.

All pip-based parameters are translated into absolute prices via `PointValue`, mirroring the `Point * digits_adjust` conversion performed in the MT5 version.

## Order Management
- Pending stop orders remain active only while the strategy is flat on their respective side (no long position for a new buy-stop, no short position for a new sell-stop).
- After an entry triggers, the strategy submits matching stop-loss and take-profit orders and removes the unused opposite stop-entry.
- Position reversals cancel legacy protective orders before registering new ones, preventing orphaned stops.

## Trailing Behaviour
- Long positions: once price gains at least `TrailingStopPips + TrailingStepPips` pips, the stop is shifted to `close - TrailingStopPips` as long as the move exceeds the previous stop by at least `TrailingStepPips`.
- Short positions: when price falls by the same threshold, the stop is lowered to `close + TrailingStopPips`, also honouring the step filter.
- Trailing uses the most recent candle close as a proxy for the MT5 `PriceCurrent()` value.

## Data Requirements
- Candles for the selected `CandleType`.
- Level-1 quotes to retrieve best bid/ask prices and calculate the spread. When the spread is unavailable the strategy falls back to the instrument minimum price step or `PointValue`.

## Differences vs. MetaTrader 5 Version
- Position sizing is controlled through the StockSharp `Volume` property instead of the original Lots/Risk combination.
- Protective orders are recreated when trailing modifies prices because StockSharp stop orders cannot be amended in place.
- Execution prices reported by MyTrades are approximated by stored order prices; adjust `PointValue` and pip parameters to match broker specifications.
- The strategy runs on finished candles, equivalent to enabling “expert every tick” with bar-open evaluation in the MT5 script.

## Usage Notes
1. Set `PointValue` according to the instrument tick-to-pip relationship.
2. Configure moving average methods and period to match your MT5 template.
3. Choose suitable pip distances for difference, take-profit, and trailing components.
4. Ensure Level-1 data is available so the spread component can be applied accurately.
