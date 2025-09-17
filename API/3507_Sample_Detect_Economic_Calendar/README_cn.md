# 经济日历侦测策略

## 概述
**经济日历侦测策略** 来源于 MetaTrader 专家顾问 `SampleDetectEconomicCalendar.mq5`。该策略监控手动提供的经济事件列表，当指定货币即将公布高重要度数据时，会在当前买卖价附近同时布置买入/卖出止损单。止损、止盈与移动止损逻辑全部继承自原始 EA。

由于 StockSharp 无法直接访问 MetaTrader 的日历服务，所有事件必须通过 `CalendarDefinition` 参数手动配置。

## 工作流程
1. 策略订阅 Level1 行情以跟踪买价与卖价。
2. 启动时解析 `CalendarDefinition` 中的每一行事件。
3. 对于符合 `BaseCurrency` 且重要度为 High 的事件：
   - 在距公布时间 `LeadMinutes` 分钟时开始准备。
   - 根据固定手数或风险控制计算下单量。
   - 分别在 `BuyDistancePoints` 与 `SellDistancePoints` 点距位置挂出买入/卖出止损单。
4. 数据公布后，若订单未触发，策略会在 `PostMinutes` 分钟后或 `ExpiryMinutes` 超时时间到达时撤单。
5. 其中一侧成交后，另一侧订单会立即取消，并按设定的止损、止盈及移动止损管理持仓。

## 参数说明
| 参数 | 说明 |
|------|------|
| `TradeNews` | 是否在新闻公布前启用挂单逻辑。 |
| `OrderVolume` | 当不启用资金管理时使用的固定手数。 |
| `StopLossPoints` | 止损距离（点）。为 0 表示关闭。 |
| `TakeProfitPoints` | 止盈距离（点）。为 0 表示关闭。 |
| `TrailingStopPoints` | 移动止损距离（点）。为 0 表示关闭移动止损。 |
| `ExpiryMinutes` | 公布后保留挂单的最长时间（分钟）。 |
| `UseMoneyManagement` | 是否启用按账户风险计算手数。 |
| `RiskPercent` | 触发资金管理时单笔风险占账户净值的百分比。 |
| `BuyDistancePoints` | 买入止损相对于卖价的距离（点）。 |
| `SellDistancePoints` | 卖出止损相对于买价的距离（点）。 |
| `LeadMinutes` | 公布前提前挂单的分钟数。 |
| `PostMinutes` | 公布后等待撤单的分钟数。 |
| `BaseCurrency` | 需要匹配的货币代码，默认 `USD`。 |
| `CalendarDefinition` | 多行文本，每行描述一个经济事件。 |

## 事件格式
每行事件使用以下格式：

```
yyyy-MM-dd HH:mm;CUR;High;事件名称
```

* `yyyy-MM-dd HH:mm` —— UTC 时间戳，允许带秒，亦支持 `yyyy/MM/dd`、`dd.MM.yyyy` 等格式。
* `CUR` —— 货币代码（如 `USD`）。只有匹配 `BaseCurrency` 的事件会触发挂单。
* `High` —— 重要度关键字，可选 `High`、`Medium`、`Low`、`Nfp`，其中仅 `High` 会实际下单。
* `事件名称` —— 用于日志记录的说明文字。

示例：

```
2024-06-12 18:00;USD;High;FOMC Statement
2024-07-05 12:30;USD;Nfp;Non-Farm Payrolls
```

## 风险管理
* 当 `UseMoneyManagement` 关闭时，策略始终使用 `OrderVolume` 指定的固定手数。
* 当 `UseMoneyManagement` 打开时，策略以账户净值的 `RiskPercent`% 作为最大亏损，结合 `StopLossPoints` 计算下单量，并自动遵守交易品种的最小/最大手数及步长限制。
* 当价格向有利方向移动 `TrailingStopPoints` 点后，移动止损开始保护盈利，止损/止盈条件任意满足都会立即平仓。

## 与原始 EA 的差异
* 必须手动在 `CalendarDefinition` 中输入经济事件。
* 每个策略实例仅处理一个货币对。
* 因 StockSharp 不支持 MetaTrader 的 `ORDER_TIME_SPECIFIED` 到期设置，挂单失效由 `PostMinutes` 与 `ExpiryMinutes` 定时器控制。

## 使用建议
1. 在启动前填写 `CalendarDefinition`，并确认事件时间为 UTC。
2. 打开 `TradeNews`，根据需要调整距离及风险参数。
3. 确保连接提供 Level1 买卖价数据，以便在窗口开始时能够即时挂单。
4. 通过日志核对每次事件的下单与撤单是否符合预期。
