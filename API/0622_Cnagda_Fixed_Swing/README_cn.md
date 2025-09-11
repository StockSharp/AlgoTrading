# Cnagda Fixed Swing 策略

该策略使用 Heikin Ashi K 线并提供两种模式：
- **RSI**：在高成交量下 RSI 短期 EMA 上穿长期 EMA 时开仓。
- **Scalp**：基于 Heikin Ashi 收盘价的 EMA 与 WMA 交叉开仓。

止损设置在最近的摆动高/低，止盈按照固定风险回报倍数计算。

## 参数
- K线类型
- 交易逻辑
- 摆动回溯
- 风险回报
