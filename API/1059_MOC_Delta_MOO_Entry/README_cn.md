# MOC Delta MOO Entry 策略
[Русский](README_ru.md) | [English](README.md)

该策略在14:50–14:55期间计算买卖量差，并在08:30当差值占日成交量的百分比超出阈值时入场。策略使用开盘价的SMA过滤，并在入场时附加以tick为单位的止盈和止损。

## 细节

- **入场条件：**
  - **做多：** 08:30，MOC Δ% 高于阈值，开盘价高于SMA15和SMA30。
  - **做空：** 08:30，MOC Δ% 低于负阈值，开盘价低于SMA15和SMA30。
- **出场条件：**
  - **止损/止盈：** 使用tick的止盈和止损。
  - **时间：** 14:50 平掉所有持仓。
- **默认值：**
  - `DeltaThreshold` = 2
  - `TakeProfitTicks` = 20
  - `StopLossTicks` = 10
