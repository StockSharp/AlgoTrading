# PROphet 策略

## 概述
该策略是 MetaTrader 4 专家顾问“PROphet”的 StockSharp 版本。原始 EA 通过四根历史 K 线的权重区间来生成信号，仅在欧盘和美盘时段持仓，并在价格朝有利方向移动时不断上移（或下移）止损。移植版本完整保留这些逻辑，并针对 StockSharp 投资组合的净额模式进行了调整。

## 交易逻辑
- 订阅所设置的时间框架（`CandleType`，默认 M5），仅处理已完成的 K 线。
- 持有最近三根完成的 K 线，以复现 MQL 中 `High[i]` 与 `Low[i]` 的索引方式。
- 在每根 K 线结束时计算多头触发值 `Qu(X1, X2, X3, X4)` 与空头触发值 `Qu(Y1, Y2, Y3, Y4)`。每一项都将加权区间（例如 `|High[1] - Low[2]|`）与相应权重减一百的值相乘，完全对应原始代码。
- 仅当当前小时介于 `TradeStartHour` 与 `TradeEndHour`（含两端）之间时才允许开仓，默认窗口为 10:00 至 18:00。
- 下单时使用单笔市价委托，订单数量会先抵消反向仓位，再建立新的方向，与原策略通过 Magic Number 限定持仓的方式一致。

## 风险控制与移动止损
- 策略将 MetaTrader 的“点”转换为价格单位，转换使用标的物的 `PriceStep`。默认参数（`BuyStopLossPoints = 68`，`SellStopLossPoints = 72`）与 MQL 外部变量相同。
- 当买价（多头）或卖价（空头）超过当前止损 `spread + 2 * stopDistance` 时，系统会把止损推进到 `currentPrice ± stopDistance`。若存在 Level-1 行情，则使用实时价差；否则退化为一个最小价格步长。
- 超过 `ExitHour` 后，系统会强制平掉所有持仓。默认值 18 复现了原 EA 在 18:00 后离场的行为。
- 出场通过市价单完成，因为 StockSharp 的高级 API 不会自动生成止损单。这样可以在不同经纪商上保持确定性。

## 参数
| 参数 | 说明 |
|------|------|
| `AllowBuy` | 是否允许做多。 |
| `AllowSell` | 是否允许做空。 |
| `X1`、`X2`、`X3`、`X4` | 应用于多头 `Qu` 公式中各区间的权重。 |
| `BuyStopLossPoints` | 多头止损距离（以 MetaTrader 点为单位）。 |
| `Y1`、`Y2`、`Y3`、`Y4` | 应用于空头 `Qu` 公式中各区间的权重。 |
| `SellStopLossPoints` | 空头止损距离（以 MetaTrader 点为单位）。 |
| `TradeVolume` | 基础下单量（手数），策略会自动加量以平掉反向仓位。 |
| `TradeStartHour` | 交易窗口开始小时（含）。 |
| `TradeEndHour` | 交易窗口结束小时（含）。 |
| `ExitHour` | 超过该小时后强制离场。 |
| `CandleType` | 用于分析的 K 线时间框架。 |

## 备注
- StockSharp 默认采用净额持仓。策略在生成新信号时，会先增加足够的仓位以平掉反向持仓，再按基础手数建立新仓，从而逼近原 EA “每个方向一单”的逻辑。
- 原始 MQL 脚本通过 `MarketInfo` 获取点差。移植版本优先使用 Level-1 行情中的买卖价差，没有实时数据时退化为单个最小价位变动。
- 因为移动止损在每根完成的 K 线上更新，相比原 EA 基于逐笔报价的调整，可能会产生一定的滑点。
