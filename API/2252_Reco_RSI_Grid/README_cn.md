# Reco RSI 网格策略

## 概述
该策略使用 StockSharp 的高级 API 重现 MetaTrader 平台上的 "Reco" 智能交易系统。算法首先根据相对强弱指数（RSI）开仓，然后在价格朝不利方向移动时逐步建立反向仓位形成网格。网格订单的距离和手数按几何级数增长，当累积盈亏达到预设阈值时一次性平仓。

## 交易逻辑
- **初始信号**：当 RSI 超过设定的超买或超卖区间时触发。RSI 高于卖出区则开空，低于买入区则开多。
- **网格扩展**：首单后监控价格相对于最后一笔交易的移动，当价格偏离达到计算出的距离时发送反向市价单。每一步的距离按 *Distance Multiplier* 递增，可由 *Max Distance* 和 *Min Distance* 限制。
- **手数放大**：每个新订单的数量等于初始 *Lot* 乘以 *Lot Multiplier* 的阶乘，允许设置最大和最小手数。
- **退出规则**：启用 *Use Close Profit* 时，当累计利润超过 *Profit First Order* 并按 *Profit Multiplier* 递增的目标值时全部平仓。启用 *Use Close Lose* 时，对亏损使用 *Lose First Order* 和 *Lose Multiplier* 进行同样的判断。

## 参数
| 名称 | 说明 |
|------|------|
| `RsiPeriod` | RSI 指标周期。 |
| `RsiSellZone` | 触发卖出信号的 RSI 水平。 |
| `RsiBuyZone` | 触发买入信号的 RSI 水平。 |
| `StartDistance` | 与上一笔订单的初始距离（点）。 |
| `DistanceMultiplier` | 每增加一单距离的倍数。 |
| `MaxDistance` | 距离增长的上限，0 表示不限制。 |
| `MinDistance` | 距离增长的下限，0 表示不限制。 |
| `MaxOrders` | 同时打开的最大订单数，0 表示无限制。 |
| `Lot` | 初始下单手数。 |
| `LotMultiplier` | 手数放大的倍数。 |
| `MaxLot` | 每单的最大手数，0 表示不限制。 |
| `MinLot` | 每单的最小手数，0 表示不限制。 |
| `UseCloseProfit` | 是否按利润目标平仓。 |
| `ProfitFirstOrder` | 首单利润目标。 |
| `ProfitMultiplier` | 后续订单的利润倍数。 |
| `UseCloseLose` | 是否按亏损阈值平仓。 |
| `LoseFirstOrder` | 首单亏损阈值。 |
| `LoseMultiplier` | 后续订单的亏损倍数。 |
| `PointMultiplier` | 将品种最小报价单位转换为“点”的倍数。 |
| `CandleType` | 用于计算指标的蜡烛类型。 |

## 说明
- 策略使用市价单，假设能够立即成交。
- 采用净持仓模式，反向下单可能减少或反转当前仓位。
- 代码使用制表符缩进，并包含英文注释以符合项目规范。
