# Exp Trend Intensity Index Strategy

This strategy is a StockSharp conversion of the MetaTrader expert **Exp_Trend_Intensity_Index**. It trades finished candles on a configurable timeframe and uses the Trend Intensity Index (TII) to detect when momentum leaves extreme bullish or bearish zones. When the indicator transitions out of an upper zone the algorithm exits shorts and can start a new long. When the indicator leaves a lower zone the algorithm closes longs and can start a new short.

## How the indicator is built

1. Select the price source (close, open, weighted variants, trend-follow prices, etc.).
2. Smooth that price stream with a first moving average (`PriceMaMethod`, `PriceMaLength`).
3. Split the difference between price and the smoothed value into positive and negative flows.
4. Smooth the positive and negative flows independently with a second moving average (`SmoothingMethod`, `SmoothingLength`).
5. Calculate the Trend Intensity Index: `TII = 100 * Positive / (Positive + Negative)`.
6. Compare the result with the `HighLevel` and `LowLevel` thresholds to assign a color state: high zone (`0`), neutral (`1`), or low zone (`2`).

The implementation uses StockSharp moving averages (simple, exponential, smoothed, weighted). Advanced smoothing types from the original MQL library are not available in this port.

## Trading logic

* Signals are processed only when a candle is fully closed (`CandleStates.Finished`).
* The `SignalBar` parameter defines which completed bar is analysed (default one bar back). The strategy also inspects the bar immediately before that, matching the double-buffer lookup in the MQL code.
* When the older bar belongs to the high zone (`color == 0`):
  * Close any short position if `EnableSellExits` is true.
  * If the more recent bar left the high zone and `EnableBuyEntries` is true, open or reverse into a long.
* When the older bar belongs to the low zone (`color == 2`):
  * Close any long position if `EnableBuyExits` is true.
  * If the more recent bar left the low zone and `EnableSellEntries` is true, open or reverse into a short.
* Orders are submitted with `BuyMarket` and `SellMarket`. Position reversals use the current position volume plus the configured `Volume` property.
* Optional stop-loss and take-profit protection (price units) is configured through `StopLossPoints` and `TakeProfitPoints` and implemented with `StartProtection`.

## Parameters

| Parameter | Description |
| --- | --- |
| `CandleType` | Timeframe used for indicator calculation and trading. |
| `PriceMaMethod`, `PriceMaLength` | Moving average type and period applied to the base price stream. |
| `SmoothingMethod`, `SmoothingLength` | Moving average type and period applied to the positive and negative flows. |
| `AppliedPrice` | Price source for the indicator (close, open, median, trend-follow variants, Demark, etc.). |
| `HighLevel`, `LowLevel` | Upper and lower thresholds that define bullish and bearish zones. |
| `SignalBar` | Number of completed bars to look back for signal confirmation. |
| `EnableBuyEntries`, `EnableSellEntries` | Toggles that allow opening long/short positions. |
| `EnableBuyExits`, `EnableSellExits` | Toggles that allow automatic exits when the indicator flips. |
| `StopLossPoints`, `TakeProfitPoints` | Optional protective distances expressed in price units for `StartProtection`. |

## Differences from the original MQL expert

* Money-management options (`MM`, `MMMode`, `Deviation`) are replaced with StockSharp's standard volume property and order execution; slippage management is not replicated.
* Only the moving average types available in StockSharp (simple, exponential, smoothed, weighted) are supported.
* Phase parameters from the MQL indicator are omitted because StockSharp indicators do not expose equivalent controls.
* Orders are executed immediately after a signal is confirmed on the finished candle; there is no explicit scheduling for the next bar opening.

These changes keep the trading idea intact while following StockSharp high-level strategy guidelines.
