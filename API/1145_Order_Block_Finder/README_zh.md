# Order Block Finder 策略

该策略基于指定数量的连续K线和最小百分比变化识别多头和空头订单块。当检测到多头订单块时买入，检测到空头订单块时卖出。

## 参数
- **Relevant Periods** – 用于确认订单块的连续K线数量
- **Min Percent Move** – 订单块与最后确认K线之间的最小百分比变化
- **Use Whole Range** – 使用High/Low范围代替以Open为基准的边界
- **Candle Type** – 计算时使用的K线类型
