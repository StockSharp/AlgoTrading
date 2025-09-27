# Sunil BB Blast Heikin Ashi Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines Bollinger Bands breakout with Heikin Ashi candle confirmation.

The strategy waits for a Bollinger Band breakout aligned with the direction of the previous Heikin Ashi and standard candle. Positions use the opposite band as stop and a risk-to-reward based target.

## Details

- **Entry Criteria**: Price breaks Bollinger Bands with previous Heikin Ashi and candle in same direction.
- **Long/Short**: Configurable via `Direction`.
- **Exit Criteria**: Take-profit or stop-loss based on bands.
- **Stops**: Bollinger band and risk/reward ratio.
- **Default Values**:
  - `BollingerPeriod` = 19
  - `BollingerMultiplier` = 2m
  - `RiskRewardRatio` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `Direction` = TradeDirection.Both
  - `SessionBegin` = 09:20:00
  - `SessionEnd` = 15:00:00
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Bollinger, HeikinAshi
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
