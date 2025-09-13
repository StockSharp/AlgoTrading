
# Trade Channel Strategy

The **Trade Channel Strategy** trades breakouts and pullbacks around a Donchian price channel. When the upper band remains unchanged and price hits it or closes below it but above the pivot, a long position is opened. The opposite logic is used for short entries. A stop loss is placed beyond the opposite band by the ATR value. An optional trailing stop can tighten the stop as the trade moves in profit.

## Parameters

- `ChannelPeriod` — length of the Donchian channel.
- `AtrPeriod` — ATR period for stop-loss calculation.
- `Trailing` — trailing stop distance in price units (0 disables trailing).
- `CandleType` — candle type used for calculations.
