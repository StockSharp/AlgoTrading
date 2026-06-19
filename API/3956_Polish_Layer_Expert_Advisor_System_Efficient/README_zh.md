# Polish Layer Expert Advisor System Efficient

## 概述
该策略完整移植自 MQL4 指标交易系统 “Polish Layer Expert Advisor System Efficient”。原版建议运行在 5 或 15 分钟周期的图表上，因此这里同样只在单一时间框上运作，并且同一时间仅持有一张仓位。趋势方向由价格的快慢均线以及两条经 SMA 平滑处理的 RSI 均线共同决定；当趋势条件满足后，还需要同时通过随机指标、DeMarker 与 Williams %R 的极值反转信号确认，才会执行进出场。

## 交易逻辑
1. **趋势过滤。** 收盘价的 9 周期简单均线必须位于 45 周期线性加权均线之上才能做多，反之才能做空。与此同时，RSI 值的 9 周期 SMA 也必须高于（做多）或低于（做空）45 周期 SMA。只要任一过滤条件不同步，新的交易就会被禁止。
2. **随机指标触发。** 在满足多头趋势的前提下，%K 需要从下方突破超卖阈值（默认 19）并向上穿越 %D 才能触发买入。空头信号则要求 %K 从上方跌破超买阈值（默认 81）并且跌破 %D。所有平滑参数都继承自原始脚本。
3. **动量确认。** 多头入场还需满足 DeMarker 从下方突破 0.35，且 Williams %R 从下方突破 −81；空头则要求 DeMarker 向下突破 0.63，Williams %R 向下突破 −19。所有阈值判断均基于上一根完整 K 线与当前 K 线之间的变化。
4. **仓位管理。** 仅使用市价单开仓，策略在持仓期间不会加仓或反手。若设置了固定的止损/止盈，则会按照品种的最小报价步长把 “点” 转换为实际价格距离；若无法获得步长，则保护功能会被关闭。

## 风险管理
* **止损 / 止盈。** 参数以点数表示，并通过 `Security.PriceStep` 转换为价格差（默认约定 1 点 = 1 个最小报价步长），在入场后立即生效。将其设为 `0` 可关闭对应的保护。
* **单一仓位。** 与原版 EA 一样，不会在已有仓位时开立新单。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `Volume` | `0.1` | 下单手数。请根据账户合约规模自行调整。 |
| `CandleType` | `TimeSpan.FromMinutes(15).TimeFrame()` | 用于计算指标的 K 线类型。建议使用 5 或 15 分钟周期以贴近原版。 |
| `RsiPeriod` | `14` | RSI 基础计算周期。 |
| `ShortPricePeriod` | `9` | 趋势过滤中使用的价格快线 SMA 周期。 |
| `LongPricePeriod` | `45` | 趋势过滤中使用的价格慢线 LWMA 周期。 |
| `ShortRsiPeriod` | `9` | 应用于 RSI 数值的快速 SMA 周期。 |
| `LongRsiPeriod` | `45` | 应用于 RSI 数值的慢速 SMA 周期。 |
| `StochasticKPeriod` | `5` | 随机指标 %K 基础周期。 |
| `StochasticDPeriod` | `3` | %D 平滑周期。 |
| `StochasticSlowing` | `3` | %K 额外放缓系数。 |
| `DemarkerPeriod` | `14` | DeMarker 指标的平滑周期。 |
| `WilliamsPeriod` | `14` | Williams %R 的回溯周期。 |
| `StochasticOversoldLevel` | `19` | %K 必须向上突破的超卖阈值。 |
| `StochasticOverboughtLevel` | `81` | %K 必须向下跌破的超买阈值。 |
| `DemarkerBuyLevel` | `0.35` | 多头信号所需的 DeMarker 向上突破水平。 |
| `DemarkerSellLevel` | `0.63` | 空头信号所需的 DeMarker 向下突破水平。 |
| `WilliamsBuyLevel` | `-81` | Williams %R 向上突破的确认水平（做多）。 |
| `WilliamsSellLevel` | `-19` | Williams %R 向下突破的确认水平（做空）。 |
| `StopLossPips` | `7777` | 固定止损点数。默认的超大数值相当于关闭止损。 |
| `TakeProfitPips` | `17` | 固定止盈点数。设为 `0` 可禁用止盈。 |

## 注意事项
* 请确保 `Security.PriceStep`、`Security.MinVolume` 与 `Security.VolumeStep` 已正确配置，否则点数到价格的转换会失真。
* 所有信号都依赖相邻两根完整 K 线之间的交叉状况，回测时务必保证数据时间对齐与原策略一致。
