# Micro Trend Breakouts Strategy

## Overview
The **Micro Trend Breakouts** strategy is a conversion of the MetaTrader expert advisor "Micro Trend Breakouts" to the StockSharp high-level API. It detects short-lived breakout patterns using linear weighted moving averages, momentum spikes and MACD alignment. The strategy opens at most one position at a time and relies on candle close prices to trigger entries and exits.

## Indicators
- **Linear Weighted Moving Averages (LWMA)** – Fast and slow averages built on the analysis timeframe filter the dominant market direction.
- **Momentum** – Absolute momentum readings across the last three completed candles must exceed a configurable threshold to confirm that price accelerates in the breakout direction.
- **MACD** – The classic MACD histogram is used as a directional filter (main line above the signal for longs and below the signal for shorts).

## Entry Logic
1. Wait for a finished candle from the configured timeframe.
2. Require the fast LWMA to be above the slow LWMA for longs (below for shorts).
3. Confirm a small breakout structure: the low of the candle two bars ago must be below the previous candle’s high for longs (mirrored for shorts).
4. Demand momentum acceleration – any of the last three absolute momentum values must exceed the configured threshold.
5. Validate MACD alignment:
   - Longs: MACD main line must be above the signal line, regardless of whether it is above or below zero.
   - Shorts: MACD main line must be below the signal line, regardless of the zero line position.

When all checks agree the strategy issues a market order using the default volume parameter.

## Exit Logic and Risk Management
- Initial stop-loss and take-profit levels are expressed in price steps and calculated at entry. Setting a value of zero disables the corresponding level.
- An optional breakeven module moves the stop towards the entry price after price advances by a configured amount of steps, optionally adding a safety padding.
- Trailing protection can tighten the stop after a profitable move. Once the profit exceeds the activation threshold, the stop is dragged by the trailing distance from the highest (for longs) or lowest (for shorts) candle price seen since entry.
- Position exits are evaluated on every finished candle. If price reaches either stop-loss or take-profit levels the strategy closes the position using a market order and resets the internal state.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Order Volume` | Market order volume used for entries. | `1` |
| `Candle Type` | Timeframe for price analysis. | `15m time frame` |
| `Fast LWMA` | Period of the fast linear weighted moving average. | `6` |
| `Slow LWMA` | Period of the slow linear weighted moving average. | `85` |
| `Momentum Period` | Momentum indicator lookback. | `14` |
| `Momentum Threshold` | Minimum absolute momentum required over the last three candles. | `0.3` |
| `MACD Fast / Slow / Signal` | Moving average periods used by MACD. | `12 / 26 / 9` |
| `Stop Loss` | Stop distance in price steps. `0` disables the stop. | `20` |
| `Take Profit` | Target distance in price steps. `0` disables the target. | `50` |
| `Use Trailing` | Enable trailing stop logic. | `true` |
| `Trail Activation` | Profit in steps required before the trailing stop becomes active. | `40` |
| `Trail Step` | Distance between the extreme and the trailing stop in steps. | `40` |
| `Use Breakeven` | Enable breakeven stop adjustment. | `true` |
| `Breakeven Trigger` | Profit in steps that arms the breakeven module. | `30` |
| `Breakeven Padding` | Additional steps added when moving the stop to breakeven. | `30` |

## Notes
- The strategy subscribes to a single candle stream and avoids any low-level API calls, staying within the high-level framework requirements.
- Protective orders are not attached directly to trades; instead the strategy uses candle-based monitoring combined with `StartProtection()` to ensure the base class supervises open positions.
- All inline comments in the C# code are written in English as required by the conversion guidelines.
