# SAW System 1 策略
[English](README.md) | [Русский](README_ru.md)

该突破策略在每天开始时放置买入和卖出止损单。策略计算最近几天的平均日波幅，并将其用作止损和止盈的基准。两侧都挂单，预期只有一侧会被触发。

在设定的 `OpenHour`，策略根据当前价格和平均波幅的一半距离计算 Buy Stop 和 Sell Stop 价格。止损和止盈以平均波幅的百分比表示。当一侧触发后，另一侧可以被取消，也可以保留用于反向开仓。可选的马丁加尔选项会在触发后按 `MartingaleMultiplier` 放大剩余挂单的数量。

如果到 `CloseHour` 仍有挂单未成交，则全部撤单以避免隔夜风险。开仓后立即按照成交价放置止损和止盈保护单。

## 细节

- **入场条件：**
  - 使用 ATR 计算 `VolatilityDays` 天的平均日波幅。
  - 根据该波幅的 `StopLossRate`% 和 `TakeProfitRate`% 计算止损与止盈距离。
  - 在 `OpenHour` 以 `offset = stopLoss/2` 的距离放置买卖止损单。
- **出场条件：**
  - 保护性止损和止盈单平仓。
  - 到 `CloseHour` 未成交的挂单全部撤销。
- **反转模式：**
  - 当 `Reverse` 为真时，保留相反方向的止损单以实现反向开仓。
  - 若同时启用 `UseMartingale`，该挂单的数量乘以 `MartingaleMultiplier`。
- **方向：** 做多与做空。
- **止损：** 基于日波幅的固定止损和止盈。
- **默认参数：**
  - `VolatilityDays` = 5
  - `OpenHour` = 7
  - `CloseHour` = 10
  - `StopLossRate` = 15%
  - `TakeProfitRate` = 30%
  - `Reverse` = false
  - `UseMartingale` = false
  - `MartingaleMultiplier` = 2.0

该策略旨在在平静的夜间交易后捕捉早晨的突破，同时通过基于波动性的目标控制风险。
