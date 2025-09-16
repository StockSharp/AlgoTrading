# EMA Cross Trailing Strategy

## Overview
This strategy is the StockSharp conversion of the MetaTrader 4 expert advisor located at `MQL/8606/EMA_CROSS_2.mq4`. It preserves the original idea of tracking the relationship between a slow and a fast exponential moving average and opening a single market position when a crossover occurs. Protective exits (take profit, stop loss and trailing stop) are handled through the high-level `StartProtection` helper so the behaviour mirrors the MetaTrader implementation while using StockSharp best practices.

## Trading logic
- Build candles with the configurable `CandleType` (15-minute bars by default) and feed two EMA indicators: the slow EMA uses `SlowEmaLength` and the fast EMA uses `FastEmaLength`.
- Maintain the latest direction of the slow EMA relative to the fast EMA. The first completed candle after both indicators are formed is used only to initialise this direction, just like the `first_time` guard in the original advisor.
- When the slow EMA moves above the fast EMA (new direction becomes `1`) and the strategy is flat, send a market buy order. When the slow EMA moves below the fast EMA (new direction becomes `2`) and the strategy is flat, send a market sell order. This reproduces the exact up/down mapping of the MQL function `Crossed(LEma, SEma)`.
- Only one position can be active at a time. While a trade is open (or the entry order is still pending), additional crossovers are ignored.

## Trade and risk management
- `StartProtection` configures take profit, stop loss and trailing stop distances in price units computed from the instrument `PriceStep`. Trailing stops are optional: set `TrailingStopPips` to zero to disable them.
- Orders are placed with `BuyMarket`/`SellMarket` and closed by market when any protective level is triggered, exactly like the `OrderSend` and trailing logic from the original advisor.
- The base lot size is controlled by `OrderVolume`. Before each entry it is aligned to the instrument volume step, minimum and maximum to avoid rejection.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TakeProfitPips` | Distance in pips (price steps) used for the protective take profit. Default: 20. |
| `StopLossPips` | Distance in pips used for the protective stop loss. Default: 30. |
| `TrailingStopPips` | Trailing distance in pips. Set to `0` to disable trailing. Default: 50. |
| `OrderVolume` | Lot size of the market entries before alignment. Default: 2. |
| `FastEmaLength` | Period of the fast EMA applied to closing prices. Default: 5. |
| `SlowEmaLength` | Period of the slow EMA applied to closing prices. Default: 60. |
| `CandleType` | Time-frame for candle building. Default: 15 minutes. |

## Notes
- The strategy waits until both EMAs are fully formed before reacting to a crossover, removing the `Bars < 100` check from the MQL script while achieving the same stability.
- Because only market orders are used, there are no individual `OrderModify` calls. The built-in protection module automatically repositions the trailing stop in the same way the MetaTrader loop updated `OrderStopLoss`.
- No Python port is provided (per request); only the C# implementation is included.
