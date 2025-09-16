# RSI 布林线分形突破策略

## 概述
该策略在 StockSharp 中复现 MetaTrader 的“RSI and Bollinger Bands”专家顾问。布林带作用在 RSI 指标上而不是价格。当检测到新的确认分形时，策略会按设置的点差在分形价格外侧挂入止损单，并使用 Parabolic SAR 对持仓进行动态跟踪。

## 指标说明
- **RSI**（默认 8 周期）：核心动量指标，超买/超卖阈值用于取消挂单。
- **布林带（作用于 RSI）**：长度 14，偏差 1.0。只有当 RSI 收盘突破上轨或下轨时才会触发信号。
- **威廉分形**：寻找最近的五根 K 线模式，获得上方和下方分形价格作为突破点。
- **Parabolic SAR**：初始步长 0.003，最大 0.2，用于生成跟踪止损位置。

## 入场逻辑
1. 仅在所选时间框架的已完成 K 线上运行（默认 4 小时）。
2. 当出现**上方分形**且 RSI 收盘高于 **布林上轨**，同时上一根 K 线收盘仍低于该分形时，挂出 **买入止损单**：
   - 挂单价格 = 分形高点 + `IndentPips`（默认 15 点）。
   - 若 `StopLossPips` > 0，则止损价格 = 挂单价 − `StopLossPips`。
   - 若 `TakeProfitPips` > 0，则止盈价格 = 挂单价 + `TakeProfitPips`。
3. 当出现**下方分形**且 RSI 收盘低于 **布林下轨**，上一根 K 线收盘仍高于分形时，挂出 **卖出止损单**（分形价 − `IndentPips`）。
4. RSI 回到区间内时取消挂单：
   - RSI < `RsiLower` → 取消买入止损单。
   - RSI > `RsiUpper` → 取消卖出止损单。

## 离场与风险控制
- 固定止盈/止损使用点值设置，与原版 EA 相同；为 0 则禁用该保护。
- Parabolic SAR 只有在 SAR 与当前价格的距离大于 `SarTrailingPips` 时才会向盈利方向推进止损。
- 当价格触及动态止损或固定止盈时，通过市价单平仓。
- 挂单成交后会取消反向挂单，并记录当前仓位的止损/止盈水平以便跟踪。

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `RsiPeriod` | RSI 平滑周期。 | 8 |
| `BandsPeriod` | RSI 布林带周期。 | 14 |
| `BandsDeviation` | 布林带标准差系数。 | 1.0 |
| `SarStep` | Parabolic SAR 加速步长。 | 0.003 |
| `SarMax` | Parabolic SAR 最大加速值。 | 0.2 |
| `TakeProfitPips` | 止盈点数。 | 50 |
| `StopLossPips` | 止损点数。 | 135 |
| `IndentPips` | 相对分形的额外偏移。 | 15 |
| `RsiUpper` | RSI 超买阈值，用于取消卖单。 | 70 |
| `RsiLower` | RSI 超卖阈值，用于取消买单。 | 30 |
| `SarTrailingPips` | SAR 与价格之间的最小距离（点）。 | 10 |
| `CandleType` | 处理的蜡烛时间框架。 | 4 小时 |

## 其他说明
- 按要求不提供 Python 版本。
- 头寸数量由基础类的 `Volume` 属性控制（默认 1）。
- 推荐使用与原 EA 相同的时间框架，例如 EURUSD H4。
