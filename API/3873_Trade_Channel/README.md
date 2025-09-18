# Trade Channel

## Overview
Trade Channel is a channel-reversion strategy converted from the MetaTrader "TradeChannel" expert advisor. The system draws a price channel from the highest high and lowest low over a configurable number of completed candles. When the channel stops expanding and price retests one of its borders, the strategy opens a position in the opposite direction, expecting a reversion back inside the range.

### Core ideas
- Use the **Highest** and **Lowest** indicators to form a Donchian-like channel.
- Require the channel to be flat (no new highs or lows) before opening a trade.
- Fade the touch of resistance with short positions and the touch of support with long positions.
- Place the initial protective stop one Average True Range (ATR) away from the breakout point.
- Optionally trail the stop once the trade moves in favour of the position.

## Parameters
| Name | Description | Default | Optimization |
| --- | --- | --- | --- |
| `Volume` | Trade volume in lots/contracts. | 1 | Enabled (0.1 → 2.0, step 0.1) |
| `ChannelLength` | Number of finished candles used to compute channel boundaries. | 20 | Enabled (10 → 60, step 5) |
| `AtrPeriod` | Period of the ATR indicator for stop placement. | 4 | Enabled (2 → 20, step 2) |
| `TrailingPoints` | Trailing stop offset measured in instrument price steps. Set to `0` to disable trailing. | 30 | Enabled (0 → 100, step 10) |
| `CandleType` | Candle type and timeframe used for calculations. | 30-minute time frame | — |

## Trading logic
1. Subscribe to the configured candle series and feed three indicators: `Highest`, `Lowest` and `ATR`.
2. Wait until all indicators are fully formed. The first completed values initialise the channel state and no trades are taken on that candle.
3. For every new finished candle:
   - Update the channel boundaries and calculate the pivot `(resistance + support + close) / 3`.
   - Check whether the resistance (or support) is unchanged compared with the previous candle. A flat resistance allows short setups, a flat support allows long setups.
   - **Short entry:** if resistance is flat *and* the candle either touches the resistance high or closes between the pivot and the resistance, send a market sell order.
   - **Long entry:** if support is flat *and* the candle either touches the support low or closes between the support and the pivot, send a market buy order.
   - Only one position is allowed at a time. The strategy waits for the flat-channel signal while no trades are open.
4. Upon entry:
   - Store the entry price.
   - Set the initial stop to `resistance + ATR` for shorts and `support − ATR` for longs.
5. Manage open positions:
   - **Exit conditions for longs:**
     - Price touches the upper channel boundary while it remains flat.
     - Candle low crosses below the trailing/initial stop level.
   - **Exit conditions for shorts:**
     - Price touches the lower channel boundary while it remains flat.
     - Candle high crosses above the trailing/initial stop level.
6. Trailing stop (if `TrailingPoints` > 0):
   - Convert the input into price units using the instrument's `Security.Step` (falls back to raw value if the step is unavailable).
   - For longs, once the close exceeds the entry price by the trailing offset, move the stop to `close − offset`.
   - For shorts, once the close drops below the entry price by the offset, move the stop to `close + offset`.
   - The trailing stop never moves backwards; it only tightens the protective level.

## Notes
- All decisions are made on finished candles to stay aligned with the original MQL logic that used `High[1]`, `Low[1]` and `Close[1]`.
- The equality check between the current and previous channel boundary is tolerant to instrument price steps to avoid floating-point glitches.
- Trailing stops rely on correct `Security.Step` metadata. If the exchange does not provide it, the raw point value is used instead.
- The strategy does not send e-mails or adjust position sizing dynamically, because those features were platform-specific in the MQL implementation.
