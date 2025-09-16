# Color Schaff Momentum Trend Cycle 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用 Color Schaff Momentum Trend Cycle (STC) 指标。当指标从超买或超卖区域退出时，识别潜在的趋势反转。

## 详情

- **入场条件**:
  - 当前一个 STC 颜色值高于上区 (>5) 且当前颜色下降到 6 以下时买入，同时平掉空头仓位。
  - 当前一个 STC 颜色值低于下区 (<2) 且当前颜色上升到 1 以上时卖出，同时平掉多头仓位。
- **方向**: 多/空。
- **出场条件**: 反向信号关闭相反方向仓位。
- **止损**: 无固定止损或止盈。
- **默认参数**:
  - `FastMomentum` = 23
  - `SlowMomentum` = 50
  - `Cycle` = 10
  - `HighLevel` = 60
  - `LowLevel` = -60
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true

