# Exp Iin MA Signal MMRec 策略
[English](README.md) | [Русский](README_ru.md)

这是 MetaTrader 专家顾问 “Exp_Iin_MA_Signal_MMRec” 的 StockSharp 版本。策略使用一对可配置的移动平均线（源自 Iin_MA_Signal 指标）的交叉信号，并结合基于亏损次数的动态仓位管理来调整交易手数。

## 概览

- **信号生成**：在选定的 K 线类型和价格来源上计算快慢移动平均线。当快线向上穿越慢线时产生做多信号，反之则产生做空信号。参数 `SignalBar` 会将执行延迟若干根已完成的 K 线，以复制原始 MQL 指标缓冲区的滞后。
- **持仓管理**：`BuyPosOpen` 和 `SellPosOpen` 用于启用或禁用多/空入场。当出现相反信号并且对应的 `BuyPosClose` 或 `SellPosClose` 为真时，策略会平掉当前仓位，或直接反向开仓。
- **风险控制**：`StopLossPoints` 与 `TakeProfitPoints` 通过 `Security.PriceStep` 转换为价格距离，并在处理新信号前与当前 K 线的最高价/最低价进行比较。
- **资金管理**：分别跟踪多头和空头最近的交易结果。当在 `BuyTotalTrigger`/`SellTotalTrigger` 范围内的亏损次数达到阈值时，策略会将下次下单的数量从 `NormalVolume` 切换为 `ReducedVolume`。`MoneyMode` 用于指定数量的解释方式（固定手数、账户百分比或基于止损距离的风险百分比）。

## 参数

- `FastPeriod`, `SlowPeriod` – 快慢均线的周期。
- `FastType`, `SlowType` – 均线类型（`Simple`, `Exponential`, `Smoothed`, `Weighted`, `VolumeWeighted`）。
- `FastPrice`, `SlowPrice` – 均线使用的价格（`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`）。
- `SignalBar` – 从信号产生到下单执行之间要等待的已完成 K 线数量。
- `BuyPosOpen`, `SellPosOpen` – 是否允许开多/开空。
- `BuyPosClose`, `SellPosClose` – 是否在出现反向信号时关闭或反手现有仓位。
- `BuyTotalTrigger`, `SellTotalTrigger` – 用于统计亏损次数的历史交易数量。
- `BuyLossTrigger`, `SellLossTrigger` – 在统计窗口内触发降档手数所需的最少亏损次数。
- `NormalVolume`, `ReducedVolume` – 常规手数与降档手数（或风险系数，取决于 `MoneyMode`）。
- `StopLossPoints`, `TakeProfitPoints` – 以点数表示的止损和止盈距离。
- `MoneyMode` – 手数解释方式（`Lot`, `Balance`, `FreeMargin`, `BalanceRisk`, `FreeMarginRisk`）。百分比模式使用 `Portfolio.CurrentValue`，风险模式则用止损距离换算。
- `CandleType` – 用于指标计算的 K 线数据类型。

## 信号逻辑

1. 每根收盘完成的 K 线都会把选定的价格输入快慢均线。
2. 通过比较当前值与前一根 K 线的数值判断是否产生交叉。
3. 信号存入队列，当队列长度大于 `SignalBar` 时执行最早的信号。
4. 当执行多头信号时：
   - 如果当前持有空头并且 `SellPosClose` 启用，策略会记录该空头的盈亏，然后在 `BuyPosOpen` 启用时反手做多，否则直接平仓。
   - 如果没有持仓且 `BuyPosOpen` 启用，则按计算出的数量建立新的多头。
5. 空头信号的处理流程与多头对称。

## 资金管理细节

- 多头与空头的交易结果分别保存在 FIFO 队列中，长度受 `BuyTotalTrigger` / `SellTotalTrigger` 限制。
- 每笔亏损交易（负的 PnL）都会增加亏损计数，达到 `BuyLossTrigger` 或 `SellLossTrigger` 后，下次交易会改用 `ReducedVolume`。
- `MoneyMode = Lot` 表示直接使用配置的手数。
- `MoneyMode = Balance` 与 `FreeMargin` 将配置值乘以 `Portfolio.CurrentValue`，再除以当前收盘价以得到数量。
- `MoneyMode = BalanceRisk` 与 `FreeMarginRisk` 将配置值乘以 `Portfolio.CurrentValue`，再除以止损距离；若止损为零则退化为余额百分比计算。
- 当无法取得投资组合数据时，计算结果为零以避免意外下单。

## 风险处理

- 在每根 K 线中根据入场价与点值重新计算止损/止盈，如果价格触及这些水平，则在处理新信号前立即平仓。
- 平仓行为都会记录盈亏，保证资金管理队列与真实历史保持一致。

## 注意事项

- 请确保 `StopLossPoints` 与 `TakeProfitPoints` 与交易品种的最小跳动价位相匹配，策略会乘以 `Security.PriceStep`。
- 若选择依赖账户余额的模式，需要 `Portfolio` 对象提供 `CurrentValue`。
- 策略按净仓模式运行，不支持同时持有多头和空头。
