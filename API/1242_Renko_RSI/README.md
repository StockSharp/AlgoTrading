# Renko RSI
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy trades renko bricks using RSI overbought/oversold signals.

Testing shows moderate performance and works best on markets with clear renko trends.

Renko RSI uses renko bricks built from ATR and applies a short RSI. A cross above the oversold level triggers a buy, while a drop below the overbought level triggers a sell.

## Details

- **Entry Criteria**: RSI crosses oversold or overbought levels.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `RenkoAtrLength` = 14
  - `RsiLength` = 2
  - `RsiOverbought` = 80
  - `RsiOversold` = 20
  - `CandleType` = Renko ATR(14)
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: RSI, Renko
  - Stops: No
  - Complexity: Basic
  - Timeframe: Renko
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

