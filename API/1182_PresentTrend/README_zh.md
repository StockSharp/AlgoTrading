# PresentTrend 策略

该策略利用基于ATR的阈值以及RSI或MFI来追踪趋势。PresentTrend 线根据振荡器数值与ATR扩张或收缩。当 PresentTrend 与两根K线前的值发生交叉并且最近一次相反信号更近时产生交易信号。

- **多头**：PresentTrend 向上穿越两根K线前的值且最近一次空头信号晚于前一次多头信号。
- **空头**：PresentTrend 向下穿越两根K线前的值且最近一次多头信号晚于前一次空头信号。
- **指标**：ATR、RSI 或 MFI。
- **止损**：在单向模式下出现反向信号时平仓。
