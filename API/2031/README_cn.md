# Dig Variation 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 MQL5 的 *DigVariation* 示例，使用简单移动平均线 (SMA) 近似原始指标。当 SMA 趋势改变方向时开仓。

## 逻辑
- 对每根到来的 K 线计算 SMA。
- 如果之前的 SMA 值呈上升趋势并且当前值继续上行，则开多单。
- 如果之前的 SMA 值呈下降趋势并且当前值继续下行，则开空单。
- 当趋势反转时，关闭已有仓位。

## 参数
- **Period** – SMA 计算周期。
- **BuyOpen** – 允许做多。
- **SellOpen** – 允许做空。
- **BuyClose** – 允许平多。
- **SellClose** – 允许平空。
- **StopLoss** – 止损值（传递给 `StartProtection`）。
- **TakeProfit** – 止盈值（传递给 `StartProtection`）。

## 备注
这是简化的转换，使用标准 SMA 替代原始 DigVariation 指标。

