# News Trader Strategy

This strategy reproduces the behavior of the original **NewsTrader.mq4** script by arming both sides of the market shortly before a scheduled macroeconomic release. Ten minutes ahead of the configured news timestamp the bot submits a pair of breakout stop orders and immediately attaches protective exits when one side is triggered.

## Core Logic

- Uses a 1-minute candle subscription (configurable) purely as a timing source.
- Calculates the activation moment as `news time - LeadMinutes` and waits until the first finished candle whose open time is at or beyond that point.
- Places a sell stop below the current price and a buy stop above it, offset by `BiasPips` converted through `Security.PriceStep` (mirrors the `bias * Point` logic in MQL4).
- Once a pending order is filled the opposite pending order is cancelled; dedicated stop-loss and take-profit orders are placed using the configured pip distances.
- Stop-loss or take-profit fills cancel the remaining protective order and flatten the strategy.
- Calls `StartProtection()` on start so the strategy cooperates with higher-level StockSharp safeguards.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `TradeVolume` | Contracts submitted with each pending order. | `1` |
| `StopLossPips` | Stop-loss distance in pips (0 disables the stop order). | `10` |
| `TakeProfitPips` | Take-profit distance in pips (0 disables the target order). | `10` |
| `BiasPips` | Distance from the reference price to the breakout stop orders. | `20` |
| `LeadMinutes` | Minutes before the news timestamp when the breakout orders are armed. | `10` |
| `NewsYear`, `NewsMonth`, `NewsDay`, `NewsHour`, `NewsMinute` | Components of the scheduled news time (platform clock). | `2010`, `3`, `8`, `1`, `30` |
| `CandleType` | Candle data type used to track time progression. | `1 Minute` |

## Implementation Notes

- The strategy sets `Volume` to `TradeVolume` during `OnStarted`, ensuring that helper methods such as `BuyStop` and `SellStop` use the expected size.
- `Security.PriceStep` must be defined; otherwise the logic throws an exception because pip-based distances cannot be translated to prices.
- Candle close prices are used as a proxy for the latest bid/ask when computing stop levels—matching the original MQL4 logic that relied on the most recent quote at trigger time.
- Pending orders are placed only once; the algorithm does not re-arm itself after the configured news event passes.
- Protective orders are skipped when their respective pip distance is zero, which keeps the behavior configurable for manual intervention.

## Files

- `CS/NewsTraderStrategy.cs` — C# implementation of the strategy.

Python version is intentionally omitted as requested.
