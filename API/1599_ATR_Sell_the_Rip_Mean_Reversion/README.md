# ATR Sell the Rip Mean Reversion Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Short-only strategy that sells when price spikes above a smoothed ATR threshold and covers on a drop below the prior low. An optional EMA filter limits trades to downtrends.

## Details

- **Entry Criteria**: close above smoothed (close + ATR * multiplier)
- **Long/Short**: Short
- **Exit Criteria**: close below previous low
- **Stops**: No
- **Default Values**:
  - `AtrPeriod` = 20
  - `AtrMultiplier` = 1.0
  - `SmoothPeriod` = 10
  - `EmaPeriod` = 200
- **Filters**:
  - Category: Mean Reversion
  - Direction: Short
  - Indicators: ATR, SMA, EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
