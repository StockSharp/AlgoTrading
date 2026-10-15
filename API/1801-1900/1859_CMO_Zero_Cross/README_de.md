# CMO Nulllinien-Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis von Nulllinienkreuzungen des Chande Momentum Oscillators (CMO).
Wenn der Oszillator die Nulllinie nach unten kreuzt, wird eine Long-Position eröffnet. Wenn er nach
oben kreuzt, wird eine Short-Position eröffnet. Optionale Stop-Loss- und Take-Profit-Niveaus (in Punkten)
schützen die Position. Einstiege und Ausstiege für Long- und Short-Trades können einzeln
aktiviert oder deaktiviert werden.

## Parameter

- `Volume` – Auftragsvolumen.
- `CmoPeriod` – Periode für den CMO-Indikator.
- `StopLoss` – Stop-Loss in Punkten.
- `TakeProfit` – Take-Profit in Punkten.
- `AllowLongEntry` – Long-Positionseröffnung erlauben.
- `AllowShortEntry` – Short-Positionseröffnung erlauben.
- `AllowLongExit` – Long-Positionsschließung bei entgegengesetztem Signal erlauben.
- `AllowShortExit` – Short-Positionsschließung bei entgegengesetztem Signal erlauben.
- `CandleType` – Zeitrahmen für Berechnungen.

## Handelslogik

1. Kerzen des gewählten Zeitrahmens abonnieren und den CMO berechnen.
2. Wenn der CMO von oben nach unten die Nulllinie kreuzt:
   - Short-Positionen schließen, falls erlaubt.
   - Eine Long-Position eröffnen, falls erlaubt.
3. Wenn der CMO von unten nach oben die Nulllinie kreuzt:
   - Long-Positionen schließen, falls erlaubt.
   - Eine Short-Position eröffnen, falls erlaubt.
4. Stop-Loss und Take-Profit werden als Schutzorders in Punkten angewendet.

## Hinweise

- Handelsentscheidungen werden nur auf abgeschlossenen Kerzen getroffen.
- Die Strategie verwendet die High-Level-API von StockSharp und bindet Indikatoren über `Bind`.
