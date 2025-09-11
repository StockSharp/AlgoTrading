# ChopFlow ATR Scalp
[Русский](README_ru.md) | [中文](README_cn.md)

ChopFlow ATR Scalp enters when the market leaves choppy conditions and OBV crosses its EMA. Exits use symmetric ATR-based stops and targets.

The goal is to capture quick moves during early trend formation.

## Details

- **Entry Criteria**: `Choppiness < ChopThreshold` and OBV above/below its EMA.
- **Long/Short**: Both directions.
- **Exit Criteria**: ATR stop or take-profit distance.
- **Stops**: Yes.
- **Default Values**:
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `ChopLength` = 14
  - `ChopThreshold` = 60
  - `ObvEmaLength` = 10
  - `SessionInput` = "1700-1600"
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Scalping
  - Direction: Both
  - Indicators: ATR, Choppiness, OBV
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
