# Aroon Horn Sign
[English](README.md) | [Русский](README_ru.md)

**Aroon Horn Sign** 策略使用 Aroon 指标寻找趋势反转。
它在较大时间框架的K线中监控 Aroon Up 和 Aroon Down 线。当
Aroon Up 上穿 Aroon Down 并保持在 50 水平以上时，表明可能出现
多头反转。策略会平掉任何空头仓位并开立多头。当 Aroon Down
占优并位于 50 以上时，任何多头仓位都会被关闭并建立空头。

策略采用以价格单位表示的固定止盈和止损，通过内置的风险
保护模块触发。由于逻辑仅依赖 Aroon 指标，该方法适用于不同
市场与时间框架，无需额外过滤器。

## 细节
- **数据**：价格K线。
- **入场条件**：
  - **多头**：`Aroon Up` > `Aroon Down` 且 `Aroon Up` >= 50。
  - **空头**：`Aroon Down` > `Aroon Up` 且 `Aroon Down` >= 50。
- **出场条件**：
  - 多头在出现空头信号时平仓。
  - 空头在出现多头信号时平仓。
- **止损**：通过 `StartProtection` 设置固定止盈和止损。
- **默认值**：
  - `AroonPeriod` = 9
  - `CandleType` = 4 小时K线
  - `TakeProfit` = 2000（价格单位）
  - `StopLoss` = 1000（价格单位）
- **过滤器**：
  - 分类：趋势反转
  - 方向：多头和空头
  - 指标：Aroon
  - 复杂度：简单
  - 风险等级：中等
