# EMA WPR Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining an EMA trend filter with Williams %R signals. Buys at oversold levels and sells at overbought levels. A retracement threshold prevents consecutive entries. Optional exits close trades on opposite Williams %R extremes or after several unprofitable bars.

## Details

- **Entry Criteria**:
  - Long: Williams %R <= -100 and EMA trend up
  - Short: Williams %R >= 0 and EMA trend down
- **Long/Short**: Both
- **Exit Criteria**:
  - Williams %R crosses opposite extreme when `UseWprExit` is enabled
  - Position stays unprofitable for `MaxUnprofitBars` bars when `UseUnprofitExit` is enabled
- **Stops**: No
- **Default Values**:
  - `WprPeriod` = 46
  - `WprRetracement` = 30
  - `EmaPeriod` = 144
  - `BarsInTrend` = 1
  - `MaxUnprofitBars` = 5
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: EMA, Williams %R
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
