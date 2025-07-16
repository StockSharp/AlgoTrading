# RSI 回归
[English](README.md) | [Русский](README_ru.md)

该策略认为当RSI达到极端值后价格会回归。RSI跌破下限时买入，升破上限时卖出，RSI回到中值附近离场。阈值可根据市场调整，结合趋势过滤器可避免过早逆势。

## 详情
- **入场条件**: 基于 RSI 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号或止损
- **止损**: 是
- **默认值**:
  - `RsiPeriod` = 14
  - `OversoldThreshold` = 30m
  - `OverboughtThreshold` = 70m
  - `ExitLevel` = 50m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 均值回归
  - 方向: 双向
  - 指标: RSI
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
