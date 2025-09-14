# Ergodic Ticks Volume OSMA 策略

## 概述
该策略是 MQL5 专家 "Exp_Ergodic_Ticks_Volume_OSMA" 在 StockSharp 平台上的改写。原始专家使用自定义指标来评估逐笔成交量动量，本版本使用 MACD 直方图来近似这个指标。

策略根据直方图的连续变化开仓或平仓：
- 连续两次上升触发做多，并关闭现有空头。
- 连续两次下降触发做空，并关闭现有多头。

在启动时调用 `StartProtection()` 以避免与现有仓位冲突。

## 参数
- `FastLength` – MACD 快速 EMA 周期，默认 12。
- `SlowLength` – MACD 慢速 EMA 周期，默认 26。
- `SignalLength` – MACD 信号 EMA 周期，默认 9。
- `CandleType` – 使用的蜡烛周期，默认 8 小时。

## 交易逻辑
1. 订阅所选 `CandleType` 的蜡烛。
2. 对每根完成的蜡烛计算 MACD 直方图。
3. 直方图连续上升两次时，平空并买入。
4. 直方图连续下降两次时，平多并卖出。
5. 每当出现新蜡烛时重复上述步骤。
