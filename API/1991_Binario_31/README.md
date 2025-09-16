# Binario 31 Strategy

Breakout strategy converted from MetaTrader script **binario_31**. The algorithm builds two 144-period exponential moving averages calculated over the candle high and low prices, creating a dynamic channel. While the current price stays inside the channel the strategy prepares stop entries:

- a buy stop placed above the EMA-high plus a configurable offset;
- a sell stop placed below the EMA-low minus the same offset.

When price breaks through one of these levels a position is opened in the direction of the breakout. A protective stop is placed on the opposite side of the channel and a take profit target is calculated relative to the entry. An optional trailing stop can be enabled to protect profits.

## Parameters

- **EMA Length** – period for both high and low EMAs.
- **Pip Difference** – distance from the EMA level to the breakout entry in price steps.
- **Take Profit** – distance from entry to take profit in price steps.
- **Trailing Stop** – trailing stop distance in price steps. Set to zero to disable.
- **Volume** – order volume.
- **Candle Type** – type of candles the strategy subscribes to.
