# Stochastic-Ausstiegsalarme-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet eine Long-Position, wenn die %K-Linie des Stochastic die %D-Linie im überverkauften Bereich von unten kreuzt, und eine Short-Position, wenn %K die %D-Linie im überkauften Bereich von oben kreuzt. Positionen werden durch festen Stop-Loss und Take-Profit in Ticks geschützt. Bei einem entgegengesetzten Crossover außerhalb der Extremzone wird die Position geschlossen, ohne umzukehren.

## Parameter
- `StochLength` – Hauptperiode des Stochastic-Oszillators.
- `KLength` – Glättungsperiode der %K-Linie.
- `DLength` – Glättungsperiode der %D-Linie.
- `StopLossTicks` – Stop-Loss-Abstand in Ticks.
- `TakeProfitTicks` – Take-Profit-Abstand in Ticks.
- `CandleType` – Kerzen-Zeitrahmen.
