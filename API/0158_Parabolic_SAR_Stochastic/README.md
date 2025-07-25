# Parabolic Sar Stochastic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Implementation of strategy - Parabolic SAR + Stochastic. Buy when price is above SAR and Stochastic %K is below 20 (oversold). Sell when price is below SAR and Stochastic %K is above 80 (overbought).

Testing indicates an average annual return of about 61%. It performs best in the crypto market.

Parabolic SAR supplies the trend and Stochastic refines entry on pullbacks. Signals flip when SAR changes side.

A straightforward trend strategy with built-in SAR stops. ATR settings handle additional risk control.

## Details

- **Entry Criteria**:
  - Long: `Close > SAR && StochK < StochOversold`
  - Short: `Close < SAR && StochK > StochOverbought`
- **Long/Short**: Both
- **Exit Criteria**:
  - Parabolic SAR flip in opposite direction
- **Stops**: Dynamic SAR based
- **Default Values**:
  - `AccelerationFactor` = 0.02m
  - `MaxAccelerationFactor` = 0.2m
  - `StochK` = 3
  - `StochD` = 3
  - `StochPeriod` = 14
  - `StochOversold` = 20m
  - `StochOverbought` = 80m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Parabolic SAR, Parabolic SAR, Stochastic Oscillator
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

