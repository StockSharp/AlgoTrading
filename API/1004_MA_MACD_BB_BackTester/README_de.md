# Strategie MA MACD BB BackTester
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die drei wählbare Indikatoren kombiniert: einfacher gleitender Durchschnitt-Crossover, MACD-Crossover oder Bollinger-Bänder-Ausbruch. Es ist jeweils nur ein Indikatormodus aktiv, und die Handelsrichtung kann Long oder Short sein.

## Parameter
- `CandleType` — Kerzen-Zeitrahmen.
- `Indicator` — zu verwendender Indikator (MA, MACD, BB).
- `Direction` — Handelsrichtung (Long oder Short).
- `MaLength` — Periode des gleitenden Durchschnitts.
- `FastLength` — Länge der schnellen EMA für MACD.
- `SlowLength` — Länge der langsamen EMA für MACD.
- `SignalLength` — Länge der MACD-Signallinie.
- `BbLength` — Periode der Bollinger-Bänder.
- `BbMultiplier` — Multiplikator der Bollinger-Bänder.
- `StartDate` — Startdatum.
- `EndDate` — Enddatum.
