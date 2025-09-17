# ColorMaRsi Trigger MMRec Duplex 策略
[English](README.md) | [Русский](README_ru.md)

## 概述

该策略将 MetaTrader 专家顾问 **Exp_ColorMaRsi-Trigger_MMRec_Duplex.mq5** 迁移到 StockSharp 高级 API。系统包含两个独立的
 MaRsi-Trigger 模块：一个负责做多信号，另一个负责做空信号。每个模块都会比较快、慢移动平均线以及快、慢 RSI，并将结
果压缩在 `[-1, 1]` 区间内：`+1` 表示多头状态，`-1` 表示空头状态，`0` 表示信号不一致。

同时实现了“MMRec”资金管理模块。它会跟踪最近的多空交易结果，一旦在滑动窗口内出现指定数量的亏损，下一次开仓就会采
用缩减的手数，从而复现 MetaTrader 库 `TradeAlgorithms.mqh` 中的自适应仓位算法。

## 交易逻辑

1. **指标流程**（针对每个模块）：
   - 根据设定的应用价格和周期计算快慢移动平均 (`MA_fast`/`MA_slow`)。
   - 计算快慢 RSI (`RSI_fast`/`RSI_slow`)；两者的价格输入可以不同。
   - 构建颜色值：初始为 `0`，若 `MA_fast > MA_slow` 加 `+1`，否则减 `1`；对 RSI 亦然，随后把结果限制在 `[-1, 1]`。
   - 将颜色值写入历史，并使用 `SignalBar` 指定的偏移读取（与原始 EA 一致）。

2. **多头模块**：
   - **入场**：仅在没有持多仓时触发（若有空头先平仓）。要求前一个颜色值为 `+1`，当前颜色值小于等于 `0`，表示多头信号
     刚刚转弱。
   - **出场**：若前一个颜色值变为 `-1` 且允许平仓，则市价卖出。

3. **空头模块**：
   - **入场**：仅在没有持空仓时触发（若有多头先平仓）。要求前一个颜色值为 `-1`，当前颜色值大于等于 `0`，表示空头信号
     转弱。
   - **出场**：若前一个颜色值变为 `+1` 且允许平仓，则市价买入。

4. **止损/止盈**：可选参数，以价格最小变动单位表示，每根完成的 K 线都会检查是否触发，对应的持仓会立即平掉。

5. **资金管理**：分别记录多、空方向的交易盈亏。当最近 `HistoryDepth` 笔交易中的亏损次数达到 `LossTrigger` 时，下一笔
订单采用缩减手数，否则使用常规手数。

## 参数

| 组别 | 名称 | 说明 | 默认值 |
| --- | --- | --- | --- |
| 多头模块 | `LongCandleType` | 多头模块使用的 K 线周期。 | `H4` |
|  | `LongAllowOpen` / `LongAllowClose` | 是否允许开多 / 平多。 | `true` |
|  | `LongStopLossPoints` / `LongTakeProfitPoints` | 止损与止盈的点数（乘以 `PriceStep`）。填写 `0` 可关闭。 | `1000` / `2000` |
|  | `LongSignalBar` | 读取指标时向前回看的已完成 K 线数量。 | `1` |
|  | `LongRsiPeriod` / `LongRsiLongPeriod` | 快、慢 RSI 周期。 | `3` / `13` |
|  | `LongMaPeriod` / `LongMaLongPeriod` | 快、慢移动平均周期。 | `5` / `10` |
|  | `LongRsiPrice` / `LongRsiLongPrice` | 快、慢 RSI 使用的应用价格。 | `Weighted` / `Median` |
|  | `LongMaPrice` / `LongMaLongPrice` | 快、慢移动平均使用的应用价格。 | `Close` / `Close` |
|  | `LongMaType` / `LongMaLongType` | 移动平均算法（Simple、Exponential、Smoothed、Weighted）。 | `Exponential` / `Exponential` |
| 资金管理 | `LongNormalVolume` / `LongReducedVolume` | 常规与缩减的多头手数。 | `0.1` / `0.01` |
|  | `LongHistoryDepth` | 多头方向参与统计的历史交易数量。 | `5` |
|  | `LongLossTrigger` | 多头方向触发缩减手数所需的亏损次数。 | `3` |

| 组别 | 名称 | 说明 | 默认值 |
| --- | --- | --- | --- |
| 空头模块 | `ShortCandleType` | 空头模块使用的 K 线周期。 | `H4` |
|  | `ShortAllowOpen` / `ShortAllowClose` | 是否允许开空 / 平空。 | `true` |
|  | `ShortStopLossPoints` / `ShortTakeProfitPoints` | 止损与止盈的点数。 | `1000` / `2000` |
|  | `ShortSignalBar` | 读取指标时的偏移量。 | `1` |
|  | `ShortRsiPeriod` / `ShortRsiLongPeriod` | 快、慢 RSI 周期。 | `3` / `13` |
|  | `ShortMaPeriod` / `ShortMaLongPeriod` | 快、慢移动平均周期。 | `5` / `10` |
|  | `ShortRsiPrice` / `ShortRsiLongPrice` | 快、慢 RSI 使用的应用价格。 | `Weighted` / `Median` |
|  | `ShortMaPrice` / `ShortMaLongPrice` | 快、慢移动平均使用的应用价格。 | `Close` / `Close` |
|  | `ShortMaType` / `ShortMaLongType` | 移动平均算法。 | `Exponential` / `Exponential` |
| 资金管理 | `ShortNormalVolume` / `ShortReducedVolume` | 常规与缩减的空头手数。 | `0.1` / `0.01` |
|  | `ShortHistoryDepth` | 空头方向参与统计的历史交易数量。 | `5` |
|  | `ShortLossTrigger` | 空头方向触发缩减手数所需的亏损次数。 | `3` |

## 说明

- 应用价格选项遵循 MetaTrader 约定，例如 `Weighted = (High + Low + 2 * Close) / 4`，`Typical = (High + Low + Close) / 3`。
- 当多空模块使用相同的周期（默认设置）时，策略会复用同一份蜡烛订阅同时驱动两个计算器。
- 将 `LossTrigger` 设为 `0` 会立即启用缩减手数，与原始 MMRec 逻辑一致。
- 策略全部使用市价单，因此无需 MetaTrader 中的 `Deviation` 滑点参数。
