# 船长回测模型策略
[Русский](README_ru.md) | [English](README.md)

跟踪早盘价格区间以确定当天方向，在交易窗口内回调后突破入场。

## 详情

- **方向**：早盘区间高点或低点决定做多或做空。
- **入场**：满足回调条件后突破前一根K线。
- **多空**：双向。
- **退出**：固定盈亏比或交易窗口结束。
- **止损**：固定点差。
- **默认值**：
  - PrevRangeStart = 06:00
  - PrevRangeEnd = 10:00
  - TakeStart = 10:00
  - TakeEnd = 11:15
  - TradeStart = 10:00
  - TradeEnd = 16:00
  - Risk = 25
  - Reward = 75
