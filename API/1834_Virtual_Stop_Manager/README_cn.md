# 虚拟止损管理器

该策略由 MetaTrader 顾问 “VR---STEALS-3-EN” 转换而来，实现隐藏的止损、止盈、追踪止损和保本机制。策略在启动后开立多头仓位，并在不向交易所发送可见保护单的情况下管理退出。

## 参数
- **Volume**：下单量。
- **Take Profit (points)**：获利平仓的点数距离。
- **Stop Loss (points)**：止损平仓的点数距离。
- **Trailing Stop (points)**：从最高价回落的追踪止损距离。
- **Breakeven (points)**：达到该利润点数后将止损移动到开仓价。
- **Candle Type**：用于处理的蜡烛类型。
