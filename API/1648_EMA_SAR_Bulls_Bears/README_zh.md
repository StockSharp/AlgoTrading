# EMA SAR Bulls Bears 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合了快慢指数移动平均线 (EMA)、抛物线 SAR 以及 Bulls/Bears Power 指标。策略仅在设定的日内时间段内交易，并使用简单的止盈止损保护。

当 EMA3 低于 EMA34、抛物线 SAR 位于蜡烛高点之上且 Bears Power 为负但上升时开空仓。相反条件下开多仓：EMA3 高于 EMA34、SAR 低于蜡烛低点且 Bulls Power 为正但下降。

## 细节

- **入场条件**：
  - **做多**：EMA3 高于 EMA34，SAR 低于蜡烛最低点，Bulls Power > 0 且下降。
  - **做空**：EMA3 低于 EMA34，SAR 高于蜡烛最高点，Bears Power < 0 且上升。
- **方向**：多空皆可。
- **出场条件**：出现反向信号或触发止损/止盈。
- **止损/止盈**：是，使用固定止盈（400 点）和止损（2000 点）。
- **过滤器**：
  - 仅在 09:00 至 17:00 之间交易。
  - 使用 15 分钟蜡烛。
