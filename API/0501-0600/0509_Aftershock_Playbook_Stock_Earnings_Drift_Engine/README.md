# Aftershock Playbook Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The **Aftershock Playbook** strategy treats an unusually large one-candle price move as a proxy for an earnings surprise and follows the resulting drift. It uses market candles only and does not require an external earnings feed.

- **Signal**: On each finished `CandleType` candle, the close-to-close change is compared with ATR calculated over `AtrLength`.
- **Entry or reversal**: A rise greater than `ATR × SurpriseThreshold` opens or reverses to a long position; an equivalent fall opens or reverses to a short position.
- **Exit**: An adverse move greater than `ATR × AtrMultiplier` closes the current position. If the move also reaches the entry threshold, reversal takes priority.
- **Cooldown**: After an entry, reversal, or exit, all signals are skipped for `CooldownBars` finished candles.
