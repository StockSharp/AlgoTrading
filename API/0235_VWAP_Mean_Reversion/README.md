# VWAP Mean Reversion Strategy

This strategy fades moves away from the volume weighted average price. ATR is used to gauge how far price must deviate from VWAP before a reversal trade is considered.

A long position opens when price drops below VWAP by more than `K` times the ATR. A short is taken when price rallies above VWAP by the same amount. Trades exit as soon as price returns to the VWAP line.

The approach is designed for intraday traders who expect prices to oscillate around VWAP rather than trend strongly. Stops sized as a multiple of ATR help keep losses controlled if the move continues against the trade.

## Details
- **Entry Criteria**:
  - **Long**: Close < VWAP - K * ATR
  - **Short**: Close > VWAP + K * ATR
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when close >= VWAP
  - **Short**: Exit when close <= VWAP
- **Stops**: Yes, ATR-based stop.
- **Default Values**:
  - `K` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `AtrPeriod` = 14
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: VWAP, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
