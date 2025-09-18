# Virtual SL TP V1 策略

**Virtual SL TP V1 策略** 是 MetaTrader 指标脚本 `Virtual_SL_TP_Pending_with_SL_Trailing.mq4`（MQL ID 49146）的 StockSharp 版本。原版脚本通过维护虚拟止损 / 止盈价位来管理已有仓位，并在需要时在价格上升时挂出追踪买入止损单。本 C# 版本完全依赖 StockSharp 的高级接口（`SubscribeLevel1`、`BuyStop`、`ClosePosition`），在功能上与原脚本一致。

## 核心思路

1. **关注点差的风控** – 启动时记录初始点差。如果当前点差超过设定阈值，所有虚拟价位会整体上移，保持与市场的相对距离。
2. **虚拟出场** – 策略不会发送真实的止损或止盈订单，而是监控最优买价，只要价格突破虚拟止损或虚拟止盈，就调用 `ClosePosition()` 平仓。
3. **可选的追踪挂单** – 当 `EnableTrailing` 设为 `true` 且最优卖价触及触发价时，会在虚拟挂单价位提交买入止损单。如果点差调整导致触发价移动，挂单价格会自动刷新。

## 参数

| 名称 | 默认值 | 说明 |
| ---- | ------ | ---- |
| `StopLossPoints` | `20` | 最优卖价与虚拟止损之间的 MetaTrader 点数距离。 |
| `TakeProfitPoints` | `40` | 最优卖价与虚拟止盈之间的 MetaTrader 点数距离。 |
| `SpreadThreshold` | `2.0` | 只有当点差超过 `初始点差 + SpreadThreshold` 时，虚拟价位才会整体上移。 |
| `TrailingStopPoints` | `10` | 最优卖价与挂出买入止损单之间的 MetaTrader 点数距离。 |
| `EnableTrailing` | `false` | 是否启用追踪挂单逻辑。 |

> **MetaTrader 点数** 由 `Security.PriceStep` 自动换算。如果报价缺少最小跳动值，则使用默认值 `1`。

## 执行流程

1. 使用 `SubscribeLevel1()` 订阅最优买卖价。
2. 第一笔行情初始化虚拟止损、虚拟止盈和挂单触发价，同时记录初始点差。
3. 后续每次更新：
   - 重新计算点差，若超过 `initialSpread + SpreadThreshold`，所有虚拟价位整体上移。
   - 检查最优买价是否触及虚拟止损或虚拟止盈，满足条件即调用 `ClosePosition()`。
   - 若启用追踪功能，比较最优卖价与虚拟挂单触发价，触发后发送 `BuyStop` 订单。
4. 当禁用追踪功能或策略停止时，挂出的买入止损单会被自动撤销。

## 文件结构

- `CS/VirtualSlTpV1Strategy.cs` – 策略实现代码。
- `README.md` – 英文说明。
- `README_ru.md` – 俄文说明。
