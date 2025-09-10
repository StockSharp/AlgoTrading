# BTCUSD Adjustable SLTP Strategy
[English](README.md) | [Русский](README_ru.md)

该策略在 BTCUSD 上使用 SMA(10) 与 SMA(25) 的交叉，并配合 EMA(150) 过滤。多头在均线金叉后等待回撤：记录最高价并计算回撤水平，当价格再次上破该水平时入场。空头在均线死叉且价格低于 EMA(150) 时立即入场。

退出使用可调的止盈、止损和保本距离。当价格低于 EMA(150) 时，若 SMA(10) 下穿 SMA(25)，多头仓位也将被关闭。

## 细节

- **入场条件**：
  - 多头：SMA(10) 上穿 SMA(25)，价格回撤指定百分比后再次上破回撤水平。
  - 空头：SMA(10) 下穿 SMA(25)，且价格低于 EMA(150)。
- **多空**：做多和做空。
- **出场条件**：
  - 可调止盈、止损与保本距离。
  - 当 SMA(10) 下穿 SMA(25) 且价格低于 EMA(150) 时平多。
- **止损**：是，按点数设置。
- **默认值**：
  - `FastSmaLength` = 10
  - `SlowSmaLength` = 25
  - `EmaFilterLength` = 150
  - `TakeProfitDistance` = 1000
  - `StopLossDistance` = 250
  - `BreakEvenTrigger` = 500
  - `RetracementPercentage` = 0.01
- **筛选**：
  - 类别：趋势跟随
  - 方向：多空
  - 指标：SMA, EMA
  - 止损：是
  - 复杂度：中
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中
