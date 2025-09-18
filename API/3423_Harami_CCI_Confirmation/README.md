# Harami CCI Confirmation

## Overview
Harami CCI Confirmation is a high-level StockSharp port of the MetaTrader 5 expert advisor `Expert_ABH_BH_CCI`. The original EA trades the two-candle Bullish Harami and Bearish Harami reversal patterns. Before entering a trade, it demands confirmation from a Commodity Channel Index (CCI) oscillator and measures candle body size against a moving average to ensure that the larger candle truly dominates the range. The StockSharp conversion keeps the same confirmation logic, processes only completed candles, and uses the platform's built-in protection module for order safety.

## Strategy logic
### Pattern detection
* **Average body calculation** – maintains a moving average of absolute candle bodies over the last *N* bars (default 5). This mirrors the MetaTrader helper class that smooths the candle size and trend reference.
* **Bullish Harami** – requires the previous candle to be bullish, the prior candle to be bearish with a body longer than the average, and the bullish body to remain inside the bearish range. The midpoint of the earlier candle must also sit below the moving average of closes, confirming a downtrend.
* **Bearish Harami** – mirrored conditions: the previous candle must be bearish, the earlier candle bullish and long, the bearish body must be contained inside the bullish range, and the midpoint needs to be above the close moving average to confirm an uptrend.

### CCI confirmation
* **Entry filter** – the strategy checks the CCI value from the most recently completed candle (shift 1). Long trades require the CCI to be below `-EntryThreshold` (default 50), while short trades demand a value above `+EntryThreshold`.
* **Exit band** – the CCI history is monitored for crossings of ±`ExitBand` (default 80). When the indicator rises through `-ExitBand`, any open short position is closed. When it drops below `+ExitBand`, existing long exposure is closed. This reproduces the "votes" used by the MetaTrader expert to flatten positions.

### Trade management
* **Reversals** – if the opposite Harami setup is confirmed while the strategy already holds a position, it will trade enough volume to both close the existing exposure and open the new direction.
* **Protection** – `StartProtection()` is activated so that users can attach stop-loss or take-profit settings through the StockSharp UI if desired. No fixed stops are enforced by default to stay aligned with the source EA, which relied on manual money management settings.

## Parameters
* **Order Volume** – base volume sent with every market entry. Extra volume is automatically added to close the opposite position when a reversal occurs.
* **CCI Period** – length of the Commodity Channel Index oscillator.
* **Body Average** – number of historical candles used when averaging body sizes and close prices.
* **CCI Entry** – minimum absolute CCI value needed to accept a Harami signal.
* **CCI Exit Band** – band magnitude that defines the CCI crossover exit rules.
* **Candle Type** – timeframe used for candles (default: 1-hour time frame).

## Additional notes
* All calculations run on completed candles supplied by `SubscribeCandles`. Intrabar signals are intentionally ignored to match the MetaTrader execution model.
* The strategy keeps a short sliding history of candles and CCI values to evaluate the Harami rules without recreating full indicator buffers.
* Only the C# implementation is provided in this folder; there is no Python version for this conversion.
