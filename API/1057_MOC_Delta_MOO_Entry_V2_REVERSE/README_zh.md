# MOC Delta MOO Entry v2 Reverse 策略
[English](README.md) | [Русский](README_ru.md)

该策略是 MOC Delta MOO Entry 的反向版本。在下午 14:50–14:55 期间统计买卖量差，并将其保存为当日成交量的百分比。次日 08:30 若该百分比超过阈值，则按相反方向开仓，并使用两条移动平均线过滤。仓位通过基于 tick 的止盈/止损或在 14:50 平仓。

## 细节

- **入场条件**：
  - **多头**：08:30 时保存的差值百分比低于 `-DeltaThreshold`，且开盘价高于 SMA15 与 SMA30，并且 SMA15 高于 SMA30。
  - **空头**：08:30 时保存的差值百分比高于 `DeltaThreshold`，且开盘价低于 SMA15 与 SMA30，并且 SMA15 低于 SMA30。
- **多空方向**：双向。
- **出场条件**：
  - 基于 tick 的止盈和止损。
  - 14:50 平掉所有仓位。
- **止损**：
  - `TpTicks` = 20 tick 止盈。
  - `SlTicks` = 10 tick 止损。
- **默认值**：
  - `DeltaThreshold` = 2
  - `TpTicks` = 20
  - `SlTicks` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **筛选**：
  - 类型: 成交量
  - 方向: 双向
  - 指标: SMA
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
