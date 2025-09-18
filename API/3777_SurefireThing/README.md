# SurefireThing Strategy

## Overview
The SurefireThing strategy is a StockSharp high-level port of the MetaTrader 4 expert advisor *Surefirething*. It operates on completed candles, computes pending order levels from the previous session range, and resets exposure at the turn of each trading day. The logic is centred on deploying a symmetrical pair of limit orders that attempt to capture mean reversion around the prior close.

## Trading Logic
- At the close of every trading day the strategy attempts to flatten the position and cancels any active pending orders.
- Using the last completed candle of the previous day it measures the price range `(High - Low)` and multiplies it by `RangeMultiplier` (defaults to 1.1 as in the original EA).
- Half of the adjusted range is added to the previous close to obtain the sell-limit entry price. The same distance is subtracted from the close to place the buy-limit order.
- Stop-loss and take-profit offsets are expressed in price steps. When the instrument exposes a valid `Security.Step`, they are converted to absolute distances and managed through `StartProtection` so that filled positions receive protective orders automatically.
- Orders are submitted once per trading day. If fills occur, the attached protection handles exits; otherwise orders remain active until the next daily reset.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `OrderVolume` | Volume submitted with each pending order. | `0.1` |
| `TakeProfitPoints` | Distance (in price steps) for the profit target. Converted to an absolute offset when the step is known. | `10` |
| `StopLossPoints` | Distance (in price steps) for the protective stop. Converted the same way as the profit target. | `15` |
| `RangeMultiplier` | Factor applied to the previous candle range before computing entry prices. | `1.1` |
| `CandleType` | Primary timeframe processed by the strategy. Defaults to 1-minute candles but can be adjusted to match the original chart. | `TimeSpan.FromMinutes(1)` |

## Implementation Notes
- High-level API: candles are consumed through `SubscribeCandles(CandleType)` and processed in the `ProcessCandle` handler once they are finished.
- Daily reset: `CloseForNewDay` cancels pending orders and closes positions whenever a new calendar day is detected from candle timestamps.
- Protective logic: `ConfigureProtection` translates the point-based risk controls into `Unit` instances and activates `StartProtection` so that stop-loss and take-profit orders are automatically recreated after fills.
- Order lifecycle: references to both pending orders are stored and cleared via `CancelPendingOrder` as well as `OnOrderChanged` when the orders finish or are cancelled.
- Price normalisation: `Security.ShrinkPrice` is used to round computed prices to the instrument tick size before submitting new orders.

## Usage Recommendations
- Align `CandleType` with the timeframe used by the original EA (typically the chart where it was attached) to maintain the same reference candles.
- Adjust `RangeMultiplier` when instruments display different volatility characteristics so that the pending orders remain within realistic distances.
- If the broker enforces minimum stop distances, make sure `TakeProfitPoints` and `StopLossPoints` respect those constraints after conversion to absolute prices.
- The strategy assumes continuous intraday data. When large gaps occur (weekends, holidays), the next available candle still triggers a reset and new order placement based on the last observed bar.
