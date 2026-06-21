# Color LeMan Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein Port des ursprünglichen MQL5-Expertenberaters *ColorLeManTrend*. Sie verwendet einen benutzerdefinierten Trendindikator auf Basis von Hochs und Tiefs, um die Marktrichtung zu identifizieren.

## Idee

Der Indikator berechnet bullische und bärische Linien anhand extremer Hoch- und Tiefstwerte über drei verschiedene Rückblickperioden. Exponentielle gleitende Durchschnitte glätten diese Werte. Handelsentscheidungen basieren auf Kreuzungen der bullischen und bärischen Linien:

- Wenn die vorherige bullische Linie über der bärischen Linie lag und die aktuelle bullische Linie unter die bärische Linie fällt, wird ein **Kauf**-Signal generiert.
- Wenn die vorherige bullische Linie unter der bärischen Linie lag und die aktuelle bullische Linie über die bärische Linie steigt, wird ein **Verkauf**-Signal generiert.
- Optionale Flags steuern, ob Long- oder Short-Positionen eröffnet oder geschlossen werden dürfen.

## Parameter

- `CandleType` – Zeitrahmen für Indikatorberechnungen.
- `Min` – Periode für die kürzeste Extremwertberechnung.
- `Midle` – Periode für die mittlere Extremwertberechnung.
- `Max` – Periode für die längste Extremwertberechnung.
- `PeriodEma` – Glättungsperiode für bullische und bärische Linien.
- `StopLossPoints` – Schutz-Stop in Punkten.
- `TakeProfitPoints` – Take-Profit in Punkten.
- `AllowBuy` – Long-Einstiege aktivieren.
- `AllowSell` – Short-Einstiege aktivieren.
- `AllowBuyClose` – Schließen von Long-Positionen erlauben.
- `AllowSellClose` – Schließen von Short-Positionen erlauben.
- `Volume` – Handelsvolumen pro Order.

## Hinweise

Die Strategie verarbeitet nur abgeschlossene Kerzen und verwendet Marktorders für alle Operationen. Stop-Loss- und Take-Profit-Werte werden über den integrierten Positionsschutz angewendet.
