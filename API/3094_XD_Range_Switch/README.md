# XD Range Switch Strategy

## Overview
The XD Range Switch strategy recreates the MetaTrader 5 expert advisor **Exp_XD-RangeSwitch** using the StockSharp high-level API. It relies on the custom XD-RangeSwitch channel indicator, which plots alternating upper and lower bands together with arrows whenever the dominant band flips. The strategy can either fade those arrows (counter-trend behaviour) or trade in the direction of the breakout depending on the `TradeDirection` parameter. Order sizing follows the base `Strategy.Volume` setting, while the original money-management formulas are replaced by StockSharp's position management helpers.

## XD-RangeSwitch indicator recreation
* The indicator tracks the last `Peaks` completed candles to determine the highest high and lowest low ranges.
* A bullish channel (lower band) is printed when the current close is above the highest high of the previous `Peaks` bars. Its value equals the minimum low across the same window plus the current bar.
* A bearish channel (upper band) is printed when the current close is below the lowest low of the previous `Peaks` bars. Its value equals the maximum high across the same window plus the current bar.
* If neither breakout occurs, the previous channel values are propagated forward.
* Whenever a channel reappears after being empty the strategy records an arrow signal at the channel price. This mirrors the behaviour of the MT5 buffers 2 and 3 used by the original expert.
* Only fully finished candles are processed, ensuring consistent values across live and historical runs.

## Trading logic
1. The strategy processes candles from the timeframe selected by `CandleType` and stores the reconstructed indicator buffers.
2. For every new candle it inspects the indicator value that is `SignalBar` candles old (the MT5 code uses the same shift when calling `CopyBuffer`).
3. Signal mapping depends on the `TradeDirection` option:
   * **AgainstSignal** replicates the default MT5 behaviour — bullish arrows trigger longs and also request to close short trades, bearish arrows trigger shorts and request to close longs.
   * **WithSignal** flips the interpretation, so bullish arrows are treated as exit points for longs and entry points for shorts, effectively trading in the same direction as the channel breakout.
4. Trend buffers without arrows are still respected as exit signals, matching the original `SELL_Close` and `BUY_Close` flags.
5. Closings always execute before openings, allowing the strategy to flatten an opposite position before entering in the new direction.
6. Orders are submitted with market execution helpers (`BuyMarket`/`SellMarket`). When a flip occurs while an opposite position is open the requested quantity is automatically increased to fully offset the exposure before establishing the new position.

## Risk management
* Optional stop-loss and take-profit logic is provided through the `UseStopLoss`/`StopLossPoints` and `UseTakeProfit`/`TakeProfitPoints` parameters.
* The distances are measured in absolute price units, mirroring the "points" inputs in the MT5 script.
* Stops and targets are evaluated on every finished candle using the candle's high/low to emulate intra-bar triggering.
* If both a stop and a target are active the stop has priority — the position is closed once either level is reached.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | H4 candles | Timeframe used for the XD-RangeSwitch calculations. |
| `Peaks` | 4 | Number of peaks (lookback length) analysed by the indicator. |
| `SignalBar` | 1 | Number of completed bars back when reading indicator buffers. |
| `TradeDirection` | AgainstSignal | Choose between counter-trend or trend-following interpretation of the signals. |
| `AllowBuyEntry` / `AllowSellEntry` | true | Enable or disable opening new positions in the corresponding direction. |
| `AllowBuyExit` / `AllowSellExit` | true | Permit the strategy to close existing positions when the indicator requests it. |
| `UseStopLoss` / `StopLossPoints` | true / 1000 | Activate stop-loss handling and define its distance in price units. |
| `UseTakeProfit` / `TakeProfitPoints` | true / 2000 | Activate take-profit handling and define its distance in price units. |

## Notes
* The high/low buffers are maintained internally inside the strategy instead of relying on StockSharp collections, staying faithful to the MT5 implementation while adhering to the conversion guidelines.
* Signals are evaluated on finished candles only. If `SignalBar` is greater than zero, the order is placed on the next candle after the one that produced the signal, as in the MT5 expert.
* The indicator values are kept in a short rolling history that extends just beyond the largest of `Peaks` and `SignalBar`, ensuring deterministic memory usage even during long simulations.
* The default configuration mirrors the MT5 defaults: H4 candles, `Peaks = 4`, `SignalBar = 1`, counter-trend trading, and a 1,000/2,000 point risk envelope.
