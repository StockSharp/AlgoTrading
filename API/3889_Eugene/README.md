# Eugene Strategy

## Summary

The Eugene Strategy ports the original MetaTrader 4 expert advisor "Eugene" to the StockSharp high level API. The algorithm monitors hourly candles by default and looks for breakouts of inside candles that are confirmed by a retracement to a third of the prior candle. Once a breakout is confirmed the strategy enters in the breakout direction and can reverse existing positions when an opposite setup appears.

## Trading Logic

1. **Inside candle detection** – the previous candle must be completely inside the range of the candle before it. Its closing direction determines whether it is classified as a black (bearish) or white (bullish) insider.
2. **Bird filters** – an inside candle confirmed by another candle of the same color behind it is marked as a "bird". Black birds block long trades, white birds block short trades. This mirrors the protective filter from the MQL version.
3. **Zigzag confirmation levels** – two confirmation prices are computed at one third of the prior candle body or wick:
   - The long confirmation level is one third below the previous close (body for bullish candles, wick for bearish candles).
   - The short confirmation level is one third above the previous close (body for bearish candles, wick for bullish candles).
4. **Session filter** – if the current candle opens at 08:00 or later, confirmations are considered satisfied even without a retracement.
5. **Breakout condition** – a buy signal requires the current candle to make a higher high than the previous candle while keeping a higher low and overlapping the range of the candle two bars back. A sell signal uses the symmetric conditions with lower lows and lower highs.
6. **Position management** – before opening a new trade the strategy closes any opposite exposure. Only one long and one short entry can be issued per candle, replicating the `Counter_buy` and `Counter_sell` constraints from the original expert advisor.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `Trade Volume` | Order size for market orders. | `0.1` |
| `Candle Type` | Time frame of the processed candle series. | `1 hour` |

## Charting

When a chart area is available the strategy plots the processed candles together with the executed trades, helping to visualise the breakout behaviour.

## Notes

- The StockSharp version keeps the hourly session filter from the MQL expert. Adjust the candle type when trading other markets or time zones.
- Stop-loss and take-profit management is not included in the source MQL file. The port therefore leaves risk management to the hosting environment.
