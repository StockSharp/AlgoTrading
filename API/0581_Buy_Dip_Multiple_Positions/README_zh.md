# Buy Dip Multiple Positions 策略
[English](README.md) | [Русский](README_ru.md)

Buy Dip Multiple Positions 策略在价格下跌伴随放量及价格冲击条件时加仓做多。每笔交易风险为账户权益的 2%，并共用追踪止损和目标价。仅在上一笔交易盈利时才允许开新仓。

## 详情

- **入场条件**:
  - 收盘价比前一根K线最低价低 0.2%。
  - 当前成交量高于前两根K线平均成交量的120%。
  - 收盘价低于 N 根K线前收盘价乘以 `PriceSurgePercent` / 100。
- **方向**: 仅做多。
- **出场条件**:
  - 初始止损为入场低点的一定百分比。
  - 追踪止损在信号后每根K线按百分比上移。
  - 目标价为入场低点之上的一定百分比。
- **止损**: 有。
- **默认值**:
  - `MaxPositions` = 20
  - `TrailRatePercent` = 1
  - `InitialStopPercent` = 85
  - `TargetPricePercent` = 60
  - `PriceSurgePercent` = 89
  - `SurgeLookbackBars` = 14
- **过滤器**:
  - 分类: 反趋势
  - 方向: 多头
  - 指标: 成交量, 价格行为
  - 止损: 有
  - 复杂度: 中
  - 时间框架: 任意
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
