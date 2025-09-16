# Exp XPeriod Candle X2 Strategy

## Overview
Exp XPeriod Candle X2 recreates the original MetaTrader expert using StockSharp's high-level API. The strategy builds synthetic candles on two timeframes by smoothing each bar and comparing the delayed open of a configurable lookback window with the latest smoothed close. The higher timeframe candle color defines the trend bias, while the working timeframe waits for color transitions to trigger entries and exits. Optional stop-loss and take-profit protections replicate the money-management inputs from the source code.

## How it works
- **Trend detection** – the higher timeframe subscription smooths open and close prices with the selected moving average. Each completed candle compares its smoothed close with the delayed smoothed open from `TrendPeriod` bars ago. A close above the delayed open produces a bullish color (0), while a close below produces a bearish color (2). The stored color at `TrendSignalBar` determines whether the global trend is long (`+1`), short (`-1`) or neutral.
- **Entry logic** – the working timeframe applies the same smoothing. For every finished candle the strategy stores the current and previous colors referenced by `EntrySignalBar`. A short setup appears when the higher timeframe trend is bearish, the current color is 0 and the previous color is 2, mirroring the original XPeriodCandle signal flip. A long setup requires the trend to be bullish, the current color to be 2 and the previous color to be 0.
- **Position management** – configurable toggles close positions on trend flips (`CloseLongOnTrendFlip`, `CloseShortOnTrendFlip`) and on entry-level reversals (`CloseLongOnEntrySignal`, `CloseShortOnEntrySignal`). New trades size `Volume + |Position|`, so an opposite signal both exits and reverses like the MQL expert.
- **Risk controls** – optional stop-loss and take-profit distances are expressed in price steps (`StopLossTicks`, `TakeProfitTicks`). They are activated only when the corresponding boolean is enabled.
- **Smoothing methods** – StockSharp moving averages are used instead of the original SmoothAlgorithms library. Available modes are Simple, Exponential, Smoothed (SMMA), Weighted, Hull, Kaufman Adaptive and Jurik. The `TrendPhase` and `EntryPhase` parameters affect Jurik smoothing only and are clamped to the ±100 range.

## Parameters
| Parameter | Description |
| --- | --- |
| `TrendCandleType` | Higher timeframe candle type used for the trend filter. |
| `EntryCandleType` | Working timeframe candle type used for entries. |
| `TrendPeriod` | Number of smoothed candles that define the delayed open on the trend timeframe. |
| `EntryPeriod` | Number of smoothed candles that define the delayed open on the entry timeframe. |
| `TrendLength` | Smoothing length for higher timeframe synthetic candles. |
| `EntryLength` | Smoothing length for working timeframe synthetic candles. |
| `TrendPhase` | Jurik phase parameter for the trend timeframe (ignored by other smoothing types). |
| `EntryPhase` | Jurik phase parameter for the entry timeframe (ignored by other smoothing types). |
| `TrendSignalBar` | Shift used to read the trend candle color (`1` matches the most recently closed bar). |
| `EntrySignalBar` | Shift used to read entry colors (`1` references the latest closed bar, `2` the previous one). |
| `TrendSmoothing` | Moving-average type applied to higher timeframe smoothing. |
| `EntrySmoothing` | Moving-average type applied to working timeframe smoothing. |
| `EnableLongEntries` | Allow long positions when bullish conditions appear. |
| `EnableShortEntries` | Allow short positions when bearish conditions appear. |
| `CloseLongOnTrendFlip` | Close long positions whenever the higher timeframe trend turns bearish. |
| `CloseShortOnTrendFlip` | Close short positions whenever the higher timeframe trend turns bullish. |
| `CloseLongOnEntrySignal` | Close long positions when the entry timeframe prints a bearish color. |
| `CloseShortOnEntrySignal` | Close short positions when the entry timeframe prints a bullish color. |
| `UseStopLoss` | Enable stop-loss protection measured in price steps. |
| `StopLossTicks` | Stop-loss distance in price steps. |
| `UseTakeProfit` | Enable take-profit protection measured in price steps. |
| `TakeProfitTicks` | Take-profit distance in price steps. |

## Notes
- The delayed open logic stores the oldest smoothed open within the configured period, matching the circular buffer from the original indicator.
- When `TrendCandleType` and `EntryCandleType` are the same, only one candle subscription is created but the dual-color logic still works.
- Ensure that `Volume` is set appropriately; reversal trades automatically include the current absolute position to replicate the MetaTrader lot-handling behavior.
