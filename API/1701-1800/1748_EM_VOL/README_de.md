# EM VOL Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche rund um Pivot-basierte Unterstützungs- und Widerstandsniveaus.
Sie berechnet das gestrige Hoch und Tief plus einen ATR-Puffer, um Einstiegssignale zu bilden.
Trades werden nur platziert, wenn der ADX-Indikator ein Niedrig-Volatilitäts-Umfeld signalisiert.

## Logik

1. Berechnen Sie den Pivot der vorherigen Kerze und addieren/subtrahieren Sie ATR, um Widerstand und Unterstützung zu erhalten.
2. Wenn ADX unter dem Schwellenwert liegt und der Preis über dem Widerstand schließt, eine Long-Position eröffnen.
3. Wenn der Preis unter der Unterstützung schließt, eine Short-Position eröffnen.
4. Nach dem Einstieg werden Schutz-Stop und Take-Profit-Orders platziert.
5. Ein Trailing Stop kann den Schutz-Stop anziehen, sobald der Gewinn das angegebene Niveau erreicht.

## Parameter

- `TakeProfit` — Take-Profit-Abstand in Preisschritten.
- `StopLoss` — Stop-Loss-Abstand in Preisschritten.
- `AtrPeriod` — ATR-Indikatorperiode.
- `AdxPeriod` — ADX-Indikatorperiode.
- `AdxThreshold` — maximaler ADX-Wert zum Zulassen von Trades.
- `TrailStart` — erforderlicher Gewinn, bevor der Trailing Stop beginnt.
- `TrailStep` — Abstand des Trailing Stops.
- `CandleType` — Zeitrahmen für Berechnungen.

## Verwendete Indikatoren

- Average True Range
- Average Directional Index
