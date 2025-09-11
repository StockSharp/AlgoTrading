# SMC Strategy BTC 1H OB FVG
[Русский](README_ru.md) | [中文](README_cn.md)

Smart Money Concepts based strategy for Bitcoin on 1-hour candles. The system enters long after a bullish break of structure when price returns to the detected order block or fair value gap. Stop loss uses an ATR multiplier and take profit is calculated from a risk/reward ratio.

## Details

- **Entry Criteria**: After bullish BOS, buy if price touches order block or fair value gap within `ZoneTimeout` bars.
- **Long/Short**: Long only.
- **Exit Criteria**: Fixed take profit and stop loss.
- **Stops**: Fixed using ATR.
- **Default Values**:
  - `UseOrderBlock` = true
  - `UseFvg` = true
  - `AtrFactor` = 6
  - `RiskRewardRatio` = 2.5
  - `ZoneTimeout` = 10
  - `CandleType` = TimeSpan.FromHours(1)
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: ATR
  - Stops: Fixed
  - Complexity: Simple
  - Timeframe: Intraday (1H)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
