# Exp Spearman Rank Correlation Histogram 策略

该 StockSharp 策略移植自 MetaTrader 专家顾问 **Exp_SpearmanRankCorrelation_Histogram**。策略订阅指定周期的 K 线，计算每个已收盘柱的斯皮尔曼秩相关系数直方图，并根据颜色状态的变化执行交易。不同的交易模式决定是平掉反向仓位、直接反手还是等待极值才动作。

## 指标流程

1. 使用 `RankCorrelationIndex` 指标（斯皮尔曼秩相关，范围 ±100），输入为蜡烛的收盘价。计算窗口由 `MaxRange` 限制，默认 14 根柱。
2. 将结果归一化到 `[-1, 1]` 区间。当 `InvertCorrelation` 为真时，符号会反转，用于模拟 MQL 中的 `direction` 参数。
3. 将归一化后的值与 `HighLevel`、`LowLevel` 对比，得到颜色状态：
   * `4` – 强势多头区（`value > HighLevel`）。
   * `3` – 中度多头区（`0 < value ≤ HighLevel`）。
   * `2` – 中性区（`value == 0`）。
   * `1` – 中度空头区（`LowLevel ≤ value < 0`）。
   * `0` – 强势空头区（`value < LowLevel`）。
4. 最新颜色存储在序列式缓冲区，索引 `0` 表示最新的已收盘柱，`1` 表示上一柱，依此类推。

## 交易流程

* 仅在 K 线完全收盘 (`CandleStates.Finished`) 时评估信号。
* `SignalBar` 指定回溯的柱数（默认 1）。策略同时查看更早的一柱，以复现 MQL 中复制两个缓冲区的做法。
* `AllowBuyEntries`、`AllowSellEntries`、`AllowBuyExits`、`AllowSellExits` 控制是否允许开仓与平仓。
* 交易模式与原专家一致：
  * **Mode 1** – 当较早的颜色>2 或 <2 时平掉反向仓位；若允许，在较新的颜色跌出多头区（<3）或升出空头区（>1）后开仓。
  * **Mode 2** – 仅对极值反应。颜色 `4` 会平掉空单，并在较新的颜色降到 `4` 以下时可选开多；颜色 `0` 会平掉多单，并在较新的颜色升到 `0` 以上时可选开空。
  * **Mode 3** – Mode 2 的严格版：遇到 `4` 立即平空，遇到 `0` 立即平多，新开仓条件与 Mode 2 相同。
* 每次下单前调用 `CancelActiveOrders()` 取消尚未完成的委托。
* 反手仓位使用 `Volume` 与当前仓位绝对值之和，确保完全翻向。
* 可选的 `StopLossPoints` 与 `TakeProfitPoints`（价格单位）通过 `StartProtection` 激活风控；为 `0` 时不创建保护单。

## 参数

| 参数 | 说明 |
| --- | --- |
| `CandleType` | 指标与交易所用的 K 线周期。 |
| `RangeLength` | 名义上的 Spearman 计算窗口（受 `MaxRange` 限制）。 |
| `MaxRange` | 窗口上限，若设为 `0` 则退化为 `10`。 |
| `HighLevel`, `LowLevel` | 定义多头与空头区域的阈值。 |
| `SignalBar` | 分析前需要跳过的已收盘柱数量。 |
| `InvertCorrelation` | 翻转直方图符号，对应 MQL 的 `direction=false`。 |
| `AllowBuyEntries`, `AllowSellEntries` | 允许开多/开空。 |
| `AllowBuyExits`, `AllowSellExits` | 允许自动平多/平空。 |
| `TradeMode` | 选择原策略的 Mode 1、Mode 2 或 Mode 3。 |
| `StopLossPoints`, `TakeProfitPoints` | 以绝对价格表示的可选风控距离。 |
| `Volume`（内置） | 开仓或反手时使用的基础手数。 |

## 与 MQL 专家顾问的差异

* 资金管理参数 (`MM`, `MMMode`) 与滑点 (`Deviation_`) 未实现，仓位控制依赖标准的 `Volume` 属性与账户设置。
* `TradeAlgorithms.mqh` 中的辅助函数被直接的 `BuyMarket`/`SellMarket` 调用取代，并在下单前取消挂单。
* MQL 的 `CalculatedBars` 性能提示在 StockSharp 中无须使用，因此省略。
* `direction` 标志由 `InvertCorrelation` 体现，仅执行符号取反。
* `StopLoss_` 与 `TakeProfit_` 被视为价格位移，通过 `StartProtection` 应用；不会自动将点值转换为价格。
* 信号在柱子收盘时立即执行，没有推迟到下一柱开盘。

以上改动遵循 StockSharp 高层 API 的最佳实践，同时保留了原有的交易逻辑。
