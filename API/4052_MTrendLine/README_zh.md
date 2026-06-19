# MTrendLine 策略

## 概览
**MTrendLine Strategy** 将 MetaTrader 脚本 `MTrendLine.mq4` 移植到 StockSharp 的高层 API。原始 EA 会不断修改已有的
挂单，使其紧贴交易者在图表上绘制的趋势线。StockSharp 版本通过可配置的 `LinearRegression` 指标重建这一趋势线，
最多可同时维护三个独立的挂单槽，每个槽都可以拥有不同的订单类型、距离和成交量。每当蜡烛收盘时，策略都会
重新计算回归线的数值，生成新的偏移量，并根据结果刷新对应的挂单。

移植版补充了许多现代化特性：参数结构清晰，MetaTrader 的点值自动换算成实际价格步长，可选的止损/止盈距离
会随着挂单一起移动。策略还通过 `SubscribeLevel1()` 监听最新的买一卖一报价，确保新的挂单价格满足经纪商要求的
最小距离。

## 交易逻辑
1. 通过 `SubscribeCandles()` 订阅指定的 K 线序列，并把收盘蜡烛传递给 `LinearRegression` 指标，用以模拟原始脚本
   使用的趋势线。
2. 订阅 Level1 数据，缓存最新的买一与卖一价格；在重新布置挂单之前使用它们检查最小距离参数。
3. 对于每个启用的挂单槽，按照 **回归值 + 距离 × 点值** 计算目标价格。点值默认取自标的的 `PriceStep`，也可以
   手动设置以匹配 MetaTrader 的 `Point`。
4. 将槽位设置转换成 StockSharp 的挂单助手方法（`BuyLimit`、`SellLimit`、`BuyStop`、`SellStop`）。如果启用止损或
   止盈，则根据距离参数计算出相应的绝对价格，使得每次调整挂单时止损/止盈都会一同移动。
5. 如果槽位中已经存在一个激活的挂单且新的目标价格不同，先取消原挂单，并等待下一根蜡烛再重新下单。这种
   “取消+重下”的方式等效于 MQL 的 `OrderModify`，但可以避免重复请求。
6. 当槽位被禁用或计算出的目标价格无效（例如小于零）时，立即撤销相关挂单并清空内部状态。

## 挂单槽
每个槽相当于原脚本中一次 `modify()` 调用，配置互不影响：
- **类型**：可选择 Buy Limit、Buy Stop、Sell Limit 或 Sell Stop。
- **距离**：以 MetaTrader 点值表示，与回归值相加得到挂单价格。输入负值即可把挂单放在趋势线下方。
- **手数**：挂单的下单量。如果设置为 0 或负数，将自动回退到全局 `TradeVolume`。
- **启用开关**：便于暂时停用某个槽位，被停用的槽会主动撤销自己的挂单。

## 参数
| 名称 | 类型 | 默认值 | 描述 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1 小时 K 线 | 构建回归趋势线所用的主时间框架。 |
| `RegressionLength` | `int` | `24` | 提供给 `LinearRegression` 指标的蜡烛数量。 |
| `PointValue` | `decimal` | `0` | MetaTrader 点值。当为 0 时使用证券的 `PriceStep`。 |
| `TradeVolume` | `decimal` | `1` | 当槽位手数为 0 时使用的默认下单量。 |
| `StopLossPoints` | `decimal` | `0` | 止损距离（点）。为 0 表示不自动放置止损。 |
| `TakeProfitPoints` | `decimal` | `0` | 止盈距离（点）。为 0 表示不自动放置止盈。 |
| `MinDistancePoints` | `decimal` | `0` | 挂单与买一/卖一之间所需的最小距离（点）。 |
| `PendingOrder{1,2,3}Enabled` | `bool` | 视槽位而定 | 是否启用该槽。 |
| `PendingOrder{1,2,3}Mode` | `enum` | 视槽位而定 | 挂单类型：BuyLimit、BuyStop、SellLimit、SellStop。 |
| `PendingOrder{1,2,3}DistancePoints` | `decimal` | 视槽位而定 | 与回归值相加的点数偏移。 |
| `PendingOrder{1,2,3}Volume` | `decimal` | 视槽位而定 | 槽位手数，为 0 时使用 `TradeVolume`。 |

## 与原 MetaTrader 脚本的差异
- MetaTrader 可以直接修改挂单价格；StockSharp 通过撤单后重新下单来实现同样的效果，并在下一根蜡烛上完成替换。
- 原脚本读取手绘趋势线的价格。移植版本改用 `LinearRegression` 指标，以便在无人干预的情况下获得可重复的结果。
- StockSharp 不提供 `MODE_STOPLEVEL`，因此策略加入了 `MinDistancePoints` 参数，并利用实时买卖价进行校验。
- 止损和止盈距离改由参数控制，而不是从现有挂单读取，从而确保每次重新下单后仍然保持相同的偏移量。

## 使用建议
- 如果经纪商的点值定义不同于 `PriceStep`，请设置 `PointValue`，确保距离参数与 MetaTrader 中一致。
- 只启用需要的槽位。每个挂单都会带有 `"MTrendLine slot N"` 的注释，方便在报表和订单日志中识别。
- 如需更多风险控制（例如移动止损、账户级风控），可以结合 StockSharp 内置的保护工具；本策略专注于复制原始
  挂单跟踪逻辑。

## 指标
- `LinearRegression`（只处理收盘蜡烛）。
