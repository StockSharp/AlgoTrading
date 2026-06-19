# 交易面板策略 (ID 3468)

## 概述
**TradingPanelStrategy** 源自 MQL5 专家顾问 *EA_TradingPanel*，是一款用于手动下单的辅助策略。它提供 `PlaceBuyOrders()` 与 `PlaceSellOrders()` 两个方法，对应 MT5 面板上的 BUY/SELL 按钮：一次调用即可批量发送多笔市价单，并按设定的 pips 距离自动附加止损和止盈，还可以选择自定义交易标的。默认参数与原始 EA 完全一致（1 笔订单、2 个点止损、10 个点止盈、0.01 手）。

在 StockSharp 中该策略不再自带 UI，而是方便开发者在自定义界面或脚本中调用上述方法。策略内部负责体积归一化、价格按最小跳动对齐以及保护单的生成，最大限度复刻原面板的行为。

## 参数
| 名称 | 说明 | 备注 |
| ---- | ---- | ---- |
| `TradeCount` | 每次动作提交的市价单数量。 | 不小于 0，默认 `1`。 |
| `StopLossPips` | 止损距离（pips）。 | `0` 表示不下止损，默认 `2`。 |
| `TakeProfitPips` | 止盈距离（pips）。 | `0` 表示不下止盈，默认 `10`。 |
| `VolumePerTrade` | 单笔市价单的下单量。 | 会根据 `VolumeStep` 归一化，默认 `0.01`。 |
| `TargetSecurity` | 可选的目标合约。 | 留空时使用 `Strategy.Security`。 |

全部参数都通过 `StrategyParam<T>` 定义，支持优化器和界面实时调整。

## 执行流程
1. 确定实际交易的合约（`TargetSecurity` 优先，否则 `Strategy.Security`）。
2. 根据合约元数据计算 pip 大小：若小数位数 ≥ 3，则按照 MQL 的规则使用 `PriceStep × 10`，否则直接采用 `PriceStep`。
3. 读取最新价格（优先最佳买/卖价，退化到最近成交价），并使用 `Security.ShrinkPrice` 对齐到交易所允许的价位。
4. 计算预期下单量 `TradeCount × VolumePerTrade`，结合 `MinVolume`、`MaxVolume`、`VolumeStep` 做约束，同时如果当前持仓方向相反则追加对应的平仓量，实现“一键反手”。
5. 调用 `BuyMarket` 或 `SellMarket` 发送市价单。
6. 按设定的 pips 距离生成止损（Stop）和止盈（Limit）订单，并进行价格归一化。
7. 当方向切换或策略停止时，自动撤销旧的保护单。

## 保护单规则
- 多头下单：使用 `SellStop` 作为止损，`SellLimit` 作为止盈。
- 空头下单：使用 `BuyStop` 作为止损，`BuyLimit` 作为止盈。
- 保护单的数量与本次面板请求的下单量一致。
- 在 `OnStopped`、`OnReseted` 以及换向时都会清理无效的保护单引用。

## 使用建议
- 调用面板方法之前务必设置 `Strategy.Security` 或 `TargetSecurity`，否则不会发送任何订单。
- `PlaceBuyOrders()` 与 `PlaceSellOrders()` 需要由外部界面或脚本触发，用于替代 MT5 面板按钮。
- 如果当前没有盘口或成交价数据，策略会记录错误并跳过下单。
- `OnStarted` 中调用了 `StartProtection()`，可以在重启后自动处理遗留持仓。
- 当合约缺少 `PriceStep` 信息时，pip 大小默认为 `0.0001`。如需其他精度，请手动补充合约元数据。

## 与 MQL 面板的区别
- 不再提供图形界面，需自行集成到应用程序中。
- 止损/止盈按照一次面板操作的总量下单，而不是为每张 MT5 订单单独附加，最终净仓位效果与原策略一致。
- 额外引入了基于 `VolumeStep`、`MinVolume`、`MaxVolume`、`Security.ShrinkPrice` 的规范检查，避免因交易所限制导致的拒单。
- 通过 `LogInfo` 和 `LogError` 输出日志，便于在 StockSharp 终端中监控。

## 快速上手
1. 创建策略实例，设置投资组合并指定交易合约（或填入 `TargetSecurity`）。
2. 启动策略，以便 `StartProtection()` 启用持仓保护。
3. 在自定义界面中根据用户操作调用 `PlaceBuyOrders()` 或 `PlaceSellOrders()`。
4. 关注日志输出，可根据需要实现更多的前端提示或校验。

该策略为 MT5 交易面板在 StockSharp 框架中的高保真移植，既保持了原有的手动下单体验，又充分利用了 StockSharp 的高层 API 能力。
