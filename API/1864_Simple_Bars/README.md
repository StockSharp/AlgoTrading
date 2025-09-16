# Simple Bars Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Simple Bars strategy replicates the behavior of the original MQL5 expert `Exp_SimpleBars`. It uses the *SimpleBars* indicator to determine the current trend by comparing the latest candle with recent highs and lows. When the indicator detects a change in trend, the strategy executes a trade at the open of the next bar.

## Details

- **Entry Criteria**
  - **Long**: Indicator signal from previous bar equals *buy*.
  - **Short**: Indicator signal from previous bar equals *sell*.
- **Long/Short**: Both directions are traded.
- **Exit Criteria**
  - Position is reversed when opposite signal appears.
- **Stops**: None.
- **Default Values**
  - `Period` = 6 bars.
  - `UseClose` = `true` (closing price is used for comparison).
  - `CandleType` = 6-hour candles.
- **Filters**
  - Category: Trend following.
  - Direction: Both.
  - Indicators: Custom.
  - Stops: No.
  - Complexity: Medium.
  - Timeframe: Medium-term.
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: No.
  - Risk level: Medium.

