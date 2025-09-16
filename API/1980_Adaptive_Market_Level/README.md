# Adaptive Market Level
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that trades based on the Adaptive Market Level (AML) indicator. The indicator adapts to current volatility and plots a dynamic price level. A long position is opened when the AML line turns upward and a short position when it turns downward. Opposite positions are closed on a color change or when stop loss/take profit triggers.

The system follows medium-term trends and works on higher timeframes by default.

## Details

- **Entry Criteria**: AML line changes direction upward for longs and downward for shorts.
- **Long/Short**: Both directions.
- **Exit Criteria**: AML direction change or stop/target.
- **Stops**: Yes.
- **Default Values**:
  - `Fractal` = 6
  - `Lag` = 7
  - `StopLossTicks` = 1000
  - `TakeProfitTicks` = 2000
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Adaptive Market Level
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: H4
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
