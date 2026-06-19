# Max Gain
[English](README.md) | [Русский](README_ru.md)

Max Gain 通过比较在回溯期内从最低价到当前最高价的百分比上涨与从最高价到当前最低价的百分比下跌（调整后），当潜在涨幅大于调整后的跌幅时做多，否则做空。

## 细节
- **数据**：价格K线。
- **入场条件**：
  - **多头**：Max gain > adjusted max loss。
  - **空头**：Adjusted max loss > max gain。
- **出场条件**：反向信号。
- **止损**：无。
- **默认值**：
  - `PeriodLength` = 30
- **过滤器**：
  - 类别：动量
  - 方向：多头 & 空头
  - 指标：Highest, Lowest
  - 复杂度：低
  - 风险等级：中等
