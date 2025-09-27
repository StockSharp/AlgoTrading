# Price Flip策略
[English](README.md) | [Русский](README_ru.md)

Price Flip策略通过最近的高点和低点镜像价格，当前一根K线收盘价在该反转价格的另一侧并且快线MA上穿慢线MA时进行交易。可以启用基于慢线MA的趋势过滤。

## 详情

- **入场条件**:
  - 前一根收盘价高于反转价格。
  - 快速MA上穿慢速MA。
  - 可选：启用趋势过滤时，价格在慢速MA之上。
- **多空方向**: 双向。
- **出场条件**:
  - 相反信号触发反向开仓。
- **止损**: 无。
- **默认值**:
  - `TickerMaxLookback` = 100
  - `TickerMinLookback` = 100
  - `FastMaLength` = 12
  - `SlowMaLength` = 14
  - `UseTrendFilter` = true
- **过滤器**:
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: SMA, Highest/Lowest
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 任意
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
