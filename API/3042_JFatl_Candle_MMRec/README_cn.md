# JFatl Candle MMRec 策略

该策略将 **Exp_JFatlCandle_MMRec.mq5** 的逻辑移植到 StockSharp 平台。
它监测经 JFatl 滤波后的蜡烛颜色变化，并利用 MMRec 风险模块在出现连续亏损时自动缩减手数。

## 交易理念

* 对原始 OHLC 数据应用快速自适应趋势线 (FATL) 滤波器构建合成蜡烛。
  策略采用原版的 39 个系数，并在其后增加一次指数平滑以近似 MT5 中的 Jurik 平滑效果。
* 根据滤波后的开收价判断蜡烛颜色：
  * **2** 表示看涨（收盘价高于开盘价）；
  * **0** 表示看跌（收盘价低于开盘价）；
  * **1** 表示中性。
* 当 `SignalBar + 1` 根之前的蜡烛为看涨颜色时，策略会先平掉所有空头，同时在 `SignalBar` 根之前的蜡烛不再保持看涨时准备做多。
* 当相同位置出现看跌颜色时，策略关闭多单，并在较近一根蜡烛不再看跌时允许做空。
* 多空仓位的手数由 MMRecounter 决定：如果最近 `TotalTrigger` 笔同方向交易中有不少于 `LossTrigger` 笔亏损，则使用缩减后的手数，否则使用常规手数。

## 参数

| 参数 | 说明 |
|------|------|
| `CandleType` | 进入 FATL 滤波器的蜡烛周期（默认 12 小时）。
| `SignalBar` | 读取颜色时回溯的完成蜡烛数量。`0` 表示使用当前已完成的蜡烛，`1` 与 MT5 默认值一致。
| `SmoothingLength` | FATL 输出之后的指数平滑长度。
| `NormalVolume` | 正常情况下的下单手数。
| `ReducedVolume` | 触发风险限制后使用的缩减手数。
| `BuyTotalTrigger` / `SellTotalTrigger` | MMRecounter 在计算手数时检查的历史交易数量。
| `BuyLossTrigger` / `SellLossTrigger` | 在上述窗口中需要出现的最少亏损笔数，达到后启用缩减手数。
| `EnableBuyEntries` / `EnableSellEntries` | 允许开多 / 开空。
| `EnableBuyExits` / `EnableSellExits` | 允许在出现反向信号时平多 / 平空。
| `StopLossPoints` | 基于价格步长的止损，`0` 表示关闭。
| `TakeProfitPoints` | 基于价格步长的止盈，`0` 表示关闭。

## 交易规则

1. 在累积不少于 39 根历史蜡烛后，计算滤波 OHLC 并确定颜色。
2. 令 `C1` 为 `SignalBar + 1` 根之前的颜色，`C0` 为 `SignalBar` 根之前的颜色（若 `SignalBar = 0`，则当前蜡烛视为 `C0`，上一根视为 `C1`）。
3. 若 `C1 == 2`：
   * 在 `EnableSellExits` 为真时平掉所有空头；
   * 在 `EnableBuyEntries` 为真且 `C0 != 2` 时按计算出的手数做多。
4. 若 `C1 == 0`：
   * 在 `EnableBuyExits` 为真时平掉所有多头；
   * 在 `EnableSellEntries` 为真且 `C0 != 0` 时做空。
5. 当蜡烛的最高/最低触及止损或止盈水平时，同样会提前平仓。

## 资金管理

策略分别记录每笔多单和空单的盈亏。准备开仓时，它会回看同方向最近 `TotalTrigger` 笔交易。
若其中亏损笔数不少于 `LossTrigger`，则使用 `ReducedVolume`；否则使用 `NormalVolume`。

## 注意事项

* 止损和止盈基于 `Security.PriceStep`。若标的未提供该值，则默认使用步长 `1`。
* FATL 滤波需要 39 根历史数据才能输出有效值，在此之前不会产生信号或交易。
* 为了保持效率，MMRecounter 的历史记录上限为 100 条，超过部分会自动丢弃最早的数据。
