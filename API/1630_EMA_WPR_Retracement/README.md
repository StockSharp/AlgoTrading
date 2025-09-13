# EMA WPR Retracement Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trend-following strategy that combines an EMA trend filter with Williams %R extremes. It waits for a retracement in Williams %R before allowing another trade and can pyramid up to a set number of positions.

## Details

- **Entry Criteria**:
  - **Long**: Williams %R drops below -100 and then a retracement occurs above `WPR Retracement`. Optional uptrend confirmed by EMA.
  - **Short**: Williams %R rises above 0 and then retraces below `-WPR Retracement`. Optional downtrend confirmed by EMA.
- **Long/Short**: Both directions with pyramiding.
- **Exit Criteria**:
  - Williams %R leaves the extreme zone.
  - Optional exit after `Max Unprofit Bars` without profit.
  - Stop loss, take profit and optional trailing stop managed by protection module.
- **Stops**: Fixed stop loss and take profit with optional trailing stop.
- **Default Values**:
  - `Use EMA Trend` = true
  - `Bars In Trend` = 1
  - `EMA Trend` = 144
  - `WPR Period` = 46
  - `WPR Retracement` = 30
  - `Use WPR Exit` = true
  - `Order Volume` = 0.1
  - `Max Trades` = 2
  - `Stop Loss` = 50
  - `Take Profit` = 200
  - `Use Trailing` = false
  - `Trailing Stop` = 10
  - `Use Unprofit Exit` = false
  - `Max Unprofit Bars` = 5
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: EMA, Williams %R
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
