# Second Easiest Strategy

## Overview
The Second Easiest strategy is the StockSharp port of the MetaTrader expert *Second_Easiest.mq4*. The original robot scans the
daily candle of the current trading session and opens a single intraday position once price proves it is trending away from the
day's open. When the market closes the expert liquidates any exposure, preparing itself for the next session. The StockSharp
version preserves this intraday breakout behaviour while taking advantage of the framework's high-level API for candle
subscriptions, order management and position tracking.

Unlike momentum strategies that require multiple indicators, Second Easiest only needs the running open, high and low of the
current day. This makes it very lightweight while still reacting to the earliest signs of directional conviction. The code keeps
one position at a time and never reverses immediately; the new trade can be opened only after the previous one has been closed.

## Trading logic
1. Subscribe to the intraday candle series defined by `CandleType`. The default is a one-minute time frame, which gives an early
   view of daily extremes while remaining compatible with the daily logic of the original EA.
2. For every finished candle, update the in-memory record of the session's open, high and low prices. The first candle processed
   on a new trading day defines all three values; subsequent candles expand only the high or low whenever a new extreme is reached.
3. Ignore new setups once the clock reaches `EntryCutoffHour`. The MetaTrader code stops opening trades at 16:00 server time and
   the port follows the same rule.
4. A long position is allowed only when the current close trades above the daily open **and** the distance between the open and the
   daily low exceeds `RangePointsThreshold`. This reproduces the "Bid > open" and "open - low > 15 points" conditions from MQL.
5. A short position is allowed only when the current close sits below the daily open **and** the distance between the daily high and
   the open exceeds the same threshold.
6. Whenever an entry signal appears and no position is open, send a market order using `TradeVolume` lots. The helper methods from
   the base `Strategy` class take care of selecting the correct side.
7. After the market reaches `MarketCloseHour`, flatten any existing exposure by calling `ClosePosition()`. No new trades are placed
   after this cut-off until the next session begins.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-minute time frame | Primary intraday candles driving the entry and exit logic. |
| `TradeVolume` | `decimal` | `1` | Lot size used for every market order. |
| `EntryCutoffHour` | `int` | `16` | Hour (0-23) after which the strategy refuses to open new positions. |
| `MarketCloseHour` | `int` | `20` | Hour (0-23) when any open position is forcefully closed. |
| `RangePointsThreshold` | `decimal` | `15` | Minimum distance, expressed in broker points, between the daily open and the closest extreme. |

## Differences vs. the MetaTrader version
- The StockSharp version tracks positions in a netted manner. The behaviour is identical to the original single-order logic
  because only one trade can be open at any time and the position is flattened before new entries are evaluated.
- MetaTrader retrieves the running open, high and low through `iOpen/iHigh/iLow` calls on the daily timeframe. The port rebuilds
  the same information from intraday candles, avoiding forbidden indicator calls and ensuring the data remains available even when
  the brokerage feed does not provide daily bars.
- Order closing is performed through `ClosePosition()` instead of looping through ticket identifiers. The end result is the same:
  open exposure is removed as soon as the configured closing hour is reached.
- If the security's `PriceStep` is not provided, the conversion treats the `RangePointsThreshold` as an absolute price distance.
  This safety fallback keeps the system operational on instruments that report prices without step metadata.

## Usage notes
- `Volume` is set to `TradeVolume` in `OnStarted`, so changing the parameter immediately affects subsequent orders without
  modifying the rest of the code.
- When choosing a different `CandleType`, make sure it still provides enough granularity to track the intraday open/high/low
  accurately. For example, five-minute candles work well, but hourly bars might delay the detection of daily extremes.
- Increase `RangePointsThreshold` to filter out low-volatility sessions. Decreasing it allows the strategy to trigger even when
  the early range is small.
- Because the algorithm closes all positions at the end of the day, it does not require overnight margin. Brokers that enforce
  session breaks will also reset the internal range counters automatically when trading resumes.
