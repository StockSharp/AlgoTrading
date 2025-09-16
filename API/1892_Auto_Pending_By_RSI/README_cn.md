# 基于RSI的自动挂单策略

当相对强弱指标（RSI）在超买或超卖区域连续保持指定数量的K线时，本策略会自动放置限价挂单。

当RSI连续 `MatchCount` 根K线低于超卖水平时，在收盘价下方 `PendingOffset` 个价格点处挂出买入限价单；当RSI连续位于超买水平以上时，在收盘价上方同样距离处挂出卖出限价单。

## 参数
- `RsiPeriod` – RSI的计算周期。
- `RsiOverbought` – 判断超买区域的水平。
- `RsiOversold` – 判断超卖区域的水平。
- `PendingOffset` – 挂单距离收盘价的偏移量（价格点）。
- `MatchCount` – 触发挂单所需的连续K线数量。
- `CandleType` – 用于分析的K线周期。

默认设置仿照原始MQL脚本并使用4小时K线。
