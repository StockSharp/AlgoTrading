# TrendGuard Scalper SSL + Hama Candle 策略（含盘整区域）
[English](README.md) | [Русский](README_ru.md)

该策略结合 SSL 通道与 Hama 蜡烛趋势。当收盘价高于 SSL 均线、Hama 收盘价 (EMA 20) 高于 Hama 长期线 (EMA 100)，且价格保持在 Hama 收盘价之上时开多仓；反向条件开空仓。ATR 用于标记低波动的盘整区域。

## 细节
- **入场**：SSL 与 Hama 趋势一致，并得到价格确认。
- **出场**：固定的止盈和止损百分比。
- **指标**：SMA、EMA、ATR。
- **过滤**：盘整检测仅用于分析。
