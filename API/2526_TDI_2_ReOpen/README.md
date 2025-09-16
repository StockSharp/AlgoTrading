# 2526 TDI-2 Re-Open Strategy

## Overview
This strategy is a C# conversion of the MetaTrader 5 expert advisor **Exp_TDI-2_ReOpen**. It trades using the Trend Direction Index (TDI-2) indicator and applies the original position re-entry logic. The C# port uses the high-level StockSharp API and keeps the core behavior of the MQL version: it reacts to crossings between the TDI momentum line and the TDI index line, scales into profitable positions after a configurable price advance, and manages trades with optional protective stops.

## Indicators
- **TDI-2 indicator** – a custom momentum-based indicator implemented in this repository. It builds two lines:
  - *Directional line*: `Period × SmoothedMomentum`, where the momentum equals the applied price minus the price `Period` bars ago.
  - *Index line*: `|Directional| − (2 × Period × Smooth(|Momentum|, 2×Period) − |Momentum|)`.
- The indicator supports the following smoothing methods: Simple, Exponential, Smoothed (RMA), and Linear Weighted moving averages.
- Supported applied price options replicate the original MQL implementation, including the TrendFollow and Demark formulas.

## Trading Logic
1. On every finished candle the strategy evaluates the TDI-2 values at the bar specified by **Signal Bar** (default: previous closed candle) and one bar earlier.
2. When the directional line was above the index line and then crosses below it:
   - If **Allow Long Entries** is enabled and no long position is active, the strategy prepares a new long entry.
   - If a short position exists and **Allow Short Exits** is enabled, it closes the short position.
3. When the directional line was below the index line and then crosses above it:
   - If **Allow Short Entries** is enabled and no short position is active, the strategy prepares a new short entry.
   - If a long position exists and **Allow Long Exits** is enabled, it closes the long position.
4. Re-entry logic (scale-in):
   - While holding a long position the strategy tracks the fill price of the latest long trade. If the market moves in favor by **Re-entry Step (points)** and the number of executed long trades is still below **Max Entries**, it opens an additional long order with the base volume.
   - The same logic applies to short positions using the most recent short fill price.
5. When opening a position while an opposite position exists, the strategy sends a combined market order sized to both close the opposite exposure and establish the new position with the configured base volume.
6. Optional stop-loss and take-profit levels are activated through `StartProtection` using the instrument's `PriceStep` multiplier.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| Money Management | Base order volume. | 0.1 |
| Max Entries | Maximum number of entries per direction (initial trade + re-entries). | 10 |
| Stop Loss (points) | Stop-loss distance in instrument points. | 1000 |
| Take Profit (points) | Take-profit distance in instrument points. | 2000 |
| Slippage (points) | Retained for compatibility; not used in the StockSharp implementation. | 10 |
| Re-entry Step (points) | Minimum favorable move before scaling into an existing position. | 300 |
| Allow Long Entries / Allow Short Entries | Enable opening long/short positions. | true |
| Allow Long Exits / Allow Short Exits | Enable closing long/short positions. | true |
| Candle Type | Candle series used for calculations. | H4 candles |
| TDI Smoothing | Smoothing method for the TDI-2 indicator. | Simple MA |
| TDI Period | Momentum lookback period. | 20 |
| TDI Phase | Reserved for compatibility with the MQL input (no effect in supported smoothing modes). | 15 |
| Applied Price | Price source used by TDI-2. | Close |
| Signal Bar | Number of closed candles to look back when evaluating crosses. | 1 |

## Additional Notes
- Only the smoothing methods supported by StockSharp indicators (SMA, EMA, SMMA, LWMA) are implemented. Other MQL modes such as JJMA or T3 are not available.
- The **TDI Phase** parameter is kept for completeness. It does not influence the supported smoothing methods and can be left at its default value.
- The **Slippage (points)** parameter is provided for parity with the original expert advisor but is not used by the high-level API.
- The scale-in counters reset automatically whenever the net position returns to zero.
