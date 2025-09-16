# ChannelEA2 Strategy

## Overview
ChannelEA2 Strategy replicates the MetaTrader expert "ChannelEA2" in StockSharp. The strategy builds an intraday price channel between the configured session start and end hours. When the session ends, it places stop orders above the channel high and below the channel low. Each stop order carries a protective stop loss defined by the opposite edge of the channel. The approach aims to capture breakouts after a period of consolidation during the session window.

## Trading Logic
- At the first finished candle whose open time crosses the `BeginHour`, the strategy resets the session.
  - All open positions are closed with market orders.
  - Any active orders, including previous stop entries or protection stops, are cancelled.
  - Session high and low are initialised using the first candle inside the new session.
- During the session (from `BeginHour` until `EndHour`), the high and low of each finished candle update the channel boundaries.
- On the first candle that opens after the session has ended (`EndHour`), the strategy calculates:
  - A buy stop order at the recorded session high plus an optional buffer measured in price steps.
  - A sell stop order at the recorded session low minus the same buffer.
  - The stop loss for the buy order is the session low, while the stop loss for the sell order is the session high.
- If a position opens, the opposite entry order is cancelled and a protective stop is registered in the market using the stored stop level.
- Orders remain active until the next session start, when everything is reset again.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `BeginHour` | Hour (0-23) when the session resets and the channel starts collecting data. | `1` |
| `EndHour` | Hour (0-23) when stop orders are scheduled. Supports overnight sessions when `BeginHour > EndHour`. | `10` |
| `TradeVolume` | Volume used for each entry order. | `1` |
| `CandleType` | Candle series used to build the channel (defaults to 1-hour candles). | `1 hour` |
| `StopBufferMultiplier` | Multiplier of the instrument price step used as a safety buffer for entry activation and protective stops. | `2` |

## Risk Management
- The strategy automatically calls `StartProtection()` to let StockSharp manage unexpected positions.
- Protective stop orders are submitted immediately after a position appears. They are cancelled when the position returns to zero.
- Stop prices are offset by `StopBufferMultiplier * PriceStep` to avoid violating exchange stop distance limits.

## Additional Notes
- The channel range freezes once the stop orders are generated; later candles do not affect the entry levels until the next session starts.
- If the instrument has no `PriceStep` defined, the buffer is ignored and orders are placed at the exact channel levels.
- Volume values are decimals, allowing fractional contracts or lots when supported by the broker.
- The strategy draws candles and executed trades on the chart area for visual tracking.
