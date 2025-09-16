# SilverTrend CrazyChart 策略
[English](README.md) | [Русский](README_ru.md)

本策略将 MetaTrader 专家顾问“Exp_SilverTrend_CrazyChart”移植到 StockSharp 平台。策略同时监控 SilverTrend CrazyChart 指
标的两个缓冲区：当前通道与延迟通道。当延迟通道跨越当前通道时，系统顺势开仓，并平掉反向持仓。

## 细节

- **入场条件**：
  - **做多**：在上一根用于信号的已完成 K 线上，当前带位于延迟带之上；而在评估的 K 线上当前带跌破或触及延迟带。通过 `AllowBuyEntry` 可关闭多头开仓。
  - **做空**：在上一根信号 K 线上当前带位于延迟带之下；在评估的 K 线上当前带上穿或触及延迟带。通过 `AllowSellEntry` 可关闭空头开仓。
- **方向**：支持做多与做空。
- **离场条件**：
  - 多头在延迟带重新高于当前带时平仓（`AllowBuyExit` 控制），或在触发止损/止盈时退出。
  - 空头在当前带重新高于延迟带时平仓（`AllowSellExit` 控制），或在触发止损/止盈时退出。
- **止损/止盈**：`StopLossPoints` 与 `TakeProfitPoints` 以绝对价格距离定义。当数值为 0 时忽略相应限制。
- **过滤器**：
  - `SignalBar` 指定回溯的已完成 K 线数量，用于判断交叉。
  - `CandleType` 决定用于全部计算的时间框架。

## 参数

- `CandleType` – 指标使用的 K 线类型（默认：1 小时）。
- `Length` – SilverTrend CrazyChart 指标的摆动周期（原脚本的 `SSP`）。
- `KMin` – 控制延迟带偏移的系数。
- `KMax` – 控制当前带偏移的系数。
- `SignalBar` – 向后查看的已完成 K 线数量，用于生成信号。
- `AllowBuyEntry` / `AllowSellEntry` – 是否允许开多/开空。
- `AllowBuyExit` / `AllowSellExit` – 是否允许平多/平空。
- `StopLossPoints` – 多头止损与空头止盈的绝对价格距离。
- `TakeProfitPoints` – 多头止盈与空头止损的绝对价格距离。
- `Volume` – 策略基准下单手数。

## 指标逻辑

自定义的 `SilverTrendCrazyChartIndicator` 按原始 MQL 实现两个缓冲区：

- `Length`、`KMin` 与 `KMax` 根据最近高低点计算通道边界。
- “当前带”对应 MetaTrader 的 0 号缓冲区，直接响应最新完成的 K 线。
- “延迟带”对应 1 号缓冲区，将当前带向后移动 `Length + 1` 根 K 线以匹配原始绘图。

当延迟带从下向上穿越当前带时产生买入信号，反向穿越产生卖出信号。`SignalBar` 参数确保只使用已经收盘的 K 线，与原始专家顾问的行为一致。
