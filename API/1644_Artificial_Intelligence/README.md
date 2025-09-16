# Artificial Intelligence Perceptron Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Artificial Intelligence strategy uses a simple perceptron to combine multiple Accelerator Oscillator (AC) readings at different time shifts. The weighted sum of the current AC value and three lagged values (7, 14, 21 bars back) determines the direction of the trade. When the perceptron output is positive the strategy opens or maintains a long position; when negative it opens or maintains a short position.

After an entry the strategy protects the trade with a stop-loss expressed in points. As price moves in the profitable direction the stop level trails behind the price. If the perceptron output flips sign while the position is profitable the strategy reverses, closing the current position and entering the opposite one.

Testing shows this approach can react quickly to momentum changes while keeping risk under control. It works on any instrument that provides candle data and does not rely on specific market regimes.

## Details

- **Entry Criteria**  
  - **Long**: Perceptron output > 0 and no existing long position.  
  - **Short**: Perceptron output < 0 and no existing short position.
- **Exit / Reverse**  
  - Trailing stop triggered.  
  - Perceptron output changes sign; strategy reverses position.
- **Stops**: Yes, trailing stop based on `StopLoss` parameter.
- **Default Values**  
  - `X1 = 135`  
  - `X2 = 127`  
  - `X3 = 16`  
  - `X4 = 93`  
  - `StopLoss = 85`
- **Filters**  
  - Category: Momentum  
  - Direction: Both  
  - Indicators: Accelerator Oscillator  
  - Stops: Yes  
  - Complexity: Medium  
  - Timeframe: Short-term  
  - Neural networks: Perceptron  
  - Divergence: No  
  - Risk level: Medium
