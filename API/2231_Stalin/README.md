# Stalin Indicator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the "Stalin" indicator logic from MQL5.
It uses a pair of exponential moving averages (EMAs) and an optional RSI filter.
A long signal appears when the fast EMA crosses above the slow EMA and RSI is above 50.
A short signal appears when the fast EMA crosses below the slow EMA and RSI is below 50.

Signals may be confirmed by a required price move and filtered by the distance from the last signal.
Positions are opened with market orders and reversed on opposite signals.

## Details

- **Entry Criteria**:
  - **Long**: `FastEMA(t-1) < SlowEMA(t-1)` && `FastEMA(t) > SlowEMA(t)` && `RSI(t) > 50`.
  - **Short**: `FastEMA(t-1) > SlowEMA(t-1)` && `FastEMA(t) < SlowEMA(t)` && `RSI(t) < 50`.
- **Confirm**: Trade is delayed until price moves by `Confirm` points from the breakout level.
- **Flat Filter**: New signals are ignored if they are closer than `Flat` points to the previous signal price.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `FastLength` = 14.
  - `SlowLength` = 21.
  - `RsiLength` = 17.
  - `Confirm` = 0 points (disabled).
  - `Flat` = 0 points (disabled).
  - `CandleType` = 1 hour candles.
- **Filters**:
  - Category: Trend following.
  - Direction: Both.
  - Indicators: Multiple.
  - Stops: No.
  - Complexity: Medium.
  - Timeframe: Medium-term.
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: No.
  - Risk level: Medium.
