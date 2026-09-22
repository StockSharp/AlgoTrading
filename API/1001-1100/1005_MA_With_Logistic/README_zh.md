# MA With Logistic 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

尽管保留了历史名称，本实现实际是双 EMA 交叉策略，并不计算逻辑回归模型。已完成 K 线上的交叉会开仓或反转仓位，百分比止盈和止损从成交价计算。

## 细节
- **数据**：价格K线。
- **入场条件**：
  - **多头**：快速 EMA 向上穿越慢速 EMA。
  - **空头**：快速 EMA 向下穿越慢速 EMA。
- **出场条件**：达到 `TakeProfitPercent` 或 `StopLossPercent`；反向交叉会反转仓位。
- **冷却期**：每次交叉下单后等待五根已完成 K 线。
- **默认值**：
  - `FastLength` = 12
  - `SlowLength` = 25
  - `TakeProfitPercent` = 8
  - `StopLossPercent` = 5
  - `CandleType` = 20 分钟
- **过滤器**：
  - 分类：趋势跟随
  - 方向：多头 & 空头
  - 指标：MA
  - 复杂度：低
  - 风险等级：中等
