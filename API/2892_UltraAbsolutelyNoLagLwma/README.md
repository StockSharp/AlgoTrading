# Ultra Absolutely No Lag LWMA Strategy

## Overview

The **Ultra Absolutely No Lag LWMA Strategy** replicates the signals of the Ultra Absolutely No Lag LWMA MetaTrader expert using the StockSharp high-level API. The indicator stack evaluates a double weighted moving average ladder and measures how many smoothing stages point upward or downward. The resulting counts are smoothed again to generate a color-coded state that drives the trading logic. The strategy optionally places protective stop-loss and take-profit orders for every new position.

## Indicator Pipeline

1. **Double LWMA filter** – the applied price (close by default) is processed by two consecutive weighted moving averages to remove noise.
2. **Smoothing ladder** – the filtered series passes through a configurable set of moving averages. Each step uses the selected smoothing method (Jurik by default) and a length that increases by a fixed step.
3. **Bull/Bear counter** – every step compares the current value with the previous value. Rising steps contribute to the bullish counter, falling steps to the bearish counter.
4. **Final smoothing** – the bullish and bearish counters are smoothed again using the selected method. These two values form the final state of the indicator.

The strategy re-creates the color logic of the original indicator: strong bullish states produce codes 7–8, neutral bullish states 5–6, strong bearish states 1–2 and neutral bearish states 3–4. Zero denotes an undefined state.

## Trading Logic

* When the older bar reported a bullish code (`> 4`) and the most recent bar switches to a bearish code (`< 5` and non-zero), the strategy closes open short positions and can open a new long position.
* When the older bar reported a bearish code (`< 5` and non-zero) and the most recent bar switches to a bullish code (`> 4`), the strategy closes open long positions and can open a new short position.
* Stop-loss and take-profit orders can be registered automatically after each entry when the corresponding offsets are greater than zero.

The evaluation uses the previous two completed bars from the indicator time frame, matching the behaviour of the MetaTrader expert that works on bar close.

## Parameters

| Name | Description |
| ---- | ----------- |
| `CandleType` | Candle type/time frame used for the indicator calculations. |
| `BaseLength` | Length of the double LWMA pre-filter. |
| `AppliedPriceMode` | Applied price (close, open, typical, DeMark, etc.) used as indicator input. |
| `TrendMethod` | Moving average method for the smoothing ladder (Jurik, SMA, EMA, etc.). |
| `StartLength` | Initial length of the smoothing ladder. |
| `StepSize` | Step added to the smoothing length on each ladder stage. |
| `StepsTotal` | Number of stages in the smoothing ladder. |
| `SmoothingMethod` | Method used to smooth the bull/bear counters. |
| `SmoothingLength` | Length of the final smoothing stage. |
| `UpLevelPercent` | Percentage threshold that marks a strong bullish state. |
| `DownLevelPercent` | Percentage threshold that marks a strong bearish state. |
| `SignalBar` | Index of the bar used for trading signals (1 = previous closed bar). |
| `AllowBuyOpen` / `AllowSellOpen` | Enable opening long/short positions. |
| `AllowBuyClose` / `AllowSellClose` | Enable closing existing long/short positions. |
| `StopLossOffset` | Absolute distance between entry price and the protective stop-loss (0 disables). |
| `TakeProfitOffset` | Absolute distance between entry price and the take-profit (0 disables). |

## Usage Notes

1. Configure the candle type to match the desired indicator time frame (the MetaTrader version uses H4 by default).
2. Adjust ladder parameters if you need faster or slower reactions. A larger `StepsTotal` creates a smoother but slower indicator.
3. Leave `StopLossOffset` and `TakeProfitOffset` at zero to disable protective orders.
4. The indicator mapping uses StockSharp moving averages. Methods that are not available in StockSharp fall back to Jurik or EMA smoothing.
5. The strategy only trades on finished candles to remain consistent with the original expert.
