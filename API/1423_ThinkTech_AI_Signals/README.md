# ThinkTech AI Signals Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades breakouts of the first 15-minute candle of the session. It uses ATR-based stop-loss and take-profit levels and can apply optional trend and RSI filters.

## Details

- **Entry Criteria**:
  - **Long**: Price breaks above the first candle high with trend and RSI filters satisfied.
  - **Short**: Price breaks below the first candle low with trend and RSI filters satisfied.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Reach take-profit or stop-loss level.
- **Stops**: Yes, ATR-based.
- **Default Values**:
  - `RiskRewardRatio` = 2
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `RsiPeriod` = 14
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
