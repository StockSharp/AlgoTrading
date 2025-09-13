# Milestone Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp port of the Milestone 22.5 expert advisor. It trades pullbacks within a trend by combining two smoothed moving averages with a volatility and spike filter. When a candle breaks the previous bar's extreme and the fast average supports the move, a position is opened in the direction of the dominant trend. ATR prevents trading in quiet markets and large candle bodies are treated as spikes.

Backtests of the original MQL version show good performance on major forex pairs. The C# translation focuses on clarity and uses only market orders for entries and exits.

## Details

- **Entry Criteria**:
  - Trend strength between `MinTrend` and `MaxTrend`.
  - Candle breaks the prior high or low and fast SMA confirms.
  - ATR above `MinRange` and candle body below `CandleSpike`.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal closes the position.
- **Stops**: Not implemented; opposite signal acts as stop.
- **Default Values**:
  - `SlowMaPeriod` = 120
  - `FastMaPeriod` = 30
  - `AtrPeriod` = 14
  - `MinTrend` = 10
  - `MaxTrend` = 100
  - `MinRange` = 5
  - `CandleSpike` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA, ATR
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

