# Three EMA Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Three EMA Cross strategy combines a classic fast/slow moving average crossover with a longer trend filter. After the fast EMA crosses above the slow EMA, the strategy waits for a pullback to the fast average while the closing price remains above a broader trend EMA. This setup attempts to capture continuation moves after a brief retracement within the prevailing trend.

Positions are exited when momentum fades and the fast EMA crosses back below the slow EMA. A percentage-based stop loss protects the position if price moves against the trade. The technique works well on markets with persistent trends and tends to avoid choppy ranges.

## Details

- **Entry Criteria**:
  - Recent fast EMA cross above slow EMA within last *N* bars.
  - Current close ≥ fast EMA and session low ≤ fast EMA.
  - Trend EMA ≤ current close.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Fast EMA drops below slow EMA.
- **Stops**: Stop loss at `stop_loss_percent` of entry price.
- **Default Values**:
  - `FastEmaLength` = 10
  - `SlowEmaLength` = 20
  - `TrendEmaLength` = 100
  - `StopLossPercent` = 2.0
  - `CrossBackBars` = 10
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
