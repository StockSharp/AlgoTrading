# Laguerre CCI MA

该策略结合了 Laguerre 滤波器、商品通道指数（CCI）和指数移动平均线。

## 概述
- Laguerre 滤波器在 0-1 范围内标识超买和超卖区域。
- CCI 用于确认价格动量。
- EMA 的斜率保证交易顺应主要趋势。

## 入场规则
- 当 Laguerre 值为 0、EMA 上升且 CCI 低于负的 `CciLevel` 阈值时做多。
- 当 Laguerre 值为 1、EMA 下降且 CCI 高于正的 `CciLevel` 阈值时做空。

## 出场规则
- 当 Laguerre 值高于 0.9 时平仓多头。
- 当 Laguerre 值低于 0.1 时平仓空头。

## 参数
- `LagGamma` – Laguerre 滤波器的 gamma 参数。
- `CciPeriod` – CCI 的周期。
- `CciLevel` – 用于入场的 CCI 绝对阈值。
- `MaPeriod` – 移动平均线的周期。
- `TakeProfit` – 以绝对价格单位表示的止盈（可选）。
- `StopLoss` – 以绝对价格单位表示的止损（可选）。
- `CandleType` – 用于计算的蜡烛类型。

该策略仅处理已完成的蜡烛，并使用 StockSharp 的高级 API 绑定来获取指标数据。
