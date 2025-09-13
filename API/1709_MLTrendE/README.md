# MLTrendE Strategy

This strategy trades in the direction of a weighted moving average (WMA) and optionally pyramids positions when price moves favorably.

## Logic

- Calculate a WMA of the selected candle series.
- If no position is open:
  - **Trade type 0**: open a long position when the close price is above the WMA, or a short position when it is below.
  - **Trade type 1**: always open a long position.
  - **Trade type 2**: always open a short position.
- When a position is open and reaches the specified profit target, add another trade with scaled volume.
- Once the maximum number of trades is reached, the entire position is closed at the next profit target.

## Parameters

- `Volume` – base trade volume.
- `Multiplier1` – volume multiplier for the second trade.
- `Multiplier2` – volume multiplier for the third trade.
- `TakeProfit` – profit in price units required to scale or close.
- `Map` – period of the weighted moving average.
- `MaxTrades` – maximum number of consecutive trades.
- `TradeType` – 0 trend following, 1 force long, 2 force short.
- `CandleType` – timeframe of the analyzed candles.

## Notes

The strategy uses only completed candles and market orders. It does not manage stops or risk; use account protection if needed.
