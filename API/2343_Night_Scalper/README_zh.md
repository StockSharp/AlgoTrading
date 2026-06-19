# 夜间剥头皮策略
[English](README.md) | [Русский](README_ru.md)

该策略在晚上时段利用布林带交易。只有在达到设定的开始时间后，且带宽较窄并且价格突破带外时才开仓。

## 细节

- **入场条件**：
  - **多头**：在 `Start Hour` 之后，收盘价低于下轨且带宽小于 `Range Threshold`。
  - **空头**：在 `Start Hour` 之后，收盘价高于上轨且带宽小于 `Range Threshold`。
- **多/空**：双向。
- **出场条件**：
  - 若时间回到次日 `Start Hour` 之前，平掉现有仓位。
  - 通过 `StartProtection` 管理止损与止盈。
- **止损**：使用 `StartProtection` 固定止损和止盈。
- **默认值**：
  - `BB Period` = 40
  - `BB Deviation` = 1
  - `Range Threshold` = 450
  - `Stop Loss` = 370
  - `Take Profit` = 20
  - `Start Hour` = 19
  - `Candle Type` = 1h
- **过滤条件**：
  - 类别：均值回归
  - 方向：双向
  - 指标：布林带
  - 止损：是
  - 复杂度：低
  - 时间框架：短期
