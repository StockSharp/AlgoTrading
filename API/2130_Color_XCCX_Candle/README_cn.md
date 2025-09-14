# Color XCCX Candle 策略

基于 MQL 代码 `MQL/14260` 转换。

该策略比较两条基于蜡烛开盘价和收盘价计算的简单移动平均线（SMA）。当收盘价 SMA 上穿开盘价 SMA 时，策略开多单；当收盘价 SMA 下穿开盘价 SMA 时，策略开空单。在开新仓之前，会平掉相反方向的持仓。

参数:

- `SMA Length` – 计算两条 SMA 所用的蜡烛数量。
- `Candle Type` – 处理蜡烛的时间框架。
- `Stop Loss %` – 按入场价百分比设置的止损。
- `Take Profit %` – 按入场价百分比设置的止盈。

策略使用 StockSharp 的高级 API 订阅蜡烛并绑定指标。如有图表环境，会绘制两条 SMA 以及成交记录。
