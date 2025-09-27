# Bull vs Medved Window Strategy

## Overview
The Bull vs Medved strategy is a StockSharp conversion of the MetaTrader 4 expert *Bull_vs_Medved.mq4*. The system attempts to
enter pullbacks within a strong bullish or bearish impulse by placing pending limit orders during six predefined five-minute
windows spread across the trading day. The StockSharp version keeps the idea of trading only once per window, cancels stale
pending orders, and uses the body size of the signal candle to derive dynamic stop-loss and take-profit distances.

## Trading logic
1. Subscribe to the candle stream defined by `CandleType` and handle only finished candles.
2. Maintain the last two completed candles so the current candle (`shift1`), the previous candle (`shift2`) and the candle
   before that (`shift3`) replicate the `Close[1..3]` references used in MetaTrader.
3. During each trading window (`EntryWindowMinutes` minutes starting at `StartTime0..5`) check the following patterns:
   - **Bull**: `shift3` closes above the open of `shift2`, the body of `shift2` is at least 10 broker points and the body of
     `shift1` is at least `CandleSizePoints` points. If `IsBadBull` is false (three long bodies in a row) place a buy limit.
   - **Cool Bull**: `shift2` is a minimum 20-point pullback that closes below the open of `shift1`, which in turn closes above
     the `shift2` open with a body of at least 40% of the threshold; place a buy limit.
   - **Bear**: the body of `shift1` is at least `CandleSizePoints` points but bearish; place a sell limit.
4. Buy limits are placed at `ask - BuyIndentPoints * PriceStep`, sell limits at `bid + SellIndentPoints * PriceStep`. Only one
   pending order or position may exist at a time, so the strategy skips new signals if a trade is already active within the
   window.
5. Stops and targets are hidden inside the strategy. When an entry order fills, the candle body of `shift1` is multiplied by
   `StopLossMultiplier` and `TakeProfitMultiplier`, normalized to `PriceStep`, and stored as exit prices.
6. On every finished candle the strategy evaluates whether the high/low breached the stored stop or target. Hitting the level
   closes the open position with a market order and clears the protection flags.
7. Pending orders older than 230 minutes are cancelled to mimic the MetaTrader clean-up routine, and `_orderPlacedInWindow` is
   reset when the price leaves the trading window.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `OrderVolume` | `decimal` | `0.1` | Volume used for each limit order. |
| `CandleSizePoints` | `decimal` | `75` | Minimum bullish/bearish body size (in broker points) for the signal candle. |
| `StopLossMultiplier` | `decimal` | `0.8` | Multiplier applied to the signal candle body to build the stop distance. |
| `TakeProfitMultiplier` | `decimal` | `0.8` | Multiplier applied to the signal candle body to build the target distance. |
| `BuyIndentPoints` | `decimal` | `16` | Number of broker points subtracted from the ask when placing buy limits. |
| `SellIndentPoints` | `decimal` | `20` | Number of broker points added to the bid when placing sell limits. |
| `EntryWindowMinutes` | `int` | `5` | Duration of each session in minutes. |
| `CandleType` | `DataType` | 5-minute candles | Candle series processed by the strategy. |
| `StartTime0..5` | `TimeSpan` | `00:05`, `04:05`, `08:05`, `12:05`, `16:05`, `20:05` | Start time of each trading window. |

## Differences from the original expert
- The MetaTrader expert assigns stop-loss and take-profit to the pending order itself. The StockSharp port simulates that
  behaviour by storing hidden levels and closing the net position with market orders when candles break them.
- Price thresholds use `Security.PriceStep` so the conversion works on both 4- and 5-digit forex quotes without additional
  parameters.
- Only finished candles are used to evaluate the stop/target rules, whereas MetaTrader stops can be triggered intrabar by the
  trade server.
- Sound alerts and comment fields from the original EA are omitted; the StockSharp logs provide feedback instead.

## Usage tips
- The strategy is designed for forex symbols that use fractional pip pricing. Verify `PriceStep` to confirm that point-based
  filters match the intended pip distance.
- Because the stop and take profit are hidden, consider running the strategy in a dedicated environment or protecting it with a
  broker-side risk module in case the connection drops.
- Adjust `StartTime` values if your broker session differs from the original GMT-based schedule. Each window can be disabled by
  setting the start times outside your trading day.
- Attach the strategy to a chart to visualise the limit orders and confirm that only one entry is attempted in each window.
