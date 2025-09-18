# FXF Safe Trend Scalp V1 (C#)

The FXF Safe Trend Scalp V1 strategy trades breakouts from ZigZag-based trendlines and mirrors the behaviour of the original MetaTrader 4 expert advisor. It observes the distance between the current price and dynamic resistance/support lines constructed from recent ZigZag pivots and aligns trades with a pair of simple moving averages. Protective stop-loss, take-profit and a floating-profit exit reproduce the money management rules from the source code.

## Trading Logic

1. **ZigZag Trendlines**
   - A manual ZigZag detector searches for alternating swing highs and lows using the configurable depth, deviation and backstep parameters.
   - The last four swing highs define the active resistance line, while the last four swing lows define the active support line. The strategy continuously extrapolates those lines into the current bar.
   - An entry signal is prepared when the closing price approaches a line within a fixed offset (10 points by default).
2. **Moving Average Filter**
   - A fast simple moving average (length 2) and a slow simple moving average (length 50) filter the trend.
   - Short positions require the fast MA below the slow MA, whereas long positions require the fast MA above the slow MA.
3. **Order Execution**
   - Signals are stored and activated on the next finished candle, matching the “new bar” logic of the MetaTrader version.
   - Before opening a position, the strategy verifies that the spread does not exceed the configured maximum and that no position is currently open.
4. **Risk Management**
   - Stop-loss and take-profit distances are expressed in points and applied immediately after the order is filled.
   - A floating-profit target closes the position once unrealised profit (in price units times volume) exceeds the configured reward per lot.

## Parameters

| Name | Description |
| --- | --- |
| `Candle Type` | Time frame used for signal generation. |
| `Volume` | Trade volume submitted with each entry. |
| `ZigZag Depth` | Minimum number of bars between confirmed pivots. |
| `ZigZag Deviation (pts)` | Minimum price move in points before the direction changes. |
| `ZigZag Backstep` | Bars required before accepting an opposite pivot. |
| `Trend Offset (pts)` | Distance from the trendline that triggers a signal. |
| `Fast MA Length` | Length of the fast simple moving average. |
| `Slow MA Length` | Length of the slow simple moving average. |
| `Max Spread (pts)` | Maximum allowed spread, expressed in points. |
| `Stop Loss (pts)` | Protective stop distance measured from the entry price. |
| `Take Profit (pts)` | Profit target distance measured from the entry price. |
| `Profit Target per Lot` | Floating profit required (price units × volume) to close the position. |

## Notes

- Only one position is held at a time. Signals are ignored while a trade is open.
- The spread filter relies on best bid/ask quotes, so the strategy should be connected to a data source providing level 1 information.
- The Python version of the strategy is intentionally omitted as requested.

## Files

- `CS/FXFSafeTrendScalpV1Strategy.cs` – StockSharp implementation of the expert advisor.

