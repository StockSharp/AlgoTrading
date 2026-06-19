# CandlesticksBW 策略

该策略复现 Bill Williams 的 CandlesticksBW 方法。通过 Awesome Oscillator (AO) 与 Accelerator Oscillator (AC) 的动量变化为每根蜡烛上色，并根据颜色转换开仓或平仓。

## 工作原理
- AO 计算为中值价格 5 与 34 周期 SMA 之差。
- AC 计算为 AO 与其 5 周期 SMA 的差值。
- 每根蜡烛根据 AO/AC 增减及蜡烛方向被划分为六种颜色。
- 当倒数第二根蜡烛颜色小于 2 且上一根颜色大于 1 时开多，并平空。
- 当倒数第二根蜡烛颜色大于 3 且上一根颜色小于 4 时开空，并平多。
- 通过 `StartProtection` 应用止损和止盈。

## 参数
- `CandleType` – 蜡烛周期。
- `SignalBar` – 信号偏移的柱数。
- `StopLoss` – 止损点数。
- `TakeProfit` – 止盈点数。
- `BuyPosOpen` – 允许做多开仓。
- `SellPosOpen` – 允许做空开仓。
- `BuyPosClose` – 允许平多。
- `SellPosClose` – 允许平空。

## 指标
- Awesome Oscillator（由 SMA 计算）。
- Accelerator Oscillator。

## 交易规则
- **开多：** 倒数第二根颜色 <2 且上一根颜色 >1。
- **开空：** 倒数第二根颜色 >3 且上一根颜色 <4。
- **平多：** 满足开空条件且持有多头。
- **平空：** 满足开多条件且持有空头。
