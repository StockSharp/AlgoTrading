# Training 策略

## 概述
该策略复刻了 MetaTrader 4 中的手动训练专家顾问。原始脚本通过在图表上拖动标签来触发买入或卖出请求。在 StockSharp 中，这些操作改为通过布尔参数完成。一个 250 毫秒的定时器重现了 MQL 的 `Control()` 循环：它轮询参数、发送市价单、平仓并在日志中输出最新的账户信息。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `OrderVolume` | 每次手动操作的交易数量。 | `1` |
| `TakeProfitPoints` | 以价格步长为单位的止盈距离，`0` 表示不挂止盈单。 | `30` |
| `StopLossPoints` | 以价格步长为单位的止损距离，`0` 表示不挂止损单。 | `30` |
| `RequestBuy` | 设为 `true` 时提交市价买单，处理后会自动复位。 | `false` |
| `RequestSell` | 设为 `true` 时提交市价卖单，处理后会自动复位。 | `false` |
| `CloseBuy` | 设为 `true` 时平掉当前多头仓位，即使没有仓位也会复位。 | `false` |
| `CloseSell` | 设为 `true` 时平掉当前空头仓位，即使没有仓位也会复位。 | `false` |

## 交易逻辑
- 以 250 毫秒为周期的定时器驱动全部操作，相当于原脚本中的 `Control()`。
- 当 `RequestBuy` 或 `RequestSell` 被置为 `true` 时，策略会先取消遗留的保护单，再计算合适的手数并通过 `BuyMarket`/`SellMarket` 发送市价单。
- `CloseBuy` 和 `CloseSell` 用于平掉对应方向的仓位，同时撤掉所有保护单。
- 入场成交后，策略根据 `Security.PriceStep` 将点数转换为绝对价格，并为多头挂 `SellStop`/`SellLimit`，为空头挂 `BuyStop`/`BuyLimit`。如果品种未配置价格步长，将跳过保护单并在日志中给出警告。
- 任意保护单成交时，会自动撤销另一张保护单，使仓位回到无管理状态，与原 EA 一致。
- 每 5 秒调用一次 `AddInfoLog` 输出投资组合价值、已实现盈亏以及当前净仓位，替代 MQL 的 `Comment()` 文本。

## 转换说明
- 图表拖动事件无法直接移植，因此使用布尔参数来模拟按钮操作。
- `OrderSend` 被映射到高级 API `BuyMarket`/`SellMarket`，它们自动处理下单逻辑，并在净头寸模式下运行。
- 止盈和止损通过 `SellStop`/`SellLimit` 或 `BuyStop`/`BuyLimit` 重建，对应 MQL 中附带的保护订单。
- 原始的 `Delete_My_Obj` 函数实现为 `CancelProtectionOrders`，在仓位归零时取消所有保护单。

## 使用建议
1. 启动前务必为品种配置 `PriceStep`（如有需要还包括 `StepPrice`），否则无法计算保护价格。
2. 运行中可以在界面上切换布尔参数，或通过代码修改它们，以模拟按钮按下。
3. 在发送请求前调整 `OrderVolume`、`TakeProfitPoints` 和 `StopLossPoints`，定时器会自动应用最新的参数值。
4. 关注平台日志中的定期提示，它们替代了原脚本在图表上的文字输出。
