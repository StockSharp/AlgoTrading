# Channel EA Limits Strategy

## Overview
- **Origin**: converted from the MetaTrader 5 expert `ChannelEA1.mq5`.
- **Purpose**: monitor an intraday price channel between two user-defined hours and queue limit orders at the end of that window.
- **Approach**: the strategy keeps track of the highest and lowest prices observed during the session and places symmetric limit orders to trade potential reversals back toward the opposite side of the channel.

The strategy is suitable for symbols that exhibit mean reversion once a daily range is established. By design it works on netting accounts: a filled sell limit order will close an existing long before opening a new short and vice versa.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `BeginHour` | `1` | Hour (0-23) when the intraday range tracking starts. The strategy cancels outstanding orders and closes positions at this time. |
| `EndHour` | `10` | Hour (0-23) when the accumulated range is evaluated and new limit orders are placed. Supports overnight sessions: if `BeginHour > EndHour`, the session spans midnight. |
| `OrderVolume` | `1` | Volume applied to every pending order. |
| `CandleType` | `1 hour` time frame | Candle series used to build the channel. You can switch to any time frame supported by StockSharp. |

## Trading Logic
1. **Session handling**
   - The strategy derives the session start and end timestamps from the `BeginHour` and `EndHour` parameters using the candle timestamps. When `BeginHour > EndHour` the end is moved to the next day.
   - At the first finished candle whose close time reaches the start boundary, the strategy cancels all active orders, closes the open position, and resets the session statistics.
2. **Channel construction**
   - Only candles whose open time lies inside the session window contribute to the range. The strategy keeps the running maximum high and minimum low for the session and counts the number of contributing candles.
   - At least two finished candles are required to form a valid range, mirroring the behaviour of the original MQL5 expert (`n > 2` condition).
3. **Order placement at session end**
   - When a finished candle crosses the end boundary, the strategy checks that the range has been formed and that the low is strictly below the high.
   - It then places two pending orders:
     - `BuyLimit` at the recorded session low with `OrderVolume` volume.
     - `SellLimit` at the recorded session high with the same volume.
   - Orders stay active until the next session starts. Because the strategy runs on a netting account, these orders serve both as entries and exits: for example, the `SellLimit` closes an existing long at the session high before establishing a new short.
4. **Next session preparation**
   - On the next start boundary the strategy closes any remaining position and removes leftover pending orders before measuring the new channel.

## Additional Notes
- No explicit stop-loss is set. Risk management must be controlled through position sizing, manual overrides, or external protective logic.
- The logic uses finished candles only (`CandleStates.Finished`) to stay aligned with the original EA behaviour.
- Ensure that the data feed and server time zone match your expectations, because session boundaries are evaluated in exchange/local time.
- When optimising, consider both the trading hours and the candle duration; the strategy is sensitive to the combination because the recorded range depends on the selected time frame.
