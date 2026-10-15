# RSI EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie emuliert einen klassischen RSI-Expert-Advisor. Sie handelt, wenn der Relative Strength Index vordefinierte Niveaus kreuzt, und verwaltet das Risiko mit Stop-Loss, Take-Profit und optionalem Trailing Stop.

## Strategielogik
- Berechnet den RSI anhand des konfigurierbaren Parameters `RsiPeriod`.
- **Long-Einstieg**, wenn der RSI über `BuyLevel` steigt und keine Long-Position besteht.
- **Short-Einstieg**, wenn der RSI unter `SellLevel` fällt und keine Short-Position besteht.
- Wenn `CloseBySignal` aktiviert ist, schließt ein entgegengesetzter Kreuzung die bestehende Position.
- Positionen können mit `StopLoss`, `TakeProfit` und `TrailingStop` in Preiseinheiten abgesichert werden.
- Funktioniert mit Kerzendaten, die durch `CandleType` definiert sind.

## Parameter
- `OpenBuy` – Long-Einstiege aktivieren.
- `OpenSell` – Short-Einstiege aktivieren.
- `CloseBySignal` – Schließen durch entgegengesetztes RSI-Signal.
- `StopLoss` – Verlust in Preiseinheiten.
- `TakeProfit` – Gewinn in Preiseinheiten.
- `TrailingStop` – Trailing-Abstand in Preiseinheiten.
- `RsiPeriod` – RSI-Berechnungslänge.
- `BuyLevel` – RSI-Schwelle für Long-Signale.
- `SellLevel` – RSI-Schwelle für Short-Signale.
- `CandleType` – Kerzen-Zeitrahmen oder -Typ zum Abonnieren.

Das Standard-Handelsvolumen wird über die Eigenschaft `Volume` der Strategie gesteuert.
