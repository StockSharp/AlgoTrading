# Angrybird xScalpingn
[Русский](README_ru.md) | [中文](README_cn.md)

Angrybird xScalpingn is a martingale-style scalping strategy. It opens an initial trade based on short-term price direction and an RSI filter. When price moves against the open position by a dynamic step derived from recent range, the strategy adds another trade with volume multiplied by a factor. All positions are closed when CCI shows a strong counter move or when stop loss or take profit is hit.

## Details

- **Entry Criteria**: Initial trade follows the recent close direction with an RSI filter. Additional trades are opened when price moves against the position by the calculated step.
- **Long/Short**: Both directions.
- **Exit Criteria**: CCI reversal or protective stop loss/take profit.
- **Stops**: Yes.
- **Default Values**:
  - `Volume` = 0.01
  - `LotExponent` = 2
  - `DynamicPips` = true
  - `DefaultPips` = 12
  - `Depth` = 24
  - `Del` = 3
  - `TakeProfit` = 20
  - `StopLoss` = 500
  - `Drop` = 500
  - `RsiMinimum` = 30
  - `RsiMaximum` = 70
  - `MaxTrades` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Grid
  - Direction: Both
  - Indicators: RSI, CCI
  - Stops: Yes
  - Complexity: Advanced
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: High
