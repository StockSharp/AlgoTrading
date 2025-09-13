# MA2CCI
[Русский](README_ru.md) | [中文](README_cn.md)

Moving average crossover strategy confirmed by CCI. Uses ATR for stop-loss.

## Details

- **Entry Criteria**:
  - Long when fast SMA crosses above slow SMA and CCI crosses above 0.
  - Short when fast SMA crosses below slow SMA and CCI crosses below 0.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite crossover or stop-loss at 1 ATR from entry.
- **Stops**: ATR-based stop at entry price ± ATR.
- **Default Values**:
  - `FastMaPeriod` = 4
  - `SlowMaPeriod` = 8
  - `CciPeriod` = 4
  - `AtrPeriod` = 4
  - `CandleType` = 1 minute
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: SMA, CCI, ATR
  - Stops: ATR
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
