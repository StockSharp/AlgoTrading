# Forex Profit 策略
[English](README.md) | [Русский](README_ru.md)

来自 MetaTrader 的 “Forex Profit” 专家顾问的移植版本。策略在每根完成的K线上检查三条指数移动平均线是否排列，并用 Parabolic SAR 做趋势确认，然后按收盘价入场。风险控制包括不对称的止损/止盈距离、跟踪止损以及基于 EMA 反转的利润锁定。

## 细节

- **入场条件**：
  - 多头：`EMA10` 同时高于 `EMA25` 和 `EMA50`，上一根K线的 `EMA10` 不高于 `EMA50`，且 Parabolic SAR 位于上一收盘价之下。
  - 空头：`EMA10` 同时低于 `EMA25` 和 `EMA50`，上一根K线的 `EMA10` 不低于 `EMA50`，且 Parabolic SAR 位于上一收盘价之上。
  - 每根完结K线只评估一次信号。
- **出场条件**：
  - 当 `EMA10` 跌破其前值且当前利润超过 `ProfitThreshold` 时平掉多头。
  - 当 `EMA10` 升破其前值且当前利润超过 `ProfitThreshold` 时平掉空头。
  - 开仓时同时设置止损和止盈（多空使用不同的距离）。
  - 价格朝有利方向运行 `TrailingStopPoints` 后启动跟踪止损，之后按 `TrailingStepPoints` 的步长上调。
- **止损**：是 — 固定止损、固定止盈与跟踪止损结合。
- **默认参数**：
  - `FastEmaLength` = 10
  - `MediumEmaLength` = 25
  - `SlowEmaLength` = 50
  - `TakeProfitBuyPoints` = 55
  - `TakeProfitSellPoints` = 65
  - `StopLossBuyPoints` = 60
  - `StopLossSellPoints` = 85
  - `TrailingStopPoints` = 74
  - `TrailingStepPoints` = 5
  - `ProfitThreshold` = 10
  - `SarAcceleration` = 0.02
  - `SarMaxAcceleration` = 0.2
  - `Volume` = 1
  - `CandleType` = 1 小时时间框架
- **补充说明**：
  - 所有距离以价格最小变动单位表示，系统会根据品种的最小跳动值自动换算。
  - 盈利平仓依据仓位的总利润（包含持仓量）并将价格跳动转换为账户货币。
  - 跟踪止损保持在价格背后，只有当变动超过设定步长时才会移动。
- **过滤标签**：
  - 类型: Trend following
  - 方向: 多空双向
  - 指标: EMA, Parabolic SAR
  - 止损: 是（固定 + 跟踪）
  - 复杂度: 中等
  - 时间框架: 可配置（默认 1 小时）
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
