# DSS Bressert-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Double Smoothed Stochastic (DSS) Bressert-Indikator. Es werden zwei Linien berechnet:

- **DSS-Linie** – Stochastik-Wert, zweimal mit exponentiellem gleitenden Durchschnitt geglättet.
- **MIT-Linie** – Zwischenwert nach der ersten Glättung.

Ein Trade wird eröffnet, wenn diese Linien kreuzen:

- Kaufen, wenn die DSS-Linie die MIT-Linie von oben nach unten kreuzt, nachdem sie darüber lag.
- Verkaufen, wenn die MIT-Linie die DSS-Linie von oben nach unten kreuzt, nachdem sie darüber lag.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `EmaPeriod` | EMA-Glättungsperiode (Standard: 8) |
| `StoPeriod` | Stochastik-Berechnungsperiode (Standard: 13) |
| `TakeProfitPercent` | Take-Profit-Prozentsatz für Schutzaufträge (Standard: 2) |
| `StopLossPercent` | Stop-Loss-Prozentsatz für Schutzaufträge (Standard: 1) |
| `CandleType` | Zeitrahmen für die Berechnungen (Standard: 4 Stunden) |

## Hinweise

- Die Strategie arbeitet nur auf geschlossenen Kerzen.
- Der Schutz verwendet prozentualen Stop-Loss und Take-Profit.
