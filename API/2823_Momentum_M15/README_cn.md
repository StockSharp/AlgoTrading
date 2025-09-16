# Momentum M15 策略

本策略移植自 MetaTrader 5 顾问 **Momentum-M15**（原始文件 `Momentum-M15.mq5`）。它在 15 分钟级别蜡烛上运
行，结合带有水平偏移的移动平均线与按开盘价计算的 Momentum 指标。核心思想是在价格位于偏移平均线相对
侧时反向交易：价格偏低时做多、价格偏高时做空，同时使用跳空过滤器和可选的追踪止损控制风险。

## 移植要点

* 使用 StockSharp 自带组件实现指标：可配置的移动平均线（默认 Smoothed）以及可以选择价格来源的
  `Momentum` 指标。
* MA 的水平偏移通过缓存指标数值并回取 `MaShift` 个已完成柱的值来模拟，无需重写指标算法。
* Momentum 单调性检测保留原始 `CheckMO_Up` / `CheckMO_Down` 逻辑，仅存储所需数量的最新数值。
* 大幅向上跳空保护 (`GapLevel` / `GapTimeout`) 与 MQL 版本一致，使用 `Security.PriceStep` 将点值转换为价
  格步长。
* 追踪止损通过监控价位并在触发时市价平仓来完成，对应 MQL 代码中每个新柱修改止损订单的行为。

## 参数

| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `TradeVolume` | 每笔交易的数量。 | `0.1` |
| `CandleType` | 主要时间框架。 | `15m` |
| `MaPeriod` | 移动平均线周期。 | `26` |
| `MaShift` | 移动平均线向前偏移的柱数。 | `8` |
| `MaMethod` | 移动平均线类型（`Simple`、`Exponential`、`Smoothed`、`Weighted`）。 | `Smoothed` |
| `MaPrice` | 送入移动平均线的价格。 | `Low` |
| `MomentumPeriod` | Momentum 指标周期。 | `23` |
| `MomentumPrice` | Momentum 指标使用的价格。 | `Open` |
| `MomentumThreshold` | Momentum 判定基准值。 | `100` |
| `MomentumShift` | 在基准值基础上的偏移量。 | `-0.2` |
| `MomentumOpenLength` | 触发进场所需的 Momentum 单调序列长度。 | `6` |
| `MomentumCloseLength` | 触发离场所需的单调序列长度。 | `10` |
| `GapLevel` | 暂停交易的最小正跳空（按价格步长计）。 | `30` |
| `GapTimeout` | 跳空后保持暂停的柱数。 | `100` |
| `TrailingStop` | 追踪止损距离（价格步长，0 为关闭）。 | `0` |

## 交易逻辑

### 进场

* **做多** 条件：
  * 最新 Momentum 小于 `MomentumThreshold + MomentumShift`。
  * 前一根收盘价与当前开盘价都在偏移均线之下。
  * Momentum 连续 `MomentumOpenLength` 根保持非上升趋势。
* **做空** 条件：
  * 最新 Momentum 大于 `MomentumThreshold - MomentumShift`。
  * 前收与今开均高于偏移均线。
  * Momentum 连续 `MomentumOpenLength` 根保持非下降趋势。

只有在没有持仓且未被跳空过滤器锁定时才会开仓。

### 离场

* **多头** 平仓条件：
  * Momentum 连续 `MomentumCloseLength` 根不升，或
  * 前一根收盘价跌破偏移均线，或
  * 触发追踪止损（当前最低价减去 `TrailingStop` 距离）。
* **空头** 平仓条件：
  * Momentum 连续 `MomentumCloseLength` 根不降，或
  * 前一根收盘价突破偏移均线，或
  * 触发追踪止损（当前最高价加上 `TrailingStop` 距离）。

### 跳空过滤

1. 计算当前开盘价与前一收盘价的差值（换算为价格步长）。
2. 当差值超过 `GapLevel` 时，将计时器设置为 `GapTimeout`。
3. 每根完成的蜡烛将计时器减一，直到归零才允许再次交易。

## 重要说明

* 策略只处理已完成的蜡烛 (`CandleStates.Finished`)，因此信号会在下一根柱子开仓/平仓，与原始 EA 在新柱
  第一笔成交触发的效果一致。
* MetaTrader 中的“点”通过 `Security.PriceStep` 进行近似转换。如果合约没有正确配置价格步长，则跳空过滤
  和追踪止损会自动停用。
* 移动平均线和 Momentum 的价格来源可以独立设置，与原始版本保持一致。
* 策略不会下达止损订单，而是通过市价单实现与 `PositionModify` 类似的止损调整。

## 使用建议

1. 选择目标证券，并确保 `CandleType` 与回测时的时间框架一致（原始脚本为 15 分钟）。
2. 根据账户规模设置 `TradeVolume`。
3. 通过调整 `MomentumOpenLength` / `MomentumCloseLength` 控制 Momentum 序列的严格程度。
4. 若希望完全匹配原始“点”距离，可根据交易所的价格步长换算出合适的 `TrailingStop` 和 `GapLevel`。
