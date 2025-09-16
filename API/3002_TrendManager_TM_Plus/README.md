# TrendManager TM Plus Strategy

## Overview
TrendManager TM Plus is a trend-following strategy converted from the original MetaTrader 5 expert advisor `Exp_TrendManager_Tm_Plus.mq5`. The strategy relies on the TrendManager custom indicator, which compares two smoothed moving averages and highlights the distance between them. When the distance exceeds a configurable threshold the strategy opens positions in the direction of the prevailing trend and closes positions when the trend reverses or when protective rules are triggered.

## Trading Logic
1. Build two moving averages on the selected candle series. The smoothing methods and lengths of both lines are configurable.
2. Calculate the distance between the fast and slow averages. If the distance is greater than or equal to the threshold, the indicator reports an uptrend. If the distance is less than or equal to the negative threshold, the indicator reports a downtrend. Otherwise there is no actionable signal.
3. Store the color states (0 for uptrend, 1 for downtrend, 3 for neutral) in a short history. The `SignalBar` parameter selects how many closed bars back are evaluated, following the original MQL logic.
4. When a new uptrend color appears the strategy optionally closes existing short positions and can open a long position if long entries are allowed. Conversely, a new downtrend color can close longs and open shorts.
5. Optional time-based and price-based exits close open trades when the holding time exceeds `MaxPositionAge`, when the price drops below `StopLossDistance` for longs (or above for shorts), or when `TakeProfitDistance` is reached.

## Parameters
- **Candle Type** – timeframe used for signal generation (default: 4-hour candles to match the original script).
- **Fast MA Method / Slow MA Method** – smoothing algorithms for the fast and slow lines. Available options: Simple, Exponential, Smoothed, Weighted, Jurik, and Kaufman Adaptive.
- **Fast Length / Slow Length** – periods for the moving averages.
- **Distance Threshold (`DvLimit`)** – minimum absolute distance between the fast and slow averages that is required to detect a trend. Convert original MT5 point-based values into price units (e.g., 70 points on a 5-digit symbol ≈ 0.00070).
- **Signal Bar** – number of closed bars back used to confirm a fresh signal. A value of 1 reproduces the default behaviour of the MQL strategy.
- **Allow Long Entries / Allow Short Entries** – enable or disable entries for each direction.
- **Close Long / Close Short on Opposite Signal** – immediately close open positions when a signal of the opposite color appears.
- **Use Time Exit / Max Position Age** – enable and configure the maximum holding time before a position is forcefully closed.
- **Order Volume** – fixed volume sent with market orders. This parameter replaces the money-management settings of the MetaTrader version.
- **Stop Loss Distance / Take Profit Distance** – optional protective price offsets measured in absolute price units (set to zero to disable).

## Implementation Notes
- StockSharp indicators are used to reproduce TrendManager behaviour. Unsupported exotic smoothing modes from the original library fall back to the closest available StockSharp moving average.
- Signal processing keeps a small history buffer so the `SignalBar` check can detect transitions just like the MT5 advisor.
- Protective exits are evaluated on completed candles. Intrabar fills from the original environment are approximated by comparing candle highs and lows to the configured distances.
- MT5-specific parameters such as `Deviation` and margin-based position sizing have been replaced with StockSharp-friendly counterparts.

## Usage Recommendations
1. Choose a candle type that matches the intended trading horizon. H4 is kept as the default for parity with the source code.
2. Calibrate the threshold to the instrument’s volatility. Instruments with larger ticks or volatility require higher values.
3. Combine the time exit with stop-loss and take-profit distances to emulate the original advisor’s risk controls.
4. For assets that trade in both directions, keep both entry toggles enabled so the strategy can reverse positions when the trend changes.

## Differences from the Original Expert Advisor
- Order sizing uses a fixed `OrderVolume` instead of the MT5 money-management module.
- Stop-loss and take-profit orders are simulated using candle data rather than immediate MT5 order placement.
- The strategy uses StockSharp’s native moving averages. Some smoothing options (e.g., Jurik, Kaufman adaptive) are mapped directly, while unsupported MT5 variants revert to the closest match.
- Time-based exits rely on `MaxPositionAge` with `TimeSpan` precision instead of raw minute counters.

This document provides the essential information required to configure, run, and extend the TrendManager TM Plus strategy inside the StockSharp ecosystem.
