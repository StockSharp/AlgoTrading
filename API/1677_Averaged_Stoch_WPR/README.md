# Averaged Stoch & WPR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines the Stochastic oscillator with Williams %R to detect extreme market conditions.
A long position is opened when the Stochastic value drops below 0.1 and Williams %R is under -90, signaling deep oversold pressure.
A short position is opened when the Stochastic rises above 99.9 and Williams %R exceeds -5, indicating strong overbought conditions.

The strategy works on any instrument and timeframe supported by the selected candle type. It can trade both long and short positions and offers an optional percentage stop loss for risk management.

## Details

- **Entry Criteria**:
  - **Long**: Stochastic < 0.1 and Williams %R < -90.
  - **Short**: Stochastic > 99.9 and Williams %R > -5.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal or triggered stop loss.
- **Stops**: Optional percentage-based stop loss.
- **Indicators**:
  - Stochastic oscillator (default period 26).
  - Williams %R (default period 26).

## Parameters

- `StochPeriod` – Stochastic calculation period.
- `WprPeriod` – Williams %R calculation period.
- `StopLossPercent` – Percent-based stop loss size.
- `CandleType` – Candle type used for indicator calculations.
