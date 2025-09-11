# EMA 10/55/200 Long-Only MTF 策略
[English](README.md) | [Русский](README_ru.md)

当 4 小时图上的 EMA 金叉得到日线和周线趋势确认时，本策略开多仓。

## 细节

- **入场条件**：
  - `EMA10` 上穿 `EMA55` 且蜡烛最高价高于 `EMA55`，或 `EMA55` 上穿 `EMA200`，或 `EMA10` 上穿 `EMA500`。
  - 日线 `EMA55` 高于 `EMA200` 且周线 `EMA55` 高于 `EMA200`。
- **出场条件**：
  - `EMA10` 下穿 `EMA200` 或 `EMA500`。
  - 价格触及止损水平。
- **参数**：
  - `EMA 10 Length` = 10
  - `EMA 55 Length` = 55
  - `EMA 200 Length` = 200
  - `EMA 500 Length` = 500
  - `Stop Loss %` = 5
