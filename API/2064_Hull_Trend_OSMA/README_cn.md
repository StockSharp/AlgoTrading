# Hull Trend OSMA 策略

该策略是 MetaTrader "Exp_HullTrendOSMA" 专家的转换版本。

## 概述

策略使用 Hull Trend OSMA 指标。该指标计算 Hull 移动平均线以及其平滑版本，两者的差值即为振荡器。当振荡器在连续两个已完成的 K 线中上升时，策略开多仓；当振荡器在连续两个已完成的 K 线中下降时，策略开空仓。每次信号出现时都会平掉反向仓位。

## 参数

- **Hull Period** – Hull 移动平均线的周期。
- **Signal Period** – 应用于振荡器的平滑移动平均线周期。
- **Take Profit** – 以价格单位表示的止盈距离。
- **Stop Loss** – 以价格单位表示的止损距离。
- **Candle Type** – 计算所使用的 K 线时间框架（默认 8 小时）。

## 说明

- 使用 StockSharp 高级 API，自动订阅 K 线。
- 入场和平仓均使用市价单执行。
- 止损和止盈保护在策略启动时初始化一次。
