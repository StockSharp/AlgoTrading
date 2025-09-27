# IU 4 Bar UP Strategy
[English](README.md) | [Русский](README_ru.md)

IU 4 Bar UP Strategy 是一种仅做多策略，当价格位于 SuperTrend 上方并出现连续四根上涨 K 线时买入。

## 细节
- **数据**：价格K线。
- **入场条件**：
  - **多头**：连续四根上涨 K 线并且收盘价在 SuperTrend 之上。
- **出场条件**：收盘价跌破 SuperTrend。
- **止损**：无。
- **默认值**：
  - `SupertrendLength` = 14
  - `SupertrendMultiplier` = 1
- **过滤器**：
  - 类别：趋势跟随
  - 方向：多头
  - 指标：SuperTrend
  - 复杂度：低
  - 风险等级：中等
