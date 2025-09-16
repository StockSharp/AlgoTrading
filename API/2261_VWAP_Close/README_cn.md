# VWAP Close 策略

## 概述
该策略计算收盘价的成交量加权移动平均线（VWMA）。当 VWMA 发生方向变化时，产生潜在的入场或出场信号：

- 当 VWMA 先下降后在当前K线上转为上行（形成谷底）时，策略会平掉空头并视情况开多。
- 当 VWMA 先上升后在当前K线上转为下行（形成峰值）时，策略会平掉多头并视情况开空。

## 参数
- **Period** – 用于 VWMA 计算的K线数量。
- **Candle Type** – 处理的K线时间框架。
- **Buy Open** – 允许开多。
- **Sell Open** – 允许开空。
- **Buy Close** – 当 VWMA 向下转折时允许平多。
- **Sell Close** – 当 VWMA 向上转折时允许平空。

## 说明
策略使用 StockSharp 的 `VolumeWeightedMovingAverage` 指标，仅处理已完成的K线。交易数量来自策略的 `Volume` 属性；开新仓时会自动平掉反向仓位。
