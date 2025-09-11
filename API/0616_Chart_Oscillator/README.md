# Chart Oscillator
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades using a selectable oscillator. Choose between Stochastic, RSI, or MFI. It buys when the oscillator signals oversold conditions and sells when overbought. For the Stochastic option, signals use %K and %D crossovers.

Testing shows good performance on volatile markets like cryptocurrencies.

Positions reverse when opposite conditions appear or the stop-loss triggers.

## Details

- **Entry Criteria**: Oscillator oversold/overbought levels and %K/%D crossovers.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `Choice` = OscillatorChoice.Stochastic
  - `Length` = 14
  - `KPeriod` = 14
  - `DPeriod` = 3
  - `SmoothK` = 3
  - `Overbought` = 80
  - `Oversold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2.0m
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: Stochastic/RSI/MFI
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

