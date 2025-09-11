# Long and Short with Multi Indicators Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses RSI, Rate of Change and a selectable moving average to generate long and short signals. It applies an ATR-based trailing stop for exits.

## Details

- **Entry Criteria**:
  - Long: RSI between oversold and overbought, ROC > 0 and price above MA.
  - Short: Bearish trend confirmed, ROC < 0 and price below MA.
- **Long/Short**: Long and short.
- **Exit Criteria**:
  - ATR-based trailing stop or indicator stop conditions.
- **Stops**: ATR trailing stop.
- **Default Values**:
  - `RsiLength` = 5
  - `RsiOverbought` = 70
  - `RsiOversold` = 44
  - `RocLength` = 4
  - `MaLength` = 24
  - `MaTypeParam` = TEMA
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `BearishMaLength` = 200
  - `BearishTrendDuration` = 5
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: RSI, ROC, MA, ATR
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
