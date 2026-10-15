# Ultimate Strategy Template 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

使用双 EMA 趋势过滤器的 RSI 动量策略。仅在 K 线完成后评估信号；在 80 根 K 线的信号冷却期内，百分比止盈和止损保护仍保持有效。

## 详情

- **入场条件**：RSI 向上穿越 50 且快 EMA 高于慢 EMA 时做多；RSI 向下穿越 50 且快 EMA 低于慢 EMA 时做空。
- **多空方向**：双向。
- **出场条件**：RSI 反向穿越 50，或触发止盈、止损。
- **止损**：百分比止损和止盈。
- **冷却期**：信号入场或 RSI 出场后等待 80 根已完成 K 线；该限制不会停用止盈或止损保护。
- **默认值**：
  - `FastLength` = 9
  - `SlowLength` = 21
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 3
  - `CandleType` = 5 分钟
- **筛选**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：RSI、EMA
  - 止损：支持
  - 复杂度：基础
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：中
