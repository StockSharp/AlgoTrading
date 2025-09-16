# SMI Correct 策略

## 概述
SMI Correct 策略基于 Stochastic Momentum Index (SMI) 指标进行交易。策略跟踪 SMI 线及其移动平均信号线。当 SMI 线下穿信号线时开多仓；当 SMI 线上穿信号线时开空仓。

## 参数
- **Candle Type** – 使用的K线周期。
- **SMI Length** – 计算 SMI 的周期数。
- **Signal Length** – 信号线的平滑周期。

## 工作原理
1. 策略订阅指定类型的K线。
2. 每根收盘K线更新 SMI 和信号线的数值。
3. 当 SMI 下穿信号线时，平掉空头并开多头。
4. 当 SMI 上穿信号线时，平掉多头并开空头。

示例还在图表上绘制K线和指标以供可视化。
