# Parabolic SAR Bug
[English](README.md) | [Русский](README_ru.md)

**Parabolic SAR Bug** 策略利用 Parabolic SAR 指标捕捉趋势反转。当 SAR 点位从价格上方翻到下方时开多单，反之开空单。可选的反向模式会反转信号。通过内置的保护模块支持止损、止盈以及可选的跟踪止损。

## 细节

- **入场条件**：Parabolic SAR 方向变化。
- **多空方向**：双向。
- **出场条件**：相反的 SAR 信号或保护性止损。
- **止损类型**：止损、止盈、可选跟踪止损。
- **默认值**：
  - `Step` = 0.02
  - `MaxStep` = 0.2
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 1
  - `UseTrailingStop` = false
  - `Reverse` = false
  - `CloseOnSar` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选**：
  - 类别: 趋势
  - 方向: 双向
  - 指标: Parabolic SAR
  - 止损: 止损、止盈
  - 复杂度: 基础
  - 时间框架: 日内 (5 分钟)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
