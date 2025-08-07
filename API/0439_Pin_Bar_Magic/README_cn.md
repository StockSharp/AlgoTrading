# Pin Bar Magic 策略
[English](README.md) | [Русский](README_ru.md)

在由三条移动平均线定义的趋势中识别多头和空头 Pin Bar。挂单放置于K线极值处，若数个周期内未成交则取消。仓位大小基于账户风险百分比和 ATR 计算的止损距离。

目标是捕捉关键支撑阻力位的快速反转。当快 EMA 与中 EMA 反向交叉时退出，表明趋势减弱。

## 细节

- **入场条件**：
  - **多头**：快 EMA > 中 EMA > 慢 SMA，且出现向上刺穿均线的看涨 Pin Bar。
  - **空头**：快 EMA < 中 EMA < 慢 SMA，且出现向下刺穿均线的看跌 Pin Bar。
- **出场条件**：
  - 快 EMA 与中 EMA 出现反向交叉。
- **指标**：
  - 慢 SMA（周期50）
  - 中 EMA（18）和快 EMA（6）
  - ATR（周期14）
- **止损**：仓位风险 = EquityRisk% * 账户净值，止损距离 = ATR * Multiplier。
- **默认值**：
  - `EquityRisk` = 3
  - `AtrMultiplier` = 0.5
  - `SlowSmaLength` = 50
  - `MediumEmaLength` = 18
  - `FastEmaLength` = 6
  - `AtrLength` = 14
  - `CancelEntryBars` = 3
- **过滤**：
  - 价格行为反转
  - 默认周期为1小时K线
  - 指标：EMA、SMA、ATR
  - 止损：是
  - 复杂度：较高
