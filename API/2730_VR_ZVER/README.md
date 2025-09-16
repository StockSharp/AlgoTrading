# VR ZVER Strategy

## Overview
The VR ZVER strategy is a trend-following system that combines three confirmation layers: a fast/slow/very-slow EMA stack, the Stochastic Oscillator, and the Relative Strength Index (RSI). All active filters must agree before a position is opened, which helps to avoid trades during choppy and conflicting market regimes. The conversion keeps the original break-even and protective logic while using StockSharp's high-level API.

## Market Regime Detection
1. **EMA Structure** – The default configuration uses exponential moving averages with periods 3, 5, and 7. A long bias requires the fast EMA to be above the slow EMA and the slow EMA to remain above the very slow EMA. A short bias inverts this relationship.
2. **Stochastic Oscillator** – The %K/%D pair is inspected for both direction and level. Long trades require %K to be below the lower band and above %D, signalling an oversold bounce. Short trades require %K above the upper band and below %D, pointing to an overbought reversal.
3. **RSI Filter** – The RSI must be below the lower threshold to allow long entries or above the upper threshold to enable short trades.

Only when every enabled filter aligns does the strategy send a market order using the configured volume.

## Risk Management
- **Stop Loss** – Each entry projects a price-based stop using the `StopLossPips` setting multiplied by the instrument's pip size. Long positions exit when the candle low pierces the stop, while short positions close if the candle high hits their stop.
- **Take Profit** – A symmetrical take-profit level is applied. If the current candle reaches the target in favour of the trade, the position is closed immediately.
- **Breakeven Protection** – After price advances by the `BreakevenPips` distance, a breakeven mode is armed. Any retracement back to the entry price will flatten the position to preserve capital.
- **Order Cleanup** – All active orders are cancelled before opening a new trade to avoid unintended stacking.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `CandleType` | Candle series used for calculations. |
| `UseMovingAverage` | Enables or disables the EMA trend filter. |
| `FastMaPeriod`, `SlowMaPeriod`, `VerySlowMaPeriod` | Periods for the fast, slow, and very slow EMAs. |
| `UseStochastic` | Toggles the Stochastic confirmation layer. |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | Period settings for the Stochastic Oscillator. |
| `StochasticUpperLevel`, `StochasticLowerLevel` | Overbought and oversold thresholds for %K. |
| `UseRsi` | Enables or disables the RSI confirmation layer. |
| `RsiPeriod` | RSI averaging period. |
| `RsiUpperLevel`, `RsiLowerLevel` | RSI thresholds that define overbought/oversold regions. |
| `StopLossPips`, `TakeProfitPips` | Distances (in pips) for stop-loss and take-profit placement. |
| `BreakevenPips` | Price progress required before activating breakeven protection. |
| `Volume` | Quantity to trade for every market order. |

## Implementation Notes
- The pip size is derived from the instrument's price step and number of decimals. Instruments with 3 or 5 decimal places automatically apply the standard 10x adjustment used in the original MQL version.
- All indicator data is accessed through `BindEx`, ensuring the strategy reacts only to completed candles with finalised indicator values.
- The strategy is flat by default; positions are never flipped without closing the existing exposure first.
