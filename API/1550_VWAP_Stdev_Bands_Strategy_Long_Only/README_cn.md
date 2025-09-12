# VWAP 标准差通道策略（仅多头）

当价格跌破下方 VWAP 标准差通道时买入，达到盈利目标后平仓。

## 参数

- **DevUp**：上方标准差倍数。
- **DevDown**：下方标准差倍数。
- **ProfitTarget**：盈利目标（价格单位）。
- **GapMinutes**：下单间隔（分钟）。
- **CandleType**：蜡烛类型。

