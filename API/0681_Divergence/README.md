# Divergence Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on price and RSI divergence with simple pivot detection.

Divergence Strategy uses pivot highs and lows in price and RSI to detect bullish and bearish divergences. When price makes a new high but RSI fails to confirm, the strategy sells. Conversely, when price makes a new low while RSI rises, it buys.

## Details

- **Entry Criteria**: Price and RSI divergences.
- **Long/Short**: Both directions (configurable).
- **Exit Criteria**: Opposite RSI signal or protective orders.
- **Stops**: Yes (stop loss & take profit).
- **Default Values**:
  - `TradeDirection` = Both
  - `RsiPeriod` = 14
  - `StopLossPercent` = 2m
  - `RiskReward` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes
  - Risk Level: Medium
