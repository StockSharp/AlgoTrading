# ColorXPWMA Digit MMRec 策略

## 策略概述
**ColorXPWMA Digit MMRec** 策略将 MetaTrader 专家顾问 `Exp_ColorXPWMA_Digit_MMRec` 移植到 StockSharp 平台。核心思想是使用 ColorXPWMA Digit 指标捕捉趋势拐点，并保留原始版本的“递归资金管理”(MM Recounter) 机制。指标首先计算一条带权移动平均线，权重按 `(period - index)^power` 递减，然后再通过选定的移动平均方法进行平滑。平滑后的斜率被量化为三个离散颜色：`2` 表示上升，`0` 表示下降，`1` 表示水平。

交易信号在指定的历史 K 线 (`SignalBar`) 上评估：如果 `SignalBar + 1` 处的颜色为 `2`（趋势向上），但 `SignalBar` 上的颜色已经不再为 `2`，则认为上涨动能失效，此时平掉空头并（如果允许）开立多单；反之，当 `SignalBar + 1` 的颜色为 `0`（趋势向下）而 `SignalBar` 上颜色不再为 `0` 时，平掉多单并可开立空单。

## 指标逻辑
- **Power Weighted Moving Average**：最新的价格拥有更高权重，权重为 `(period - index)^power`。
- **平滑处理**：支持多种移动平均（SMA、EMA、SMMA、LWMA、Jurik、T3、Kaufman AMA）。由于 StockSharp 没有 JurX、ParMa、VIDYA 的原生实现，这三种模式使用 EMA 近似。
- **颜色编码**：根据平滑线的斜率符号生成 `0/1/2` 颜色序列，驱动交易信号。
- **数位控制**：可将最终数值四舍五入到指定的小数位数，复刻原始指标的 “Digit” 行为。

## 交易规则
1. **多头条件（上涨失效）**
   - 条件：`SignalBar + 1` 处颜色为 `2`，`SignalBar` 处颜色不为 `2`。
   - 操作：若有空头则立即平仓；若允许开多则按照资金管理结果开立新的多头头寸。
2. **空头条件（下跌失效）**
   - 条件：`SignalBar + 1` 处颜色为 `0`，`SignalBar` 处颜色不为 `0`。
   - 操作：若有多头则平仓；若允许开空则按资金管理结果建立空头。

所有操作均在触发信号的蜡烛收盘价执行。若需要翻转持仓，会一次性提交合并数量的市价单，先平旧仓再开新仓。

## 资金管理 Recounter
在每次开仓前，策略会回顾近期交易结果：

- 针对多头，检查最近 `BuyTotalTrigger` 笔交易，如果亏损次数达到 `BuyLossTrigger`，下一笔多头使用 `ReducedVolume`；否则使用 `NormalVolume`。
- 针对空头，同样使用 `SellTotalTrigger` 与 `SellLossTrigger` 的组合。

这与原始 MQL 代码中的 `BuyTradeMMRecounterS` / `SellTradeMMRecounterS` 行为完全一致。

## 参数说明
| 分组 | 参数 | 说明 |
| --- | --- | --- |
| General | `CandleType` | 指标与信号使用的时间框架。 |
| Indicator | `IndicatorPeriod` | Power Weighted MA 的周期。 |
| Indicator | `IndicatorPower` | 权重的指数，越大越强调最新数据。 |
| Indicator | `SmoothingMethod` | 平滑方法。`JurX`、`ParMa`、`Vidya` 在 StockSharp 中采用 EMA 近似。 |
| Indicator | `SmoothingLength` | 平滑移动平均的长度。 |
| Indicator | `SmoothingPhase` | 某些平滑算法使用的相位参数。 |
| Indicator | `AppliedPrice` | 指标所用的价格类型（收盘、开盘、最高、最低等）。 |
| Indicator | `RoundingDigits` | 最终值的保留小数位数。 |
| Logic | `SignalBar` | 读取颜色缓冲区时的历史位移。 |
| Permissions | `EnableBuyEntries` / `EnableSellEntries` | 是否允许开多/开空。 |
| Permissions | `EnableBuyExits` / `EnableSellExits` | 是否允许平多/平空。 |
| Money Management | `NormalVolume` | 默认开仓数量。 |
| Money Management | `ReducedVolume` | 连续亏损后使用的降低数量。 |
| Money Management | `BuyTotalTrigger`, `BuyLossTrigger` | 多头统计窗口及亏损阈值。 |
| Money Management | `SellTotalTrigger`, `SellLossTrigger` | 空头统计窗口及亏损阈值。 |
| Risk Management | `StopLossPoints`, `TakeProfitPoints` | 以点数表示的止损/止盈距离，非零时通过 `StartProtection` 自动应用。 |

## 使用建议
- 推荐保持 `SignalBar = 1` 以确保仅根据完全收盘的蜡烛发出信号，贴近原始 EA 行为。
- 策略只保留 recounter 所需的最新绩效数据，不会无限增长内存。
- 由于 StockSharp 订单执行是异步的，策略在更新盈亏计数时假定成交价等于触发信号的收盘价，与原 MQL 策略在测试器中的假设一致。
- 若需要原汁原味的 JurX、ParMa、VIDYA 平滑方式，可自行实现对应指标并替换当前的 EMA 近似。

