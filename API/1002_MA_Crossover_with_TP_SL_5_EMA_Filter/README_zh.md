# MA 均线交叉策略，带 TP/SL 和 5 EMA 过滤

当快速 SMA 上穿慢速 SMA 且收盘价高于 5 周期 EMA 时开多；当快速 SMA 下穿慢速 SMA 且收盘价低于 EMA(5) 时开空。策略使用基于百分比的止盈和止损。

## 参数
- Fast MA Length
- Slow MA Length
- EMA Length
- Target %
- Stop %
- Candle Type
