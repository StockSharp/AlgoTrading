# Crypto Strategy SUSDT 10 min
[English](README.md) | [Русский](README_ru.md)

基于 EMA 的简单策略：当收盘价高于 EMA 且开盘价低于 EMA 时做多，反之做空。止损和止盈按照入场价的百分比设置。

## 详情

- **入场条件**：
  - **多头**：`close > EMA` 且 `open < EMA`
  - **空头**：`close < EMA` 且 `open > EMA`
- **多空方向**：双向。
- **离场条件**：止盈或止损。
- **止损/止盈**：有，均为百分比。
- **默认值**：
  - `CandleType` = 10 分钟
  - `EmaLength` = 24
  - `TakeProfitPercent` = 4
  - `StopLossPercent` = 2
  - `OrderPercent` = 30
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：EMA
  - 止损：是
  - 复杂度：低
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
