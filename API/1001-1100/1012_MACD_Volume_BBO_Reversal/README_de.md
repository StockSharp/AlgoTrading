# MACD Volumen BBO Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die einen Volumenoszillator mit MACD-Nulllinienkreuzungen und Signallinienvergleich kombiniert.
Einstieg Long, wenn MACD die Nulllinie nach oben kreuzt, der Volumenoszillator positiv ist und MACD über seiner Signallinie liegt.
Short-Einstiege sind symmetrisch. Stop-Loss verwendet den jüngsten Tief-/Hochpunkt und Take-Profit basiert auf dem Risiko-Ertrags-Verhältnis.

## Parameter
- `VolumeShortLength` – kurze EMA-Periode für Volumen (Standard: 6)
- `VolumeLongLength` – lange EMA-Periode für Volumen (Standard: 12)
- `MacdFastLength` – schnelle MA-Periode für MACD (Standard: 11)
- `MacdSlowLength` – langsame MA-Periode für MACD (Standard: 21)
- `MacdSignalLength` – Signallinie-Periode für MACD (Standard: 10)
- `LookbackPeriod` – Kerzen zur Berechnung des jüngsten Hoch/Tiefs (Standard: 10)
- `RiskReward` – Take-Profit zu Stop-Loss Verhältnis (Standard: 1.5)
- `CandleType` – Zeitrahmen für Kerzen (Standard: 5 Minuten)
