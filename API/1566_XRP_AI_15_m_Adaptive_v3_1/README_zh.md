# XRP AI 15-m Adaptive v3.1 策略

该策略在 15 分钟图上交易 XRP，并使用更高周期的趋势过滤。它在小回调、中等量冲洗或大动量爆发之间选择，并基于 ATR 设置止损、止盈、追踪止损以及时间退出。

## 参数
- **Risk Mult** – 初始止损的 ATR 倍数。
- **Small TP** – 小回调路径的 ATR 止盈倍数。
- **Med TP** – 中等量冲洗路径的 ATR 止盈倍数。
- **Large TP** – 大动量路径的 ATR 止盈倍数。
- **Volume Mult** – 判断量能尖峰的 20 期 SMA 倍数。
- **Trail Percent** – 追踪止损占 ATR 的百分比。
- **Trail Arm** – 激活追踪止损所需的 ATR 盈利倍数。
- **Max Bars** – 持仓的最大 15 分钟柱数。
- **Candle Type** – 主计算使用的 K 线类型。
- **Trend Candle Type** – 趋势过滤使用的 K 线类型。
