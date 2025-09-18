# Synchronized Hour Breakout Strategy

## Overview
The **Synchronized Hour Breakout Strategy** is a StockSharp port of the MetaTrader 4 expert advisor `JK_sinkhro1`. It analyses the balance of bullish and bearish candles during the recent trading window and only trades during two carefully selected synchronization hours (by default 19:00 and 22:00 plus an offset). The strategy focuses on capturing directional breakouts while enforcing conservative risk management rules similar to the original EA.

## Trading Logic
- Works on the candle series selected by the `Candle Type` parameter (default: 1-hour candles).
- Maintains a sliding window of the latest `Analysis Period` completed candles and counts how many closed bullish vs. bearish.
- When the bullish count exceeds the bearish count, the strategy prepares for a long breakout during the first synchronization hour (`22 + Hour Offset`).
- When the bearish count exceeds the bullish count, it prepares for a short breakout during the second synchronization hour (`19 + Hour Offset`).
- Signals are only valid within the first five minutes of the hour so that the order is synchronized with the new bar open, as in the MQL original.
- New trades are ignored if there are already `Max Active Orders` registered or an open position is present.

## Risk Management and Trade Management
- Positions are opened with either a fixed lot size (`Fixed Volume`) or a risk-based size using the account cash and `Risk %` parameter. The risk model divides the permitted cash risk by the stop distance in price steps to approximate the behaviour of the source EA.
- Every position uses three layers of exit logic:
  - A primary take-profit at `Take Profit (pts)` from the entry price.
  - A secondary faster take-profit at `Secondary TP (pts)` to mimic the early manual close in the original code.
  - A hard stop-loss at `Stop Loss (pts)` below/above the entry price.
- Optional trailing stop: once the price advances more than `Trailing Stop (pts)`, the trailing threshold follows the favourable extreme and closes the position if price retreats beyond the trailing distance.
- Position state is reset after every full exit to prepare for the next synchronization window.

## Parameters
| Parameter | Description |
| --- | --- |
| `Take Profit (pts)` | Primary take-profit distance in security price steps. |
| `Secondary TP (pts)` | Faster take-profit distance triggered before the main target. |
| `Stop Loss (pts)` | Stop-loss distance measured in price steps. |
| `Trailing Stop (pts)` | Trailing stop distance; set to 0 to disable. |
| `Analysis Period` | Number of recent candles inspected when counting bullish/bearish closes. |
| `Hour Offset` | Offset added to the original 19:00 and 22:00 trading hours. |
| `Max Active Orders` | Maximum number of simultaneously active orders allowed before new entries are blocked. |
| `Fixed Volume` | Trade volume used when risk-based sizing is disabled. |
| `Use Risk Volume` | Enables dynamic position sizing based on portfolio cash and stop distance. |
| `Risk %` | Percentage of portfolio cash risked per trade in risk-based mode. |
| `Candle Type` | Candle type/timeframe used for calculations and signal generation. |

## Usage Notes
- The default configuration emulates the MetaTrader version that traded EURUSD during the New York session; adjust the hour offset to match your broker/server time zone.
- Ensure the security definition provides accurate `PriceStep`, `VolumeStep`, and `MinVolume` values so that the risk-based position sizing can align volumes with the exchange lot increments.
- Because the strategy relies on candle close data, attach it to a history provider or live data feed that can deliver the selected candle series with minimal delay.
- The trailing exit uses close prices from finished candles, which closely matches the tick-based trailing logic from the source EA while remaining compatible with StockSharp's high-level API.
