# 自适应斐波那契回撤策略
[English](README.md) | [Русский](README_ru.md)

该策略将三条使用斐波那契倍数(0.618、1.618、2.618)构建的SuperTrend线求平均，并通过EMA平滑。交易基于回撤至此自适应趋势，同时使用AMA中线和可选的RSI过滤方向。

## 细节

- **入场条件**：
  - 最低价跌破平均SuperTrend且收盘价高于其平滑值。
  - 前一根K线相对于AMA中线的位置定义回撤。
  - **多头**：收盘价高于中线且RSI > 阈值。
  - **空头**：收盘价低于中线且RSI < 阈值。
- **多空方向**：双向。
- **离场条件**：
  - 收盘价反向穿越平滑后的SuperTrend。
- **止损/止盈**：通过 `StartProtection` 设置百分比止损和止盈。
- **默认值**：
  - `AtrPeriod` = 8
  - `SmoothLength` = 21
  - `AmaLength` = 55
  - `RsiLength` = 7
  - `RsiBuy` = 70
  - `RsiSell` = 30
  - `TakeProfitPercent` = 5
  - `StopLossPercent` = 0.75
- **筛选项**：
  - 类别：趋势回撤
  - 方向：双向
  - 指标：SuperTrend、EMA、AMA、RSI
  - 止损：是
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中等
