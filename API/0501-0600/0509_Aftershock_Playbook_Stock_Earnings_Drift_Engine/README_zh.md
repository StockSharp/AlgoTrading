# Aftershock Playbook 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

**Aftershock Playbook** 策略将单根 K 线中的异常大幅价格变动视为财报意外的代理信号，并跟随随后产生的漂移。策略仅使用市场 K 线，不需要外部财报数据源。

- **信号**：在每根已完成的 `CandleType` K 线上，将相邻收盘价的变化与按 `AtrLength` 计算的 ATR 进行比较。
- **入场或反转**：上涨幅度超过 `ATR × SurpriseThreshold` 时开多仓或反转为多仓；同等幅度的下跌则开空仓或反转为空仓。
- **离场**：不利方向的变动超过 `ATR × AtrMultiplier` 时平掉当前仓位。如果该变动也达到入场阈值，则优先反转仓位。
- **冷却**：入场、反转或离场后，在接下来的 `CooldownBars` 根已完成 K 线内跳过所有信号。
