# Open Close 策略 (ID 3996)

## 概述
本策略对应 MetaTrader 4 智能交易系统 `open_close.mq4`。它仅关注一个品种，通过比较最近两根 K 线的开盘价与收盘价来寻找反转机会。当没有持仓时，策略逆势交易单根 K 线的过度波动；持仓期间，一旦出现相反信号或浮动亏损达到阈值便立即离场。

## 交易逻辑
### 入场条件
- 只有在上一根 K 线完全形成后才会交易（原始代码中的 `Volume[0] == 1` 判断）。
- 做多：当前 K 线开盘价高于上一根开盘价，且收盘价低于上一根收盘价，按市价买入设定的手数。
- 做空：当前 K 线开盘价低于上一根开盘价，且收盘价高于上一根收盘价，按市价卖出设定的手数。

任意时刻仅允许一笔净头寸，未平仓期间忽略新的信号。

### 离场条件
1. **风险保护**：根据平均持仓价计算浮动盈亏。当浮亏超过 `MaximumRisk × Portfolio.CurrentValue` 时，立即平仓。原版使用的 `AccountMargin` 在本实现中由账户市值的最佳可用估计替代。
2. **形态反转**：
   - 多头：若下一根 K 线继续下跌（`open < 前一根 open` 且 `close < 前一根 close`），立即平仓。
   - 空头：若下一根 K 线继续上涨（`open > 前一根 open` 且 `close > 前一根 close`），立即平仓。

## 仓位管理
- 订单基础手数由 `MaximumRisk` 决定：账户权益乘以风险系数再除以 `1000`，与原始公式 `AccountFreeMargin * MaximumRisk / 1000` 等效。
- 当无法获得账户数据时，退回到 `InitialVolume` 参数。
- 连续两笔以上亏损后，手数按 `volume × losses / DecreaseFactor` 减少，复刻 MQL 中遍历历史订单的逻辑。
- 强制最小手数为 `0.1`，随后根据交易品种的成交量步长和交易所限制进行对齐。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `InitialVolume` | `decimal` | `0.1` | 当账户数据不可用时的备用手数。 |
| `MaximumRisk` | `decimal` | `0.3` | 控制下单手数与允许浮亏上限的账户资金比例。 |
| `DecreaseFactor` | `decimal` | `100` | 连续亏损后减少手数的衰减系数。 |
| `CandleType` | `DataType` | 15 分钟 | 用于分析形态的 K 线类型。 |

## 实现细节
- 订阅所选时间周期的 K 线，并仅在 K 线完结时运行逻辑，对应原策略的 `Volume[0] > 1` 约束。
- 由于 StockSharp 无法直接提供 MetaTrader 的 `AccountProfit` 和 `AccountMargin`，浮动盈亏通过当前仓位和最近收盘价估算。
- 连续亏损统计依赖成交回报，因此 `DecreaseFactor` 的行为与原始历史遍历保持一致。
- 手数会根据 `Security.VolumeStep`、`MinVolume`、`MaxVolume` 自动调整，避免违反交易所规则。
- 若可用图表区域，策略会绘制 K 线与自身成交，方便回测与可视化调试。

## 使用建议
- 选择与原 MetaTrader 策略相同的 K 线周期进行对比测试。
- 通过调节 `MaximumRisk` 与 `DecreaseFactor` 控制下单节奏与风险敞口。
- 该策略为逆势思路，更适合存在单根 K 线超买/超卖并伴随快速回落的市场环境。
