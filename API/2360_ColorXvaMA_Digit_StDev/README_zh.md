# ColorXvaMA Digit StDev 策略

## 概述
该策略基于价格相对于指数移动平均线 (EMA) 的偏离程度进行交易。两个偏差倍数（K1 和 K2）定义了由价格标准差计算的内外带。

当价格高于 EMA 且超过 K2 倍标准差时，策略做多；当价格低于 EMA 且超过 K2 倍标准差时，策略做空。持仓在价格回到由 K1 定义的内带内时平仓。

## 参数
- **EMA Length** – EMA 的周期。
- **StdDev Length** – 标准差的计算周期。
- **Deviation K1** – 用于平仓的内带倍数。
- **Deviation K2** – 用于开仓的外带倍数。
- **Candle Type** – 蜡烛图时间周期。

## 指标
- Exponential Moving Average
- StandardDeviation

## 工作原理
1. 订阅所选时间周期的蜡烛图。
2. 计算价格的 EMA 和标准差。
3. 计算价格与 EMA 的偏差。
4. 当偏差超过 ±K2×StdDev 时开仓。
5. 当偏差回到 ±K1×StdDev 以内时平仓。
