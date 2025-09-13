# EURUSD V2.0 策略
[English](README.md) | [Русский](README_ru.md)

针对 EURUSD 的均值回归策略，利用长期简单移动平均线 (SMA) 和基于 ATR 的波动性过滤。

## 策略逻辑

- 根据所选周期计算长度为 *MA Length* 的 SMA。
- 当价格在 SMA 之上并回落到 *Buffer* 点以内，同时 ATR 低于 *ATR Threshold* 时开 **空**。
- 当价格在 SMA 之下并接近到 *Buffer* 点以内，同时 ATR 较低时开 **多**。
- 仓位大小由账户余额和 *Risk Factor Z* 共同决定。
- 止损与止盈按固定点数距离设置。
- 平仓后，价格需距离入场价 *Noise Filter* 点后方可再次交易。

## 参数

- **MA Length** – SMA 周期（默认 218）。
- **Buffer (pips)** – 触发入场的最大 SMA 偏离（默认 0）。
- **Stop Loss (pips)** – 止损距离（默认 20）。
- **Take Profit (pips)** – 止盈距离（默认 350）。
- **Noise Filter (pips)** – 重新允许交易的价格距离（默认 50）。
- **ATR Length** – ATR 计算周期（默认 200）。
- **ATR Threshold (pips)** – 允许入场的最大 ATR（默认 40）。
- **Max Spread (pips)** – 允许的最大点差（默认 4）。
- **Risk Factor Z** – 资金管理系数（默认 2）。
- **Candle Type** – 使用的 K 线周期（默认 15 分钟）。

该策略使用市价单进行进场和出场。

