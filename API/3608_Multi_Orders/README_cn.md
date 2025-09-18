# Multi Orders 策略

**Multi Orders Strategy** 是 MetaTrader 5 专家顾问 `multi.mq5` 的 C# 复刻版本。原始脚本在图表上提供两个按钮，用于一次性提交多笔市价单，同时还有一套简单的自动逻辑在点差缩小时触发交易。本移植版利用 StockSharp 的高级 Level1 订阅与持仓保护机制，完整还原这些特性。

## 交易逻辑
- 通过 `SubscribeLevel1()` 订阅最优买卖报价，并缓存最新的 bid/ask。
- 当点差小于 `SlippagePoints`（换算为价格步长）时，策略比较中间价与当前的 ask/bid：
  - 若中间价高于 ask，则发送买入市价单；
  - 若中间价低于 bid，则发送卖出市价单。
- 批量下单与 MetaTrader 上的按钮一致。通过界面、测试或自动化调用 `TriggerBuyBatch()` / `TriggerSellBatch()`，即可在下一次报价更新时依次提交多笔市价单。
- 使用 `StartProtection` 配置的步长距离自动管理止损与止盈。

## 仓位管理
- `RiskPercentage` 表示单笔交易愿意承担的账户权益占比。优先读取 `Portfolio.CurrentValue`，若无则回退到 `CurrentBalance` 与 `BeginValue`。
- 止损距离来自 `StopLossPoints` 与品种的 `StepPrice`。若无法获得这些数据，将直接使用参数 `BaseVolume` 作为下单量。
- 订单数量按照 `VolumeStep` 对齐，并强制满足 `MinVolume` / `MaxVolume` 限制（若交易所提供）。

## 使用提示
- 策略面向净持仓账户。多次同向下单会累加当前净头寸，而不是生成多张独立合约。
- 在策略尚未启动、连接或未获交易许可 (`IsFormedAndOnlineAndAllowTrading`) 之前，批量触发会被忽略。
- 请根据标的的最小跳动值调整 `SlippagePoints`。数值过低会阻止自动进场。

## 参数一览
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `BuyOrdersCount` | 触发批量买入时提交的市价单数量。 | `5` |
| `SellOrdersCount` | 触发批量卖出时提交的市价单数量。 | `5` |
| `RiskPercentage` | 按账户权益计算下单量的百分比，`0` 表示禁用。 | `1` |
| `StopLossPoints` | 止损距离（价格步长），同时用于风险测算。 | `200` |
| `TakeProfitPoints` | 止盈距离（价格步长）。 | `400` |
| `SlippagePoints` | 自动交易允许的最大点差（价格步长）。 | `3` |
| `BaseVolume` | 风险测算不可用时的备用下单量。 | `1` |

