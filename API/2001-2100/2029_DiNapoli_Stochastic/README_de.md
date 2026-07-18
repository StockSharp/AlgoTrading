# DiNapoli Stochastic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert ein Handelssystem, das auf dem **DiNapoli Stochastic**-Oszillator basiert. Sie reagiert auf die Kreuzungen zwischen den %K- und %D-Linien des Stochastic-Indikators.

## Strategie-Logik

1. Abonnieren von Kerzen des gewählten Zeitrahmens.
2. Berechnung der DiNapoli-Stochastic-Werte mit dem Standard-Stochastic-Oszillator und Glättungsperioden.
3. Short-Positionen schließen, wenn der vorherige %K über %D lag.
4. Long-Positionen schließen, wenn der vorherige %K unter %D lag.
5. Eine Long-Position öffnen, wenn %K %D von oben nach unten kreuzt und Long-Trades erlaubt sind.
6. Eine Short-Position öffnen, wenn %K %D von unten nach oben kreuzt und Short-Trades erlaubt sind.

## Parameter

- `FastK` – Basisperiode für %K.
- `SlowK` – Glättungsperiode für %K.
- `SlowD` – Glättungsperiode für %D.
- `BuyOpen` – Long-Einstiege aktivieren oder deaktivieren.
- `SellOpen` – Short-Einstiege aktivieren oder deaktivieren.
- `BuyClose` – Schließen von Long-Positionen aktivieren oder deaktivieren.
- `SellClose` – Schließen von Short-Positionen aktivieren oder deaktivieren.
- `CandleType` – Kerzen-Zeitrahmen für Berechnungen.

## Hinweise

Die Strategie verwendet die High-Level-API von StockSharp und verarbeitet nur abgeschlossene Kerzen. Indikatorwerte werden über `BindEx` bezogen, ohne historische Werte abzufragen.
