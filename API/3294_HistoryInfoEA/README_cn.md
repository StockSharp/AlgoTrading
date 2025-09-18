# HistoryInfoEaStrategy

## 概述
**HistoryInfoEaStrategy** 将 MT4 的 “HistoryInfo” 工具移植到 StockSharp。策略不会在图表上绘制文本，而是监听 `OnNewMyTrade` 事件，对满足筛选条件的成交进行统计。聚合结果通过 `LastSnapshot` 属性提供，同时写入日志，方便界面或外部服务读取。

策略本身不会发送订单。它适用于与其他自动化策略或人工交易共同运行，只负责分析匹配条件的成交。

## 参数
| 参数 | 说明 |
|------|------|
| `FilterType` | 交易筛选模式：`CountByUserOrderId`、`CountByComment`、`CountBySecurity`。|
| `MagicNumber` | 期望的 `Order.UserOrderId`（仅在 `CountByUserOrderId` 模式下使用）。留空表示不匹配任何成交。|
| `OrderComment` | 订单注释前缀（`Order.Comment`）。仅在 `CountByComment` 模式下使用。默认值 `"OrdersComment"` 延续 MT4 的占位符，通常需要替换。|
| `SecurityId` | 证券标识（`Security.Id`）。仅在 `CountBySecurity` 模式下有效。默认值 `"OrdersSymbol"` 也是占位符。|

## 聚合指标
每当找到一笔符合条件的成交，`LastSnapshot` 都会刷新，包含以下字段：

- `FirstTrade` / `LastTrade`：最早和最新成交的时间戳。
- `TotalVolume`：累积成交量，单位与交易工具相同（手、合约等）。
- `TotalProfit`：`MyTrade.PnL` 减去手续费后的已实现盈亏。
- `TotalPips`：根据 `Security.PriceStep` 与 `Security.StepPrice` 计算的点数收益，并考虑 MT4 常见的 5/3 位小数放大规则。
- `TradeCount`：通过筛选的成交数量。

日志中会打印一行与 MT4 `Comment()` 输出类似的摘要，方便快速查看。

## 使用方法
1. 将策略连接到与其它策略相同的投资组合和证券。
2. 选择 `FilterType`，并填写相应参数（魔术号、注释前缀或证券标识）。
3. 启动策略，满足条件的第一笔成交出现后，`LastSnapshot` 会立即提供统计结果，同时日志也会更新。
4. 重启或手动重置策略时，所有计数会自动清零。

> **提示：** 点数统计依赖正确的合约元数据。请确保 `Security.PriceStep` 与 `Security.StepPrice` 已配置；若缺失其中任意一个，`TotalPips` 会保持为零，但盈亏金额仍然会累加。

## 移植注意事项
- MT4 脚本在 `OnTick` 中循环遍历 `OrdersHistoryTotal()`。本策略改为响应式实现，只在收到新的 `MyTrade` 时更新数据，无需轮询。
- MT4 通过 `OrderProfit + OrderCommission + OrderSwap` 计算收益。StockSharp 提供 `MyTrade.PnL` 与 `MyTrade.Commission` 字段（多数交易所已在 `PnL` 中包含掉期）。为保持一致，策略会从 `PnL` 中减去手续费。
- 为了贴近原作的默认体验，保留了 `"OrdersComment"`、`"OrdersSymbol"` 等占位符。实际使用前请替换成有效值。
- 原本的图表文字输出被结构化数据和日志取代，界面层可以按需展示。
- 策略不会创建新的订单，因此可以在“观察”模式下分析第三方策略的成交流。

## 扩展建议
- 订阅 `LastSnapshot`，将统计数据推送到监控面板或告警系统。
- 根据接入方提供的元数据，增加更多筛选条件（例如按投资组合或自定义标签）。
- 定期导出快照到 CSV/JSON，以生成历史报表。
