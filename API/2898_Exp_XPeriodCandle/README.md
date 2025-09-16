# Exp XPeriod Candle Strategy

This strategy is a StockSharp port of the MQL5 expert advisor `Exp_XPeriodCandle`. It rebuilds the custom XPeriodCandle indicator with high-level API components and uses candle color transitions to open and close positions.

## Concept

* Smooth the open, high, low, and close of each finished candle using a configurable moving-average approximation.
* Track the resulting "candle color" (bullish if the smoothed close is above the smoothed open, bearish otherwise).
* Use the color of the last two completed candles (configurable shift) to detect reversals and issue trading signals.
* Optionally close opposite positions when a new signal appears and apply protective stop-loss/take-profit levels expressed in price points.

## Implementation details

* Smoothing types supported directly: Simple, Exponential, Smoothed (RMA), and Linear Weighted. All other options are approximated with an exponential smoother because StockSharp does not include direct equivalents of JJMA/JurX/Parabolic/T3/VIDYA/AMA. Documented in code comments to keep behaviour transparent.
* Sliding queues store the last `Period` smoothed highs and lows to keep the price range consistent with the original indicator.
* The strategy waits until enough history is available before calling `BuyMarket`/`SellMarket` and marks itself as formed to work with StockSharp backtesting filters.
* Optional slippage, stop-loss, and take-profit conversions rely on the security price step. When the step is unknown the raw point values are used.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Time frame of the processed candles. |
| `Period` | Depth of the smoothing window (same as the indicator period). |
| `SmoothingMethod` | Moving-average approximation used for all OHLC series. Unsupported methods fall back to EMA. |
| `SmoothingLength` | Length parameter for the smoother. |
| `SmoothingPhase` | Additional phase input (kept for completeness; only active in original MQL JJMA family). |
| `SignalBar` | Which completed candle to evaluate (1 = previous candle, replicating the MQL expert default). |
| `EnableLongEntry` / `EnableShortEntry` | Allow opening positions in the corresponding direction. |
| `EnableLongExit` / `EnableShortExit` | Close existing positions when an opposite signal is detected. |
| `StopLossPoints` / `TakeProfitPoints` | Protective exits expressed in price points. Set to zero to disable. |
| `SlippagePoints` | Allowed slippage in price points applied to market orders. |

## Trading rules

1. Smooth the latest finished candle and append its color to the rolling history.
2. When `SignalBar` and older colors exist:
   * If the older candle was bullish (color < 1) and the newer candle is non-bullish (color > 0), open a long position (if allowed) and optionally close shorts.
   * If the older candle was bearish (color > 1) and the newer candle is non-bearish (color < 2), open a short position (if allowed) and optionally close longs.
3. Position size follows the strategy `Volume` setting; opposing exposure is flattened before reversing.
4. Risk management is handled by `StartProtection` using the provided point distances.

## Notes

* The original expert uses the proprietary `SmoothAlgorithms.mqh`. Because StockSharp lacks direct JJMA/JurX/T3 implementations, the C# conversion approximates those modes with exponential smoothing. This behaviour is documented in code comments and the README so that optimisers can adjust the parameters if needed.
* Inputs and defaults mirror the MQL version, allowing similar optimisation ranges.
