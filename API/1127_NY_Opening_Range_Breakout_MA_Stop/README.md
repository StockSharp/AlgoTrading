# NY Opening Range Breakout - MA Stop Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trades breakouts of the New York 9:30-9:45 opening range with optional moving-average based exits. Entries occur on the bar after a breakout if within the cutoff time and the price aligns with the moving average filter.

## Details

- **Entry Criteria**:
  - Previous candle closes beyond the opening range high (long) or low (short) before the cutoff time.
  - Current candle is the first after the breakout and satisfies the MA filter when enabled.
- **Long/Short**: Configurable via `TradeDirection`.
- **Exit Criteria**:
  - Stop at the opposite side of the opening range.
  - Take profit according to `TakeProfitType`: fixed risk-reward, moving average cross, or both.
- **Stops**: Yes, at range boundaries.
- **Default Values**:
  - `CutoffHour` = 12
  - `CutoffMinute` = 0
  - `TradeDirection` = LongOnly
  - `TakeProfitType` = FixedRiskReward
  - `TpRatio` = 2.5
  - `MaType` = SMA
  - `MaLength` = 100
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Breakout
  - Direction: Configurable
  - Indicators: Moving Average
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
