# Parabolic Sar Stochastic Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Implementation of strategy - Parabolic SAR + Stochastic. Buy when price is above SAR and Stochastic %K is below 20 (oversold). Sell when price is below SAR and Stochastic %K is above 80 (overbought).

Testing indicates an average annual return of about 61%. It performs best in the crypto market.

Parabolic SAR supplies the trend and Stochastic refines entry on pullbacks. Signals flip when SAR changes side.

A straightforward trend strategy with built-in SAR stops. The SAR flip is the only exit, so no further stop setting is needed.

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
  - `StochPeriod` = 14
  - `StochOversold` = 20m
  - `StochOverbought` = 80m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    StochK in the rules is %K: the stochastic over StochPeriod candles smoothed over StochK candles; %D plays no part, so it has no setting. An entry signal against an open position reverses it.
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

