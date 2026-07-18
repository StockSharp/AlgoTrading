# Aftershock Playbook-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Aftershock Playbook**-Strategie handelt Post-Earnings-Drift basierend auf EPS-Überraschungen.

- **Einstieg**: Bei einer Gewinnveröffentlichung Long gehen, wenn die Überraschung ≥ `PositiveSurprise`, oder Short, wenn die Überraschung ≤ `NegativeSurprise`. Signale können mit `ReverseSignals` umgekehrt werden.
- **Stop**: Optionaler ATR-Stop (`AtrLength`, `AtrMultiplier`) für Short-Positionen.
- **Ausstieg**: Optional Positionen nach `HoldDays` Kalendertagen schließen (`UseTimeExit`).
- **Wiedereinstieg**: Nach einem profitablen Ausstieg steigt die Strategie einmal in dieselbe Richtung wieder ein. Verlusttrades blockieren neue Einstiege bis zur nächsten Gewinnveröffentlichung.

*Eine externe Gewinndate-Datenquelle ist erforderlich.*
