# Forex Fraus Slogger-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert das Envelope-Reversal-System aus MetaTrader.

## Logik

- Berechnet einen 1-Perioden-SMA als Basispreis.
- Die obere und untere Hüllkurve werden auf `EnvelopePercent` Prozent vom Basispreis gesetzt.
- Wenn der Preis über dem oberen Band schließt und dann darunter zurückkehrt, wird eine Short-Position eröffnet.
- Wenn der Preis unter dem unteren Band schließt und dann darüber zurückkehrt, wird eine Long-Position eröffnet.
- Positionen werden durch einen Trailing-Stop geschützt.

## Parameter

- `EnvelopePercent` – prozentualer Versatz für die Hüllkurven (Standard 0.1).
- `TrailingStop` – Trailing-Stop-Abstand in Preiseinheiten (Standard 0.001).
- `TrailingStep` – minimale Preisbewegung zum Vorrücken des Trailing-Stops (Standard 0.0001).
- `ProfitTrailing` – Trailing nur aktivieren, wenn die Position profitabel ist.
- `UseTimeFilter` – nur während der angegebenen Stunden handeln.
- `StartHour` – Beginn des Handelsfensters.
- `StopHour` – Ende des Handelsfensters.
- `CandleType` – Kerzen-Zeitrahmen für die Berechnungen.

## Hinweise

- Die Strategie verwendet Marktaufträge über `BuyMarket` und `SellMarket`.
- Der Trailing-Stop schließt die Position, wenn der Preis das Stop-Niveau kreuzt.
