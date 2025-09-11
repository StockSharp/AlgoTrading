# SJ NIFTY 策略
[English](README.md) | [Русский](README_ru.md)

该趋势策略结合 SuperTrend、VWAP、RSI 和 EMA200。Keltner 通道基线可作为可选趋势过滤器。仓位大小根据账户风险百分比计算，并设置技术止损和风险回报比的止盈。

## 细节

- **入场条件**：
  - **做多**：收盘价 > SuperTrend 且 > VWAP，RSI 高于超买阈值，收盘价 > EMA200，通过 Keltner 基线过滤，且收盘价突破前一根K线高点。
  - **做空**：收盘价 < SuperTrend 且 < VWAP，RSI 低于超卖阈值，收盘价 < EMA200，通过 Keltner 基线过滤，且收盘价跌破前一根K线低点。
- **出场条件**：根据止损或风险回报比的止盈退出。
- **仓位规模**：账户风险百分比除以止损距离，并按合约大小取整。
- **指标**：SuperTrend、VWAP、RSI、EMA、Keltner 通道。
