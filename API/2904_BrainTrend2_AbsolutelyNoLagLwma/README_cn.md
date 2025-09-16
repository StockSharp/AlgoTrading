# BrainTrend2 + AbsolutelyNoLagLWMA 策略

## 概述
该策略结合了最初在 MetaTrader 5 中实现的 BrainTrend2_V2 和 AbsolutelyNoLagLWMA 两个独立模块。每个模块都订阅各自的K线数据，独立判断多头、空头或观望状态。C# 版本完整保留了这两套决策流程，并在 StockSharp 中把它们的目标仓位汇总成一个组合头寸。

* **BrainTrend2 模块**：依靠 BrainTrend2 指标生成的颜色状态来跟踪趋势。指标内部使用基于 ATR 的通道，当价格突破反向边界时颜色发生翻转。
* **AbsolutelyNoLagLWMA 模块**：计算两次线性加权移动平均，以所选价格的双重平滑坡度来判断方向。

任一模块需要调整仓位时，策略都会重新计算组合目标量，并通过市价单把实际仓位调整到该目标。默认两个模块都使用 H4 周期，但也可以为每个模块指定不同的时间框架。

## 指标说明
### BrainTrend2
BrainTrend2 指标在 C# 中重建了原始 MQL 文件中的五色蜡烛显示：
* 使用给定周期的三角形加权真波动幅度（TR）序列，并乘以 0.7 得到动态带宽 `widcha`。
* 浮动参考值 `Emaxtra` 会在当前趋势中跟随价格极值。
* 当最低价跌破 `Emaxtra - widcha` 时趋势翻转为看跌；当最高价突破 `Emaxtra + widcha` 时趋势翻转为看涨。
* 最终颜色与趋势对应：绿色/青色（取值 0 或 1）代表多头，酒红/洋红（取值 3 或 4）代表空头，灰色（取值 2）表示指标尚未就绪。

C# 指标保留了三角形 ATR 的计算方法，因此颜色输出与原始 Expert Advisor 保持一致。

### AbsolutelyNoLagLWMA
AbsolutelyNoLagLWMA 模块对所选应用价格连续执行两次线性加权移动平均，并根据最终平滑曲线的斜率生成颜色：
* **2（蓝色）** —— 曲线向上。
* **1（灰色）** —— 曲线横盘。
* **0（紫色）** —— 曲线向下。

两个指标都会暴露 `IsFormed` 状态，策略会在指标准备好之前忽略信号。

## 交易逻辑
策略内部维护 `_brainTrendTarget` 和 `_lwmaTarget` 两个目标仓位，分别代表两个模块希望持有的数量。一旦某个模块调整目标，就会调用 `RebalancePosition`，把实际仓位调整为两个目标之和。

### BrainTrend2 模块
* 读取 `SignalBar` 根之前的颜色（默认为 1 根）以及它前一根的颜色，识别趋势切换。
* 当当前颜色为多头（< 2）且前一颜色不是多头（> 1）时：
  * 平掉该模块可能存在的空头仓位。
  * 若允许做多，则按 `BrainTrendVolume` 开仓。
* 当当前颜色为空头（> 2）且前一颜色不是空头（< 3）时：
  * 平掉该模块可能存在的多头仓位。
  * 若允许做空，则按 `BrainTrendVolume` 开空。

### AbsolutelyNoLagLWMA 模块
* 同样基于 `SignalBar` 偏移，但只对颜色 2（上行）和 0（下行）做出反应。
* 当颜色变为 **2** 且上一颜色不同：
  * 若 `LwmaCloseShortAllowed` 为真，则平掉该模块的空头。
  * 若 `LwmaBuyAllowed` 为真，则按 `LwmaVolume` 开多。
* 当颜色变为 **0** 且上一颜色不同：
  * 若 `LwmaCloseLongAllowed` 为真，则平掉该模块的多头。
  * 若 `LwmaSellAllowed` 为真，则按 `LwmaVolume` 开空。

两个模块仅修改自己的目标量，因此可以同时持仓。例如 BrainTrend2 保持趋势多单，而 LWMA 模块在趋势附近做短线对冲。

## 参数
| 名称 | 说明 |
| --- | --- |
| `BrainTrendAtrPeriod` | BrainTrend2 使用的三角形 ATR 周期。 |
| `BrainTrendSignalBar` | BrainTrend2 信号偏移量，`1` 表示等待上一根K线收盘确认。 |
| `BrainTrendBuyAllowed` / `BrainTrendSellAllowed` | 是否允许 BrainTrend2 模块开多 / 开空。 |
| `BrainTrendVolume` | BrainTrend2 模块开仓时使用的数量。 |
| `BrainTrendCandleType` | BrainTrend2 模块订阅的蜡烛类型（时间框架）。 |
| `LwmaLength` | AbsolutelyNoLagLWMA 每次加权平均的长度。 |
| `LwmaSignalBar` | LWMA 模块的信号偏移量，语义与 BrainTrend 模块相同。 |
| `LwmaAppliedPrice` | LWMA 计算所用的应用价格（收盘价、开盘价、中位价、Demark 等）。 |
| `LwmaBuyAllowed` / `LwmaSellAllowed` | 是否允许 LWMA 模块开多 / 开空。 |
| `LwmaCloseLongAllowed` / `LwmaCloseShortAllowed` | LWMA 模块在信号反转时是否平掉对应方向的仓位。 |
| `LwmaVolume` | LWMA 模块开仓时使用的数量。 |
| `LwmaCandleType` | LWMA 模块订阅的蜡烛类型。 |

## 仓位与下单
* 策略始终通过 `BuyMarket` / `SellMarket` 市价单实现目标仓位。
* 两个模块的仓位是可叠加的。例如各自下 1 手方向相反的订单时，净头寸为 0，相当于对冲。
* 原 Expert Advisor 中依赖经纪商的止损/止盈逻辑未在此移植，如需风控可使用 StockSharp 提供的保护组件。
* 当两个模块使用不同时间框架时，策略会自动订阅两路蜡烛并在图表区域绘制行情与成交。

## 注意事项
* 指标计算全部在策略内部完成，不需要外部库。
* `SignalBar = 0` 可在最近一根完成的蜡烛后立即行动，更大的偏移可以提供额外确认。
* BrainTrend2 至少需要 `AtrPeriod + 2` 根历史K线才会输出有效颜色；AbsolutelyNoLagLWMA 需要至少 `Length` 根。
* 两个模块共用同一个 `Strategy.Security`，类似原 MT5 EA 里通过不同 magic number 管理的两个子策略。

## 扩展建议
* 如果需要原策略中的固定止损，可结合 StockSharp 的保护或风控组件实现。
* 独立调节 `BrainTrendVolume` 与 `LwmaVolume`，即可强化趋势跟随或短线对冲的权重。
* 通过 `ProcessBrainTrend` / `ProcessLwma` 中的指标值添加额外过滤条件，实现更复杂的策略逻辑。
