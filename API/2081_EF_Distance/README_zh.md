# EF Distance Reversal 策略
[English](README.md) | [Русский](README_ru.md)

该策略是 MetaTrader "Exp_EF_distance" 顾问在 StockSharp 平台上的改写。原始的 EF Distance 和 Flat-Trend 指标被简单移动平均线（SMA）和平均真实波幅（ATR）过滤器所取代，用于寻找市场的转折点。算法观察三个连续的 SMA 值以识别局部极值。当 SMA 形成局部底部且波动性超过阈值时开多仓；当出现相反形态时开空仓。头寸在出现反向信号或触及止损/止盈水平时关闭。

## 详情

- **入场条件**：
  - **多头**：`SMA(t-1) < SMA(t-2)` 且 `SMA(t) > SMA(t-1)` 且 `ATR(t) ≥ AtrThreshold`。
  - **空头**：`SMA(t-1) > SMA(t-2)` 且 `SMA(t) < SMA(t-1)` 且 `ATR(t) ≥ AtrThreshold`。
- **方向**：双向。
- **出场条件**：
  - 出现相反方向的信号。
  - 触发止损或止盈。
- **指标**：
  - 简单移动平均线（SMA）——近似原 EF Distance。
  - 平均真实波幅（ATR）——波动性过滤器。
- **默认值**：
  - `SMA period` = 10。
  - `ATR period` = 20。
  - `ATR threshold` = 1。
  - `StopLoss` = 100。
  - `TakeProfit` = 200。
- **过滤器**：
  - 类别：反转
  - 方向：双向
  - 指标数量：两个
  - 止损：是
  - 复杂度：中等
  - 时间框架：可配置
  - 季节性：否
  - 神经网络：否
  - 背离：是（使用转折点）
  - 风险水平：中等
