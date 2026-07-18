# Binary Wave Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Binary Wave kombiniert mehrere klassische technische Indikatoren zu einer einzigen „binären" Welle. Jeder Indikator trägt je nach bullischem oder bärischem Zustand entweder +1 oder -1 bei. Die gewichtete Summe aller Signale bildet die endgültige Welle, die für Handelsentscheidungen verwendet wird.

## Parameter

- **Mode** – Einstiegsalgorithmus: `Breakdown` reagiert auf Null-Kreuzungen; `Twist` reagiert auf Richtungsänderungen der Welle.
- **Candle Type** – Zeitrahmen der Kerzen für alle Berechnungen.
- **Indicator Periods** – Längen für MA, MACD (schnell, langsam, Signal), CCI, Momentum, RSI und ADX.
- **Weights** – Beitrag jedes Indikators zur Welle. Ein Gewicht von 0 deaktiviert den Indikator.
- **Trading Permissions** – Long/Short-Einstiege und -Ausstiege separat aktivieren oder deaktivieren.
- **Risk** – Stop-Loss und Take-Profit in Prozent des Einstiegspreises.

## Funktionsweise

1. Die angegebene Kerzenserie abonnieren und alle Indikatoren berechnen.
2. Für jede abgeschlossene Kerze den Zustand jedes Indikators bewerten und in einen Binärwert (+1 / -1) umwandeln.
3. Gewichtete Werte summieren, um die aktuelle Welle zu erhalten.
4. Handelssignale generieren:
   - **Breakdown**: Long einsteigen, wenn die Welle über null kreuzt; Short einsteigen, wenn sie unter null kreuzt.
   - **Twist**: Long einsteigen, wenn die Welle die Richtung nach oben ändert; Short einsteigen, wenn sie nach unten dreht.
5. Optionaler Schutz-Stop-Loss und Take-Profit werden durch den integrierten Positionsschutz verwaltet.

Dieser Ansatz ermöglicht die flexible Kombination mehrerer Indikatoren bei gleichzeitig einfacher Handelslogik.
