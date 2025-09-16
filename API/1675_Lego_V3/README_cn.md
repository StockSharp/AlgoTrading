# Lego V3 策略

该策略移植自 MQL4 的 “Lego_v3” EA。  
它结合多个常见指标来生成交易信号：

- **移动平均线** – 快速和慢速 SMA 用于判断趋势方向。
- **随机指标** – %K 和 %D 值确定超买和超卖区域。
- **Awesome Oscillator** – 用于确认动量是否与趋势一致。
- **平均真实波幅 (ATR)** – 用于设定止损和止盈距离。

当快速均线向上穿越慢速均线、随机指标 %K 低于买入水平并且 AO 为正时开多单。  
反向条件时开空。ATR 在首次使用时启动保护性止损管理。

## 参数

- `FastMaPeriod` – 快速均线周期。
- `SlowMaPeriod` – 慢速均线周期。
- `StochK` – 随机指标 %K 周期。
- `StochD` – 随机指标 %D 周期。
- `StochBuy` – %K 买入阈值。
- `StochSell` – %K 卖出阈值。
- `AtrPeriod` – ATR 周期。
- `AtrMultiplier` – ATR 止损倍数。
- `CandleType` – 使用的蜡烛类型。
