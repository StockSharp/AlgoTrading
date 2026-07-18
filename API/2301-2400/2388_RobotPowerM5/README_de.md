# RobotPower M5-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert die Bulls Power- und Bears Power-Indikatoren auf einem 5-Minuten-Chart.
Sie eröffnet Positionen, wenn der kombinierte Schwung von Bullen und Bären die Nulllinie kreuzt, und verwaltet Ausstiege mit festen Zielen und einem Trailing Stop.

## Funktionsweise
- **Indikatoren**: Bulls Power und Bears Power mit einer gemeinsamen Periode `BullBearPeriod`.
- **Zeitrahmen**: 5-Minuten-Kerzen standardmäßig (`CandleType`).

### Einstiegsregeln
- **Long-Einstieg**: Wenn `BullsPower + BearsPower > 0` und keine Position offen ist, zum Marktpreis kaufen.
- **Short-Einstieg**: Wenn `BullsPower + BearsPower < 0` und keine Position offen ist, zum Marktpreis verkaufen.

### Ausstiegsregeln
- **Take Profit**: Position schließen, wenn sich der Preis `TakeProfit` Einheiten in Handelsrichtung bewegt.
- **Stop Loss**: Position schließen, wenn sich der Preis `StopLoss` Einheiten gegen die Position bewegt.
- **Trailing Stop**: Nach dem Einstieg verfolgt der Stop Loss den Preis um `TrailingStep`, sobald der Preis mehr als das Doppelte dieser Distanz vorrückt.

### Parameter
- `BullBearPeriod` – Periode für die Berechnungen von Bulls Power und Bears Power.
- `TrailingStep` – Schrittgröße beim Anpassen des Trailing Stops.
- `TakeProfit` – Abstand vom Einstieg zum Take-Profit-Level.
- `StopLoss` – Abstand vom Einstieg zum Stop-Loss-Level.
- `CandleType` – Kerzen-Zeitrahmen für die Signalberechnung.

### Positionsgröße
Verwendet die `Volume`-Eigenschaft der Strategie für die Ordergröße.

## Hinweise
Für Lernzwecke konzipiert und dient als Beispiel für die Konvertierung einer MQL-Strategie in die StockSharp-API.
