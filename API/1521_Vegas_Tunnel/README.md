# Vegas Tunnel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Uses four EMAs to define a tunnel and optional ATR based stops.
Opens long when price and fast EMA are above slow and tunnel EMAs, short when below.

## Details

- **Entry Criteria**: alignment of EMAs with price relative to tunnel
- **Long/Short**: Both
- **Exit Criteria**: stop loss or take profit
- **Stops**: ATR or EMA based
- **Default Values**:
  - `RiskRewardRatio` = 2
  - `UseAtr` = true
  - `AtrLength` = 14
  - `AtrMult` = 1.5
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
