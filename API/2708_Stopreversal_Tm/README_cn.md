# Stopreversal Tm 策略

## 概述
Stopreversal Tm 策略完整移植自 MetaTrader 5 专家顾问 `Exp_Stopreversal_Tm.mq5`。策略基于自定义指标 Stopreversal，它利用选定的价格类型计算跟踪止损，并在价格越过该止损时发出趋势反转信号。策略仅针对一个标的和一组 K 线数据运行，并保留了原策略可配置的交易时段过滤功能。

## 信号生成
Stopreversal 指标从所选价格模式（收盘价、开盘价、趋势跟随价、Demark 价等）取样，然后按照 `Sensitivity`（即 `nPips` 参数）调整动态止损。当新的价格高于止损且上一根 K 线仍低于止损时，产生做多信号；当新的价格跌破止损且上一根 K 线仍高于止损时，产生做空信号。做多信号会要求平掉现有空头并开多，做空信号则会平多并开空。

为了复现原 MQL5 程序的行为，可以通过 `Signal Bar Delay`（对应原输入 `SignalBar`）延迟若干根已完成的 K 线再执行信号，从而避免在尚未收盘的蜡烛上开仓或平仓。

## 交易时段与仓位处理
原专家顾问允许用户限制交易时段。本移植版本同样提供 `Use Time Filter` 开关，并通过 `Start Hour/Minute` 与 `End Hour/Minute` 设定交易窗口。若当前时间超出允许范围，策略会立即平掉净持仓。即便关闭时段过滤，信号驱动的平仓仍会生效。

策略按照净仓方式运作。每当方向发生改变时，先执行平仓再执行反向开仓，确保不会出现同时持有多空的情况。

## 参数说明
- **Allow Buy Entries / Allow Sell Entries** – 控制在收到多头或空头信号时是否允许开仓。
- **Allow Long Exits / Allow Short Exits** – 控制是否允许由相反信号触发的平仓动作。
- **Use Time Filter** – 打开或关闭交易时段限制。
- **Start Hour / Start Minute / End Hour / End Minute** – 定义交易时段的起止时间（开始时间包含，结束时间不包含），支持跨夜时段。
- **Sensitivity (`nPips`)** – 调整跟踪止损距离的比例系数，例如 `0.004` 表示 0.4%。
- **Signal Bar Delay (`SignalBar`)** – 指定在信号触发后需要等待的已完成 K 线数量，`0` 表示立即执行，`1` 为默认的上一根 K 线。
- **Candle Type** – 计算指标时使用的 K 线周期。
- **Applied Price** – 选择用于计算的价格类型（收盘价、均价、趋势跟随价、Demark 价等）。

## 实现细节
- 跟踪止损逻辑直接在策略内部实现，与原始 MQL5 指标保持一致，无需额外的缓冲区。
- 时段管理与信号执行顺序遵循原策略：先平仓后反向开仓。
- 转换版本使用 StockSharp 的高级 API（K 线订阅、信号延迟队列、`BuyMarket`/`SellMarket` 市价单）。MetaTrader 账号层面的资金管理在 StockSharp 中没有直接对应，因此未实现该部分。
