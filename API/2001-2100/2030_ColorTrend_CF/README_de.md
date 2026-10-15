# Color Trend CF-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine Konvertierung des MQL-Experten **Exp_ColorTrend_CF**. Sie verwendet zwei exponentielle gleitende Durchschnitte zur Erkennung von Trendwechseln. Der schnelle EMA reagiert schnell auf Kursbewegungen, während der langsame EMA als Trendfilter dient. Eine Long-Position wird eröffnet, wenn der schnelle EMA den langsamen EMA von unten nach oben kreuzt. Eine Short-Position wird eröffnet, wenn der schnelle EMA den langsamen EMA von oben nach unten kreuzt.

## Parameter

- `Period` – Basisperiode für den schnellen EMA; der langsame EMA verwendet den doppelten Wert.
- `StopLoss` – Stop-Loss-Distanz in Preiseinheiten.
- `TakeProfit` – Take-Profit-Distanz in Preiseinheiten.
- `AllowBuyOpen` – Erlaubnis zum Öffnen von Long-Positionen.
- `AllowSellOpen` – Erlaubnis zum Öffnen von Short-Positionen.
- `AllowBuyClose` – Erlaubnis zum Schließen von Long-Positionen bei Verkaufssignal.
- `AllowSellClose` – Erlaubnis zum Schließen von Short-Positionen bei Kaufsignal.
- `CandleType` – Zeitrahmen für die Indikatorberechnung.

## Handelslogik

1. Kerzen des gewählten Zeitrahmens abonnieren.
2. Schnellen und langsamen EMA berechnen.
3. Wenn der schnelle EMA den langsamen EMA von unten nach oben kreuzt:
   - Short-Positionen schließen, wenn erlaubt.
   - Long-Position öffnen, wenn erlaubt.
4. Wenn der schnelle EMA den langsamen EMA von oben nach unten kreuzt:
   - Long-Positionen schließen, wenn erlaubt.
   - Short-Position öffnen, wenn erlaubt.
5. Für offene Positionen Stop-Loss- und Take-Profit-Level anwenden.

Diese Implementierung verwendet die High-Level-API von StockSharp mit Indikator-Binding.
