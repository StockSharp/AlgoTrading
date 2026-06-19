# Histo Scalper 策略

## 概述
**Histo Scalper Strategy** 是将 *HistoScalperEA v1.0* 移植到 StockSharp 的版本。策略同时结合 ADX、ATR、布林带、Bulls/Bears Power、CCI、MACD、RSI 与随机指标八个直方图过滤器。只有在所有启用的过滤器统一给出同方向信号，并且至少有一个过滤器在上一根 K 线上给出相反信号时，才会开仓，从而保留原始 EA 的“双 K 线确认”逻辑。

## 信号生成
1. **ADX**：比较 +DI 与 −DI，可根据需要反向解释。
2. **ATR**：将当前 ATR 与 SMA 基线比较，计算百分比偏离度。多头需要大于 `AtrPositiveThreshold`，空头需要低于 `AtrNegativeThreshold`。
3. **布林带**：收盘价突破上轨或下轨时产生信号。
4. **Bulls/Bears Power**：多头使用 Bulls Power，空头使用 Bears Power 的绝对值。
5. **CCI**：价格进入超买或超卖区域时触发。
6. **MACD**：监控 MACD 主线与信号线之间的差值（直方图）。
7. **RSI**：使用经典超买/超卖水平。
8. **随机指标**：比较 %K 线与自定义上下限。

若某个启用的过滤器返回中性值，则当根 K 线不进行交易。策略会缓存前一根 K 线的信号，以确保满足“上一根方向相反”的限制。

## 风险管理
* 交易手数由 `TradeVolume` 控制。
* `AllowPyramiding` 允许在已有仓位的方向上继续加仓；否则信号翻转时直接反手。
* `TakeProfitPoints` 与 `StopLossPoints` 以价格步长为单位，在下单后通过 `SetTakeProfit` 和 `SetStopLoss` 立即设置。
* `UseTimeFilter`、`SessionStart`、`SessionEnd` 可限制每日的交易时间段。

## 参数
| 参数 | 说明 |
|------|------|
| `TradeVolume` | 每次开仓的基础手数。
| `AllowPyramiding` | 是否允许在同方向加仓。
| `CloseOnOppositeSignal` | 综合信号反向时是否立即平仓。
| `UseTimeFilter`, `SessionStart`, `SessionEnd` | 日内交易时间窗口。
| `UseTakeProfit`, `TakeProfitPoints` | 启用并设置止盈（价格步长）。
| `UseStopLoss`, `StopLossPoints` | 启用并设置止损（价格步长）。
| `UseIndicator1` … `UseIndicator8` | 启用具体过滤器。
| `ModeIndicatorX` | 指定过滤器采用正向或反向逻辑。
| 其他指标参数 | 对应原始 EA 的周期、阈值等设置。

## 与 MQL 版本的差异
* 未实现篮子平仓、声音提示以及网格开仓功能。
* 未移植自动手数、保本与跟踪止损逻辑，请使用固定止盈止损控制风险。
* 未包含点差检查和经纪商特定保护措施。

## 使用建议
1. 启动前设置好 `Security` 与 `Portfolio`。
2. 根据目标周期调整 `CandleType`。
3. 结合市场波动调整各指标阈值。
4. 优化时可以暂时关闭部分过滤器以降低维度。
5. 利用 `AllowPyramiding` 与 `CloseOnOppositeSignal` 控制快速行情下的仓位风险。
