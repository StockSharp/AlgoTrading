# 自动化5分钟机器人策略
[English](README.md) | [Русский](README_ru.md)

自动化5分钟机器人策略基于5分钟K线。
当价格处于上升趋势并且买入量大于卖出量时做多，
在相反条件下做空。

## 细节

- **入场**：上升趋势（收盘价高于50周期SMA并高于6根K线前的收盘价）且买入量超过卖出量。
- **出场**：出现反向信号时反转仓位。
- **止损**：入场价下方3%的止损和上方29%的止盈。
- **默认值**：
  - `MaLength` = 50
  - `VolumeLength` = 10
  - `StopLossPercent` = 3
  - `TakeProfitPercent` = 29
  - `CandleType` = 5m
