# T3MA Alarm Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert die Idee des T3MA-ALARM-Indikators. Sie wendet einen doppelt geglätteten exponentiellen gleitenden Durchschnitt an, um Änderungen der Trendrichtung zu erkennen.

Wenn der geglättete gleitende Durchschnitt aufwärts dreht, wird eine Long-Position eröffnet. Wenn er abwärts dreht, wird eine Short-Position eröffnet. Optional kann ein entgegengesetztes Signal die aktuelle Position schließen. Stop-Loss- und Take-Profit-Level werden als absolute Preisabstände vom Einstiegspreis gesetzt.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `MaPeriod` | Periode des exponentiellen gleitenden Durchschnitts. |
| `MaShift` | Anzahl der Bars zur Erkennung der Richtungsänderung. |
| `StopLoss` | Preisabstand für den Schutz-Stop-Loss. `0` zum Deaktivieren. |
| `TakeProfit` | Preisabstand für den Take-Profit. `0` zum Deaktivieren. |
| `ReverseOnSignal` | Gegenläufige Position schließen, wenn ein neues Signal erscheint. |
| `CandleType` | Kerzentyp für die Berechnungen. |

## Signale

* **Kauf** – die Richtung der geglätteten MA wechselt von abwärts zu aufwärts.
* **Verkauf** – die Richtung der geglätteten MA wechselt von aufwärts zu abwärts.

Positionen werden entweder durch ein entgegengesetztes Signal (wenn aktiviert) oder beim Erreichen von Stop-Loss-/Take-Profit-Levels geschlossen.
