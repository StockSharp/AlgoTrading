# Fine-tune Inputs Gann + Laplace Smooth Volume Zone Oscillator 策略

该策略使用经指数移动平均平滑的成交量振荡器。
当平滑后的振荡器上穿阈值时开多仓；
当其跌破负阈值时开空仓。
若无信号且启用 **Close All**，则平掉所有持仓。

## 参数
- **Fast Volume EMA** – 快速成交量均线周期。
- **Slow Volume EMA** – 慢速成交量均线周期。
- **Smooth Length** – 振荡器平滑周期。
- **Threshold** – 触发信号的阈值。
- **Close All** – 无信号时是否关闭持仓。
- **Candle Type** – 计算所用的K线类型。
