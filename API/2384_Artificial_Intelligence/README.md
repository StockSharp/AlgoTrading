# Artificial Intelligence Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy implements a simple perceptron model on top of Bill Williams' **Acceleration/Deceleration Oscillator (AC)**. Four oscillator readings are sampled at lags of 0, 7, 14 and 21 bars and multiplied by adjustable weights. The weighted sum acts as a decision signal: positive values imply bullish momentum and negative values imply bearish momentum. The strategy reverses its position whenever the signal changes sign and places a fixed stop-loss from the entry price.

The AC itself is derived from the Awesome Oscillator (AO) by subtracting a 5-period moving average from the AO. This makes the strategy sensitive to shifts in market acceleration.

## Details

- **Entry Criteria**:
  - **Long**: Perceptron signal > 0.
  - **Short**: Perceptron signal < 0.
- **Long/Short**: Both sides; the strategy reverses if the signal flips.
- **Exit Criteria**:
  - Stop-loss triggered from the entry price.
  - Reverse when signal crosses zero.
- **Stops**: Yes, fixed stop-loss in price units.
- **Default Values**:
  - `X1` = 76
  - `X2` = 47
  - `X3` = 153
  - `X4` = 135
  - `StopLoss` = 8355
  - `CandleType` = 1-minute candles
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: AC (derived from AO)
  - Stops: Yes
  - Complexity: Moderate
  - Timeframe: Short-term
  - Neural networks: Perceptron
  - Risk level: High
