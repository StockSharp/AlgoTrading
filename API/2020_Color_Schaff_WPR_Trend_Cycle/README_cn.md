# 彩色Schaff WPR趋势循环策略

该策略实现了MetaTrader中的 **Color Schaff WPR Trend Cycle** 专家顾问。
它利用快慢Williams %R计算的Schaff趋势循环来识别市场转向。

算法仅在已完成的K线运行。当指标值上穿上方阈值时，策略开多并平掉任何空头；当指标值下破下方阈值时，策略开空并平掉任何多头。

## 参数
- **Fast WPR** – 快速Williams %R周期。
- **Slow WPR** – 慢速Williams %R周期。
- **Cycle** – Schaff趋势计算的周期长度。
- **High Level** – 做多触发的上方阈值。
- **Low Level** – 做空触发的下方阈值。
- **Candle Type** – 用于评估指标的K线时间框架。

## 链接
- 原始MQL源码：`MQL/13489/mql5/Experts/exp_colorschaffwprtrendcycle.mq5`
- 指标：`MQL/13489/mql5/Indicators/colorschaffwprtrendcycle.mq5`
