# MTF RSI SAR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert **Relative Strength Index (RSI)**-Werte über vier Zeitrahmen, **Parabolic SAR** und **Bollinger Bänder**, um Trendfortsetzungen nach kurzen Rücksetzern zu erfassen. Signale werden auf 5‑Minuten-Kerzen generiert, während höhere Zeitrahmen als Bestätigungsfilter dienen.

## Konzept

1. **RSI-Filter** – RSI-Werte auf 5, 15, 30 und 60 Minuten müssen alle über 50 für Long-Einstiege oder unter 50 für Short-Einstiege liegen. Diese Multi-Timeframe-Bestätigung zielt darauf ab, Trades mit dem übergeordneten Trend auszurichten.
2. **Parabolic SAR-Filter** – Parabolic SAR-Werte auf 5, 15 und 30 Minuten müssen für Longs unter der aktuellen Kerze liegen oder für Shorts darüber. Dies stellt sicher, dass der Preis in die gewünschte Richtung tendiert.
3. **Bollinger Band-Trigger** – Im 5-Minuten-Chart muss das Kerzenschluss das obere Band für Longs oder das untere Band für Shorts durchbrechen. Bollinger Bänder liefern einen Überkauft-/Überverkauft-Trigger.
4. **Ein- und Ausstieg** – Eine Long-Position wird eröffnet, wenn alle aktiven Filter nach oben zeigen. Eine Short-Position wird eröffnet, wenn alle aktiven Filter nach unten zeigen. Das entgegengesetzte Signal schließt eine offene Position.

Jeder der drei Filter kann über Parameter einzeln deaktiviert werden, sodass die Strategie nur mit RSI, nur mit Bollinger Bändern, nur mit SAR oder einer beliebigen Kombination betrieben werden kann.

## Parameter

- `UseRsi` – RSI-Filter aktivieren (Standard: true).
- `UseBollinger` – Bollinger Band-Trigger aktivieren (Standard: true).
- `UseSar` – Parabolic SAR-Filter aktivieren (Standard: true).
- `RsiPeriod` – RSI-Berechnungsperiode (Standard: 14).
- `BollingerPeriod` – Anzahl der Balken für Bollinger Bänder (Standard: 20).
- `BollingerWidth` – Breite (Standardabweichungsmultiplikator) für Bollinger Bänder (Standard: 2).
- `SarStep` – Beschleunigungsfaktor für Parabolic SAR (Standard: 0.02).
- `SarMax` – Maximaler Beschleunigungsfaktor für Parabolic SAR (Standard: 0.2).
- `CandleType` – Basis-Kerzen-Zeitrahmen, standardmäßig 5 Minuten.

## Handelsregeln

- **Long**: Alle aktivierten Filter liefern bullische Signale.
- **Short**: Alle aktivierten Filter liefern bärische Signale.
- **Ausstieg**: Das entgegengesetzte Signal schließt die Position.

## Hinweise

- Die Strategie operiert auf einem Wertpapier mit vier Kerzen-Abonnements: 5, 15, 30 und 60 Minuten Zeitrahmen.
- Konzipiert als Lehrbeispiel für Multi-Timeframe-Bestätigung mit der High-Level-API von StockSharp.
- Es gibt keine festen Stop-Loss- oder Gewinnziele; Risikomanagement sollte bei Bedarf extern hinzugefügt werden.
