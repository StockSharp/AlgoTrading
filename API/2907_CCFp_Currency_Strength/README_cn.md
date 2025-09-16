# CCFp 货币强弱策略

## 概述
该策略把经典的 MetaTrader CCFp 专家顾问迁移到 StockSharp 的高级 API。它使用七个与美元相关的主要货币对（EURUSD、GBPUSD、AUDUSD、NZDUSD、USDCAD、USDCHF、USDJPY）的快慢简单移动平均线之比，计算八种主要货币（USD、EUR、GBP、CHF、JPY、AUD、CAD、NZD）的相对强弱。当两种货币的强弱差值向上突破设定阈值时，策略会建立多空组合，买入更强的货币、卖出更弱的货币。

实现遵循高层框架的最佳实践：每个品种都有单独的蜡烛订阅，指标通过 `Bind` 连接，订单通过 `RegisterOrder` 以市价形式提交。成交评论沿用原策略的 `(TOPDOWN)` 格式，方便对比历史记录。

## 必需的交易品种
启动前请在参数中指定以下证券：

- `EURUSD`
- `GBPUSD`
- `AUDUSD`
- `NZDUSD`
- `USDCAD`
- `USDCHF`
- `USDJPY`

所有品种应使用同一时间框架，并通过参数 `Candle Type` 统一设置。

## 参数说明
| 参数 | 含义 |
| --- | --- |
| `Fast MA` | 快速移动平均线周期。 |
| `Slow MA` | 慢速移动平均线周期。 |
| `Strength Step` | 触发新信号所需的最小强弱差值。 |
| `Close Opposite` | 若为 true，先平掉反向仓位，再开新仓。 |
| `Candle Type` | 指标使用的蜡烛类型与时间框架。 |
| 基础 `Volume` | 使用 `Strategy.Volume` 作为每笔市价单的数量。 |

## 交易流程
1. 对七个美元货币对分别订阅蜡烛并计算一组快慢简单移动平均线。
2. 每当收到完成的蜡烛，就把快慢均线的比例转换为与原 CCFp 指标相同的货币强弱值。
3. 当所有货币对更新完毕后，重新计算八种货币的强弱分数。
4. 如果“强势”货币与“弱势”货币之间的差值向上突破 `Strength Step`，且强势货币继续走强、弱势货币继续走弱，则视为有效信号。
5. 策略根据信号提交市价单：
   - 若 USD 是强势货币，仅在对应货币对上做反向交易（例如做空 `EURUSD`）。
   - 若 USD 是弱势货币，则买入以强势货币为基准的货币对（例如做多 `EURUSD`）。
   - 若两种货币都不是 USD，则同时做多强势货币对 USD、做空弱势货币对 USD。
6. 如果 `Close Opposite` 启用且目标品种存在反向持仓，会先发送市价单平仓，然后再建仓。

## 风险控制
- 策略不会自动设置止损或止盈；风险管理依赖 `Close Opposite` 选项和外部的资金管理工具。
- 建仓数量由 `Volume` 属性控制，请根据账户规模和承受能力自行设定。

## 与原版 MQL 策略的差异
- 货币强弱计算使用单一时间框架上的 `SimpleMovingAverage` 指标。通过调整 `Fast MA` 与 `Slow MA` 可以模拟原指标的多时间框架叠加效果。
- 没有实现自动拖动止损，重点还原的是入场/出场逻辑，高级风控留给 StockSharp 投资组合层处理。
- 订单通过 `RegisterOrder` 提交，使用 StockSharp 的 `Security` 对象，而不是 MetaTrader 的交易类。
