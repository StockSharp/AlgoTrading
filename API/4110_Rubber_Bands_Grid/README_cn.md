# Rubber Bands Grid 策略

## 概述
- 将 MetaTrader 4 专家顾问 **RUBBERBANDS_2.mq4** 转换到 StockSharp 高级 API。
- 使用最优买卖价构建对称网格，不依赖蜡烛或指标。
- 长短头寸分别记录，从而复制原始 EA 的对冲持仓方式。
- 支持会话级收益/亏损控制以及与原参数一致的暂停和停止开关。

## 交易逻辑
1. 订阅 `SubscribeLevel1()`，对每一次最优 bid/ask 的变化做出响应。
2. `_upperExtreme` 与 `_lowerExtreme` 保存自上次重置以来的最高与最低 Ask。若启用 `UseInitialValues`，则在启动时读取提供的极值，否则以首个 Ask 初始化。
3. 当没有持仓且服务器时间进入新分钟（秒数为 0）时，同时发送一笔买入市价单和卖出市价单，模拟 MT4 中“每分钟入场一次”的触发器。
4. Ask 比记录的高点高出 `GridStepPoints` 个点时发送新的卖单；Ask 比低点低出同样的点数时发送新的买单。触发后极值更新为当前 Ask，使得网格随价格延伸。
5. `MaxTrades` 限制多空两侧的未平仓订单总数。
6. 浮动盈亏基于当前 bid/ask 计算：多头使用 Bid 减去平均买入价，空头使用平均卖出价减去 Ask。`PriceToMoney` 在存在 `PriceStep`/`StepPrice` 时自动换算为账户货币。
7. 当浮盈达到 `SessionTakeProfitPerLot * OrderVolume` 且开启 `UseSessionTakeProfit` 时，策略平掉全部仓位；当浮亏跌破 `-SessionStopLossPerLot * OrderVolume` 且开启 `UseSessionStopLoss` 时同样执行全平。
8. `CloseNow` 在启动时立即清仓，`QuiesceMode` 在空仓状态下保持静默，`StopNow` 禁止新的入场但不会干预已有持仓。

## 参数说明
| 参数 | 说明 |
|------|------|
| `OrderVolume` | 每次市场委托的交易量（对应 MT4 的 `Lots`）。 |
| `MaxTrades` | 多空总持仓数量上限（对应 `maxcount`）。 |
| `GridStepPoints` | 网格间距，单位为价格点（对应 `pipstep`）。 |
| `QuiesceMode` | 空仓时保持静默（`quiescenow`）。 |
| `TriggerImmediateEntries` | 启动后立即同时买入和卖出（`donow`）。 |
| `StopNow` | 暂停新的交易信号（`stopnow`）。 |
| `CloseNow` | 启动即平仓（`closenow`）。 |
| `UseSessionTakeProfit`、`SessionTakeProfitPerLot` | 会话级浮盈目标，按每标准手计算。 |
| `UseSessionStopLoss`、`SessionStopLossPerLot` | 会话级浮亏阈值，按每标准手计算。 |
| `UseInitialValues`、`InitialMax`、`InitialMin` | 重启时恢复上一次的极值（`useinvalues`、`inmax`、`inmin`）。 |

## 实现细节
- 按仓位方向分别维护体量与均价，所有字段使用制表符缩进以符合仓库要求。
- 通过 `_activeBuyOrder` 与 `_activeSellOrder` 防止重复发送市场委托。
- 在 `OnOwnTradeReceived` 中更新多空平均价并计算浮动盈亏，为风险阈值提供实时数据。
- `TryCloseAll()` 模拟 MT4 的 `close1by1()`：连续提交反向单直到多空均归零，然后将极值重置为最新 Ask。
- 仅使用高级 API：Level1 订阅与 `BuyMarket`/`SellMarket`，未直接访问指标或底层集合。
