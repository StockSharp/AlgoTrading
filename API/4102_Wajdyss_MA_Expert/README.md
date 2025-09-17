# Wajdyss MA Expert Strategy

## Overview
The **Wajdyss MA Expert Strategy** is a C# port of the MetaTrader 4 expert advisor "wajdyss MA expert v3". It compares two moving averages configured with independent periods, calculation modes, shifts, and applied prices. A bullish crossover of the fast average above the slow average opens long exposure, while a bearish crossover opens short exposure. The conversion reproduces the original money-management rules, optional automatic closing of opposing trades, and end-of-day / end-of-week liquidation filters.

## Trading Logic
1. Subscribe to the selected `CandleType` (15-minute candles by default) and calculate the fast and slow moving averages using the chosen `MovingAverageMethod` and `PriceSource` settings for each leg.
2. Store the indicator values for finished candles. Evaluate a bullish signal when the fast average (with its configured shift) is above the slow average on the last closed bar while it was below two bars ago. Evaluate a bearish signal with the inverse condition.
3. Enforce a cooldown between new entries of the same direction. The strategy must wait at least one full candle of the subscribed timeframe after the last trade of that side, mirroring the global-variable timing guard from the MT4 version.
4. When **AutoCloseOpposite** is enabled, cancel working orders and reverse exposure in a single market order: the new order volume includes any outstanding position in the opposite direction so the account flips immediately.
5. Apply daily and Friday closing filters. After the configured `DailyCloseHour`/`DailyCloseMinute` or `FridayCloseHour`/`FridayCloseMinute`, all positions are flattened and new trades are blocked until the next session.

## Risk & Money Management
- **TakeProfitPips**, **StopLossPips**, and **TrailingStopPips** are interpreted in whole pips. The implementation converts them to price steps using the security metadata and drives StockSharp's `StartProtection` engine with market exits for parity with the original trailing logic.
- **UseMoneyManagement** emulates the MT4 lot calculation: `volume = (account_balance / BalanceReference) * InitialVolume`. Exchange limits are respected via volume step, minimum, and maximum checks.
- If money management is disabled, orders use **InitialVolume** directly.

## Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `FastPeriod` | `int` | `10` | Period of the fast moving average. |
| `FastShift` | `int` | `0` | Bars to shift the fast average before comparing crossover values. |
| `FastMethod` | `MovingAverageMethod` | `Ema` | Moving average mode for the fast line (`Sma`, `Ema`, `Smma`, `Lwma`). |
| `FastPriceType` | `PriceSource` | `Close` | Candle price fed into the fast moving average (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |
| `SlowPeriod` | `int` | `20` | Period of the slow moving average. |
| `SlowShift` | `int` | `0` | Bars to shift the slow average before comparison. |
| `SlowMethod` | `MovingAverageMethod` | `Ema` | Moving average mode for the slow line. |
| `SlowPriceType` | `PriceSource` | `Close` | Candle price fed into the slow average. |
| `TakeProfitPips` | `decimal` | `100` | Distance to the profit target in pips (set to `0` to disable). |
| `StopLossPips` | `decimal` | `50` | Distance to the protective stop in pips (set to `0` to disable). |
| `TrailingStopPips` | `decimal` | `0` | Trailing stop distance in pips (set to `0` to disable). |
| `AutoCloseOpposite` | `bool` | `true` | Close opposing exposure before opening a new trade in the other direction. |
| `InitialVolume` | `decimal` | `0.1` | Base trade volume before applying money management. |
| `UseMoneyManagement` | `bool` | `true` | Enable balance-based position sizing. |
| `BalanceReference` | `decimal` | `1000` | Divisor used when scaling volume with the account balance. |
| `DailyCloseHour` | `int` | `23` | Hour (0-23) after which daily positions are closed. |
| `DailyCloseMinute` | `int` | `45` | Minute component of the daily close filter. |
| `FridayCloseHour` | `int` | `22` | Hour (0-23) after which Friday trading stops. |
| `FridayCloseMinute` | `int` | `45` | Minute component of the Friday close filter. |
| `CandleType` | `DataType` | `15m` time frame | Candle series used for calculations and cooldown timing. |

## Notes
- The strategy relies exclusively on the high-level StockSharp API: candles are processed through `SubscribeCandles`, indicator bindings feed moving averages, and `StartProtection` manages stops/take-profit/trailing orders.
- Position flattening uses market orders to mirror the MT4 expert's immediate closures of opposite tickets.
- No Python translation is included in this folder; only the C# implementation is provided.
