# Derivative-Nulldurchgang-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf der Grundlage des Vorzeichenwechsels der Preisableitung. Die Ableitung wird als Preismomentum geteilt durch die Periode und multipliziert mit 100 berechnet. Wenn die Ableitung die Nulllinie kreuzt, wird die aktuelle Position geschlossen und die entgegengesetzte Position eröffnet.

## Parameter

- `DerivativePeriod` - Glättungsperiode für die Ableitungsberechnung.
- `PriceType` - Quellpreis für die Ableitung.
- `BuyEntry` - Eröffnung von Long-Positionen erlauben.
- `SellEntry` - Eröffnung von Short-Positionen erlauben.
- `BuyExit` - Schließen von Long-Positionen erlauben.
- `SellExit` - Schließen von Short-Positionen erlauben.
- `StopLoss` - Stop-Loss in Punkten.
- `TakeProfit` - Take-Profit in Punkten.
- `CandleType` - Kerzen-Zeitrahmen.

## Logik

1. Kerzen abonnieren und Momentum des ausgewählten Preises berechnen.
2. Die Ableitung wird durch Division des Momentums durch die Periode und Skalierung mit 100 ermittelt.
3. Wenn die Ableitung von positiv auf nicht-positiv wechselt, wird eine Long-Position eröffnet und die Short-Position geschlossen.
4. Wenn die Ableitung von negativ auf nicht-negativ wechselt, wird eine Short-Position eröffnet und die Long-Position geschlossen.
5. Stop-Loss und Take-Profit werden zur Risikoverwaltung eingesetzt.
