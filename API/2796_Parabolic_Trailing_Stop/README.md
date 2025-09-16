# Parabolic Trailing Stop Strategy

## Overview
The **Parabolic Trailing Stop Strategy** is a direct port of the MetaTrader expert advisor `Parabolic_TrailingStop.mq5`. The strategy does **not** generate entry signals. Instead, it focuses entirely on managing risk for existing positions by converting the Parabolic SAR values into dynamic stop levels. Once a position is detected, the strategy continuously evaluates the previous candle and the corresponding Parabolic SAR value to tighten the stop loss while preserving the trend-following nature of the indicator.

The implementation follows the high-level StockSharp API guidelines: candles are subscribed through `SubscribeCandles`, the Parabolic SAR indicator is bound via `Bind`, and risk management is implemented without using custom collections or manual indicator polling.

## Parameters
- **Candle Type** – timeframe of the candles that drive the trailing logic. Default value: 5-minute time frame.
- **SAR Step** – acceleration step for the Parabolic SAR. This value is applied both as the initial acceleration factor and as the incremental step. Default value: `0.1`.
- **SAR Max** – maximum acceleration factor that limits the Parabolic SAR progression. Default value: `0.11`.

All parameters are exposed through `StrategyParam<T>` making them ready for optimization or user-driven adjustments in Designer/Backtester environments.

## Trailing Logic Details
1. **Position Detection**
   - On each finished candle the strategy reads the current position value.
   - When the position changes sign (flat → long, flat → short, or direction flip), the strategy stores the candle close time as the entry timestamp and clears any previous stop levels.
2. **Stop Level Calculation**
   - The Parabolic SAR indicator is evaluated through `subscription.Bind(parabolicSar, ProcessCandle)` which delivers the value already aligned with each candle.
   - The logic uses the *previous* candle values (SAR, high, low, time) to mimic the MQL approach where `Sar_array_base[1]` refers to the prior bar.
   - **Long positions**: when the previous SAR value is above the average entry price but still below the previous candle low, and the SAR timestamp is later than the entry time, the long stop is raised to that SAR value.
   - **Short positions**: when the previous SAR value is below the average entry price but above the previous candle high, and its timestamp is later than the entry time, the short stop is lowered to that SAR value.
   - Stops are only tightened in the favorable direction (upward for longs, downward for shorts) to avoid loosening protection.
3. **Stop Execution**
   - When the running stop level is reached by the current candle (low pierces the long stop, high pierces the short stop) the strategy cancels any pending orders, closes the position at market, and resets the stored stop/entry timestamps.
4. **Chart Support**
   - If a chart area is available the strategy draws candles, the Parabolic SAR indicator line, and the trade markers to provide visual feedback.

## Usage Notes
- The strategy is designed as a trailing stop module. Combine it with other entry strategies or manually opened positions to benefit from automatic stop tightening.
- Because the logic relies on finished candles, intrabar movements between candle closes are not evaluated. Consider lower timeframes if more granular trailing is required.
- Logging statements inform about every stop update and stop hit, mirroring the diagnostics one would expect from the original MetaTrader expert.
- There is no Python implementation for this strategy (per project guidelines).
