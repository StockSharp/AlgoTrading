# Aftershock Playbook 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

**Aftershock Playbook** 策略基于每股收益 (EPS) 惊喜进行财报后的趋势交易。

- **入场**：财报发布时，当惊喜 ≥ `PositiveSurprise` 做多，当惊喜 ≤ `NegativeSurprise` 做空。`ReverseSignals` 可反转信号。
- **止损**：对空头使用 ATR 止损（`AtrLength`、`AtrMultiplier`）。
- **离场**：若启用 `UseTimeExit`，持仓 `HoldDays` 天后平仓。
- **再入场**：盈利平仓后将在同方向再入场一次。亏损交易则等待下一次财报。

*需要外部财报数据源。*
