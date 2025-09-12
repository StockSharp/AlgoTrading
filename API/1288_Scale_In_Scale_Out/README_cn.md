# Scale In Scale Out 策略
[English](README.md) | [Русский](README_ru.md)

该策略通过在每根K线投入可用资金的固定百分比来逐步建立头寸。当头寸价值达到设定的盈利阈值时，策略会卖出部分仓位，并可选择保留部分已实现利润。

## 详情

- **入场条件**：有可用资金时买入。
- **出场条件**：盈利百分比超过阈值时卖出。
- **多/空**：仅做多。
- **默认值**：
  - `Buy Scaling Size %` = 2
  - `Take Profit Level %` = 50
  - `Take Profit Size %` = 1
  - `Retain Profit Portion %` = 50
  - `Minimum Position Value` = 200000
  - `Minimum Buy Value` = 100
- **筛选**：
  - 类别: 其他
  - 方向: 多头
  - 指标: 无
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 任意
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险水平: 中等
