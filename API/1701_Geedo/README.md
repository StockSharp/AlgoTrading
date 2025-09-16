# Geedo Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Time-based strategy that compares open prices from two past bars at a specific hour. If the earlier bar is above the recent one by a threshold, a short trade is opened. If the recent bar is above the earlier one, a long trade is opened. Each position uses fixed stop loss and take profit and is closed after a maximum holding time.

## Details

- **Entry Criteria**: At `TradeTime` compare open prices `T1` and `T2` bars ago. If `Open[T1] - Open[T2]` exceeds `DeltaShort`, sell; if `Open[T2] - Open[T1]` exceeds `DeltaLong`, buy.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop loss, take profit, or `MaxOpenTime` hours after entry.
- **Stops**: Fixed point stop loss and take profit.
- **Default Values**:
  - `TakeProfitLong` = 39
  - `StopLossLong` = 147
  - `TakeProfitShort` = 15
  - `StopLossShort` = 6000
  - `TradeTime` = 18
  - `T1` = 6
  - `T2` = 2
  - `DeltaLong` = 6
  - `DeltaShort` = 21
  - `Volume` = 0.01
  - `MaxOpenTime` = 504
- **Filters**:
  - Category: Time-based
  - Direction: Both
  - Indicators: None
  - Stops: Fixed
  - Complexity: Beginner
  - Timeframe: Intraday (1h)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
