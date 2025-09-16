# N Candles v5 Strategy

## Overview
The N Candles v5 strategy searches for runs of identical candles and opens a trade
in the same direction as soon as the required streak appears. The original MQL
implementation by Vladimir Karputov has been translated to the StockSharp high
level API. The strategy operates on closed candles only and can be executed on
any timeframe, with one hour candles being the default for the StockSharp
version.

## Trading Logic
1. When a candle closes the strategy classifies it as bullish (close above open),
   bearish (close below open) or neutral (close equals open).
2. Consecutive bullish candles increase the bullish streak counter while
   resetting the bearish counter, and vice versa for bearish candles. Neutral
   candles reset both counters.
3. If the bullish streak counter reaches the configured `CandlesCount` value and
   the current net position is flat or short, the strategy sends a market buy.
   Short exposure is covered first and the configured `TradeVolume` is then
   added to establish a long position.
4. If the bearish streak counter reaches `CandlesCount` and the position is flat
   or long, the strategy sells at market, first covering any long exposure before
   entering short.
5. Trades are only opened inside the optional trading session window defined by
   `StartHour` and `EndHour`. Protective actions (take profit, stop loss and
   trailing) continue to operate outside the session to ensure positions are
   handled safely.
6. The strategy refuses to increase exposure beyond `MaxNetVolume`, mirroring the
   volume safeguard from the MQL version.

## Risk Management
- **Take Profit / Stop Loss** – expressed in pips and converted to absolute price
  distances using the security price step. Both levels are optional and can be
  disabled by setting the corresponding value to zero.
- **Trailing Stop** – activates after the price advances by `TrailingStopPips`
  from the entry price. Once active, the stop is tightened whenever price moves
  an additional `TrailingStepPips` in the trade direction.
- **Session Filter** – `UseTradingHours` enables the start and end hour filter,
  preventing new entries outside the selected window while still letting risk
  management close positions.
- **Maximum Net Volume** – the absolute position (long or short) is never
  allowed to exceed `MaxNetVolume`.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `TradeVolume` | Order size used for new entries. | `1` |
| `CandlesCount` | Number of consecutive identical candles required for a signal. | `3` |
| `TakeProfitPips` | Take profit distance in pips (0 disables). | `50` |
| `StopLossPips` | Stop loss distance in pips (0 disables). | `50` |
| `TrailingStopPips` | Distance that activates the trailing stop (0 disables). | `10` |
| `TrailingStepPips` | Additional progress required before tightening the trailing stop. | `4` |
| `UseTradingHours` | Enables the trading hour filter. | `true` |
| `StartHour` | First hour (0–23) when new positions are allowed. | `11` |
| `EndHour` | Last hour (0–23) when new positions are allowed. | `18` |
| `MaxNetVolume` | Maximum absolute position size allowed. | `2` |
| `CandleType` | Candle data type to analyse. Default is 1 hour candles. | `TimeSpan.FromHours(1)` |

## Usage Notes
- The strategy subscribes to candle data via the high level `SubscribeCandles`
  API and works with any instrument that provides candle series.
- Because the logic relies on completed bars, it is most suitable for intraday
  or higher timeframes where market noise between closes is less impactful.
- Adjust the pip-based risk settings according to the instrument’s tick size.
- When deploying on instruments with significant spread differences, verify the
  trailing stop parameters so that the stop is not triggered by normal spread
  widening.

