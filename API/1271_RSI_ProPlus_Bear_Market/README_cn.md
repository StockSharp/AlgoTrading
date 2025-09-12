# RSI Pro+ 熊市策略
[English](README.md) | [Русский](README_ru.md)

当 RSI 自下而上穿越设定阈值时，该策略买入，并在价格达到固定百分比的盈利目标时平仓。适用于预计会出现短期反弹的熊市环境。

## 详情

- **入场条件**：RSI 上穿阈值
- **多/空**：仅做多
- **出场条件**：价格达到固定百分比的止盈
- **止损**：无
- **默认值**：
  - `RSI Period` = 11
  - `RSI Level` = 8
  - `Take Profit %` = 0.11
- **筛选**：
  - Category: Momentum
  - Direction: Long
  - Indicators: RSI
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
