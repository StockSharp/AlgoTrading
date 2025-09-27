# Up3x1 Investor Range Filter Strategy

## Overview
This strategy is a direct port of the MetaTrader 4 expert adviser **up3x1_Investor**. It trades a single instrument using completed candles from a configurable timeframe (H1 by default). The port replicates the original logic with StockSharp high-level APIs and adds clear risk-management parameters.

## Trading Logic
- The strategy evaluates the last fully closed candle and checks that:
  - The candle range (high minus low) exceeds `0.0060` price units.
  - The candle body (absolute difference between open and close) exceeds `0.0050` price units.
- If the candle closed bullish and the above conditions are met, the strategy opens a **long** market position.
- If the candle closed bearish and the conditions are met, the strategy opens a **short** market position.
- Trading is completely disabled on Mondays (to mirror the `DayOfWeek()==1` guard from the MQL code).

## Position Management
- Upon entry, the strategy sets internal targets using the configured step-based distances:
  - `TakeProfitPoints` → distance to the profit target.
  - `StopLossPoints` → protective stop distance.
  - `TrailingStopPoints` → distance used to trail the stop once price moves in favor.
- Stops and targets are evaluated on every finished candle:
  - If price reaches the target, the position is closed at the target price.
  - If price reaches the stop, the position is closed to limit the loss.
  - Once price advances beyond the trailing distance, the stop is moved closer to the market price to lock in profit.
- Additionally, if the 24-period and 60-period simple moving averages calculated on the same candles become equal (within one price step), the position is closed immediately. This mimics the MQL logic where the order is closed when both averages match exactly.

## Volume and Risk Management
- `BaseVolume` defines the fallback lot size when no account-based adjustment can be computed.
- `MaximumRisk` replicates the original `AccountFreeMargin()*MaximumRisk/1000` formula. If the portfolio value is available, the strategy sizes the position as `value * MaximumRisk / 1000`, rounded to one decimal place.
- `DecreaseFactor` imitates the loss-streak reduction: after more than one consecutive loss the volume is decreased proportionally to `losses / DecreaseFactor`.
- `MinimumVolume` ensures that volume never drops below the smallest tradable size used in the MQL script (0.1 lots).

## Parameters
| Name | Default | Description |
| ---- | ------- | ----------- |
| `BaseVolume` | `0.1` | Base position size in lots when no risk adjustment is applied. |
| `MaximumRisk` | `0.2` | Risk factor used to derive volume from account equity (same as the original EA). |
| `DecreaseFactor` | `3` | Reduces position size after consecutive losses. |
| `MinimumVolume` | `0.1` | Smallest allowed volume. |
| `TakeProfitPoints` | `20` | Profit target distance measured in price steps. |
| `StopLossPoints` | `50` | Stop-loss distance measured in price steps. |
| `TrailingStopPoints` | `10` | Trailing stop distance measured in price steps. |
| `SkipMondays` | `true` | Disable all trading activity on Mondays. |
| `CandleType` | `1 hour` | Timeframe for candle subscription. |

## Notes
- The strategy only keeps one position open at a time, matching the original `CalculateCurrentOrders` guard.
- Consecutive loss tracking is purely internal because StockSharp brokers do not expose MetaTrader order history.
- No pending orders are used; all trades are sent as market orders via `BuyMarket` and `SellMarket`.
