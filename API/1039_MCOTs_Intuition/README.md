# MCOTs Intuition Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on RSI momentum relative to its standard deviation. It buys when upward momentum is strong but fading and sells on the opposite. Fixed profit target and stop loss are placed in ticks.

## Details

- **Entry Criteria**:
  - Long: momentum > stdDev * multiplier and momentum < previousMomentum * exhaustionMultiplier
  - Short: momentum < -stdDev * multiplier and momentum > previousMomentum * exhaustionMultiplier
- **Long/Short**: Both
- **Exit Criteria**:
  - Fixed profit target and stop loss in ticks
- **Stops**: Yes
- **Default Values**:
  - `RsiPeriod` = 14
  - `StdDevMultiplier` = 1m
  - `ExhaustionMultiplier` = 1m
  - `ProfitTargetTicks` = 40
  - `StopLossTicks` = 160
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: RSI, StandardDeviation
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
