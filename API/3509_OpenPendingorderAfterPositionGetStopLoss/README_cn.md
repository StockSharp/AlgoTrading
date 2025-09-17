# OpenPendingorderAfterPositionGetStopLoss

## 概览
**OpenPendingorderAfterPositionGetStopLoss** 策略将同名的 MetaTrader 5 专家顾问移植到 StockSharp 的高级 API。策略在所选时间框架的每根收盘 K 线上计算 Stochastic 指标的 %K 斜率：当 %K 向下倾斜时，在市场价下方放置 sell stop；当 %K 向上倾斜时，在市场价上方放置 buy stop。每当挂单成交，都会立即生成对应的止损和止盈保护单。如果仓位因为止损而关闭，策略会立刻重新挂出相同方向的待触发订单，从而维持原有的突破网格结构。

## 交易逻辑
- 订阅所选时间框架的收盘蜡烛并计算经典的随机指标（`KPeriod`、`DPeriod`、`Slowing`）。
- 将当前 %K 值与两根蜡烛之前的值比较：
  - `%K(current) < %K(two bars ago)` → 在最佳买价下方提交 sell stop。
  - `%K(current) > %K(two bars ago)` → 在最佳卖价上方提交 buy stop。
- 挂单价格在实时点差的基础上再加上 `MinStopDistancePoints` 指定的缓冲，与原始 MQL 策略保持一致。
- 挂单成交后，策略会发送止损 stop 单和可选的止盈 limit 单。
- 当止损触发并关闭仓位时，会立即根据最新行情重新放置相同方向的挂单。
- 当止盈成交或策略停止时，所有保护性订单都会被自动撤销。

## 参数
| 名称 | 说明 |
| --- | --- |
| `OrderVolume` | 每个挂单的交易量（手数）。 |
| `StopLossPoints` | 止损距离（以最小价格变动为单位），0 表示不使用。 |
| `TakeProfitPoints` | 止盈距离（以最小价格变动为单位），0 表示不使用。 |
| `MinStopDistancePoints` | 挂单距离当前价格的最小缓冲（点数），会与点差一起计算。 |
| `MaxPositions` | 每个方向允许的最大持仓数量（净额账户通常为 0 或 1）。 |
| `KPeriod` | 计算 %K 时使用的历史柱数。 |
| `DPeriod` | %D 平滑线的周期。 |
| `Slowing` | 对 %K 施加的额外平滑系数。 |
| `PendingExpiry` | 挂单的有效期，到期后会在下一根蜡烛上取消。 |
| `CandleType` | 用于指标计算的蜡烛类型（时间框架）。 |

## 实现细节
- 订单管理完全依赖 `BuyStop`、`SellStop`、`SellLimit`、`BuyLimit` 等高级方法，满足 `AGENTS.md` 中的要求。
- 指标值直接在 `SubscribeCandles().BindEx(...)` 回调中消费，没有使用任何 `GetValue` 调用。
- 通过 `OnOwnTradeReceived` 事件安装和撤销保护性订单，从而复现原始 EA 中 `OnTradeTransaction` 的逻辑。
- 代码中的注释全部为英文，缩进使用制表符，完全符合仓库的编码规范。
