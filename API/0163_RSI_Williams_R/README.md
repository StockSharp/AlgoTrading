# Rsi Williams R Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Implementation of strategy - RSI + Williams %R. Buy when RSI is below 30 and Williams %R is below -80 (double oversold condition). Sell when RSI is above 70 and Williams %R is above -20 (double overbought condition).

RSI outlines the overall momentum, while Williams %R gives a quicker signal of reversal. Trades act on agreement between the two oscillators.

Good for active traders chasing short swings. ATR-based stops are employed.

## Details

- **Entry Criteria**:
  - Long: `RSI < RsiOversold && WilliamsR < WilliamsROversold`
  - Short: `RSI > RsiOverbought && WilliamsR > WilliamsROverbought`
- **Long/Short**: Both
- **Exit Criteria**:
  - RSI returns to neutral zone
- **Stops**: Percent-based using `StopLoss`
- **Default Values**:
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80m
  - `WilliamsROverbought` = -20m
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: RSI, Williams %R, R
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
