# Marneni Money Tree 策略

该策略将 MQL 专家顾问 "Marneni Money Tree" 移植到 StockSharp。
它使用 40 周期简单移动平均线（SMA）及其两个偏移值来判断趋势方向。
当向前偏移四根柱的 SMA 位于当前值与向前偏移三十根柱的值之间时：
- 发送一笔按趋势方向的市价单；
- 同时按照 `Order2Pips` 至 `Order9Pips` 指定的点差依次挂出八笔限价单。

做多时在当前价格下方挂买单，做空时在当前价格上方挂卖单。
当上述 SMA 关系反转时，平仓并取消所有剩余挂单。

## 参数
- `Order2Pips`–`Order9Pips`：第 2 到第 9 笔限价单距市价的点差。
- `CandleType`：用于计算的 K 线周期。

基础下单量固定为 2，可在运行前修改 `Volume` 属性来调整。
