# Get trend 随机指标策略

本策略是 MetaTrader 4 专家顾问 **Get trend.mq4** 的 StockSharp 高级 API 版本。它以 M15 周期作为主要信号源，并使用 H1
周期确认大方向。两个平滑移动平均线（SMMA）配合双随机指标用于寻找靠近慢速均线的反转突破机会。风险控制沿用原版
思路：止盈、止损与拖尾止损均以点数形式固定设置。

## 交易逻辑

1. **数据与指标**
   - M15 蜡烛用于计算基于中位价的 SMMA（周期 `M15MaPeriod`）以及两个随机指标（快、慢）。
   - H1 蜡烛驱动另一条基于中位价的 SMMA，周期为 `H1MaPeriod`。
   - 快速随机指标（`FastStochasticPeriod`, 3, 3）提供 %K 线及其前一根值，慢速随机指标（`SlowStochasticPeriod`, 3, 3）提
     供 %D 线。
2. **做多条件**
   - 当前 M15 收盘价低于其 SMMA，同时 H1 收盘价也低于对应 SMMA。
   - M15 SMMA 与收盘价之间的距离不超过 `ThresholdPoints` 个价格步长。
   - 两条随机线均低于 20，且 %K 自下而上穿越 %D（当前 %K > %D，上一根 %K < %D）。
   - 若持有空头仓位，策略会先买入以平仓，再按 `TradeVolume` 建立新的多头仓位。
3. **做空条件** 与做多逻辑对称：
   - 两个时间框架的收盘价都高于 SMMA，距离限制满足 `ThresholdPoints`，随机指标高于 80，且 %K 自上而下穿越 %D。若
     已有多头仓位，会先平仓再开空。
4. **风险管理**
   - 每次进场后，根据合约的 `PriceStep` 将 `StopLossPoints` 与 `TakeProfitPoints` 转换为绝对价格距离并下达保护性订单。
   - 当浮动利润达到 `TrailingStopPoints` 时，拖尾止损会重新放置对应方向的止损订单：多头使用当前收盘价减去拖尾距
     离，空头则加上拖尾距离。
   - 持仓归零时，所有保护性订单都会被撤销。

## 与原版 EA 的差异

- MT4 中的 SMMA 采用向前偏移 8 根的设置。StockSharp 指标没有公开的偏移属性，因此移植版直接使用最新完成蜡烛的数
  值，从而保持信号节奏并避免额外缓存。
- 原策略使用 Bid/Ask 价格计算拖尾。移植版改为使用触发拖尾更新的已完成蜡烛收盘价，这是高级 API 中最接近的替代。
- 订单管理通过 StockSharp 提供的 `BuyMarket`、`SellMarket`、`SellStop` 等方法实现，而非 `OrderSend`/`OrderModify`。

## 参数

| 分组 | 名称 | 说明 | 默认值 |
|------|------|------|--------|
| Data | `M15 Candle Type` | 主信号所用的蜡烛类型/周期。 | M15 周期 |
| Data | `H1 Candle Type` | 趋势确认使用的蜡烛类型/周期。 | H1 周期 |
| Indicators | `M15 SMMA Period` | M15 序列上的 SMMA 周期。 | 200 |
| Indicators | `H1 SMMA Period` | H1 序列上的 SMMA 周期。 | 200 |
| Indicators | `Slow Stochastic Period` | 慢速随机指标（输出 %D）的 %K 周期。 | 14 |
| Indicators | `Fast Stochastic Period` | 快速随机指标（输出 %K）的 %K 周期。 | 14 |
| Signals | `Threshold (points)` | 允许进场时 M15 SMMA 与收盘价之间的最大点数差。 | 50 |
| Risk | `Take Profit (points)` | 以点数表示的止盈距离。 | 570 |
| Risk | `Stop Loss (points)` | 以点数表示的止损距离。 | 30 |
| Risk | `Trailing Stop (points)` | 以点数表示的拖尾止损距离。 | 200 |
| Trading | `Trade Volume` | 每次市价单的下单数量。 | 0.1 |

## 使用说明

- 请确保交易品种提供 `PriceStep`。若未定义，点数距离会退化为绝对值 1，可能导致保护性订单过宽。
- 拖尾机制通过撤销并重新发送止损订单完成，若券商限制频繁改价，可自行增加节流逻辑。
- 策略仅在蜡烛收盘后作出决策。实时运行时需要保证 StockSharp 与外部终端的蜡烛构建方式一致。
