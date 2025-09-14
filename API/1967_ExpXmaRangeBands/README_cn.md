# Exp XMA Range Bands 策略

该策略将 MetaTrader 示例“Exp_XMA_Range_Bands” 转换为 StockSharp 高级 API。它利用 Keltner 通道（由移动平均算和平均真实波幅构成）来确定动态支撑和阻力。当价格在突破通道后重新回到通道内部时产生交易信号。

## 工作原理

1. 构建 Keltner 通道：
   - EMA 周期 `MaLength`
   - ATR 周期 `RangeLength`
   - ATR 乘数 `Deviation`
2. 当上一样 K 线收盘价高于上一个上轨时，并消除所有空头。如果下一样 K 线收盘价回到通道内（收盘价 ≤ 当前上轨），则开多。
3. 当上一样 K 线收盘价低于上一个下轨时，并消除所有多头。如果下一样 K 线收盘价回到通道内（收盘价 ≥ 当前下轨），则开空。
4. 开仓后根据点数设置止损和止盈。

## 参数

- `MaLength` – 通道中心的 EMA 周期。
- `RangeLength` – 用于通道宽度的 ATR 周期。
- `Deviation` – ATR 乘数。
- `StopLoss` – 止损点数（通过 `Security.PriceStep` 换算为价格）。
- `TakeProfit` – 止盈点数。
- `CandleType` – 用于计算的 K 线系列。

## 指标

- KeltnerChannels（EMA + ATR）

