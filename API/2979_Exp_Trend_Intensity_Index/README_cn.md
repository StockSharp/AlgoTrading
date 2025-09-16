# Exp 趋势强度指数策略

该策略是 MetaTrader 专家顾问 **Exp_Trend_Intensity_Index** 的 StockSharp 版本。策略在可配置周期的收盘K线上工作，利用趋势强度指数（Trend Intensity Index，TII）识别动能离开极端多空区域的时刻：当指标离开上方阈值区时平掉空头并可以转多；当指标离开下方阈值区时平掉多头并可以转空。

## 指标计算流程

1. 选择价格来源（收盘价、开盘价、加权价格、TrendFollow 价格、Demark 价格等）。
2. 使用第一条均线（`PriceMaMethod`、`PriceMaLength`）对价格流进行平滑。
3. 计算价格与平滑值的差值，并拆分为正向流和负向流。
4. 对正、负流分别应用第二条均线（`SmoothingMethod`、`SmoothingLength`）。
5. 计算趋势强度指数：`TII = 100 * Positive / (Positive + Negative)`。
6. 将结果与 `HighLevel`、`LowLevel` 比较，得到颜色状态：高区 (`0`)、中性 (`1`)、低区 (`2`)。

实现采用 StockSharp 内置的简单、指数、平滑、加权均线；原版 MQL 库中的其他平滑算法在此移植中未提供。

## 交易逻辑

* 仅在 K 线完全收盘 (`CandleStates.Finished`) 后评估信号。
* `SignalBar` 指定回看多少根已完成的 K 线（默认上一根），同时会读取更早一根的状态，以复现 MQL 中双缓冲读取方式。
* 当较旧的颜色为高区 (`color == 0`) 时：
  * 如果开启 `EnableSellExits`，则平掉所有空头。
  * 如果最近一根离开高区且允许 `EnableBuyEntries`，则开多或反手做多。
* 当较旧的颜色为低区 (`color == 2`) 时：
  * 如果开启 `EnableBuyExits`，则平掉所有多头。
  * 如果最近一根离开低区且允许 `EnableSellEntries`，则开空或反手做空。
* 下单通过 `BuyMarket`、`SellMarket` 完成，若需要反手，会在当前仓位数量基础上加上 `Volume` 属性。
* 可选的止损/止盈（以价格单位表示）由 `StopLossPoints`、`TakeProfitPoints` 配置，并使用 `StartProtection` 实现。

## 参数说明

| 参数 | 说明 |
| --- | --- |
| `CandleType` | 指定用于计算和交易的时间框架。 |
| `PriceMaMethod`, `PriceMaLength` | 第一条均线的类型与周期。 |
| `SmoothingMethod`, `SmoothingLength` | 正、负流平滑所用均线的类型与周期。 |
| `AppliedPrice` | 指标使用的价格来源（收盘、开盘、中位、TrendFollow、Demark 等）。 |
| `HighLevel`, `LowLevel` | 定义多空极值区域的上下阈值。 |
| `SignalBar` | 回看的已完成 K 线数量，用于确认信号。 |
| `EnableBuyEntries`, `EnableSellEntries` | 是否允许开多 / 开空。 |
| `EnableBuyExits`, `EnableSellExits` | 是否允许在指标翻转时自动平多 / 平空。 |
| `StopLossPoints`, `TakeProfitPoints` | 可选的止损、止盈价格距离，传递给 `StartProtection`。 |

## 与原版 MQL 策略的差异

* 原策略的资金管理参数（`MM`、`MMMode`、`Deviation`）被 StockSharp 的标准 `Volume` 属性与市价下单逻辑取代，未实现滑点设置。
* 仅提供 StockSharp 中可用的均线类型（简单、指数、平滑、加权）。
* 原版指标的 Phase 参数被省略，因为 StockSharp 指标没有对应接口。
* 交易信号在收盘 K 线确认后立即执行，不再延迟到下一根开盘。

以上调整保持了核心交易思想，同时符合 StockSharp 高阶策略的实现规范。
