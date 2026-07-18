# Heiken Ashi Smoothed Trend 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略使用 EMA 平滑的 Heiken-Ashi 蜡烛来捕捉趋势反转。当红色转为绿色时做多并平掉空头；绿色转为红色时做空并平掉多头。

- **指标**: Heikin-Ashi（EMA 平滑）
- **入场规则**:
  - 蜡烛转为看涨时开多。
  - 蜡烛转为看跌时开空。
- **出场规则**:
  - 出现相反信号时反向持仓。
- **参数**:
  - `EmaLength` – EMA 平滑周期。
  - `CandleType` – 蜡烛时间框架。

算法在每根完成的蜡烛上计算平滑后的开盘和收盘，并据此调整持仓。
