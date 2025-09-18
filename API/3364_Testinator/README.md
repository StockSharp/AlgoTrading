# Testinator Strategy

## Overview

The strategy is a C# port of the MetaTrader expert advisor **Testinator v1.30a**. It opens only long positions and manages them as a basket. Each new buy is permitted only when a configurable set of technical filters returns "true" and the price has advanced by a minimum number of pips. The exit logic mirrors the entry logic by using another filter mask. The original EA also relied on daily ATR measurements for risk management, therefore the port subscribes to daily candles in addition to the primary timeframe.

## Trading logic

### Entry filter mask (parameter `BuySequence`)

The mask uses the lower nine bits. A bit that is set must satisfy the corresponding test on the previous finished candle.

| Bit | Condition |
| --- | --------- |
| 1 | EMA(12) is above SMA(14). |
| 2 | EMA(50) stays below the lows of the last three candles. |
| 4 | Previous low is below the lower Bollinger band (20, 2). |
| 8 | ADX(14) is above the -DI and +DI is stronger than -DI. |
| 16 | Stochastic (16, 4, 8) has %K above %D and %D above 80. |
| 32 | Williams %R(14) is greater than -20. |
| 64 | MACD(12, 26, 9) line is above the signal line. |
| 128 | Ichimoku shows Senkou Span A above Span B, Tenkan above Kijun, and the previous low above Span A. |
| 256 | RSI (period `RsiEntryPeriod`) is above `RsiEntryLevel` and rising relative to the previous value. |

### Exit filter mask (parameter `CloseBuySequence`)

| Bit | Condition |
| --- | --------- |
| 1 | SMA(14) is above EMA(12). |
| 2 | EMA(50) is above the highs of the last three candles. |
| 4 | Previous high is above the upper exit Bollinger band (`BollingerCloseLength`, `BollingerCloseDeviation`). |
| 8 | -DI is above +DI. |
| 16 | Stochastic %D is below 80. |
| 32 | Williams %R(14) is less than -80. |
| 64 | MACD line is below the signal line. |
| 128 | Ichimoku Senkou Span B is above Senkou Span A. |
| 256 | RSI (period `RsiClosePeriod`) is below `RsiCloseLevel`. |

A basket is extended only if all active entry bits return true, the number of buys is below `MaxBuys`, and the last fill price is at least `StepPips` away. The basket is flattened whenever the exit mask passes or when protective levels are triggered.

### Session control and risk management

* Trading takes place only between `TradeStartHour` and `TradeStartHour + TradeDurationHours - 1` (Eastern European Time). If the window is closed and the basket is in profit, all buys are closed.
* The protective stop and take-profit distances are expressed in pips. Setting a value to `-1` disables it, while `0` activates the ATR multiplier (`StopRatio`, `TakeRatio`).
* The trailing stop uses the same ATR logic through `StartTrailPips`, `TrailStepPips`, `StartTrailRatio`, and `TrailStepRatio`.
* The strategy computes daily ATR(15) values on D1 candles to keep the behaviour identical to the EA.

## Parameters

* `TradeVolume` – lot size (volume) for every market buy.
* `BuySequence` / `CloseBuySequence` – bit masks that enable individual indicator filters.
* `MaxBuys` – maximum number of open buys handled as a basket.
* `StepPips` – minimum price progress (pips) before adding to the basket.
* `TradeStartHour`, `TradeDurationHours` – defines the daily trading window.
* `TakeProfitPips`, `StopLossPips` – fixed protective levels (negative disables, zero switches to ATR ratios).
* `StartTrailPips`, `TrailStepPips` – trailing start distance and step (negative disables, zero uses ATR ratios).
* `TakeRatio`, `StopRatio`, `StartTrailRatio`, `TrailStepRatio` – ATR multipliers used when the fixed value equals zero.
* `RsiEntryLevel`, `RsiEntryPeriod` – RSI threshold and period for the entry mask.
* `RsiCloseLevel`, `RsiClosePeriod` – RSI threshold and period for the exit mask.
* `BollingerCloseLength`, `BollingerCloseDeviation` – parameters of the exit Bollinger bands.
* `CandleType` – timeframe of the working candles (daily candles are subscribed automatically for ATR).

## Notes

* The port keeps the basket accounting model from the original EA: all orders are buys and only market orders are used.
* The logic intentionally stores previous indicator values to mimic the "bar[1]" checks from MetaTrader.
* The strategy ignores the unused inputs of the EA (`TakeAsBasket`, `StopAsBasket`, etc.) because they did not affect the MQL logic.
