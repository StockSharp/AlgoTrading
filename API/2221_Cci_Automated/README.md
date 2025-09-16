# CCI Automated
[Русский](README_ru.md) | [中文](README_cn.md)

CCI Automated is a reversal strategy that reacts to Commodity Channel Index (CCI) threshold crossings. It goes long when CCI rises above −80 after dipping below −90, and goes short when CCI falls below 80 after exceeding 90. The system duplicates trades up to a user-defined limit, manages risk with fixed take-profit and stop-loss levels, and trails profits with a configurable trailing stop.

The approach aims to catch early momentum shifts after oversold or overbought conditions. By stacking multiple positions and moving the stop as price advances, it attempts to capitalize on sustained reversals while capping downside risk.

## Details

- **Entry Criteria**: CCI crosses above -80 after being below -90 for longs; crosses below 80 after being above 90 for shorts.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop loss, take profit, or trailing stop.
- **Stops**: Yes.
- **Default Values**:
  - `CciPeriod` = 9
  - `TradesDuplicator` = 3
  - `Volume` = 0.03
  - `StopLoss` = 50
  - `TakeProfit` = 200
  - `TrailingStop` = 50
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: CCI
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

