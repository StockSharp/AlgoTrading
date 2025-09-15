# SAR Trailing System Strategy
[English](README.md) | [Русский](README_ru.md)

该策略在固定时间间隔随机开仓多头或空头，并使用抛物线 SAR 指标管理退出。
SAR 值作为跟踪止损，当价格穿越 SAR 水平时平仓。

## 细节

- **入场条件**：
  - 每到 `TimerInterval` 且没有持仓并且 `UseRandomEntry` 启用时，随机开多或开空。
- **多空方向**：双向
- **出场条件**：价格穿越 Parabolic SAR。
- **止损**：初始止损以 tick 为单位，结合 Parabolic SAR 跟踪退出。
- **默认值**：
  - `TimerInterval` = 300 秒
  - `StopLossTicks` = 10
  - `AccelerationStep` = 0.02
  - `AccelerationMax` = 0.2
  - `UseRandomEntry` = true
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **筛选**：
  - 类别：趋势跟踪
  - 方向：双向
  - 指标：Parabolic SAR
  - 止损：是
  - 复杂度：初级
  - 时间框架：短期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
